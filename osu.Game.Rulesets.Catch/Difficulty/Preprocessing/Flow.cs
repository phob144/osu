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
        
        public bool isValid; //checks if this is where to start new flow, used in skill evals formulas which use flow

        public Flow(CatchDifficultyHitObject start, double halfCatcherWidth)
        {
            HitObjectsOfFlow = new CatchDifficultyHitObject[3][];
            DistanceMovedOfFlow = new double[3];
            StrainTimeOfFlow = new double[3];
            FlowTypes = new FlowType[3];
            HalfCatcherWidth = halfCatcherWidth;
            isValid=false;

            var pre = start.Previous(0);
            var previous = (CatchDifficultyHitObject)pre;
            if (previous == null)
                return;

            if (GetFlowType(previous.ModifiedJumpType) == GetFlowType(start.ModifiedJumpType))
                return;

            isValid = true;
            //Flow class begins here. the above code checks if the note makes new flow from previous notes

            List<CatchDifficultyHitObject>[] hitObjectGroups = { new(), new(), new() };
            CatchDifficultyHitObject? current = previous;
            FlowType prevGroup = GetFlowType(previous.ModifiedJumpType);
            int groupIndex = 2;

            FlowTypes[groupIndex] = prevGroup;

            double workingDistance = 0;
            double workingStrain = 0;

            while (current != null && groupIndex >= 0)
            {
                var currentPrev = (CatchDifficultyHitObject?)current.Previous(0);
                if (currentPrev == null) break;

                var currentGroup = GetFlowType(current.ModifiedJumpType);

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

            for (int i = 0; i < StrainTimeOfFlow.Length; i++)
            {
                if (StrainTimeOfFlow[i] == 0)
                    StrainTimeOfFlow[i] = 1500; // setting straintime to 1500 if it doesn't have the value (blocking it being 0)
            }
        }

        // updated flow type calculation using JumpType and movement direction
        private FlowType GetFlowType(JumpType jumpType)
        {
            switch (jumpType)
            {
                case JumpType.DashLeft:
                    return FlowType.DashToLeft;

                case JumpType.WalkLeft:
                    return FlowType.WalkToLeft;

                case JumpType.Standstill:
                    return FlowType.StandStill;

                case JumpType.WalkRight:
                    return FlowType.WalkToRight;

                case JumpType.DashRight:
                    return FlowType.DashToRight;

                default:
                    return FlowType.StandStill;
            }
        }
    }
}
