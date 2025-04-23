using System.Collections.Concurrent;
using System.Reflection.Emit;
using System.Data;
using System.ComponentModel.DataAnnotations;
using Internal;
using System.IO;
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
            var obj = (CatchDifficultyHitObject)current;
            var flow = obj.Flow;

            // bonus from top 3 wide jumps which makes actual playfield reading wider
            double max1 = 0, max2 = 0, max3 = 0;
            foreach (var o in flow.DistancesInFlow)
            {
                double v = Math.Abs(o.PlayerMoved);
                if (v > max1) { max3 = max2; max2 = max1; max1 = v; }
                else if (v > max2) { max3 = max2; max2 = v; }
                else if (v > max3) { max3 = v; }
            }
            double max3Sum = max1 + max2 + max3;

            double readingBonus = Math.Pow(Math.Clamp(max3Sum / 2048, 0.05, 0.6),0.885);

            // bonus from totaldistance/totalstraintime value for reading strain
            double ratio = flow.DistanceMovedOfFlow.Select(Math.Abs).Sum() / Math.Max(flow.StrainTimeOfFlow.Sum(), 1);
            double movementWeight = ratio <= 1
                ? ratio * ratio
                : 1.875 - (0.875 / ratio);

            // bonus from inconsistent distance (CV-based)
            double[] movementDistances = flow.DistanceMovedOfFlow.Select(Math.Abs).ToArray();
            double average = movementDistances.Average();
            double std = Math.Sqrt(movementDistances.Select(v => Math.Pow(v - average, 2)).Average());
            double cv = average > 0 ? std / average : 0;
            double irregularityWeight = Math.Pow(1 + cv, 0.75);

            // Final score: product of all weights
            readingBonus *= movementWeight * irregularityWeight / 12.5;

            return Math.Max(readingBonus,0.00001);

            // TODO : high density bonus is needed for streams i think. but it may overweight 1/8 streams. no idea for this yet
        }
    }
}