// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Rulesets.Catch.Difficulty.Preprocessing
{
    public class CatchDifficultyHitObject : DifficultyHitObject
    {
        public new PalpableCatchHitObject BaseObject => (PalpableCatchHitObject)base.BaseObject;

        public new PalpableCatchHitObject LastObject => (PalpableCatchHitObject)base.LastObject;

        public readonly double DistanceMoved

        /// <summary>
        /// Milliseconds elapsed since the start time of the previous <see cref="CatchDifficultyHitObject"/>, with a minimum of 25ms.
        /// </summary>
        public readonly double StrainTime;

        /// <summary>
        /// Jump type judged by direction and distance in 11 different types
        /// 0 - standstill, 1 - walk, 2 - middash, 3 - normal dash, 4 - edge dash and 5 - hyperdash. negative - movement to the left, positive - movement to the right
        /// </summary>
        public readonly int JumpType;

        public readonly double CatcherSpeed;

        public CatchDifficultyHitObject(HitObject hitObject, HitObject lastObject, double clockRate, float halfCatcherWidth, List<DifficultyHitObject> objects, int index)
            : base(hitObject, lastObject, clockRate, objects, index)
        {
            // We will scale everything by this factor, so we can assume a uniform CircleSize among beatmaps.
            // but disabled the code because it applies in only std case because catcher speed is fixed regardless resolution/cs
            // float scalingFactor = normalized_hitobject_radius / halfCatcherWidth;
            // NormalizedPosition = BaseObject.EffectiveX * scalingFactor;
            // LastNormalizedPosition = LastObject.EffectiveX * scalingFactor;


            DistanceValue = BaseObject.EffectiveX - (LastObject.EffectiveX + (getExpectableInertia()*halfCatcherWidth/2));

            // Every strain interval is hard capped at the equivalent of 25ms as a safety measure which is 1/8snap in bpm300
            StrainTime = Math.Max(25, DeltaTime);

            JumpType = getJumpType();

            CatcherSpeed = clockRate; // *hdash speed value
        }

        private double getExpectableInertia()
        {
            //TODO : if previous(0) was the destination of hdash,
            //TODO : return Math.Clamp(Math.Sqrt(hdash speed value),1,2)-1 else return 0
            //Inertia comes stronger from faster hyperdash and the reading error comes from bigger cs because edge of catcher is where to judge hdash
            return 0;

        }

        private int getJumpType()
        {
            //TODO : follow page2 of Sketch 2 docs
        }
    }
}
