// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Concurrent;
using System.Reflection.Emit;
using System.Data;
using System.ComponentModel.DataAnnotations;
using Internal;
using System.IO;
using System.Text.RegularExpressions;
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
            var flow = obj.Flow;

            // base value from distance of the jump
            double readingBonus = Math.Pow(Math.Max(obj.DistanceMoved / 512, 1.0/512.0),0.6);

            // bonus from totaldistance/totalstraintime value for reading strain
            double ratio = flow.DistanceMovedOfFlow.Select(Math.Abs).Sum() / Math.Max(flow.StrainTimeOfFlow.Sum(), 25);
            double movementWeight = ratio <= 1
                ? ratio * ratio
                : 2 - (1 / Math.Sqrt(ratio));

            // bonus from inconsistent distance (CV-based)
            double[] movementDistances = flow.DistanceMovedOfFlow.Where(v => !double.IsNaN(v) && !double.IsInfinity(v)).Select(Math.Abs).ToArray();
            double average = movementDistances.Average();
            double std = Math.Sqrt(movementDistances.Select(v => Math.Pow(v - average, 2)).Average());
            double cv = average > 0 ? std / average : 0;
            double irregularityWeight = Math.Pow(1 + cv, 0.5);

            // Final score: product of all weights
            readingBonus *= (1+movementWeight) * irregularityWeight / 25;

            return Math.Max(readingBonus,0.00001);
            
            //TODO : inconsistent distance needs make hdash normalized dash
        }
    }
}