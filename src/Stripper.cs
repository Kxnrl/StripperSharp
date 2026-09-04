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
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Kxnrl.StripperSharp.Models;
using Kxnrl.StripperSharp.Natives;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Hooks;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;

namespace Kxnrl.StripperSharp;

internal sealed unsafe class Stripper : IModSharpModule, IGameListener
{
    public string DisplayName   => "StripperSharp";
    public string DisplayAuthor => "Kxnrl";

    public static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        AllowTrailingCommas  = true,
        ReadCommentHandling  = JsonCommentHandling.Skip,
        PropertyNamingPolicy = null,
    };

    private static Stripper?                                         _sInstance;
    private static delegate* unmanaged<nint, CSingleWorldRep*, nint> _sTrampoline;

    private readonly ILogger<Stripper> _logger;
    private readonly IModSharp         _modSharp;
    private readonly IDetourHook       _detour;
    private readonly StripperConfig    _config;

    private readonly IConVar _cvarEnableVerbose;
    private readonly IConVar _cvarEnableReplace;

    public Stripper(ISharedSystem sharedSystem,
        string                    dllPath,
        string                    sharpPath,
        Version                   version,
        IConfiguration            coreConfiguration,
        bool                      hotReload)
    {
        _logger = sharedSystem.GetLoggerFactory()
                              .CreateLogger<Stripper>();

        _modSharp = sharedSystem.GetModSharp();
        _detour   = sharedSystem.GetHookManager().CreateDetourHook();
        _config   = new StripperConfig(Path.Combine(sharpPath, "stripper"));

        _cvarEnableVerbose = sharedSystem.GetConVarManager()
                                         .CreateConVar("ms_stripper_verbose_enabled",
                                                       false,
                                                       "Enable verbose logging of stripper",
                                                       ConVarFlags.Release)
                             ?? throw new EntryPointNotFoundException("Failed to create conVar 'ms_stripper_verbose_enabled'");

        _cvarEnableReplace = sharedSystem.GetConVarManager()
                                         .CreateConVar("ms_stripper_replace_enabled",
                                                       false,
                                                       "Enable 'replace' block in 'modify' section.",
                                                       ConVarFlags.Release)
                             ?? throw new EntryPointNotFoundException("Failed to create conVar 'ms_stripper_replace_enabled'");

        _sInstance = this;
    }

    public bool Init()
    {
        _modSharp.GetGameData()
                 .Register("stripper.games");

        _detour.Prepare("IWorldRendererMgr::CreateWorldInternal",
                        (nint) (delegate* unmanaged<nint, CSingleWorldRep*, nint>) &CreateWorldInternal);

        // Trampoline 仅在 Install 成功后有效
        if (!_detour.Install())
        {
            return false;
        }

        _sTrampoline = (delegate* unmanaged<nint, CSingleWorldRep*, nint>) _detour.Trampoline;

        return true;
    }

    public void PostInit()
    {
        _modSharp.InstallGameListener(this);

        CEntityKeyValues.Init(_modSharp);
        CKeyValues3.Init(_modSharp);
    }

    public void Shutdown()
    {
        _modSharp.GetGameData()
                 .Unregister("stripper.games");

        _detour.Uninstall();

        _modSharp.RemoveGameListener(this);
    }

    int IGameListener.ListenerPriority => 0;
    int IGameListener.ListenerVersion  => IGameListener.ApiVersion;

    public void OnServerInit()
        => _config.Purge();

    public void OnGameInit()
    {
        try
        {
            _config.Load(_modSharp.GetGlobals().MapName);
        }
        catch (AggregateException e)
        {
            foreach (var inner in e.InnerExceptions)
            {
                _logger.LogError(inner, "Failed to load stripper configuration");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to load stripper configuration");
        }
    }

    [UnmanagedCallersOnly]
    private static nint CreateWorldInternal(nint pWorldRendererMgr, CSingleWorldRep* pSingleWorld)
    {
        var call = _sTrampoline(pWorldRendererMgr, pSingleWorld);

        if (_sInstance is { _config.HasData: true } stripper)
        {
            stripper.ApplyOverrides(pSingleWorld);
        }

        return call;
    }

    private void ApplyOverrides(CSingleWorldRep* pSingleWorld)
    {
        var world = pSingleWorld->pWorld;

        if (world is null)
        {
            return;
        }

        var verbose        = _cvarEnableVerbose.GetBool();
        var replaceEnabled = _cvarEnableReplace.GetBool();

        try
        {
            ref var lumpHandles = ref world->EntityLumps;

            var mapName   = _modSharp.GetGlobals().MapName;
            var worldName = pSingleWorld->Name.Get();

            for (var i = 0; i < lumpHandles.Count; i++)
            {
                ref var lump     = ref lumpHandles.Element(i);
                var     lumpData = lump.AsRef().m_pLumpData;
                var     lumpName = lumpData->pName.Get();

                if (_config.Lumps.TryGetValue($"{worldName}::{lumpName}", out var lumpOverrides))
                {
                    ApplyOverrides(lumpOverrides, lumpData, verbose, replaceEnabled);
                }

                if (_config.Global is { } global)
                {
                    ApplyOverrides(global, lumpData, verbose, replaceEnabled);
                }

                if (_config.GlobalDefault is { } globalDefault
                    && mapName.Equals(worldName, StringComparison.OrdinalIgnoreCase)
                    && lumpName.Equals("default_ents", StringComparison.OrdinalIgnoreCase))
                {
                    ApplyOverrides(globalDefault, lumpData, verbose, replaceEnabled);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to apply stripper overrides");
        }
    }

    private void ApplyOverrides(StripperRules rules, CEntityLump* lump, bool verbose, bool replaceEnabled)
    {
        foreach (var remove in rules.Remove)
        {
            for (var j = 0; j < lump->EntityKeyValues.Size; j++)
            {
                if (!Matcher.DoesEntityMatch(lump->EntityKeyValues.Element(j).Value, remove))
                {
                    continue;
                }

                lump->EntityKeyValues.Remove(j--);

                if (verbose)
                {
                    _logger.LogInformation("Removed\n{e}", remove.RawText);
                }
            }
        }

        foreach (var add in rules.Add)
        {
            var kv = CEntityKeyValues.Create(lump->pAllocatorContext, CEntityKeyValues.AllocatorType.External);

            Modifier.Insert(kv, add, _logger);

            kv->RefCount++;
            lump->EntityKeyValues.Add(kv);

            if (verbose)
            {
                _logger.LogInformation("Added\n{e}", add.RawText);
            }
        }

        foreach (var modify in rules.Modify)
        {
            var replace = replaceEnabled ? modify.Replace : null;
            var detail  = verbose ? Describe(modify, replace) : null;

            for (var j = 0; j < lump->EntityKeyValues.Size; j++)
            {
                var kv = lump->EntityKeyValues.Element(j).Value;

                if (!Matcher.DoesEntityMatch(kv, modify.Match))
                {
                    continue;
                }

                // replace 写的键可能正是 delete 要删的, 三步顺序不能调
                if (replace is not null)
                {
                    Modifier.Replace(kv, replace, _logger);
                }

                if (modify.Delete is { } delete)
                {
                    Modifier.Delete(kv, delete);
                }

                if (modify.Insert is { } insert)
                {
                    Modifier.Insert(kv, insert, _logger);
                }

                if (detail is not null)
                {
                    _logger.LogInformation("Modified\n{m}\n{b}", modify.Match.RawText, detail);
                }
            }
        }
    }

    private static string Describe(StripperModify modify, StripperReplace? replace)
    {
        var builder = new StringBuilder();

        if (replace is not null)
        {
            builder.Append("  Replaced\n    ").Append(replace.RawText).Append('\n');
        }

        if (modify.Delete is { } delete)
        {
            builder.Append("  Deleted\n    ").Append(delete.RawText).Append('\n');
        }

        if (modify.Insert is { } insert)
        {
            builder.Append("  Inserted\n    ").Append(insert.RawText).Append('\n');
        }

        return builder.ToString();
    }
}

file static unsafe class Matcher
{
    private const float FloatEpsilon = 0.0001f;

    internal static bool DoesEntityMatch(CEntityKeyValues* kv, StripperMatch match)
    {
        foreach (var (key, expect) in match.Fields)
        {
            var member = kv->FindKeyValuesMember(key);

            if (member == null)
            {
                return false;
            }

            var allowWildcard = key.Equals("targetname",   StringComparison.OrdinalIgnoreCase)
                                || key.Equals("classname", StringComparison.OrdinalIgnoreCase);

            if (!MatchValue(member->GetStringAuto(), expect, allowWildcard))
            {
                return false;
            }
        }

        foreach (var rule in match.Connections)
        {
            if (!HasMatchingConnection(kv, rule))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasMatchingConnection(CEntityKeyValues* kv, ConnectionRule rule)
    {
        var count = kv->ConnectionDescs.Count;

        for (var i = 0; i < count; i++)
        {
            if (MatchesRule(in kv->ConnectionDescs[i], rule))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool MatchesRule(in EntityIOConnectionDescFat desc, ConnectionRule rule)
    {
        if (rule.Input is { } input && !MatchValue(desc.InputName, input))
        {
            return false;
        }

        if (rule.Output is { } output && !MatchValue(desc.OutputName, output, true))
        {
            return false;
        }

        if (rule.Target is { } target && !MatchValue(desc.TargetName, target))
        {
            return false;
        }

        if (rule.Param is { } param && !MatchValue(desc.OverrideParam, param, true))
        {
            return false;
        }

        if (rule.Delay is { } delay && MathF.Abs(desc.Delay - delay) >= FloatEpsilon)
        {
            return false;
        }

        if (rule.Limit is { } limit && desc.TimesToFire != limit)
        {
            return false;
        }

        return true;
    }

    internal static bool MatchValue(string value, string match, bool allowWildcard = false)
    {
        if (allowWildcard && match.EndsWith('*'))
        {
            return value.StartsWith(match[..^1], StringComparison.OrdinalIgnoreCase);
        }

        return value.Equals(match, StringComparison.OrdinalIgnoreCase);
    }
}

file static unsafe class Modifier
{
    internal static void Insert(CEntityKeyValues* kv, StripperInsert insert, ILogger logger)
    {
        foreach (var (key, value) in insert.Fields)
        {
            kv->AddOrSetKeyValueMemberString(key, value);
        }

        foreach (var connection in insert.Connections)
        {
            if (!connection.IsInsertable)
            {
                logger.LogWarning("Invalid IO connection, missing 'output', 'input' or 'target': {o}>{t}>{i}",
                                  connection.Output,
                                  connection.Target,
                                  connection.Input);

                continue;
            }

            kv->AddConnectionDesc(connection.Output!,
                                  EntityIOTargetType.EntityNameOrClassName,
                                  connection.Target!,
                                  connection.Input!,
                                  connection.Param ?? string.Empty,
                                  connection.Delay ?? 0f,
                                  connection.Limit ?? -1);
        }
    }

    internal static void Delete(CEntityKeyValues* kv, StripperMatch delete)
    {
        foreach (var (key, expect) in delete.Fields)
        {
            var member = kv->FindKeyValuesMember(key);

            if (member == null)
            {
                continue;
            }

            if (Matcher.MatchValue(member->GetStringAuto(), expect, true))
            {
                kv->RemoveKeyValues(key);
            }
        }

        foreach (var rule in delete.Connections)
        {
            for (var i = 0; i < kv->ConnectionDescs.Count; i++)
            {
                if (!Matcher.MatchesRule(in kv->ConnectionDescs[i], rule))
                {
                    continue;
                }

                // QueuedForSpawnCount > 0 时后续也删不动, 直接放弃避免空转
                if (!kv->TryRemoveConnectionDesc(i))
                {
                    return;
                }

                i--;
            }
        }
    }

    internal static void Replace(CEntityKeyValues* kv, StripperReplace replace, ILogger logger)
    {
        foreach (var (key, value) in replace.Fields)
        {
            var member = kv->FindKeyValuesMember(key);

            if (member == null)
            {
                logger.LogWarning("Skipped replace of '{key}': the entity has no such key", key);

                continue;
            }

            kv->SetKeyValuesMemberString(member, value);
        }
    }
}
