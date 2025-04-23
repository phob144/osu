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
            var obj = (CatchDifficultyHitObject)current;
            var flow = obj.Flow;

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

            //TODO: currently it detects only the first 3 middash/edge dash but its better to get top3 of most ambiguous middash(middle of full walk and full dash)
            //TODO: and the most harsh edgeDash. also it's better to detect edge dashes from different flow since it counts not once when edgedashes are on same direction

            double midDashWeight = CircleSize * 0.05 * Math.Sqrt(midDashCount);
            double edgeDashWeight = edgeDashCount > 0 ? edgeRatioSum : 0;
            double inputPrecisionBonus = 1 + (midDashWeight + edgeDashWeight);

            precisionBonus *= inputPrecisionBonus; // middash, edgddash bonus

            double[] strainTimes = flow.StrainTimeOfFlow;
            double average = strainTimes.Average();
            double std = Math.Sqrt(strainTimes.Select(v => Math.Pow(v - average, 2)).Average());
            double cv = average > 0 ? std / average : 0;
            double irregularityBonus = Math.Pow(1 + cv, 0.5);

            precisionBonus *= irregularityBonus; // inconsistent rhythm bonus

            double averageCatcherSpeed = flow.DistancesInFlow.Length > 0
            ? flow.DistancesInFlow.Average(o => o.CatcherSpeed)
            : obj.CatcherSpeed;;

            double environmentBonus = Math.Sqrt(Math.Min(averageCatcherSpeed,3)) * Math.Pow(CircleSize, 1.5)/ 450.0;

            precisionBonus *= environmentBonus; // cs,catcher speed bonus

            return Math.Max(precisionBonus,0.00001);

            // TODO: make Inertia public in Flow and If it's the opposite side of next object, give proper bonus <- this can give more bonus on hyperchain
        }
    }
}