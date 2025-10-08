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

using System.Text.Json.Serialization;

namespace Kxnrl.StripperSharp.Models;

internal class StripperConnection
{
    [JsonPropertyName("output")]
    public string? Output { get; private set; }

    [JsonPropertyName("target")]
    public string? Target { get; private set; }

    [JsonPropertyName("input")]
    public string? Input { get; private set; }

    [JsonPropertyName("param")]
    public string? Param { get; private set; }

    [JsonPropertyName("delay")]
    public float? Delay { get; init; }

    [JsonPropertyName("limit")]
    public int? Limit { get; private set; }

    [JsonPropertyName("outputname")]
    private string? OutputName
    {
        get => Output;
        set => Output = value;
    }

    [JsonPropertyName("inputname")]
    private string? InputName
    {
        get => Input;
        set => Input = value;
    }

    [JsonPropertyName("targetname")]
    private string? TargetName
    {
        get => Target;
        set => Target = value;
    }

    [JsonPropertyName("overrideparam")]
    private string? OverrideParam
    {
        get => Param;
        set => Param = value;
    }

    [JsonPropertyName("timestofire")]
    private int? TimesToFire
    {
        get => Limit;
        set => Limit = value;
    }
}
