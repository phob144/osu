// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Catch.Difficulty.Evaluators;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Catch.Difficulty.Skills
{
    public class Speed : StrainDecaySkill
    {
        protected override double SkillMultiplier => 0.075;
        protected override double StrainDecayBase => 0.525;

        private readonly float halfCatcherWidth;

        public Speed(Mod[] mods, float halfCatcherWidth)
            : base(mods)
        {
            this.halfCatcherWidth = halfCatcherWidth;
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            return SpeedEvaluator.EvaluateDifficultyOf(current,halfCatcherWidth);
        }
    }
}
