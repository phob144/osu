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
        StandStill = 0,        // {0}
        WalkToRight = 1,  // {1, 2}
        DashToRight = 2   // {3, 4, 5}
    }

    public enum TapDashType
    {
        None = 0,
        Soft = 1,
        Normal = 2,
        Heavy = 3
    }

    public class Flow
    {
        // list of hitobjects involved in this flow segment
        public readonly CatchDifficultyHitObject[] DistancesInFlow;

        // movement distances for each part of the flow
        public readonly double[] DistanceMovedOfFlow;
        public readonly double TotalDistance;

        // strain times for each part of the flow
        public readonly double[] StrainTimeOfFlow;
        public readonly double TotalStrainTime;

        // checking only key input changes of flows
        public readonly FlowType[] FlowType;

        // true if the movement pattern represents a wiggle
        public readonly bool IsWiggle;

        // type of tap-dash:
        // 0 = not applicable
        // 1 = soft (flow walk in middle)
        // 2 = normal (standstill in middle)
        // 3 = heavy (antiflow walk in middle)
        public readonly TapDashType TapDashType;

        // true if the entire flow could be caught without moving the catcher
        public readonly bool IsBuzz;

        // false if this hitobject is not the beginning of a new flow
        public readonly bool IsValid;

        public Flow(CatchDifficultyHitObject start, double halfCatcherWidth)
        {
            DistancesInFlow = Array.Empty<CatchDifficultyHitObject>();
            DistanceMovedOfFlow = new double[3];
            TotalDistance = 0;
            StrainTimeOfFlow = new double[3];
            TotalStrainTime = 0;
            FlowType = new FlowType[3];
            IsWiggle = false;
            TapDashType = 0;
            IsBuzz = false;
            IsValid = false;

            var previous = start.Previous(0);
            if (previous == null || GetFlowType(start.JumpType) == GetFlowType(previous.JumpType))
                return;

            IsValid = true;

            List<CatchDifficultyHitObject> distancesInFlowList = new();
            double[] distanceMovedOfFlow = new double[3];
            double[] strainTimeOfFlow = new double[3];
            FlowType[] flowType = new FlowType[3];

            int jumpTypeChangeCount = 0;
            int i = 0;

            // walk through next hitobjects to build a flow of up to 3 segments
            while (jumpTypeChangeCount < 3)
            {
                var current = start.Next(i);
                if (current == null)
                    break;

                distancesInFlowList.Add(current);

                var currentGroup = GetFlowType(current.JumpType);

                if (i > 0)
                {
                    var prev = start.Next(i - 1);
                    if (prev == null)
                        break;

                    var prevGroup = GetFlowType(prev.JumpType);

                    if (prevGroup != currentGroup)
                    {
                        jumpTypeChangeCount++;
                        if (jumpTypeChangeCount >= 3)
                            break;
                    }
                }

                flowType[jumpTypeChangeCount] = currentGroup;
                distanceMovedOfFlow[jumpTypeChangeCount] += current.DistanceMoved;
                strainTimeOfFlow[jumpTypeChangeCount] += current.StrainTime;

                i++;
            }

            DistancesInFlow = distancesInFlowList.ToArray();
            DistanceMovedOfFlow = distanceMovedOfFlow;
            TotalDistance = distanceMovedOfFlow.Sum();
            StrainTimeOfFlow = strainTimeOfFlow;
            TotalStrainTime = strainTimeOfFlow.Sum();
            FlowType = flowType;

            // Wiggle: flow[0]*flow[1] == -4 && flow[1]*flow[2] == -4
            if (flowType.Length >= 3)
                IsWiggle = (int)flowType[0] * (int)flowType[1] == -4 && (int)flowType[1] * (int)flowType[2] == -4;

            // Buzz: max movement + total movement <= 2 * halfCatcherWidth
            double maxMove = distanceMovedOfFlow.Max();
            if (maxMove + TotalDistance <= halfCatcherWidth * 2)
                IsBuzz = true;

            // TapDashType: flow[0]*flow[2] == 4
            if (FlowType[0] * FlowType[2] == 4)
            {
                TapDashType = (TapDashType)Math.Abs(FlowType[1] - FlowType[0]);
            }
        }

        // maps jumpType value to flow group
        private FlowType GetFlowType(int jumpType)
        {
            if (jumpType <= -3) return FlowType.DashToLeft;
            if (jumpType <= -1) return FlowType.WalkToLeft;
            if (jumpType == 0) return FlowType.StandStill;
            if (jumpType <= 2) return FlowType.WalkToRight;
            return FlowType.DashToRight;
        }
    }
}