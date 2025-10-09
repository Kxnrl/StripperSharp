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
using System.Text.Json.Serialization;

namespace Kxnrl.StripperSharp.Models;

// ReSharper disable MemberCanBePrivate.Global
internal class StripperConnection
{
    [JsonPropertyName("output")]
    public string? Output { get; init; }

    [JsonPropertyName("target")]
    public string? Target { get; init; }

    [JsonPropertyName("input")]
    public string? Input { get; init; }

    [JsonPropertyName("param")]
    public string? Param { get; init; }

    [JsonPropertyName("delay")]
    public float? Delay { get; init; }

    [JsonPropertyName("limit")]
    public int? Limit { get; init; }

    [Obsolete("Use Output instead", true)]
    [JsonPropertyName("outputname")]
    public string? OutputName
    {
        init => Output = value;
    }

    [Obsolete("Use Input instead", true)]
    [JsonPropertyName("inputname")]
    public string? InputName
    {
        init => Input = value;
    }

    [Obsolete("Use Target instead", true)]
    [JsonPropertyName("targetname")]
    public string? TargetName
    {
        init => Target = value;
    }

    [Obsolete("Use Param instead", true)]
    [JsonPropertyName("overrideparam")]
    public string? OverrideParam
    {
        init => Param = value;
    }

    [Obsolete("Use Limit instead", true)]
    [JsonPropertyName("timestofire")]
    public int? TimesToFire
    {
        init => Limit = value;
    }
}
