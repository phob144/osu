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
        DashToLeft = -2, // {-5, -4, -3}
        WalkToLeft = -1, // {-2, -1}
        StandStill = 0,  // {0}
        WalkToRight = 1, // {1, 2}
        DashToRight = 2  // {3, 4, 5}
    }

    public class Flow
    {
        // list of hitobjects involved in this flow segment
        public readonly CatchDifficultyHitObject[] DistancesInFlow;

        // movement distances for each part of the flow
        public readonly double[] DistanceMovedOfFlow;

        // strain times for each part of the flow
        public readonly double[] StrainTimeOfFlow;

        // checking only key input changes of flows
        public readonly FlowType[] FlowTypes;

        // false if this hitobject is not the beginning of a new flow
        public readonly bool IsValid;

        public Flow(CatchDifficultyHitObject start, double halfCatcherWidth)
        {
            DistancesInFlow = Array.Empty<CatchDifficultyHitObject>();
            DistanceMovedOfFlow = new double[3];
            StrainTimeOfFlow = new double[3];
            FlowTypes = new FlowType[3];
            IsValid = false;

            var next = (CatchDifficultyHitObject)start.Next(0);
            var prev = (CatchDifficultyHitObject)start.Previous(0);
            if (next == null || prev == null)
            return;

            if (GetFlowType(start.jumpType) == GetFlowType(next.jumpType))
            return;

            IsValid = true;

            CatchDifficultyHitObject? current = start;
            List<CatchDifficultyHitObject> distancesInFlowList = new();
            FlowType prevGroup = GetFlowType(current.jumpType);
            int groupIndex = 0;
            FlowTypes[groupIndex] = prevGroup;

            double workingDistance = 0;
            double workingStrain = 0;

            while (current != null && groupIndex < 3)
            {
                var currentGroup = GetFlowType(current.jumpType);

                if (currentGroup != prevGroup)
                {
                    // 기록 후 다음 그룹으로
                    DistanceMovedOfFlow[groupIndex] = workingDistance;
                    StrainTimeOfFlow[groupIndex] = workingStrain;

                    groupIndex++;
                    if (groupIndex >= 3)
                        break;

                    FlowTypes[groupIndex] = currentGroup;
                    workingDistance = 0;
                    workingStrain = 0;
                }

                workingDistance += current.DistanceMoved;
                workingStrain += current.StrainTime;
                distancesInFlowList.Add(current);

                prevGroup = currentGroup;
                current = current.Next(0);
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