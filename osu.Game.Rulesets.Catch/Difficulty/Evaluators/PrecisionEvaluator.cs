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
    public static class PrecisionEvaluator
    {
        public static double EvaluateDifficultyOf(DifficultyHitObject current, float CircleSize, float halfCatcherWidth, double clockRate)
        {
            var obj = (CatchDifficultyHitObject)current;
            var flow = obj.Flow;
            var HalfCatcherWidth = halfCatcherWidth;

            double precisionBonus = clockRate;

            // Base precision: movement difficulty with inertia
            var prev = obj.Previous(0) as CatchDifficultyHitObject;
            double inertiaCase = 0;
            double inertiaPressure = 1500;
            if (prev != null){
                inertiaPressure = Math.Pow(prev.StrainTime,0.75);
                if(Math.Sign(prev.DistanceMoved) != Math.Sign(obj.DistanceMoved))
                    inertiaCase = Math.Abs(obj.Inertia);
            }

            //normalize hyperdash to normal dash
            double AdjustedDistance = obj.LastObject.HyperDash
                ? obj.StrainTime * (0.6 + 0.2 * Math.Clamp(2 - 1 / Math.Sqrt( Math.Max(Math.Abs(obj.DistanceMoved),1) / Math.Clamp(obj.StrainTime,25,1500) ),0,2))
                : Math.Abs(obj.DistanceMoved);

            //base precision from each distance
            double baseRatio = (AdjustedDistance + 7.5*inertiaCase/inertiaPressure) / Math.Max(obj.StrainTime - 25.0/3.0, 20);
            precisionBonus *= baseRatio > 1 ? Math.Pow(baseRatio, 2) : Math.Pow(baseRatio, 1.25);

            //bonus from hyperwiggle
            int hyperWiggleBonus = 0;
            var target = obj;

            while (true)
            {
                var step = target.Previous(0) as CatchDifficultyHitObject;
                if (step == null) break;
                if (!step.LastObject.HyperDash) break;
                if (Math.Sign(step.DistanceMoved) == Math.Sign(target.DistanceMoved)) break;

                hyperWiggleBonus++;
                target = step;
            }

            precisionBonus *= Math.Pow(1.075,Math.Min(hyperWiggleBonus,8));

            // MidDash bonus with streak multiplier and CS
            double movementRatio = Math.Abs(obj.DistanceMoved) / Math.Max(obj.StrainTime, 1);
            bool isMidDash = movementRatio >= 5.0 / 8.0 && movementRatio <= 7.0 / 8.0;
            if (isMidDash)
            {
                double midDashBase = 1.66 - Math.Pow(Math.Abs(movementRatio - 0.75), 0.2);

                int streak = 0;
                var currentObj = obj;
                while (streak < 5)
                {
                    var previous = currentObj.Previous(0) as CatchDifficultyHitObject;
                    if (previous == null) break;

                    double previousRatio = Math.Abs(previous.DistanceMoved) / Math.Max(previous.StrainTime, 1);
                    bool previousMidDash = previousRatio >= 2.0 / 3.0 && previousRatio <= 5.0 / 6.0;
                    if (!previousMidDash || Math.Sign(previous.DistanceMoved) != Math.Sign(currentObj.DistanceMoved))
                        break;

                    streak++;
                    currentObj = previous;
                }

                double streakMultiplier = Math.Pow(1.1, (streak-1));
                precisionBonus *= Math.Pow(streakMultiplier,midDashBase) * Math.Pow(Math.Max((CircleSize-3.9),0.1),0.3);
            }

            //reduce bonus if it's on same direction and not middash
            if(prev != null && obj.ModifiedJumpType == prev.ModifiedJumpType && !isMidDash)
                precisionBonus *= 0.2;

            // CS bonus
            precisionBonus *= Math.Pow(Math.Max(CircleSize,0.1), 1.4);

            if(!flow.isValid)
                return Math.Max(precisionBonus/480,0.00001);

            // checks bonus from flow below

            // Irregular rhythm bonus
            double[] strainTimes = flow.StrainTimeOfFlow;
            double avg = strainTimes.Average();
            double std = Math.Sqrt(strainTimes.Select(s => Math.Pow(s - avg, 2)).Average());
            double cv = avg > 0 ? Math.Pow(std / avg, 1.3) : 0;
            precisionBonus += Math.Pow(1 + cv, 0.7)/400;

            // TODO : if f0 is hdash or big (whatever disturbs next precision),and f2 and f3 are all wiggle (f2 is ok to be walk), give bonus this is actual antiflow in stremas.

            return Math.Max(precisionBonus/480, 0.00001);
        }
    }
}
