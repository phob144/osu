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
        public static double EvaluateDifficultyOf(DifficultyHitObject current, float CircleSize, float halfCatcherWidth)
        {
            var obj = (CatchDifficultyHitObject)current;
            var flow = obj.Flow;
            var HalfCatcherWidth = halfCatcherWidth;

            double precisionBonus = 1.0;

            // Base precision: movement difficulty with inertia
            var prev = obj.Previous(0) as CatchDifficultyHitObject;
            double inertiaCase = 0;
            double inertiaPressure = 1500;
            if (prev != null){
                inertiaPressure = Math.Pow(prev.StrainTime,0.75);
                if(Math.Sign(prev.DistanceMoved) != Math.Sign(obj.DistanceMoved))
                    inertiaCase = Math.Abs(obj.Inertia);
            }

            if(obj.LastObject.HyperDash){
                precisionBonus = 0.6;
            }else{
                double baseRatio = (Math.Abs(obj.DistanceMoved) + 75*inertiaCase/inertiaPressure + HalfCatcherWidth) / Math.Max(obj.StrainTime - 25.0 / 3.0, 25);
                precisionBonus *= baseRatio > 1 ? Math.Pow(baseRatio, 2) : Math.Pow(baseRatio, 1.25);
                if (obj.BuzzCount>=1){
                    precisionBonus = 0.45 * Math.Max((1-Math.Min(obj.BuzzCount,4)/4),0.001);   
                }
            }
            ////TODO : need to make hdash case to normal dash instead of fixing on 0.6


            // MidDash bonus with streak multiplier and CS
            double movementRatio = Math.Abs(obj.DistanceMoved) / Math.Max(obj.StrainTime, 1);
            bool isMidDash = movementRatio >= 5.0 / 8.0 && movementRatio <= 7.0 / 8.0;
            if (isMidDash && obj.BuzzCount<1)
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

                double streakMultiplier = Math.Pow(1.1, streak);
                precisionBonus *= midDashBase * streakMultiplier * Math.Pow(Math.Max((CircleSize-3.9),0.1),0.3);
            }

            // Irregular rhythm bonus
            double[] strainTimes = flow.StrainTimeOfFlow;
            double avg = strainTimes.Average();
            double std = Math.Sqrt(strainTimes.Select(s => Math.Pow(s - avg, 2)).Average());
            double cv = avg > 0 ? Math.Pow(std / avg, 1.3) : 0;
            precisionBonus *= Math.Pow(1 + cv, 0.7);

            // CS bonus
            precisionBonus *= Math.Pow(Math.Max(CircleSize,0.1), 1.6) / 480.0;

            return Math.Max(precisionBonus, 0.00001);
        }
    }
}
