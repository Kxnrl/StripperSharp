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
using Sharp.Shared.Types.Tier;

namespace Kxnrl.StripperSharp.Natives;

[StructLayout(LayoutKind.Explicit, Size = 56)]
internal unsafe struct CEntityKeyValues
{
    internal enum AllocatorType : byte
    {
        Normal,
        EntSystem1,
        EntSystem2,
        External,
    }

    [FieldOffset(16)]
    public void* KeyValue3;

    [FieldOffset(32)]
    public short RefCount;

    [FieldOffset(34)]
    public ushort QueuedForSpawnCount;

    [FieldOffset(40)]
    public CUtlLeanVectorBase<EntityIOConnectionDescFat, int> ConnectionDescs;

    public static void Init(IGameData gameData, IModSharp sharp)
    {
        _fnFindKeyValues
            = (delegate* unmanaged<CEntityKeyValues*, CHashKey*, bool*, CKeyValues3*>) gameData.GetAddress(
                "CEntityKeyValues::FindKeyValues");

        _fnFindOrCreateKeyValues
            = (delegate* unmanaged<CEntityKeyValues*, CHashKey*, CKeyValues3*>) gameData.GetAddress(
                "CEntityKeyValues::FindOrCreateKeyValues");

        _fnSetKeyValuesString
            = (delegate* unmanaged<CEntityKeyValues*, CKeyValues3*, byte*, void>) gameData.GetAddress(
                "CEntityKeyValues::SetString");

        _fnRemoveKeyValues
            = (delegate* unmanaged<CEntityKeyValues*, CHashKey*, void>) gameData.GetAddress(
                "CEntityKeyValues::RemoveKeyValues");

        _fnMakeStringToken
            = (delegate* unmanaged<byte*, uint>) sharp.GetNativeFunctionPointer("Core.MakeStringToken");

        _fnConstructor
            = (delegate* unmanaged<CEntityKeyValues*, nint, AllocatorType, void>) gameData.GetAddress(
                "CEntityKeyValues::CEntityKeyValues");

        _fnAddConnectionDesc
            = (delegate* unmanaged<CEntityKeyValues*, byte*, EntityIOTargetType, byte*, byte*, byte*, int, float, CKeyValues3*,
                void>)
            gameData.GetAddress("CEntityKeyValues::AddConnectionDesc");

        _initialized = true;
    }

    ////////////////////////////////////////////////////////

    private static bool                                                                   _initialized;
    private static delegate* unmanaged<CEntityKeyValues*, CHashKey*, bool*, CKeyValues3*> _fnFindKeyValues;
    private static delegate* unmanaged<CEntityKeyValues*, CHashKey*, CKeyValues3*>        _fnFindOrCreateKeyValues;
    private static delegate* unmanaged<CEntityKeyValues*, CKeyValues3*, byte*, void>      _fnSetKeyValuesString;
    private static delegate* unmanaged<CEntityKeyValues*, CHashKey*, void>                _fnRemoveKeyValues;
    private static delegate* unmanaged<CEntityKeyValues*, nint, AllocatorType, void>      _fnConstructor;

    private static delegate* unmanaged<CEntityKeyValues*, byte*, EntityIOTargetType, byte*, byte*, byte*, int, float,
        CKeyValues3*, void>
        _fnAddConnectionDesc;

    private static delegate* unmanaged<byte*, uint> _fnMakeStringToken;

