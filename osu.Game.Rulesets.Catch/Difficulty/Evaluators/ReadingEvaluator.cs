// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Catch.Difficulty.Evaluators
{
    public static class ReadingEvaluator
    {
        public static double EvaluateDifficultyOf(DifficultyHitObject hitObject)
        {
            var current = (CatchDifficultyHitObject)hitObject;

            // Base value from distance of the jump
            double readingDifficulty = 0.5 * Math.Pow(Math.Abs(current.DistanceMoved) / 512.0, 1.25);

            if (current.Flow.Index < 2 || !current.Flow.IsEnd(current))
                return readingDifficulty;

            var currentFlow = current.Flow;
            var prevFlow = currentFlow.Previous(0);
            var prevPrevFlow = currentFlow.Previous(1);

            float distanceMovedSum = Math.Abs(currentFlow.DistanceMoved) + Math.Abs(prevFlow!.DistanceMoved) + Math.Abs(prevPrevFlow!.DistanceMoved);
            float deltaDistance = Math.Abs(currentFlow.DistanceMoved + prevFlow!.DistanceMoved + prevPrevFlow!.DistanceMoved);
            double strainTimeSum = currentFlow.StrainTime + prevFlow.StrainTime + prevPrevFlow.StrainTime;

            // Bonus from total distance/total strain time value for reading strain
            double ratio = distanceMovedSum / strainTimeSum;
            double movementWeightBonus = (ratio <= 1 ? Math.Pow(ratio,2) : Math.Pow(ratio,0.75)) + deltaDistance/strainTimeSum;

            readingDifficulty += movementWeightBonus * 0.6;

            // Bonus from how sudden and quick the new jump requires dash
            double suddenBonus = 0;

            if (prevFlow.MovementType.IsDash() && !prevPrevFlow.MovementType.IsDash())
            {
                double suddenRatio = Math.Clamp(prevPrevFlow.StrainTime / prevFlow.StrainTime, 1, 3);
                if (currentFlow.MovementType.IsDash() && currentFlow.MovementType != prevFlow.MovementType)
                    suddenRatio *= 1.25;
                suddenBonus = suddenRatio / Math.Pow(prevFlow.StrainTime / 20.0, 1.5);
            }

            readingDifficulty += suddenBonus;

            // Give penalty if every flow is formed with 1 note
            int totalObjects = currentFlow.Length + prevFlow.Length + prevPrevFlow.Length;
            double densityAdjustment = Math.Min(totalObjects * 0.04 + 0.76, 1.12);

            readingDifficulty *= densityAdjustment;

            // Temporarily making max cap for kaede case.
            return Math.Min(readingDifficulty, 1.5);
        }
    }
}
