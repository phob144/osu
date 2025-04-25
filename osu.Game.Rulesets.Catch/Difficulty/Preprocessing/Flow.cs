// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Rulesets.Catch.Difficulty.Preprocessing
{
    public enum FlowType
    {
        DashToLeft = -2,
        WalkToLeft = -1,
        StandStill = 0,
        WalkToRight = 1,
        DashToRight = 2
    }

    public class Flow
    {
        // hitobjects per flow group, from past to present (index 0 = oldest group)
        public readonly CatchDifficultyHitObject[][] HitObjectsOfFlow;

        // movement distances per segment group
        public readonly double[] DistanceMovedOfFlow;

        // strain time per segment group
        public readonly double[] StrainTimeOfFlow;

        // flow types, ordered from past to present
        public readonly FlowType[] FlowTypes;

        private double HalfCatcherWidth;

        public Flow(CatchDifficultyHitObject start, double halfCatcherWidth)
        {
            HitObjectsOfFlow = new CatchDifficultyHitObject[3][];
            DistanceMovedOfFlow = new double[3];
            StrainTimeOfFlow = new double[3];
            FlowTypes = new FlowType[3];
            HalfCatcherWidth = halfCatcherWidth;

            var pre = start.Previous(0);
            var previous = (CatchDifficultyHitObject)pre;
            if (previous == null)
                return;
            var prevPrev = (CatchDifficultyHitObject?)previous.Previous(0);

            if (prevPrev == null || GetFlowType(prevPrev.jumpType, prevPrev.DistanceMoved, previous.jumpType, previous.DistanceMoved, previous.StrainTime) == GetFlowType(previous.jumpType, previous.DistanceMoved, start.jumpType, start.DistanceMoved, start.StrainTime))
                return;

            //Flow class begins here. the above code checks if the note makes new flow from previous notes

            List<CatchDifficultyHitObject>[] hitObjectGroups = { new(), new(), new() };
            CatchDifficultyHitObject? current = previous;
            FlowType prevGroup = GetFlowType(prevPrev.jumpType, prevPrev.DistanceMoved, previous.jumpType, previous.DistanceMoved, previous.StrainTime);
            int groupIndex = 2;

            FlowTypes[groupIndex] = prevGroup;

            double workingDistance = 0;
            double workingStrain = 0;

            while (current != null && groupIndex >= 0)
            {
                var currentPrev = (CatchDifficultyHitObject?)current.Previous(0);
                if (currentPrev == null) break;

                var currentGroup = GetFlowType(currentPrev.jumpType, currentPrev.DistanceMoved, current.jumpType, current.DistanceMoved, current.StrainTime);

                if (currentGroup != prevGroup)
                {
                    DistanceMovedOfFlow[groupIndex] = workingDistance;
                    StrainTimeOfFlow[groupIndex] = workingStrain;
                    HitObjectsOfFlow[groupIndex] = hitObjectGroups[groupIndex].ToArray();

                    groupIndex--;
                    if (groupIndex < 0)
                        break;

                    FlowTypes[groupIndex] = currentGroup;
                    workingDistance = 0;
                    workingStrain = 0;
                }

                workingDistance += current.DistanceMoved;
                workingStrain += current.StrainTime;
                hitObjectGroups[groupIndex].Add(current);

                prevGroup = currentGroup;
                current = (CatchDifficultyHitObject)current.Previous(0);
            }

            if (groupIndex >= 0)
            {
                DistanceMovedOfFlow[groupIndex] = workingDistance;
                StrainTimeOfFlow[groupIndex] = workingStrain;
                HitObjectsOfFlow[groupIndex] = hitObjectGroups[groupIndex].ToArray();
            }
        }

        // updated flow type calculation using JumpType and movement direction
        private FlowType GetFlowType(JumpType prevJump, double prevDist, JumpType currJump, double currDist, double strainTime)
        {
            int direction = Math.Sign(currDist);

            bool sameDirection = Math.Sign(prevDist) == direction && direction != 0;
            bool prevIsDash = (int)prevJump == 2; // Dash
            bool wideDashZone = HalfCatcherWidth > strainTime;
            bool treatAsDash = sameDirection && prevIsDash && wideDashZone;

            if ((int)currJump == 2 || ((int)currJump == 1 && treatAsDash))
                return direction < 0 ? FlowType.DashToLeft : FlowType.DashToRight;

            if ((int)currJump == 1)
                return direction < 0 ? FlowType.WalkToLeft : FlowType.WalkToRight;

            return FlowType.StandStill;
        }
    }
}
