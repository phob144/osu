using System.Data;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Reflection.Metadata;
using System.Security.AccessControl;
using System.Net.Http.Headers;
using System.ComponentModel;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.RegularExpressions;
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
        HyperDashToLeft = -5,
        EdgeDashToLeft = -4,
        DashToLeft = -3,
        MidDashToLeft = -2,
        WalkToLeft = -1,
        Standstill = 0,
        WalkToRight = 1,
        MidDashToRight = 2,
        DashToRight = 3,
        EdgeDashToRight = 4,
        HyperDashToRight = 5
    }

    public class CatchDifficultyHitObject : DifficultyHitObject
    {
        public new PalpableCatchHitObject BaseObject => (PalpableCatchHitObject)base.BaseObject;
        public new PalpableCatchHitObject LastObject => (PalpableCatchHitObject)base.LastObject;

        public readonly float NormalizedPosition;
        public readonly float LastNormalizedPosition;
        private const float normalized_hitobject_radius = 41.0f;

        public readonly double DistanceMoved;
        public readonly double PlayerMoved;
        public readonly double StrainTime;
        public readonly JumpType jumpType;

        public readonly List<JumpType> JumpTypes;
        public readonly JumpType ResolvedJumpType;

        public readonly double CatcherSpeed;
        public readonly bool IsHyper;
        public readonly double EdgeRatio;

        public readonly Flow Flow;

        public CatchDifficultyHitObject(HitObject hitObject, HitObject lastObject, double clockRate, float halfCatcherWidth, List<DifficultyHitObject> objects, int index)
            : base(hitObject, lastObject, clockRate, objects, index)
        {
            float scalingFactor = normalized_hitobject_radius / halfCatcherWidth;

            NormalizedPosition = BaseObject.EffectiveX * scalingFactor;
            LastNormalizedPosition = LastObject.EffectiveX * scalingFactor;

            DistanceMoved = BaseObject.EffectiveX - LastObject.EffectiveX;

            PlayerMoved = DistanceMoved - (getExpectableInertia(clockRate) * halfCatcherWidth / 2);
            StrainTime = Math.Max(25, DeltaTime);
            EdgeRatio = Math.Max(0, (PlayerMoved - halfCatcherWidth) / StrainTime);
            CatcherSpeed = clockRate * getHyperDashSpeed(this);
            IsHyper = LastObject.HyperDash;
            JumpTypes = getJumpTypeCandidates(PlayerMoved, halfCatcherWidth, EdgeRatio);
            ResolvedJumpType = resolveJumpType(JumpTypes, base.Previous(0) as CatchDifficultyHitObject);
            jumpType = ResolvedJumpType;
            Flow = new Flow(this, halfCatcherWidth);
        }

        private double getExpectableInertia(double clockRate)
        {
            var prev = base.Previous(0);
            if (prev is CatchDifficultyHitObject p && p.IsHyper)
            {
                double inertia = Math.Clamp(Math.Sqrt(getHyperDashSpeed(p) * clockRate), 1, 2) - 1;
                return Math.Sign(p.PlayerMoved) * inertia;
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

        private List<JumpType> getJumpTypeCandidates(double PlayerMoved, float halfCatcherSize, double edgeRatio)
        {
            List<JumpType> result = new();
            double absMove = Math.Abs(PlayerMoved);
            int direction = Math.Sign(PlayerMoved);

            // 1. StandStill condition
            if (absMove <= halfCatcherSize * 2)
                result.Add(JumpType.Standstill);

            // 2. Walk condition
            if (absMove >= StrainTime * 0.5 - halfCatcherSize*2 &&
                absMove <= StrainTime * 0.5 + halfCatcherSize*2)
                result.Add((JumpType)(1 * direction));

            // 3. Dash condition
            if (absMove >= StrainTime - halfCatcherSize*1.8 - 7)
                result.Add((JumpType)(3 * direction));

            // 4. MidDash condition
            if (absMove > StrainTime * 0.5 + halfCatcherSize*2 &&
                absMove < StrainTime - halfCatcherSize*1.8)
                result.Add((JumpType)(2 * direction));

            // 5. EdgeDash condition
            if (!LastObject.HyperDash && absMove > StrainTime - halfCatcherSize - 7)
                result.Add((JumpType)(4 * direction));

            // 6. HyperDash condition
            if (LastObject.HyperDash)
                result.Add((JumpType)(5 * direction));

            return result.Distinct().ToList();
        }

        private JumpType resolveJumpType(List<JumpType> current, CatchDifficultyHitObject? prev)
        {
            if (current == null || current.Count == 0)
            {
                return JumpType.Standstill;
            }

            List<JumpType> previous = prev?.JumpTypes ?? new List<JumpType>();

            //check continuous
            var shared = previous.Intersect(current).ToList();
            if (shared.Count > 0)
                return shared.First();

            //check similarity
            if (previous.Count > 0)
            {
                int prevValue = (int)previous.First();
                return current.OrderBy(j => Math.Abs((int)j - prevValue)).First();
            }

            //none of above case
            var priority = new List<JumpType>
            {
                JumpType.HyperDashToLeft, JumpType.HyperDashToRight,
                JumpType.EdgeDashToLeft, JumpType.EdgeDashToRight,
                JumpType.DashToLeft, JumpType.DashToRight,
                JumpType.MidDashToLeft, JumpType.MidDashToRight,
                JumpType.WalkToLeft, JumpType.WalkToRight,
                JumpType.Standstill
            };

            foreach (var p in priority)
            {
                if (current.Contains(p))
                    return p;
            }

            return current.First();
        }
    }
}
