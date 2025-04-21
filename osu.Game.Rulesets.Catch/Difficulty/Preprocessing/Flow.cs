using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Rulesets.Catch.Difficulty.Preprocessing
{
    public class Flow
    {
        public readonly CatchDifficultyHitObject[] DistancesInFlow;
        public readonly double[] DistanceMovedOfFlow;
        public readonly double TotalDistance;
        public readonly double[] StrainTimeOfFlow;
        public readonly double TotalStrainTime;
        public readonly int[] FlowType; // checking only key input changes
        public readonly bool IsWiggle; // checking if it has wiggle flow with dash
        public readonly int TapDashType; // checking tapdash type 1:soft(flow walk at the middle) / 2:normal(standstill at the middle) / 3:heavy(antiflow walk at the middle)
        public readonly bool IsBuzz; // checking if it is the theoretically catchable without movement
        public readonly bool IsValid;

        public Flow(CatchDifficultyHitObject start, double halfCatcherWidth)
        {
            // the class contains value of the next 3 linear flow of the hitobject.
            // if the hitobject is not a start of new flow, isValid gets false so same flow won't be stacked in several hitobject.

            DistancesInFlow = Array.Empty<CatchDifficultyHitObject>();
            DistanceMovedOfFlow = new double[3];
            TotalDistance = 0;
            StrainTimeOfFlow = new double[3];
            TotalStrainTime = 0;
            FlowType = new int[3];
            IsWiggle = false;
            TapDashType = 0;
            IsBuzz = false;
            IsValid = false;

            var previous = start.Previous(0);
            if (previous == null || GetJumpTypeGroup(start.JumpType) == GetJumpTypeGroup(previous.JumpType))
                return;

            IsValid = true;

            List<CatchDifficultyHitObject> distancesInFlowList = new List<CatchDifficultyHitObject>();
            double[] distanceMovedOfFlow = new double[3];
            double[] strainTimeOfFlow = new double[3];
            int[] flowType = new int[3];

            int jumpTypeChangeCount = 0;
            int i = 0;

            while (jumpTypeChangeCount < 3)
            {
                var current = start.Next(i);
                if (current == null)
                    break;

                distancesInFlowList.Add(current);

                int currentGroup = GetJumpTypeGroup(current.JumpType);

                if (i > 0)
                {
                    var prev = start.Next(i - 1);
                    if (prev == null)
                        break;

                    int prevGroup = GetJumpTypeGroup(prev.JumpType);

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

            if (flowType.Length >= 3)
                IsWiggle = flowType[0] * flowType[1] == -4 && flowType[1] * flowType[2] == -4;

            double maxMove = distanceMovedOfFlow.Max();
            if (maxMove + TotalDistance <= halfCatcherWidth * 2)
                IsBuzz = true;

            if (flowType[0] * flowType[2] == 4)
            {
                TapDashType = Math.Abs(flowType[1] - flowType[0]) <= 3 ? 1 : 2;
            }
        }

        private int GetJumpTypeGroup(int jumpType)
        {
            if (jumpType <= -3) return -2;      // {-5, -4, -3}
            if (jumpType <= -1) return -1;      // {-2, -1}
            if (jumpType == 0) return 0;        // {0}
            if (jumpType <= 2) return 1;        // {1, 2}
            return 2;                           // {3, 4, 5}
        }
    }
}