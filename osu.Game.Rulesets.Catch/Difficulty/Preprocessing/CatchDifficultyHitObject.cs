using System.Net.Http.Headers;
using System.ComponentModel;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.RegularExpressions;
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
        protected new PalpableCatchHitObject BaseObject => (PalpableCatchHitObject)base.BaseObject;

        protected new PalpableCatchHitObject LastObject => (PalpableCatchHitObject)base.LastObject;

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
        public readonly int JumpType;

        /// <summary>
        /// Catcher speed modified by mods and hyperdash
        /// </summary>
        public readonly double CatcherSpeed;

        public readonly bool IsHyper;

        public CatchDifficultyHitObject(HitObject hitObject, HitObject lastObject, double clockRate, float halfCatcherWidth, List<DifficultyHitObject> objects, int index)
            : base(hitObject, lastObject, clockRate, objects, index)
        {
            // We will scale everything by this factor, so we can assume a uniform CircleSize among beatmaps.
            // but disabled the code because it applies in only std case because catcher speed is fixed regardless resolution/cs
            // float scalingFactor = normalized_hitobject_radius / halfCatcherWidth;
            // NormalizedPosition = BaseObject.EffectiveX * scalingFactor;
            // LastNormalizedPosition = LastObject.EffectiveX * scalingFactor;
         
            DistanceMoved = BaseObject.EffectiveX - LastObject.EffectiveX;

            //Inertia comes stronger from faster hyperdash and the reading error comes from bigger cs because edge of catcher is where to judge hdash
            PlayerMoved = DistanceMoved + getExpectableInertia(clockRate)*halfCatcherWidth/2

            // Every strain interval is hard capped at the equivalent of 25ms as a safety measure which is 1/8snap in bpm300
            StrainTime = Math.Max(25, DeltaTime);

            JumpType = getJumpType(halfCatcherWidth);

            CatcherSpeed = clockRate * getHyperDashSpeed(BaseObject);
            
            IsHyper = LastObject.hyperDash;

        private double getExpectableInertia(double clockRate)
        {
            return Match.Clamp(Math.Sqrt(getHyperDashSpeed(base.Previous(0)) / clockRate),1,2)-1;
        }

        private double getHyperDashSpeed(PalpableCatchHitObject current){
            return Math.Max(1,(current.EffectiveX-current.Previous(0).EffectiveX) / Math.Max(1.0, DeltaTime - 1000.0 / 60.0));
        }

        private int getJumpType(float halfCatcherSize)
        {
            int jumpType = 0;
            if (PlayerMoved > halfCatcherSize*1.2)
            {
                jumpType += 1;
                if (LastObject.hyperDash){
                    jumpType += 4;
                }else if (PlayerMoved/StrainTime >= 0.5){
                    jumpType += 1;
                    if ((PlayerMoved-halfCatcherSize)/StrainTime > 0.9)
                    {
                        jumpType += 2;
                    }else jumpType += 1;
                }
                jumpType *= Math.Sign(jumpType);
            }
            return jumpType;
        }
    }
}
