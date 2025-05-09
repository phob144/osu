// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Catch.Difficulty.Evaluators
{
    public static class SpeedEvaluator
    {
        public static double EvaluateDifficultyOf(DifficultyHitObject hitObject, float halfCatcherWidth)
        {
            var current = (CatchDifficultyHitObject)hitObject;

            if (current.Flow.Index < 2 || !current.Flow.IsEnd(current))
                return 0;

            var currentFlow = current.Flow;
            var prevFlow = current.Flow.Previous(0);
            var prevPrevFlow = current.Flow.Previous(1);

            double keyDifficultyBonus = getTransitionCost(prevPrevFlow!.MovementType, prevFlow!.MovementType) + getTransitionCost(prevFlow.MovementType, currentFlow.MovementType);

            // Curved flow penalty
            if (!currentFlow.MovementType.IsSameDirection(prevPrevFlow.MovementType) && prevPrevFlow.MovementType != MovementType.Standstill
                                                                                     && currentFlow.MovementType != MovementType.Standstill)
                keyDifficultyBonus -= 1;

            bool checkTwoFlow = false;

            // Tap-dash/Wiggle bonus
            if (currentFlow.MovementType == prevPrevFlow.MovementType && currentFlow.MovementType.IsDash())
            {
                int adjacencyIndex = getAdjacencyIndex(currentFlow.MovementType, prevFlow.MovementType);
                keyDifficultyBonus += adjacencyIndex * (adjacencyIndex > 2 ? 0.25 : 0.1);
                checkTwoFlow = true;
            }

            double strainTimeSum = currentFlow.StrainTime + prevFlow.StrainTime + prevPrevFlow.StrainTime;
            double smallerSum = Math.Min(currentFlow.StrainTime + prevFlow.StrainTime, prevFlow.StrainTime + prevPrevFlow.StrainTime);
            double minCap = Math.Max(100 * halfCatcherWidth / Math.Max(Math.Abs(current.DistanceMoved), 0.1), 50);

            double adjustedTotalStrain = Math.Max(checkTwoFlow ? smallerSum : strainTimeSum * 2.0 / 3.0, minCap);
            double strainTimeBonus = 1 / Math.Pow(adjustedTotalStrain / 150.0, adjustedTotalStrain > 150 ? 2 : 2.5);

            double buzzAdjustment = (1 - Math.Clamp(current.BuzzCount - 2, 0, 6) / 6.0) * Math.Clamp(Math.Sign(Math.Abs(current.DistanceMoved)-halfCatcherWidth)+1,0,1);

            double speedDifficulty = keyDifficultyBonus * strainTimeBonus * buzzAdjustment;

            // Temporarily making max cap for kaede case.
            return Math.Min(speedDifficulty, 15.75);
        }

        private static int getAdjacencyIndex(MovementType movementType, MovementType otherMovementType) => Math.Abs(movementType - otherMovementType);

        private static double getTransitionCost(MovementType from, MovementType to)
        {
            double cost = 0;

            if ((!from.IsLeft() && to.IsLeft()) || (!from.IsRight() && to.IsRight()))
                cost += 1;
            if ((from.IsLeft() && !to.IsLeft()) || (from.IsRight() && !to.IsRight()))
                cost += 0.3;
            if (from.IsDash() != to.IsDash())
                cost += 0.1;

            return cost;
        }
    }
}
