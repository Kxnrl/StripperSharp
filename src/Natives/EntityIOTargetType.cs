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

namespace Kxnrl.StripperSharp.Natives;

internal enum EntityIOTargetType : int
{
    Invalid               = -1,
    Classname             = 0,
    ClassnameDerivesFrom  = 1,
    EntityName            = 2,
    ContainsComponent     = 3,
    SpecialActivator      = 4,
    SpecialCaller         = 5,
    EntityHandle          = 6,
    EntityNameOrClassName = 7,
}
