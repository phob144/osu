using Microsoft.VisualBasic.CompilerServices;
// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Catch.Beatmaps;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;
using osu.Game.Rulesets.Catch.Difficulty.Skills;
using osu.Game.Rulesets.Catch.Mods;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch.UI;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Catch.Difficulty
{
    public class CatchDifficultyCalculator : DifficultyCalculator
    {
        private const double difficulty_multiplier = 1;

        private float halfCatcherWidth;

        public override int Version => 20250306;

        public CatchDifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap)
            : base(ruleset, beatmap)
        {
        }

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills, double clockRate)
        {
            if (beatmap.HitObjects.Count == 0)
                return new CatchDifficultyAttributes { Mods = mods };

            var speed = skills.OfType<Speed>().Single();
            var precision = skills.OfType<Precision>().Single();
            var reading = skills.OfType<Reading>().Single();

            double speedValue = speed.DifficultyValue();
            double precisionValue = precision.DifficultyValue();
            double readingValue = reading.DifficultyValue();

            double total = speedValue + precisionValue + readingValue;

            CatchDifficultyAttributes attributes = new CatchDifficultyAttributes
            {
                StarRating = Math.Sqrt(total) * difficulty_multiplier,
                Mods = mods,
                MaxCombo = beatmap.GetMaxCombo(),

                SpeedDifficulty = speedValue,
                PrecisionDifficulty = precisionValue,
                ReadingDifficulty = readingValue,
            };

            return attributes;
        }

        protected override IEnumerable<DifficultyHitObject> CreateDifficultyHitObjects(IBeatmap beatmap, double clockRate)
        {
            CatchHitObject? lastObject = null;
            List<DifficultyHitObject> objects = new List<DifficultyHitObject>();

            foreach (var hitObject in CatchBeatmap.GetPalpableObjects(beatmap.HitObjects))
            {
                if (hitObject is Banana || hitObject is TinyDroplet)
                    continue;

                if (lastObject != null)
                {
                    objects.Add(new CatchDifficultyHitObject(
                        hitObject,
                        lastObject,
                        clockRate,
                        halfCatcherWidth,
                        objects,
                        objects.Count
                    ));
                }

                lastObject = hitObject;
            }

            return objects;
        }

        protected override Skill[] CreateSkills(IBeatmap beatmap, Mod[] mods, double clockRate)
        {
            halfCatcherWidth = Catcher.CalculateCatchWidth(beatmap.Difficulty) * 0.5f;

            return new Skill[]
            {
                new Speed(mods, halfCatcherWidth, clockRate),
                new Precision(mods, halfCatcherWidth, clockRate,beatmap.Difficulty.CircleSize),
                new Reading(mods, halfCatcherWidth, clockRate),
            };
        }

        protected override Mod[] DifficultyAdjustmentMods => new Mod[]
        {
            new CatchModDoubleTime(),
            new CatchModHalfTime(),
            new CatchModHardRock(),
            new CatchModEasy(),
        };

        
    }
}
