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
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;
using Sharp.Shared;
using Sharp.Shared.Enums;

namespace Kxnrl.StripperSharp.Natives;

[StructLayout(LayoutKind.Explicit, Size = 16)]
internal unsafe struct CKeyValues3
{
    public static void Init(IGameData gameData)
    {
        _fnGetString = (delegate* unmanaged<CKeyValues3*, byte*, sbyte*>) gameData.GetAddress("KeyValues3::GetString");

        _fnConstructor
            = (delegate* unmanaged<CKeyValues3*, KeyValues3Type, KeyValues3SubType, void>) gameData.GetAddress(
                "KeyValues3::KeyValues3");

        _fnDeconstructor = (delegate* unmanaged<CKeyValues3*, bool, void>) gameData.GetAddress("KeyValues3::~KeyValues3");
        _initialized     = true;
    }

    public KeyValues3Type GetKeyValue3Type()
        => (KeyValues3Type) (GetKeyValue3TypeEx() & 0xF);

    private byte GetKeyValue3TypeEx()
    {
        fixed (CKeyValues3* ptr = &this)
        {
            var npt = (byte*) ptr;

            return (byte) (*npt >> 2);
        }
    }

    ////////////////////////////////////////////////////////

    private static bool                                                                       _initialized;
    private static delegate* unmanaged<CKeyValues3*, byte*, sbyte*>                           _fnGetString;
    private static delegate* unmanaged<CKeyValues3*, KeyValues3Type, KeyValues3SubType, void> _fnConstructor;
    private static delegate* unmanaged<CKeyValues3*, bool, void>                              _fnDeconstructor;

    public static CKeyValues3* Create(KeyValues3Type type, KeyValues3SubType subType)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("CKeyValues3 not initialized.");
        }

        var ptr = (CKeyValues3*) MemoryAllocator.Alloc((nuint) Unsafe.SizeOf<CKeyValues3>());
        _fnConstructor(ptr, type, subType);

        return ptr;
    }

    public void DeleteThis()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("CKeyValues3 not initialized.");
        }

        fixed (CKeyValues3* pThis = &this)
        {
            _fnDeconstructor(pThis, false);
        }
    }

    public string GetString(string defaultString = "")
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("CKeyValues3 not initialized.");
        }

        var    pool = ArrayPool<byte>.Shared;
        byte[] defaultBytes;

        {
            defaultBytes = pool.Rent(Encoding.UTF8.GetMaxByteCount(defaultString.Length));
            Utf8.FromUtf16(defaultString, defaultBytes, out _, out var bytesWritten);
            defaultBytes[bytesWritten] = 0;
        }

        try
        {
            fixed (byte* ptr = defaultBytes)
            {
                fixed (CKeyValues3* pThis = &this)
                {
                    var result = _fnGetString(pThis, ptr);

                    return Sharp.Shared.Utilities.Utils.ReadString(result);
                }
            }
        }
        finally
        {
            pool.Return(defaultBytes);
        }
    }

    public string GetStringAuto()
    {
        switch (GetKeyValue3Type())
        {
            case KeyValues3Type.Null:
                return "<KV3_TYPE_NULL>";
            case KeyValues3Type.Bool:
                return _bValue ? "true" : "false";
            case KeyValues3Type.Int:
                return _iValue.ToString();
            case KeyValues3Type.UInt:
                return _uValue.ToString();
            case KeyValues3Type.Double:
                return _dValue.ToString("F6");
            case KeyValues3Type.String:
                return GetString();
            case KeyValues3Type.BinaryBlob:
                return "<KV3_TYPE_BINARY_BLOB>";
            case KeyValues3Type.Array:
                return "<KV3_TYPE_ARRAY>";
            case KeyValues3Type.Table:
                return "KV3_TYPE_TABLE";
            default:
                return "<KV3_TYPE_INVALID>";
        }
    }

    [FieldOffset(8)]
    private bool _bValue;

    [FieldOffset(8)]
    private long _iValue;

    [FieldOffset(8)]
    private ulong _uValue;

    [FieldOffset(8)]
    private double _dValue;
}
