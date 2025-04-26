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
            var obj = (CatchDifficultyHitObject)current;
            var flow = obj.Flow;

            // base value from distance of the jump
            double readingBonus = Math.Pow(Math.Max(obj.DistanceMoved / 512, 1.0/512.0),0.65);

            if(!flow.isValid)
                return Math.Max(readingBonus/25,0.00001);

            // checks bonus from flow below

            // bonus from totaldistance/totalstraintime value for reading strain
            double ratio = flow.DistanceMovedOfFlow.Select(Math.Abs).Sum() / Math.Max(flow.StrainTimeOfFlow.Sum(), 25);
            double movementWeight = ratio <= 1
                ? ratio * ratio
                : 2 - (1 / Math.Sqrt(ratio));

            readingBonus += movementWeight/16;

            //TODO : time to actually put bonus on SUDDEN flows

            return Math.Max(readingBonus/25,0.00001);
        }
    }
}