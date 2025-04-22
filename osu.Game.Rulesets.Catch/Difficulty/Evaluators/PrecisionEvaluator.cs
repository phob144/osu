using System.Text.RegularExpressions;
// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Catch.Difficulty.Evaluators
{
    public static class PrecisionEvaluator
    {
        public static double EvaluateDifficultyOf(DifficultyHitObject current, float CircleSize)
        {
            var obj = current as CatchDifficultyHitObject;
            var flow = obj?.Flow;

            if (flow == null || !flow.IsValid)
                return 0;

            double precisionBonus = 1;

            int midDashCount = 0;
            int edgeDashCount = 0;
            double edgeRatioSum = 0;

            foreach (var o in flow.DistancesInFlow)
            {
                int absJump = Math.Abs((int)o.jumpType);

                if (absJump == 2 && midDashCount < 3)
                    midDashCount++;

                if (absJump == 4 && edgeDashCount < 3)
                {
                    edgeDashCount++;
                    edgeRatioSum += o.EdgeRatio;
                }
            }

            double midDashWeight = CircleSize * 0.05 * Math.Sqrt(midDashCount);
            double edgeDashWeight = edgeDashCount > 0 ? edgeRatioSum / Math.Sqrt(edgeDashCount) : 0;
            double inputPrecisionBonus = 1 + (midDashWeight + edgeDashWeight) / 3.0;

            precisionBonus *= inputPrecisionBonus; // middash, edgddash bonus

            double[] strainTimes = flow.StrainTimeOfFlow;
            double average = strainTimes.Average();
            double std = Math.Sqrt(strainTimes.Select(v => Math.Pow(v - average, 2)).Average());
            double cv = average > 0 ? std / average : 0;
            double irregularityBonus = Math.Pow(1 + cv, 0.3);

            precisionBonus *= irregularityBonus; // inconsistent rhythm bonus

            double environmentBonus = Math.Sqrt(obj.CatcherSpeed)
                * Math.Pow(CircleSize, 1.5)
                / 20.0;

            precisionBonus *= environmentBonus; // cs,catcher speed bonus

            return precisionBonus/15;
        }
    }
}