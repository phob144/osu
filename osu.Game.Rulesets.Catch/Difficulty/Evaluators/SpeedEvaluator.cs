// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.RegularExpressions;
using System;
using System.Linq;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Catch.Difficulty.Evaluators
{
    public static class SpeedEvaluator
    {
        public static double EvaluateDifficultyOf(DifficultyHitObject current, float halfCatcherWidth)
        {
            var obj = (CatchDifficultyHitObject)current;
            var flow = obj.Flow;
            var HalfCatcherWidth = halfCatcherWidth;

            if (!flow.isValid)
                return 0.00001;

            var f0 = (int)flow.FlowTypes[0];
            var f1 = (int)flow.FlowTypes[1];
            var f2 = (int)flow.FlowTypes[2];

            var s0 = GetKeyState(f0);
            var s1 = GetKeyState(f1);
            var s2 = GetKeyState(f2);

            double keyDifficulty =
                GetTransitionCost(s0, s1) +
                GetTransitionCost(s1, s2);

            // Curved flow penalty
            if (f0 * f2 < 0)
                keyDifficulty -= 1;

            // Tapdash/Standstillable bonus
            if (f0 == f2 && Math.Abs(f0) == 2)
            {
                keyDifficulty += 0.35 * Math.Abs(f1 - f0);
            }

            double adjustedTotalStrain = Math.Max(flow.StrainTimeOfFlow.Sum(), 75);
            double powValue = adjustedTotalStrain >= 250 ? 1.3 : 1.5;

            double speedBonus = 1 / (Math.Pow(adjustedTotalStrain / 250, powValue) * 150);
            speedBonus *= keyDifficulty * Math.Max(1 - 1.0/6.0 * Math.Clamp(obj.BuzzCount-2,0,6), 0.001);

            return Math.Max(speedBonus, 0.00001);
        }

        private static bool[] GetKeyState(int flowType)
        {
            return flowType switch
            {
                -2 => new[] { true, false, true },
                -1 => new[] { true, false, false },
                 0 => new[] { false, false, false },
                 1 => new[] { false, true, false },
                 2 => new[] { false, true, true },
                _ => new[] { false, false, false }
            };
        }

        private static double GetTransitionCost(bool[] from, bool[] to)
        {
            double cost = 0;

            for (int i = 0; i < 3; i++)
            {
                if (!from[i] && to[i])
                    cost += (i == 2) ? 0.3 : 1.5;
                else if (from[i] && !to[i])
                    cost += (i == 2) ? 0.1 : 0.5;
            }

            return cost;
        }
    }
}
