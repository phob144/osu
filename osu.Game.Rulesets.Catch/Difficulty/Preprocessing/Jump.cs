// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Catch.Difficulty.Preprocessing
{
    public static class Jump
    {
        public static List<JumpType> GetCandidates(double distanceMoved, float halfCatcherWidth, double strainTime)
        {
            List<JumpType> result = new();

            // 1. StandStill condition
            if (Math.Abs(distanceMoved) <= halfCatcherWidth * 2)
                result.Add(JumpType.Standstill);

            // 2. WalkToLeft condition
            if ((-1) * distanceMoved >= strainTime * 0.5 - halfCatcherWidth &&
                (-1) * distanceMoved <= strainTime * 0.5 + halfCatcherWidth)
                result.Add(JumpType.WalkLeft);

            // 3. WalkToRight condition
            if (distanceMoved >= strainTime * 0.5 - halfCatcherWidth &&
                distanceMoved <= strainTime * 0.5 + halfCatcherWidth)
                result.Add(JumpType.WalkRight);

            // 4. DashToLeft condition
            if ((-1) * distanceMoved >= strainTime * 0.8 || (-1) * distanceMoved >= strainTime - halfCatcherWidth)
                result.Add(JumpType.DashLeft);

            // 5. DashToRight condition
            if (distanceMoved >= strainTime * 0.8 || distanceMoved >= strainTime - halfCatcherWidth)
                result.Add(JumpType.DashRight);


            return result.Distinct().ToList();
        }

        public static JumpType Resolve(List<JumpType> current, JumpType? previous)
        {
            if (current == null || current.Count == 0)
                return JumpType.Standstill;

            // 1. If there is exactly the same jump type as previous, return it
            if (previous != null && current.Contains(previous.Value))
                return previous.Value;

            // 2. If not, prioritize based on similarity to previous
            if (previous != null)
            {
                bool previousIsRight = (int)previous > 0;
                bool previousIsLeft = (int)previous < 0;

                var priority = new List<Func<JumpType, bool>>
                {
                    // Same direction dash
                    j => (int)j > 0 && previousIsRight && (JumpType)Math.Abs((int)j) == JumpType.DashRight,
                    j => (int)j < 0 && previousIsLeft && (JumpType)Math.Abs((int)j) == JumpType.DashLeft,

                    // Opposite direction dash
                    j => (int)j < 0 && previousIsRight && (JumpType)Math.Abs((int)j) == JumpType.DashLeft,
                    j => (int)j > 0 && previousIsLeft && (JumpType)Math.Abs((int)j) == JumpType.DashRight,

                    // Standstill
                    j => j == JumpType.Standstill,

                    // Same direction walk
                    j => (int)j > 0 && previousIsRight && (JumpType)Math.Abs((int)j) == JumpType.WalkRight,
                    j => (int)j < 0 && previousIsLeft && (JumpType)Math.Abs((int)j) == JumpType.WalkLeft,

                    // Opposite direction walk
                    j => (int)j < 0 && previousIsRight && (JumpType)Math.Abs((int)j) == JumpType.WalkLeft,
                    j => (int)j > 0 && previousIsLeft && (JumpType)Math.Abs((int)j) == JumpType.WalkRight
                };

                foreach (var condition in priority)
                {
                    var match = current.FirstOrDefault(j => condition(j));
                    if (match != default)
                        return match;
                }
            }

            // 3. If no match, just return first
            return current.First();
        }

        public static JumpType GetExactJumpType(double distanceMoved, float halfCatcherWidth, double strainTime)
        {
            double absMove = Math.Abs(distanceMoved);

            // 1. Standstill condition
            if (absMove <= halfCatcherWidth)
                return JumpType.Standstill;

            // 2. Walk condition
            if (absMove <= strainTime * 0.5 + halfCatcherWidth)
                return distanceMoved < 0 ? JumpType.WalkLeft : JumpType.WalkRight;

            // 3. Dash condition
            return distanceMoved < 0 ? JumpType.DashLeft : JumpType.DashRight;
        }
    }
}
