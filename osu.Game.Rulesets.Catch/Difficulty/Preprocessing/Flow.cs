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
        DashToLeft = -2,  // {-5, -4, -3}
        WalkToLeft = -1,  // {-2, -1}
        StandStill = 0,   // {0}
        WalkToRight = 1,  // {1, 2}
        DashToRight = 2   // {3, 4, 5}
    }

    public class Flow
    {
        // list of hitobjects involved in this flow segment, in reversed order (from current going backward)
        public readonly CatchDifficultyHitObject[] DistancesInFlow;

        // movement distances per segment group
        public readonly double[] DistanceMovedOfFlow;

        // strain time per segment group
        public readonly double[] StrainTimeOfFlow;

        // flow types, ordered from past to present
        public readonly FlowType[] FlowTypes;

        // true only if the current hitobject starts a new flow
        public readonly bool IsValid;

        private double HalfCatcherWidth;

        public Flow(CatchDifficultyHitObject start, double halfCatcherWidth)
        {
            DistancesInFlow = Array.Empty<CatchDifficultyHitObject>();
            DistanceMovedOfFlow = new double[3];
            StrainTimeOfFlow = new double[3];
            FlowTypes = new FlowType[3];
            IsValid = false;
            HalfCatcherWidth = halfCatcherWidth;

            var pre = start.Previous(0);
            var previous = (CatchDifficultyHitObject)pre;
            if (previous == null)
                return;
            var prevPrev = (CatchDifficultyHitObject?)previous.Previous(0);

            // validation: only consider start of a new flow when jumpType group changes
            if (prevPrev == null || GetFlowType(prevPrev.jumpType, previous.jumpType, previous.StrainTime) == GetFlowType(previous.jumpType, start.jumpType, start.StrainTime))
                return;

            IsValid = true;

            List<CatchDifficultyHitObject> distancesInFlowList = new();
            CatchDifficultyHitObject? current = previous;
            FlowType prevGroup = GetFlowType(prevPrev.jumpType, previous.jumpType, previous.StrainTime);
            int groupIndex = 2;

            FlowTypes[groupIndex] = prevGroup;

            double workingDistance = 0;
            double workingStrain = 0;

            while (current != null && groupIndex >= 0)
            {

                var currentPrev = (CatchDifficultyHitObject?)current.Previous(0);
                if (currentPrev == null) break;
                var currentGroup = GetFlowType(currentPrev.jumpType, current.jumpType, current.StrainTime);

                if (currentGroup != prevGroup)
                {
                    // store values for the current group
                    DistanceMovedOfFlow[groupIndex] = workingDistance;
                    StrainTimeOfFlow[groupIndex] = workingStrain;

                    groupIndex--;
                    if (groupIndex < 0)
                        break;

                    FlowTypes[groupIndex] = currentGroup;
                    workingDistance = 0;
                    workingStrain = 0;
                }

                workingDistance += current.DistanceMoved;
                workingStrain += current.StrainTime;
                distancesInFlowList.Add(current);

                prevGroup = currentGroup;
                current = (CatchDifficultyHitObject)current.Previous(0);
            }

            // store last collected group
            if (groupIndex >= 0)
            {
                DistanceMovedOfFlow[groupIndex] = workingDistance;
                StrainTimeOfFlow[groupIndex] = workingStrain;
            }

            DistancesInFlow = distancesInFlowList.ToArray();
        }

        // maps jumpType value to flow group
        private FlowType GetFlowType(JumpType prevJump, JumpType currentJump, double strainTime)
        {
            int curr = (int)currentJump;
            int prev = (int)prevJump;

            bool sameDirection = (prev * curr > 0);
            bool prevIsStrong = Math.Abs(prev) >= 3;
            bool wideDashZone = HalfCatcherWidth > strainTime;
            bool treatAsDash = sameDirection && prevIsStrong && wideDashZone;

            if (curr <= -3 || (curr <= -1 && treatAsDash))
                return FlowType.DashToLeft;

            if (curr <= -1)
                return FlowType.WalkToLeft;

            if (curr == 0)
                return FlowType.StandStill;

            if (curr <= 2)
            {
                if (treatAsDash)
                    return FlowType.DashToRight;

                return FlowType.WalkToRight;
            }

            return FlowType.DashToRight;
        }

        //TODO: any idea to detect if player decides not to standstill on standstillable stuff -> ignorable standstill in hyperchain or wiggle
    }
}