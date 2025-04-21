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
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Catch.Objects;

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

        private readonly List<CatchDifficultyHitObject> catchDifficultyHitObjects;
        
        public new PalpableCatchHitObject BaseObject => (PalpableCatchHitObject)base.BaseObject;

        public new PalpableCatchHitObject LastObject => (PalpableCatchHitObject)base.LastObject;

        /// <summary>
        /// Exact Distance Value between 2 notes
        /// </summary>
        public readonly double DistanceMoved;
        
        /// <summary>
        /// Adjusted Distance Value with Hyperdash Inertia 
        /// </summary>
        public readonly double PlayerMoved;

        /// <summary>
        /// Milliseconds elapsed since the start time of the previous <see cref="CatchDifficultyHitObject"/>, with a minimum of 25ms.
        /// </summary>
        public readonly double StrainTime;

        /// <summary>
        /// Jump type judged by direction and distance in 11 different types
        /// 0 - standstill, 1 - walk, 2 - middash, 3 - normal dash, 4 - edge dash and 5 - hyperdash. negative - movement to the left, positive - movement to the right
        /// </summary>
        public readonly JumpType jumpType;
        /// <summary>
        /// Catcher speed modified by mods and hyperdash
        /// </summary>
        public readonly double CatcherSpeed;

        public readonly bool IsHyper;

        /// <summary>
        /// Higher means the object is near the edge, thus requiring higher precision.
        /// </summary>
        public readonly double EdgeRatio;

        public Flow Flow { get; set; }

        public CatchDifficultyHitObject(HitObject hitObject, HitObject lastObject, double clockRate, float halfCatcherWidth, List<DifficultyHitObject> objects, int index)
            : base(hitObject, lastObject, clockRate, objects, index)
        {
            // We will scale everything by this factor, so we can assume a uniform CircleSize among beatmaps.
            // but disabled the code because it applies in only std case because catcher speed is fixed regardless resolution/cs
            // float scalingFactor = normalized_hitobject_radius / halfCatcherWidth;
            // NormalizedPosition = BaseObject.EffectiveX * scalingFactor;
            // LastNormalizedPosition = LastObject.EffectiveX * scalingFactor;

            DistanceMoved = BaseObject.EffectiveX - LastObject.EffectiveX;

            // Inertia comes stronger from faster hyperdash and the reading error comes from bigger cs because edge of catcher is where to judge hdash
            PlayerMoved = DistanceMoved + getExpectableInertia(clockRate) * halfCatcherWidth / 2;

            // Every strain interval is hard capped at the equivalent of 25ms as a safety measure which is 1/8snap in bpm300
            StrainTime = Math.Max(25, DeltaTime);

            jumpType = getJumpType(halfCatcherWidth, EdgeRatio);

            CatcherSpeed = clockRate * getHyperDashSpeed(this);

            IsHyper = LastObject.HyperDash;

            EdgeRatio = Math.Max(0, (PlayerMoved - halfCatcherWidth) / StrainTime);

            catchDifficultyHitObjects = objects.Cast<CatchDifficultyHitObject>().ToList();

            Flow = new Flow(this, halfCatcherWidth);

            Index = index;
        }

        double getExpectableInertia(double clockRate)
        {
            var prev = base.Previous(0);
            if (prev is CatchDifficultyHitObject p)
                return Math.Clamp(Math.Sqrt(getHyperDashSpeed(p) / clockRate), 1, 2) - 1;
            return 0;
        }

        double getHyperDashSpeed(CatchDifficultyHitObject current){
            var prev = LastObject;
            if (prev == null)
                return 1;

            double dx = current.BaseObject.EffectiveX - prev.EffectiveX;
            double dt = Math.Max(1.0, DeltaTime - 1000.0 / 60.0);
            return Math.Max(1, dx / dt);
        }

        JumpType getJumpType(float halfCatcherSize, double edgeRatio)
        {
            if (PlayerMoved <= halfCatcherSize * 1.2)
                return JumpType.Standstill;

            if (LastObject.HyperDash)
                return (JumpType)(5 * Math.Sign(PlayerMoved));

            double speed = PlayerMoved / StrainTime;

            if (speed >= 0.875)
            {
                if (edgeRatio > 0.9)
                    return (JumpType)(4 * Math.Sign(PlayerMoved)); // EdgeDash
                return (JumpType)(3 * Math.Sign(PlayerMoved));     // Dash
            }

            return (JumpType)(2 * Math.Sign(PlayerMoved));         // MidDash
        }

        public new CatchDifficultyHitObject Previous(int backwardsIndex = 0)
        {
            int index = Index - (backwardsIndex + 1);
            return index >= 0 && index < catchDifficultyHitObjects.Count ? catchDifficultyHitObjects[index] : null;
        }

        public new CatchDifficultyHitObject Next(int forwardsIndex = 0)
        {
            int index = Index + (forwardsIndex + 1);
            return index >= 0 && index < catchDifficultyHitObjects.Count ? catchDifficultyHitObjects[index] : null;
        }
    }
}
