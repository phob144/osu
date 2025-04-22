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

        public Flow(CatchDifficultyHitObject start, double halfCatcherWidth)
        {
            DistancesInFlow = Array.Empty<CatchDifficultyHitObject>();
            DistanceMovedOfFlow = new double[3];
            StrainTimeOfFlow = new double[3];
            FlowTypes = new FlowType[3];
            IsValid = false;

            var pre = start.Previous(0);
            var previous = (CatchDifficultyHitObject)pre;

            // validation: only consider start of a new flow when jumpType group changes
            if ( previous == null || GetFlowType(start.jumpType) == GetFlowType(previous.jumpType))
                return;

            IsValid = true;
            Console.WriteLine("isValid True");

            List<CatchDifficultyHitObject> distancesInFlowList = new();
            CatchDifficultyHitObject? current = previous;
            FlowType prevGroup = GetFlowType(previous.jumpType);
            int groupIndex = 2;

            FlowTypes[groupIndex] = prevGroup;

            double workingDistance = 0;
            double workingStrain = 0;

            while (current != null && groupIndex >= 0)
            {
                var currentGroup = GetFlowType(current.jumpType);

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
        private FlowType GetFlowType(JumpType jumpType)
        {
            int value = (int)jumpType;

            if (value <= -3) return FlowType.DashToLeft;
            if (value <= -1) return FlowType.WalkToLeft;
            if (value == 0) return FlowType.StandStill;
            if (value <= 2) return FlowType.WalkToRight;
            return FlowType.DashToRight;
        }
    }
}