    public static CEntityKeyValues* Create(nint pContext, AllocatorType type)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("CEntityKeyValues not initialized.");
        }

        var ptr = (CEntityKeyValues*) MemoryAllocator.Alloc((nuint) Unsafe.SizeOf<CEntityKeyValues>());
        _fnConstructor(ptr, pContext, type);

        return ptr;
    }

    public CKeyValues3* FindKeyValuesMember(string member)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("CEntityKeyValues not initialized.");
        }

        var    pool = ArrayPool<byte>.Shared;
        byte[] memberBytes;

        {
            memberBytes = pool.Rent(Encoding.UTF8.GetMaxByteCount(member.Length));
            Utf8.FromUtf16(member, memberBytes, out _, out var bytesWritten);
            memberBytes[bytesWritten] = 0;
        }

        try
        {
            fixed (byte* ptr = memberBytes)
            {
                var unknown = false;
                var hashKey = stackalloc CHashKey[1];

                hashKey->HashCode   = _fnMakeStringToken(ptr);
                hashKey->KeyPointer = ptr;

                fixed (CEntityKeyValues* pThis = &this)
                {
                    return _fnFindKeyValues(pThis, hashKey, &unknown);
                }
            }
        }
        finally
        {
            pool.Return(memberBytes);
        }
    }

    public CKeyValues3* FindOrCreateKeyValues(string member)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("CEntityKeyValues not initialized.");
        }

        var    pool = ArrayPool<byte>.Shared;
        byte[] memberBytes;

        {
            memberBytes = pool.Rent(Encoding.UTF8.GetMaxByteCount(member.Length));
            Utf8.FromUtf16(member, memberBytes, out _, out var bytesWritten);
            memberBytes[bytesWritten] = 0;
        }

        try
        {
            fixed (byte* ptr = memberBytes)
            {
                var hashKey = stackalloc CHashKey[1];

                hashKey->HashCode   = _fnMakeStringToken(ptr);
                hashKey->KeyPointer = ptr;

                fixed (CEntityKeyValues* pThis = &this)
                {
                    return _fnFindOrCreateKeyValues(pThis, hashKey);
                }
            }
        }
        finally
        {
            pool.Return(memberBytes);
        }
    }

    public void SetKeyValuesMemberString(CKeyValues3* pMember, string value)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("CEntityKeyValues not initialized.");
        }

        var    pool = ArrayPool<byte>.Shared;
        byte[] valueBytes;

        {
            valueBytes = pool.Rent(Encoding.UTF8.GetMaxByteCount(value.Length));
            Utf8.FromUtf16(value, valueBytes, out _, out var bytesWritten);
            valueBytes[bytesWritten] = 0;
        }

        try
        {
            fixed (byte* ptr = valueBytes)
            {
                fixed (CEntityKeyValues* pThis = &this)
                {
                    _fnSetKeyValuesString(pThis, pMember, ptr);
                }
            }
        }
        finally
        {
            pool.Return(valueBytes);
        }
    }

    public void AddOrSetKeyValueMemberString(string key, string value)
    {
        var kv3 = FindOrCreateKeyValues(key);

        if (kv3 != null)
        {
            SetKeyValuesMemberString(kv3, value);
        }
    }

    public void AddConnectionDesc(string output,
        EntityIOTargetType               targetType,
        string                           target,
        string                           input,
        string                           param,
        float                            delay,
        int                              limit)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("CEntityKeyValues not initialized.");
        }

        var    pool = ArrayPool<byte>.Shared;
        byte[] outputBytes;
        byte[] targetBytes;
        byte[] inputBytes;
        byte[] paramBytes;

        {
            outputBytes = pool.Rent(Encoding.UTF8.GetMaxByteCount(output.Length));
            Utf8.FromUtf16(output, outputBytes, out _, out var bytesWritten);
            outputBytes[bytesWritten] = 0;
        }

        {
            targetBytes = pool.Rent(Encoding.UTF8.GetMaxByteCount(target.Length));
            Utf8.FromUtf16(target, targetBytes, out _, out var bytesWritten);
            targetBytes[bytesWritten] = 0;
        }

        {
            inputBytes = pool.Rent(Encoding.UTF8.GetMaxByteCount(input.Length));
            Utf8.FromUtf16(input, inputBytes, out _, out var bytesWritten);
            inputBytes[bytesWritten] = 0;
        }

        {
            paramBytes = pool.Rent(Encoding.UTF8.GetMaxByteCount(param.Length));
            Utf8.FromUtf16(param, paramBytes, out _, out var bytesWritten);
            paramBytes[bytesWritten] = 0;
        }

        try
        {
            fixed (byte* pOutput = outputBytes)
            fixed (byte* pTarget = targetBytes)
            fixed (byte* pInput = inputBytes)
            fixed (byte* pParam = paramBytes)
            fixed (CEntityKeyValues* pThis = &this)
            {
                var kv3 = CKeyValues3.Create(KeyValues3Type.Null, KeyValues3SubType.UnSpecified);
                _fnAddConnectionDesc(pThis, pOutput, targetType, pTarget, pInput, pParam, limit, delay, kv3);
                kv3->DeleteThis();
            }
        }
        finally
        {
            pool.Return(outputBytes);
            pool.Return(targetBytes);
            pool.Return(inputBytes);
            pool.Return(paramBytes);
        }
    }

    public void RemoveKeyValues(string member)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("CEntityKeyValues not initialized.");
        }

        var    pool = ArrayPool<byte>.Shared;
        byte[] memberBytes;

        {
            memberBytes = pool.Rent(Encoding.UTF8.GetMaxByteCount(member.Length));
            Utf8.FromUtf16(member, memberBytes, out _, out var bytesWritten);
            memberBytes[bytesWritten] = 0;
        }

        try
        {
            fixed (byte* ptr = memberBytes)
            {
                var hashKey = stackalloc CHashKey[1];

                hashKey->HashCode   = _fnMakeStringToken(ptr);
                hashKey->KeyPointer = ptr;

                fixed (CEntityKeyValues* pThis = &this)
                {
                    _fnRemoveKeyValues(pThis, hashKey);
                }
            }
        }
        finally
        {
            pool.Return(memberBytes);
        }
    }

    public void RemoveConnectionDesc(int index)
    {
        if (QueuedForSpawnCount > 0)
        {
            return;
        }

        ConnectionDescs.Remove(index);
    }
}
