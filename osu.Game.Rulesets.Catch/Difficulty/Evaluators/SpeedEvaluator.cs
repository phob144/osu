// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Catch.Difficulty.Evaluators
{
    public static class SpeedEvaluator
    {
        public static double EvaluateDifficultyOf(DifficultyHitObject current)
        {
            var obj = current as CatchDifficultyHitObject;
            var flow = obj?.Flow;

            if (flow == null || !flow.IsValid)
                return 0;

            var f0 = (int)flow.FlowTypes[0];
            var f1 = (int)flow.FlowTypes[1];
            var f2 = (int)flow.FlowTypes[2];

            var s0 = GetKeyState(f0);
            var s1 = GetKeyState(f1);
            var s2 = GetKeyState(f2);

            // Sum of key transition costs across the 3 flow segments
            double keyDifficulty =
                GetTransitionCost(s0, s1) +
                GetTransitionCost(s1, s2);

            // Apply rule-based difficulty modifiers
            if (f0 * f2 < 0) // normal flow nerf
                keyDifficulty -= 0.5;

            if (f0 == f2 && Math.Abs(f0) == 2 && (f1 == 0 || f1 == -Math.Sign(f0))) // tapdash bonus
                keyDifficulty += 0.5;

            if ((f0 == 2 && f1 == -2 && f2 == 2) || (f0 == -2 && f1 == 2 && f2 == -2)) // wiggle bonus
                keyDifficulty += 1.5;

            // Normalize and scale with speed bonus based on time
            double speedBonus = 0.5 / Math.Max(flow.StrainTimeOfFlow.Sum(), 1);
            return keyDifficulty * speedBonus;
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
                _  => new[] { false, false, false }
            };
        }

        private static double GetTransitionCost(bool[] from, bool[] to)
        {
            double cost = 0;

            for (int i = 0; i < 3; i++)
            {
                if (!from[i] && to[i])
                    cost += (i == 2) ? 0.75 : 1.0; // Dash: 0.75, Direction: 1.0
                else if (from[i] && !to[i])
                    cost += 0.25;
            }

            return cost;
        }
    }
}