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
            double absMove = Math.Abs(distanceMoved);

            // 1. StandStill condition
            if (absMove <= halfCatcherWidth * 2)
                result.Add(JumpType.Standstill);

            // 2. Walk condition
            if (absMove >= strainTime * 0.5 - halfCatcherWidth * 2 &&
                absMove <= strainTime * 0.5 + halfCatcherWidth * 2)
                result.Add(JumpType.Walk);

            // 3. Dash condition
            if (absMove <= strainTime + halfCatcherWidth * 2 && absMove >= strainTime * 0.8)
                result.Add(JumpType.Dash);

            return result.Distinct().ToList();
        }

        public static JumpType Resolve(List<JumpType> current, List<JumpType>? previous)
        {
            if (current == null || current.Count == 0)
                return JumpType.Standstill;

            previous ??= new List<JumpType>();

            var shared = previous.Intersect(current).ToList();
            if (shared.Count > 0)
                return shared.First();

            if (previous.Count > 0)
            {
                int prevValue = (int)previous.First();
                return current.OrderBy(j => Math.Abs((int)j - prevValue)).First();
            }

            var priority = new List<JumpType>
            {
                JumpType.Dash,
                JumpType.Walk,
                JumpType.Standstill
            };

            foreach (var p in priority)
            {
                if (current.Contains(p))
                    return p;
            }

            return current.First();
        }
    }
}
