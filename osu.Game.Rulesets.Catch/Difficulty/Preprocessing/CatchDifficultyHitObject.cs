// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Collections.Generic;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Rulesets.Catch.Difficulty.Preprocessing
{
    public enum JumpType
    {
        Standstill = 0,
        Walk = 1,
        Dash = 2
    }

    public class CatchDifficultyHitObject : DifficultyHitObject
    {
        public new PalpableCatchHitObject BaseObject => (PalpableCatchHitObject)base.BaseObject;
        public new PalpableCatchHitObject LastObject => (PalpableCatchHitObject)base.LastObject;

        public readonly float NormalizedPosition;
        public readonly float LastNormalizedPosition;
        private const float normalized_hitobject_radius = 41.0f;

        public readonly double DistanceMoved;
        public readonly double StrainTime; // capped at 25ms
        public readonly JumpType jumpType;
        public readonly List<JumpType> JumpTypeCandidates;
        public readonly double CatcherSpeed;
        public readonly double Inertia; // from hyperdash speed
        public readonly int BuzzCount;

        public readonly float HalfCatcherWidth;

        public readonly Flow Flow;

        public CatchDifficultyHitObject(HitObject hitObject, HitObject lastObject, double clockRate, float halfCatcherWidth, List<DifficultyHitObject> objects, int index)
            : base(hitObject, lastObject, clockRate, objects, index)
        {
            float scalingFactor = normalized_hitobject_radius / halfCatcherWidth;

            NormalizedPosition = BaseObject.EffectiveX * scalingFactor;
            LastNormalizedPosition = LastObject.EffectiveX * scalingFactor;

            HalfCatcherWidth = halfCatcherWidth;
            DistanceMoved = BaseObject.EffectiveX - LastObject.EffectiveX;
            Inertia = getExpectableInertia(clockRate);
            BuzzCount = CountBuzzCluster(HalfCatcherWidth);
            StrainTime = Math.Max(25, DeltaTime);
            CatcherSpeed = clockRate * getHyperDashSpeed(this);
            JumpTypeCandidates = Jump.GetCandidates(DistanceMoved, halfCatcherWidth, StrainTime);
            jumpType = Jump.Resolve(JumpTypeCandidates, (base.Previous(0) as CatchDifficultyHitObject)?.JumpTypeCandidates);
            Flow = new Flow(this, halfCatcherWidth);
        }

        private double getExpectableInertia(double clockRate)
        {
            var prev = base.Previous(0);
            if (prev is CatchDifficultyHitObject p && p.LastObject.HyperDash)
            {
                return Math.Clamp(getHyperDashSpeed(p) * clockRate, 1, 2.5) - 1;
            }

            return 0;
        }

        private double getHyperDashSpeed(CatchDifficultyHitObject current)
        {
            var prev = LastObject;
            if (prev == null && !prev.HyperDash)
                return 1;

            double dx = current.BaseObject.EffectiveX - prev.EffectiveX;
            double dt = Math.Max(1.0, DeltaTime - 1000.0 / 60.0);
            return Math.Max(1, dx / dt);
        }

        public int CountBuzzCluster(float halfCatcherWidth)
        {
            var positions = new List<float> { this.BaseObject.EffectiveX };

            var current = this;
            int count = 0;

            while (true)
            {
                var prev = current.Previous(0) as CatchDifficultyHitObject;
                if (prev == null)
                    break;

                positions.Add(prev.BaseObject.EffectiveX);

                float min = positions.Min();
                float max = positions.Max();
                if (max - min > halfCatcherWidth * 2)
                    break;

                count++;
                current = prev;
            }

            return count;
        }
    }
}