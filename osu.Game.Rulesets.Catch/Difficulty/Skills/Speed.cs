// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Catch.Beatmaps;
using osu.Game.Rulesets.Catch.Difficulty.Evaluators;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Catch.Difficulty.Skills
{
    public class Speed : StrainDecaySkill
    {
        protected override double SkillMultiplier => 0.8;
        protected override double StrainDecayBase => 0.5;
        protected override int SectionLength => 1500;

        protected readonly float HalfCatcherWidth;
        private readonly double clockRate;

        public Speed(Mod[] mods, float halfCatcherWidth, double clockRate)
            : base(mods)
        {
            HalfCatcherWidth = halfCatcherWidth;
            this.clockRate = clockRate;
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            return SpeedEvaluator.EvaluateDifficultyOf(current,HalfCatcherWidth);
        }
    }
}