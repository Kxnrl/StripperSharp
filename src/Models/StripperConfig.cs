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
using System.Text;
using System.Text.Json;

namespace Kxnrl.StripperSharp.Models;

internal class StripperConfig
{
    public StripperRules?                                        Global        { get; private set; }
    public StripperRules?                                        GlobalDefault { get; private set; }
    public Dictionary<string /* (World::Lump) */, StripperRules> Lumps         { get; init; }

    public bool HasData => Global is not null || GlobalDefault is not null || Lumps.Count > 0;

    private readonly string       _stripperPath;
    private readonly UTF8Encoding _encoding;

    public StripperConfig(string path)
    {
        _stripperPath = path;
        _encoding     = new UTF8Encoding(false);
        Lumps         = new Dictionary<string, StripperRules>(StringComparer.OrdinalIgnoreCase);
    }

    public void Purge()
    {
        Global        = null;
        GlobalDefault = null;
        Lumps.Clear();
    }

    /// <exception cref="AggregateException">任一文件解析失败或世界名撞车;抛出前所有已加载规则均已丢弃</exception>
    public void Load(string mapName)
    {
        Purge();

        if (!Directory.Exists(_stripperPath))
        {
            return;
        }

        var errors = new List<Exception>();

        Global        = LoadFile(Path.Combine(_stripperPath, "global.jsonc"),         errors, false);
        GlobalDefault = LoadFile(Path.Combine(_stripperPath, "global_default.jsonc"), errors, false);

        var mapPath = Path.Combine(_stripperPath, "maps", mapName);

        if (Directory.Exists(mapPath))
        {
            foreach (var filePath in Directory.GetFiles(mapPath, "*.jsonc", SearchOption.AllDirectories))
            {
                var rules = LoadFile(filePath, errors, true);

                if (rules is null)
                {
                    continue;
                }

                var cleanPath = Path.GetRelativePath(mapPath, filePath);
                var parentDir = Path.GetDirectoryName(cleanPath);
                var worldName = string.IsNullOrWhiteSpace(parentDir) ? mapName : parentDir;
                var lumpName  = Path.GetFileNameWithoutExtension(cleanPath);
                var key       = $"{worldName}::{lumpName}";

                if (!Lumps.TryAdd(key, rules))
                {
                    errors.Add(new FileLoadException("Duplicated stripper world::lump key",
                                                     filePath,
                                                     new InvalidDataException($"'{key}' is already defined")));
                }
            }
        }

        if (errors.Count == 0)
        {
            return;
        }

        Purge();

        throw new AggregateException("Failed to load stripper configuration", errors);
    }

    private StripperRules? LoadFile(string file, List<Exception> errors, bool required)
    {
        if (!File.Exists(file))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(file, _encoding);

            var dto = JsonSerializer.Deserialize<StripperFile>(json, Stripper.SerializerOptions);

            if (dto is null)
            {
                return required ? throw new InvalidDataException("Failed to parse config") : null;
            }

            return StripperRules.Build(dto);
        }
        catch (Exception e)
        {
            errors.Add(new FileLoadException("Failed to parse stripper file", file, e));

            return null;
        }
    }
}
