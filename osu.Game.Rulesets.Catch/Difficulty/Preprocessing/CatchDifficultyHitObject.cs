using System.Text.RegularExpressions;
// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch.UI;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Rulesets.Catch.Difficulty.Preprocessing
{
    public class CatchDifficultyHitObject : DifficultyHitObject
    {
        public new PalpableCatchHitObject BaseObject => (PalpableCatchHitObject)base.BaseObject;
        public new PalpableCatchHitObject LastObject => (PalpableCatchHitObject)base.LastObject;

        public readonly int BuzzCount;
        public readonly double CatcherDashSpeed;
        private readonly double catcherWalkSpeed;
        public readonly float DistanceMoved;
        public readonly Flow Flow;

        private int movementDiscrepancyCount;
        private double movementDiscrepancyStack;
        public readonly MovementType MovementType;

        /// <summary>
        /// Milliseconds elapsed since the start time of the previous <see cref="CatchDifficultyHitObject"/>, with a minimum of 20ms.
        /// </summary>
        public readonly double StrainTime;

        public CatchDifficultyHitObject(HitObject hitObject, HitObject lastObject, double clockRate, float halfCatcherWidth, List<DifficultyHitObject> objects, List<Flow> flows, int index)
            : base(hitObject, lastObject, clockRate, objects, index)
        {
            DistanceMoved = BaseObject.EffectiveX - LastObject.EffectiveX;

            catcherWalkSpeed = Catcher.BASE_WALK_SPEED * clockRate;
            CatcherDashSpeed = Catcher.BASE_DASH_SPEED * clockRate * getHyperDashModifier(clockRate);

            BuzzCount = getBuzzCount(halfCatcherWidth);
            DistanceMoved *= 1 - Math.Clamp(BuzzCount - 1, 0, 4) / 4.0f;

            MovementType = getMovementType(halfCatcherWidth, clockRate);

            // Every strain interval is hard capped at the equivalent of 375 BPM 1/8 speed as a safety measure
            StrainTime = Math.Max(20, DeltaTime);

            var prev = (CatchDifficultyHitObject)Previous(0);

            if (prev != null && prev.Flow.MovementType == MovementType)
            {
                prev.Flow.Add(this);
                Flow = prev.Flow;
            }
            else
            {
                Flow = new Flow(new List<CatchDifficultyHitObject> { this }, flows, flows.Count);
                flows.Add(Flow);
            }
        }

        private int getBuzzCount(float halfCatcherWidth)
        {
            if (DistanceMoved > halfCatcherWidth * 2)
                return 0;

            float min = DistanceMoved < 0 ? BaseObject.EffectiveX : LastObject.EffectiveX;
            float max = DistanceMoved < 0 ? LastObject.EffectiveX : BaseObject.EffectiveX;
            int count = 1;

            for (int i = 0; i < Index; i++)
            {
                var prev = (CatchDifficultyHitObject)Previous(i);

                min = Math.Min(min, prev.LastObject.EffectiveX);
                max = Math.Max(max, prev.LastObject.EffectiveX);

                if (max - min > halfCatcherWidth * 2)
                    break;

                count++;
            }

            return count;
        }

        private double getHyperDashModifier(double clockRate) => LastObject.HyperDash ? Math.Max(1, Math.Abs(DistanceMoved) / Math.Max(1, DeltaTime * clockRate - 1000.0 / 60.0)) : 1;

        private MovementType getMovementType(float halfCatcherWidth, double clockRate)
        {
            var naiveMovementType = getNaiveMovementType(halfCatcherWidth);
            var adjustedMovementType = getAdjustedMovementType(naiveMovementType, halfCatcherWidth, clockRate);

            var prev = (CatchDifficultyHitObject)Previous(0);

            if (prev == null)
                return adjustedMovementType;

            movementDiscrepancyStack = adjustedMovementType != naiveMovementType ? (halfCatcherWidth*2)/Math.Max(Math.Max(Math.Abs(DistanceMoved),Math.Abs(DistanceMoved + prev.DistanceMoved)),0.1) : 0;
            movementDiscrepancyCount = 1;

            if ((prev.movementDiscrepancyStack >= 4.5 && prev.movementDiscrepancyCount == 3)) // double checking for buzz case and linear case both
                return naiveMovementType;

            if (adjustedMovementType != naiveMovementType)
                movementDiscrepancyStack += prev.movementDiscrepancyStack;
                movementDiscrepancyCount += prev.movementDiscrepancyCount;

            return adjustedMovementType;
        }

        private MovementType getNaiveMovementType(float halfCatcherWidth)
        {
            if (Math.Abs(DistanceMoved) <= halfCatcherWidth)
                return MovementType.Standstill;

            if (Math.Abs(DistanceMoved) <= catcherWalkSpeed * DeltaTime + halfCatcherWidth)
                return DistanceMoved < 0 ? MovementType.WalkLeft : MovementType.WalkRight;

            return DistanceMoved < 0 ? MovementType.DashLeft : MovementType.DashRight;
        }

        private MovementType getAdjustedMovementType(MovementType naiveMovementType, float halfCatcherWidth, double clockRate)
        {
            var prev = (CatchDifficultyHitObject)Previous(0);
            var movementTypeCandidates = getMovementTypeCandidates(halfCatcherWidth, clockRate);

            if (prev == null || movementTypeCandidates.Count == 0)
                return naiveMovementType;

            if (movementTypeCandidates.Contains(prev.MovementType))
                return prev.MovementType;

            var priority = new List<Func<MovementType?, bool>>()
            {
                m => m!.Value.IsDash() && m.Value.IsSameDirection(prev.MovementType),
                m => m!.Value.IsDash() && !m.Value.IsSameDirection(prev.MovementType),
                m => m!.Value == MovementType.Standstill,
                m => m!.Value.IsWalk() && m.Value.IsSameDirection(prev.MovementType),
                m => m!.Value.IsWalk() && !m.Value.IsSameDirection(prev.MovementType),
            };

            var movementTypeCandidatesNullable = movementTypeCandidates.Cast<MovementType?>();

            foreach (var condition in priority)
            {
                var match = movementTypeCandidatesNullable.FirstOrDefault(condition);
                if (match != null)
                    return match.Value;
            }

            return naiveMovementType;
        }

        private List<MovementType> getMovementTypeCandidates(float halfCatcherWidth, double clockRate)
        {
            var movementTypeCandidates = new List<MovementType>();

            if (Math.Abs(DistanceMoved) <= halfCatcherWidth * 2)
                movementTypeCandidates.Add(MovementType.Standstill);

            if (Math.Abs(-1 * DistanceMoved - catcherWalkSpeed * DeltaTime) <= halfCatcherWidth)
                movementTypeCandidates.Add(MovementType.WalkLeft);

            if (Math.Abs(DistanceMoved - catcherWalkSpeed * DeltaTime) <= halfCatcherWidth)
                movementTypeCandidates.Add(MovementType.WalkRight);

            if (-1 * DistanceMoved >= 0.8 * clockRate * DeltaTime || -1 * DistanceMoved >= Catcher.BASE_DASH_SPEED * clockRate * DeltaTime - halfCatcherWidth)
                movementTypeCandidates.Add(MovementType.DashLeft);

            if (DistanceMoved >= 0.8 * clockRate * DeltaTime || DistanceMoved >= Catcher.BASE_DASH_SPEED * clockRate * DeltaTime - halfCatcherWidth)
                movementTypeCandidates.Add(MovementType.DashRight);

            return movementTypeCandidates;
        }
    }
}
