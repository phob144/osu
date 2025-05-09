// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Catch.Difficulty.Evaluators;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Catch.Difficulty.Skills
{
    public class Precision : StrainDecaySkill
    {
        protected override double SkillMultiplier => 0.175;
        protected override double StrainDecayBase => 0.5;

        private readonly float halfCatcherWidth;
        private readonly double clockRate;

        public Precision(Mod[] mods, float halfCatcherWidth, double clockRate)
            : base(mods)
        {
            this.halfCatcherWidth = halfCatcherWidth;
            this.clockRate = clockRate;
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            return PrecisionEvaluator.EvaluateDifficultyOf(current, halfCatcherWidth, clockRate);
        }
    }
}
