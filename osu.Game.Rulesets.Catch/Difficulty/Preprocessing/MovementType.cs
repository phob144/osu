// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Game.Rulesets.Catch.Difficulty.Preprocessing
{
    public enum MovementType
    {
        DashLeft = -2,
        WalkLeft = -1,
        Standstill = 0,
        WalkRight = 1,
        DashRight = 2,
    }

    public static class MovementTypeExtensions
    {
        public static bool IsLeft(this MovementType movementType) => Math.Sign((int)movementType) == -1;
        public static bool IsRight(this MovementType movementType) => Math.Sign((int)movementType) == 1;
        public static bool IsWalk(this MovementType movementType) => Math.Abs((int)movementType) == 1;
        public static bool IsDash(this MovementType movementType) => Math.Abs((int)movementType) == 2;
        public static bool IsStandstill(this MovementType movementType) => Math.Abs((int)movementType) == 0;
        public static bool IsSameDirection(this MovementType movementType, MovementType otherMovementType) => Math.Sign((int)movementType) == Math.Sign((int)otherMovementType);
    }
}
