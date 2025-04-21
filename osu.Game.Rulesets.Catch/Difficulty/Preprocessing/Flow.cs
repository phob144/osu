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

            var previous = start.Previous(0);
            if (previous == null || GetFlowType(start.jumpType) == GetFlowType(previous.jumpType))
                return;

            IsValid = true;

            List<CatchDifficultyHitObject> distancesInFlowList = new();
            int jumpTypeChangeCount = 0;
            int i = 0;

            // walk through next hitobjects to build a flow of up to 3 segments
            while (jumpTypeChangeCount < 3)
            {
                var current = start.Next(i);
                if (current == null)
                    break;

                distancesInFlowList.Add(current);

                var currentGroup = GetFlowType(current.jumpType);

                if (i > 0)
                {
                    var prev = start.Next(i - 1);
                    if (prev == null)
                        break;

                    var prevGroup = GetFlowType(prev.jumpType);

                    if (prevGroup != currentGroup)
                    {
                        jumpTypeChangeCount++;
                        if (jumpTypeChangeCount >= 3)
                            break;
                    }
                }

                FlowTypes[jumpTypeChangeCount] = currentGroup;
                DistanceMovedOfFlow[jumpTypeChangeCount] += current.DistanceMoved;
                StrainTimeOfFlow[jumpTypeChangeCount] += current.StrainTime;

                i++;
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