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

            PlayerMoved = DistanceMoved + getExpectableInertia(clockRate) * halfCatcherWidth / 2;
            StrainTime = Math.Max(25, DeltaTime);
            EdgeRatio = Math.Max(0, (PlayerMoved - halfCatcherWidth) / StrainTime);
            CatcherSpeed = clockRate * getHyperDashSpeed(this);
            IsHyper = LastObject.HyperDash;
            jumpType = getJumpType(PlayerMoved, halfCatcherWidth, EdgeRatio);
            Flow = new Flow(this, halfCatcherWidth);
        }

        private double getExpectableInertia(double clockRate)
        {
            var prev = base.Previous(0);
            if (prev is CatchDifficultyHitObject p)
                return Math.Clamp(Math.Sqrt(getHyperDashSpeed(p) / clockRate), 1, 2) - 1;
            return 0;
        }

        private double getHyperDashSpeed(CatchDifficultyHitObject current)
        {
            var prev = LastObject;
            if (prev == null)
                return 1;

            double dx = current.BaseObject.EffectiveX - prev.EffectiveX;
            double dt = Math.Max(1.0, DeltaTime - 1000.0 / 60.0);
            return Math.Max(1, dx / dt);
        }

        private JumpType getJumpType(double PlayerMoved, float halfCatcherSize, double edgeRatio)
        {
            if (Math.Abs(PlayerMoved) <= (double)halfCatcherSize * 1.2)
            {
                return JumpType.Standstill;
            }

            if (LastObject.HyperDash)
            {
                return (JumpType)(5 * Math.Sign(PlayerMoved));
            }

            double speed = Math.Abs(PlayerMoved) / StrainTime;
            if(speed>=0.5){
                if (speed >= 0.75)
                {
                    if (edgeRatio > 0.875)
                        return (JumpType)(4 * Math.Sign(PlayerMoved));
                    return (JumpType)(3 * Math.Sign(PlayerMoved));
                }
                return (JumpType)(2 * Math.Sign(PlayerMoved));
            }


            return (JumpType)(Math.Sign(PlayerMoved));
        }
    }
}
