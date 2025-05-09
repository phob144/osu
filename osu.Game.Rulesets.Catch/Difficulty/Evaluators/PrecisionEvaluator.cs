// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;
using osu.Game.Rulesets.Catch.UI;
using osu.Game.Rulesets.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Catch.Difficulty.Evaluators
{
    public static class PrecisionEvaluator
    {
        public static double EvaluateDifficultyOf(DifficultyHitObject hitObject, float halfCatcherWidth, double clockRate)
        {
            var current = (CatchDifficultyHitObject)hitObject;
            var prev = (CatchDifficultyHitObject)current.Previous(0);

            float circleSize = 5.0f / 7.0f * (17 - 20 * halfCatcherWidth / (Catcher.BASE_SIZE * Catcher.ALLOWED_CATCH_RANGE));

            // Inertia from hyperdash
            double inertia = 0;
            if (prev != null && !current.MovementType.IsSameDirection(prev.MovementType))
                inertia = prev.LastObject.HyperDash ? 4.5 * Math.Min(prev.CatcherDashSpeed, 15) : 3;

            // Normalize hyperdash to normal dash
            double adjustedDistance = current.LastObject.HyperDash ? current.StrainTime * (0.75 + 0.125 * (2 - 1 / current.CatcherDashSpeed)) : Math.Abs(current.DistanceMoved);

            // Players tend to use plate size in their movement to catch if the jump is larger than plate size
            if (adjustedDistance > halfCatcherWidth * 1.5)
                adjustedDistance -= halfCatcherWidth * 0.5;

            // Base precision from each distance
            double basePrecisionBonus = (adjustedDistance + inertia) / Math.Max(current.StrainTime - 3, 25);

            if (adjustedDistance < halfCatcherWidth * 1.5)
                basePrecisionBonus *= adjustedDistance / (halfCatcherWidth * 1.8);
            else if (!current.LastObject.HyperDash)
                basePrecisionBonus *= Math.Pow(1.075, Math.Min(100.0 / current.StrainTime, 4) - 1);

            basePrecisionBonus = basePrecisionBonus > 1 ? Math.Pow(basePrecisionBonus, 1.8) : Math.Pow(basePrecisionBonus, 2.35);

            basePrecisionBonus *= !current.LastObject.HyperDash && current.StrainTime>=100 ? 100/current.StrainTime : 1; // nerfing salad~platter level to make it traditional

            // Bonus from Catcher speed from mods
            double clockRateBonus = Math.Pow(clockRate, 0.15);

            // Check how sudden it is in edge dash case
            double edgeDashBonus = 1;

            if (basePrecisionBonus >= 1 && prev != null)
            {
                // Antiflow after fast hyperdash or fast antiflow after hyperdash
                if (!current.MovementType.IsSameDirection(prev.MovementType) && !current.LastObject.HyperDash && prev.LastObject.HyperDash)
                    edgeDashBonus *= Math.Min(Math.Max(Math.Abs(current.DistanceMoved) / prev.StrainTime, prev.StrainTime / current.StrainTime), 2.25);

                // Edge dash without suggestion
                if (Math.Abs(current.DistanceMoved) > Math.Abs(prev.DistanceMoved) * 2 && !current.LastObject.HyperDash && Math.Sign(current.DistanceMoved) != Math.Sign(prev.DistanceMoved))
                    edgeDashBonus *= 1.3;
            }

            // Bonus from hyperwiggle
            double hyperWiggleDirectionChanges = 0;

            for (int i = 0; i < Math.Min(current.Index, 6); i++)
            {
                var currObj = (CatchDifficultyHitObject)current.Previous(i - 1);
                var prevObj = (CatchDifficultyHitObject)current.Previous(i);

                if (!currObj.LastObject.HyperDash || !prevObj.LastObject.HyperDash || currObj.MovementType.IsSameDirection(prevObj.MovementType))
                    break;

                hyperWiggleDirectionChanges++;
            }

            double hyperWiggleBonus = hyperWiggleDirectionChanges > 0 ? 1.1 * Math.Pow(1.1, 100/Math.Clamp(current.StrainTime-3,20,100)) : 1;
            hyperWiggleBonus *= Math.Pow(1.0125, Math.Min(hyperWiggleDirectionChanges,7));

            // Mid-dash bonus with streak multiplier and CS
            double midDashBonus = 1;
            double movementSpeed = Math.Abs(current.DistanceMoved) / current.StrainTime;

            if (movementSpeed >= 5.0 / 8.0 && movementSpeed <= 7.0 / 8.0)
            {
                int streak = 0;

                for (int i = 0; i < Math.Min(current.Index, 5); i++)
                {
                    var currObj = (CatchDifficultyHitObject)current.Previous(i - 1);
                    var prevObj = (CatchDifficultyHitObject)current.Previous(i);

                    double prevMovementSpeed = Math.Abs(prevObj.DistanceMoved) / prevObj.StrainTime;
                    if (prevMovementSpeed < 5.0 / 8.0 || prevMovementSpeed > 7.0 / 8.0 || !currObj.MovementType.IsSameDirection(prevObj.MovementType))
                        break;

                    streak++;
                }

                double streakMultiplier = Math.Pow(1.075, Math.Max(streak - 1, 0));
                double midDashBase = 1.66 - Math.Pow(Math.Abs(movementSpeed - 0.75), 0.25);
                double CSModifier = circleSize >= 4 ? Math.Pow(circleSize - 3, 0.0875) : 0;
                midDashBonus *= Math.Pow(midDashBase, streakMultiplier) * CSModifier;
            }
            // Reduce bonus if it's same movement type and not mid-dash
            else if (prev != null && current.MovementType == prev.MovementType)
                midDashBonus = 0.2;

            // base CS bonus
            double circleSizeBonus = Math.Pow(Math.Max(circleSize, 0.1), 1.1);

            double precisionDifficulty = 0.33 * basePrecisionBonus * clockRateBonus * edgeDashBonus * hyperWiggleBonus * midDashBonus * circleSizeBonus;

            if (current.Flow.Index < 2 || !current.Flow.IsEnd(current))
                return precisionDifficulty;

            var currentFlow = current.Flow;
            var prevFlow = currentFlow.Previous(0);
            var prevPrevFlow = currentFlow.Previous(1);

            double[] strainTimes = new[] { currentFlow.StrainTime, prevFlow!.StrainTime, prevPrevFlow!.StrainTime };

            // Inconsistent rhythm bonus
            double strainTimeAvg = strainTimes.Average();
            double strainTimeStdDev = Math.Sqrt(strainTimes.Select(s => Math.Pow(s - strainTimeAvg, 2)).Average());
            double cv = strainTimeStdDev / strainTimeAvg;

            double bonusMultiplier;

            if (!prevFlow.MovementType.IsSameDirection(prevPrevFlow.MovementType) && prevPrevFlow.MovementType.IsStandstill())
            {
                bonusMultiplier = 0.8;
                if (currentFlow.MovementType.IsSameDirection(prevPrevFlow.MovementType) && prevPrevFlow.MovementType != MovementType.Standstill)
                    bonusMultiplier = 1.5;
            }
            else
                bonusMultiplier = 0.1;

            double inconsistentRhythmBonus = (500 * Math.Pow(cv, 1.75) * bonusMultiplier) / Math.Pow(prevPrevFlow.StrainTime, 1.25);
            precisionDifficulty += inconsistentRhythmBonus*clockRateBonus;

            return precisionDifficulty;
        }
    }
}
