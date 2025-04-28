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

            double precisionBonus = Math.Pow(clockRate,0.35);

            // inertia from hyperdash
            var prev = obj.Previous(0) as CatchDifficultyHitObject;
            double inertiaCase = 0;
            if (prev != null){
                if(Math.Sign(prev.DistanceMoved) != Math.Sign(obj.DistanceMoved))
                    inertiaCase = 3;
                    if (prev.LastObject.HyperDash)
                        inertiaCase *= 1.5 * Math.Min(prev.CatcherSpeed,15);
            }

            //normalize hyperdash to normal dash
            double AdjustedDistance = obj.LastObject.HyperDash
                ? obj.StrainTime * (0.4 + 0.3 * Math.Clamp(2 - 1 / obj.CatcherSpeed,0,2))
                : Math.Abs(obj.DistanceMoved);
            AdjustedDistance = AdjustedDistance > halfCatcherWidth*1.5 ? AdjustedDistance - halfCatcherWidth*0.5 : AdjustedDistance; // players tend to use plate size in their movement to catch if the jump is larger than plate size

            //base precision from each distance
            double baseRatio = Math.Max((AdjustedDistance + inertiaCase),1) / Math.Max(obj.StrainTime - 3.0, 25);
            baseRatio = AdjustedDistance < halfCatcherWidth*1.5 ? AdjustedDistance/(halfCatcherWidth*1.8)*baseRatio : baseRatio * (obj.LastObject.HyperDash? 1 : Math.Pow(1.075, Math.Clamp(100/obj.StrainTime,0,4)-1)); // if the distance is smaller than 0.75x of catcher,
            double BasePrecisionBonus = baseRatio > 1 ? Math.Pow(baseRatio,3) : Math.Pow(baseRatio,1.75);
            

            //check how sudden the it is in edgedash case
            double EdgeDashBonus = 1;
            if (precisionBonus>=1){
                if(prev!=null){
                    if(Math.Sign(prev.DistanceMoved) != Math.Sign(obj.DistanceMoved) && prev.LastObject.HyperDash && !obj.LastObject.HyperDash)
                        EdgeDashBonus *= Math.Min(Math.Max(obj.DistanceMoved/prev.StrainTime,prev.StrainTime/obj.StrainTime),2.25); // antiflow after fast hyperdash or fast antiflow after hyperdash
                    if(Math.Abs(prev.DistanceMoved)*2 < Math.Abs(obj.DistanceMoved) && !obj.LastObject.HyperDash)
                        EdgeDashBonus *= 1.5; // edgedash without suggestion
                }
            }

            //bonus from hyperwiggle
            double HyperWiggleBonus = 0;
            var target = obj;

            while (true)
            {
                var step = target.Previous(0) as CatchDifficultyHitObject;
                if (step == null) break;
                if (!step.LastObject.HyperDash) break;
                if (Math.Sign(step.DistanceMoved) == Math.Sign(target.DistanceMoved)) break;

                HyperWiggleBonus+= 1;
                target = step;
            }
            HyperWiggleBonus = Math.Pow(Math.Pow(1.01,Math.Pow(Math.Min(obj.CatcherSpeed,10),0.6)),Math.Min(HyperWiggleBonus,6));

            // MidDash bonus with streak multiplier and CS
            double movementRatio = Math.Abs(obj.DistanceMoved) / Math.Max(obj.StrainTime, 1);
            bool isMidDash = movementRatio >= 5.0 / 8.0 && movementRatio <= 7.0 / 8.0;
            double MidDashBonus = 1;
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

                double streakMultiplier = Math.Pow(1.1, Math.Max((streak-1),0));
                MidDashBonus *= Math.Pow(streakMultiplier,midDashBase) * Math.Pow(Math.Max((CircleSize-3.9),1),0.3);
            }

            //reduce bonus if it's on same direction and not middash
            if(prev != null && obj.ModifiedJumpType == prev.ModifiedJumpType && !isMidDash)
                precisionBonus *= 0.2;

            // CS bonus
            precisionBonus *= Math.Pow(Math.Max(CircleSize,0.1), 1.1);


            precisionBonus *= BasePrecisionBonus * EdgeDashBonus * HyperWiggleBonus * MidDashBonus;

            if(!flow.isValid)
                return Math.Max(precisionBonus/150,0.00001);

            // bonus calc with flow starts below

            // Inconsistent rhythm bonus
            double[] strainTimes = flow.StrainTimeOfFlow;
            double avg = strainTimes.Average();
            double std = Math.Sqrt(strainTimes.Select(s => Math.Pow(s - avg, 2)).Average());
            double cv = avg > 0 ? std / avg : 0;

            int sign(int value) => value == 0 ? 0 : (value > 0 ? 1 : -1);

            int sign0 = sign((int)flow.FlowTypes[0]);
            int sign1 = sign((int)flow.FlowTypes[1]);
            int sign2 = sign((int)flow.FlowTypes[2]);

            double bonusMultiplier;

            if (sign0 != sign1 || sign1 != sign2)
            {
                bonusMultiplier = 0.8;

                if (sign0 == sign2 && sign0 != 0)
                    bonusMultiplier = 1.5;
            }
            else
            {
                bonusMultiplier = 0.1;
            }

            double InconsistentRhythmBonus = ( Math.Pow(cv,1.75) * bonusMultiplier / flow.StrainTimeOfFlow[0] ) * 300;
            precisionBonus += InconsistentRhythmBonus;

            return Math.Max(precisionBonus/150, 0.00001);
        }
    }
}
