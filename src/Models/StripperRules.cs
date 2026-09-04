/*
 * StripperSharp
 * Copyright (C) 2023-2025 Kxnrl. All Rights Reserved.
 *
 * This file is part of StripperSharp.
 * ModSharp is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 *
 * ModSharp is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ModSharp. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Kxnrl.StripperSharp.Models;

internal sealed record ConnectionRule(
    string? Output,
    string? Target,
    string? Input,
    string? Param,
    float?  Delay,
    int?    Limit)
{
    public bool IsInsertable
        => !string.IsNullOrWhiteSpace(Output)
           && !string.IsNullOrWhiteSpace(Input)
           && !string.IsNullOrWhiteSpace(Target);

    public bool IsEmpty
        => string.IsNullOrWhiteSpace(Output)
           && string.IsNullOrWhiteSpace(Input)
           && string.IsNullOrWhiteSpace(Target);
}

internal sealed record StripperMatch(
    Dictionary<string, string> Fields,
    List<ConnectionRule>       Connections,
    string                     RawText);

internal sealed record StripperInsert(
    Dictionary<string, string> Fields,
    List<ConnectionRule>       Connections,
    string                     RawText);

internal sealed record StripperReplace(
    Dictionary<string, string> Fields,
    string                     RawText);

internal sealed record StripperModify(
    StripperMatch    Match,
    StripperMatch?   Delete,
    StripperReplace? Replace,
    StripperInsert?  Insert,
    string           RawText);

internal sealed record StripperRules(
    List<StripperMatch>  Remove,
    List<StripperInsert> Add,
    List<StripperModify> Modify)
{
    /// <exception cref="InvalidDataException">
    ///     结构非法:值不是字符串 / connections 不是数组 / modify 缺 match /
    ///     待插入的 IO 三个名字全空 / replace 里出现 connections
    /// </exception>
    public static StripperRules Build(StripperFile file)
    {
        var remove = new List<StripperMatch>();
        var add    = new List<StripperInsert>();
        var modify = new List<StripperModify>();

        foreach (var element in file.Remove ?? [])
        {
            remove.Add(BuildMatch(element, "remove"));
        }

        foreach (var element in file.Filter ?? [])
        {
            remove.Add(BuildMatch(element, "filter"));
        }

        foreach (var element in file.Add ?? [])
        {
            add.Add(BuildInsert(element, "add"));
        }

        foreach (var element in file.Modify ?? [])
        {
            modify.Add(BuildModify(element));
        }

        return new StripperRules(remove, add, modify);
    }

    private static StripperMatch BuildMatch(JsonElement element, string scope)
    {
        var fields      = new Dictionary<string, string>(StringComparer.Ordinal);
        var connections = new List<ConnectionRule>();

        foreach (var member in Members(element, scope))
        {
            if (IsConnectionKey(member.Name))
            {
                foreach (var entry in Entries(member.Value, member.Name, scope))
                {
                    connections.Add(ReadRule(entry, scope));
                }

                continue;
            }

            fields[member.Name] = ReadString(member.Value, member.Name, scope);
        }

        return new StripperMatch(fields, connections, element.GetRawText());
    }

    private static StripperInsert BuildInsert(JsonElement element, string scope)
    {
        var fields      = new Dictionary<string, string>(StringComparer.Ordinal);
        var connections = new List<ConnectionRule>();

        foreach (var member in Members(element, scope))
        {
            if (IsConnectionKey(member.Name))
            {
                foreach (var entry in Entries(member.Value, member.Name, scope))
                {
                    var rule = ReadRule(entry, scope);

                    if (rule.IsEmpty)
                    {
                        throw new InvalidDataException($"[{scope}] connection requires 'output', 'input' or 'target'");
                    }

                    connections.Add(rule);
                }

                continue;
            }

            fields[member.Name] = ReadString(member.Value, member.Name, scope);
        }

        return new StripperInsert(fields, connections, element.GetRawText());
    }

    private static StripperReplace BuildReplace(JsonElement element, string scope)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var member in Members(element, scope))
        {
            if (IsConnectionKey(member.Name))
            {
                throw new InvalidDataException($"[{scope}] does not support '{member.Name}'; use 'delete' and 'insert' instead");
            }

            fields[member.Name] = ReadString(member.Value, member.Name, scope);
        }

        return new StripperReplace(fields, element.GetRawText());
    }

    private static StripperModify BuildModify(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"[modify] must be an object, got {element.ValueKind}");
        }

        if (!element.TryGetProperty("match", out var match))
        {
            throw new InvalidDataException("[modify] requires a 'match' block");
        }

        return new StripperModify(BuildMatch(match, "modify.match"),
                                  Optional(element, "delete") is { } delete
                                      ? BuildMatch(delete, "modify.delete")
                                      : null,
                                  Optional(element, "replace") is { } replace
                                      ? BuildReplace(replace, "modify.replace")
                                      : null,
                                  Optional(element, "insert") is { } insert
                                      ? BuildInsert(insert, "modify.insert")
                                      : null,
                                  element.GetRawText());
    }

    private static JsonElement? Optional(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null ? value : null;

    private static ConnectionRule ReadRule(JsonElement element, string scope)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"[{scope}] connection entry must be an object, got {element.ValueKind}");
        }

        var dto = element.Deserialize<StripperConnection>(Stripper.SerializerOptions)
                  ?? throw new InvalidDataException($"[{scope}] invalid connection entry");

        return new ConnectionRule(dto.Output ?? dto.OutputName,
                                  dto.Target ?? dto.TargetName,
                                  dto.Input  ?? dto.InputName,
                                  dto.Param  ?? dto.OverrideParam,
                                  dto.Delay,
                                  dto.Limit ?? dto.TimesToFire);
    }

    private static bool IsConnectionKey(string key)
        => key.Equals("connections", StringComparison.Ordinal) || key.Equals("io", StringComparison.Ordinal);

    private static JsonElement.ObjectEnumerator Members(JsonElement element, string scope)
        => element.ValueKind == JsonValueKind.Object
            ? element.EnumerateObject()
            : throw new InvalidDataException($"[{scope}] must be an object, got {element.ValueKind}");

    private static JsonElement.ArrayEnumerator Entries(JsonElement element, string key, string scope)
        => element.ValueKind == JsonValueKind.Array
            ? element.EnumerateArray()
            : throw new InvalidDataException($"[{scope}] '{key}' must be an array, got {element.ValueKind}");

    private static string ReadString(JsonElement element, string key, string scope)
        => element.ValueKind == JsonValueKind.String
            ? element.GetString()!
            : throw new InvalidDataException($"[{scope}] value of '{key}' must be a string, got {element.ValueKind}");
}
