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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kxnrl.StripperSharp.Models;

// ReSharper disable MemberCanBePrivate.Global
internal class StripperFile
{
    [JsonPropertyName("add")]
    public List<Dictionary<string, JsonDocument>>? Add { get; init; }

    [JsonPropertyName("modify")]
    public List<Dictionary<string, JsonDocument>>? Modify { get; init; }

    [JsonPropertyName("remove")]
    public List<Dictionary<string, JsonDocument>>? Remove { get; init; }

    [Obsolete("Use Remove instead", true)]
    [JsonPropertyName("filter")]
    public List<Dictionary<string, JsonDocument>>? Filter
    {
        init => Remove = value;
    }
}
