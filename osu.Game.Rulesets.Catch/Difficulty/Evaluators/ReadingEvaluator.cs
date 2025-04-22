using System.Text.RegularExpressions;
// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Catch.Difficulty.Evaluators
{
    public static class ReadingEvaluator
    {
        public static double EvaluateDifficultyOf(DifficultyHitObject current)
        {
            var obj = current as CatchDifficultyHitObject;
            var flow = obj?.Flow;

            if (flow == null || !flow.IsValid)
                return 0;

            // bonus from wide jumps which makes actual playfield reading wider
            double max1 = 0, max2 = 0, max3 = 0;
            foreach (var o in flow.DistancesInFlow)
            {
                double v = o.PlayerMoved;
                if (v > max1) { max3 = max2; max2 = max1; max1 = v; }
                else if (v > max2) { max3 = max2; max2 = v; }
                else if (v > max3) { max3 = v; }
            }
            double max3Sum = max1 + max2 + max3;

            double readingBonus = Math.Pow(Math.Max(max3Sum / 3072.0, 5), 2) * 1.0625;

            // bonus from totaldistance/totalstraintime value for reading strain
            double ratio = flow.DistanceMovedOfFlow.Sum() / Math.Max(flow.StrainTimeOfFlow.Sum(), 1);
            double movementWeight = ratio <= 1
                ? ratio * ratio
                : 2 - (1.0 / ratio);

            // bonus from inconsistent distance (CV-based)
            double[] movementDistances = flow.DistanceMovedOfFlow;
            double average = movementDistances.Average();
            double std = Math.Sqrt(movementDistances.Select(v => Math.Pow(v - average, 2)).Average());
            double cv = average > 0 ? std / average : 0;
            double irregularityWeight = Math.Pow(1 + cv, 0.3);

            // Final score: product of all weights
            return readingBonus * movementWeight * irregularityWeight / 350;
        }
    }
}