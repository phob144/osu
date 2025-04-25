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
    public class Precision : StrainDecaySkill
    {
        protected override double SkillMultiplier => 1;
        protected override double StrainDecayBase => 0.75;
        protected override double DecayWeight => 0.55;
        protected override int SectionLength => 1500;

        protected readonly float HalfCatcherWidth;
        private readonly double catcherSpeedMultiplier;

        protected float CircleSize;

        public Precision(Mod[] mods, float halfCatcherWidth, double clockRate, float beatmapCirclesize)
            : base(mods)
        {
            HalfCatcherWidth = halfCatcherWidth;
            catcherSpeedMultiplier = clockRate;
            CircleSize = beatmapCirclesize;
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            return PrecisionEvaluator.EvaluateDifficultyOf(current,CircleSize,HalfCatcherWidth);
        }
    }
}