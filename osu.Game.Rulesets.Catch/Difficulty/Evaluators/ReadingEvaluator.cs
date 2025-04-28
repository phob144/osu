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
            double readingBonus = Math.Pow(Math.Max(obj.DistanceMoved / 1024, 0.1/1024.0),2.5);

            if(!flow.isValid)
                return Math.Clamp(readingBonus/10,0.00001,0.15);

            // checks bonus from flow below

            // bonus from totaldistance/totalstraintime value for reading strain
            double ratio = Math.Pow(flow.DistanceMovedOfFlow.Select(Math.Abs).Sum(),1.2) / Math.Max(flow.StrainTimeOfFlow.Sum(), 20);
            double MovementWeightBonus = ratio <= 1
                ? ratio * ratio
                : ratio;
            MovementWeightBonus *= 1.0/70.0;

            readingBonus += MovementWeightBonus;

            // bonus from how sudden and quick the new jump requires dash
            if (Math.Abs((int)flow.FlowTypes[1]) == 2)
            {
                var currentStrainTime = Math.Max(flow.StrainTimeOfFlow[1], 1);
                var previousStrainTime = Math.Max(flow.StrainTimeOfFlow[0], 1);

                if (flow.FlowTypes[0] == FlowType.StandStill || Math.Sign((int)flow.FlowTypes[0]) == 1)
                {
                    double suddenRatio = Math.Min(previousStrainTime / currentStrainTime,3);
                    if ( Math.Abs((int)flow.FlowTypes[2]) == 2 && flow.FlowTypes[1] != flow.FlowTypes[2] )
                        suddenRatio *=1.2;
                    double SuddenBonus = Math.Max(1.0, Math.Pow(suddenRatio,0.1)) / Math.Pow(currentStrainTime/20,1.85) * 0.35;
                    readingBonus += SuddenBonus;
                }
            }

            // give penalty if every flow is formed with 1 note
            int totalObjects = flow.HitObjectsOfFlow.Sum(group => group?.Length ?? 0);
            double DensityAdjustment = Math.Min(Math.Min(totalObjects,3)*0.04 + 0.76, 1.12);

            readingBonus *= DensityAdjustment;

            return Math.Clamp(readingBonus/10,0.00001,0.15);
        }
    }
}