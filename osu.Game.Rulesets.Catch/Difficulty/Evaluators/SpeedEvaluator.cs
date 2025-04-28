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
            bool checkTwoFlow = false;

            if (!flow.isValid)
                return 0.00001;

            var f0 = (int)flow.FlowTypes[0];
            var f1 = (int)flow.FlowTypes[1];
            var f2 = (int)flow.FlowTypes[2];

            var s0 = GetKeyState(f0);
            var s1 = GetKeyState(f1);
            var s2 = GetKeyState(f2);

            double KeyDifficultyBonus =
                GetTransitionCost(s0, s1) +
                GetTransitionCost(s1, s2);

            // Curved flow penalty
            if (f0 * f2 < 0)
                KeyDifficultyBonus -= 1;

            // Tapdash/Standstillable bonus
            if (f0 == f2 && Math.Abs(f0) == 2)
            {
                KeyDifficultyBonus += 0.1 * Math.Abs(f1 - f0);
                if(Math.Abs(f1-f0)>2)
                    KeyDifficultyBonus += 0.5 * Math.Abs(f1-f0);
                    checkTwoFlow = true;

            }

            double[] strainTimes = flow.StrainTimeOfFlow;

            double sum12 = strainTimes[0] + strainTimes[1];
            double sum23 = strainTimes[1] + strainTimes[2];
            double smallerSum = Math.Min(sum12, sum23);

            double adjustedTotalStrain = Math.Max(checkTwoFlow ? smallerSum : flow.StrainTimeOfFlow.Sum()*2.0/3.0, 50);
            double powValue = adjustedTotalStrain>150 ? 3 : 2;
            double StrainTimeBonus = 1 / Math.Pow(adjustedTotalStrain / 150, powValue);

            double BuzzAdjustment = Math.Max(1 - 1.0/6.0 * Math.Clamp(obj.BuzzCount-2,0,6), 0.001);

            double speedBonus = KeyDifficultyBonus * StrainTimeBonus * BuzzAdjustment;

            return Math.Clamp(speedBonus/210, 0.00001,0.075); //temporary making max cap for kaede case. same to reading side
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
                    cost += (i == 2) ? 0.1 : 1;
                else if (from[i] && !to[i])
                    cost += (i == 2) ? 0.1 : 0.3;
            }

            return cost;
        }
    }
}
