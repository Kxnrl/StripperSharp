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

using System.Runtime.InteropServices;
using Sharp.Shared.Utilities;

namespace Kxnrl.StripperSharp.Natives;

[StructLayout(LayoutKind.Explicit, Size = 64, Pack = 8)]
internal unsafe struct EntityIOConnectionDescFat
{
    [FieldOffset(0)]
    private byte* m_pszOutputName;

    [FieldOffset(8)]
    public EntityIOTargetType TargetType;

    [FieldOffset(16)]
    private byte* m_pszTargetName;

    [FieldOffset(24)]
    private byte* m_pszInputName;

    [FieldOffset(32)]
    private byte* m_pszOverrideParam;

    [FieldOffset(40)]
    public float Delay;

    [FieldOffset(44)]
    public int TimesToFire;

    [FieldOffset(48)]
    public CKeyValues3 KeyValues;

    public string OutputName    => NativeString.ReadString(m_pszOutputName);
    public string TargetName    => NativeString.ReadString(m_pszTargetName);
    public string InputName     => NativeString.ReadString(m_pszInputName);
    public string OverrideParam => NativeString.ReadString(m_pszOverrideParam);
}
