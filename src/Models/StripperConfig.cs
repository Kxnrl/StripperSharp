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
    public StripperFile?                                        Global        { get; private set; }
    public StripperFile?                                        GlobalDefault { get; private set; }
    public Dictionary<string /* (World::Lump) */, StripperFile> Lumps         { get; init; }

    public bool HasData => Global is not null || GlobalDefault is not null || Lumps.Count > 0;

    private readonly string       _stripperPath;
    private readonly UTF8Encoding _encoding;

    public StripperConfig(string path)
    {
        _stripperPath = path;
        _encoding     = new UTF8Encoding(false);
        Lumps         = new Dictionary<string, StripperFile>(StringComparer.OrdinalIgnoreCase);
    }

    public void Purge()
    {
        Global        = null;
        GlobalDefault = null;
        Lumps.Clear();
    }

    public void Load(string mapName)
    {
        if (!Directory.Exists(_stripperPath))
        {
            return;
        }

        try
        {
            Global        = LoadFile(Path.Combine(_stripperPath, "global.jsonc"));
            GlobalDefault = LoadFile(Path.Combine(_stripperPath, "global_default.jsonc"));
        }
        catch
        {
            Purge();

            throw;
        }

        var mapPath = Path.Combine(_stripperPath, "maps", mapName);

        if (!Directory.Exists(mapPath))
        {
            return;
        }

        foreach (var filePath in Directory.GetFiles(mapPath, "*.jsonc", SearchOption.AllDirectories))
        {
            try
            {
                var cleanPath = Path.GetRelativePath(mapPath, filePath);
                var parentDir = Path.GetDirectoryName(cleanPath);
                var worldName = string.IsNullOrWhiteSpace(parentDir) ? mapName : parentDir;
                var lumpName  = Path.GetFileNameWithoutExtension(cleanPath);
                var lumpData  = LoadFile(filePath) ?? throw new InvalidDataException("Failed to parse config");
                var keyPair   = $"{worldName}::{lumpName}";

                Lumps.Add(keyPair, lumpData);
            }
            catch (Exception e)
            {
                Lumps.Clear();

                // re-throw
                throw new FileLoadException("Failed to parse stripper file", filePath, e);
            }
        }
    }

    private StripperFile? LoadFile(string file)
    {
        if (!File.Exists(file))
        {
            return null;
        }

        var json = File.ReadAllText(file, _encoding);

        return JsonSerializer.Deserialize<StripperFile>(json, Stripper.SerializerOptions);
    }
}
