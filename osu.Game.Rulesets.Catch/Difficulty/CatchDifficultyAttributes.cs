// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;

namespace osu.Game.Rulesets.Catch.Difficulty
{
    public class CatchDifficultyAttributes : DifficultyAttributes
    {
        /// <summary>
        /// The difficulty value corresponding to precision
        /// </summary>
        public double PrecisionDifficulty { get; set; }

        /// <summary>
        /// The difficulty value corresponding to reading
        /// </summary>
        public double ReadingDifficulty { get; set; }

        /// <summary>
        /// The difficulty value corresponding to speed
        /// </summary>
        public double SpeedDifficulty { get; set; }

        public override IEnumerable<(int attributeId, object value)> ToDatabaseAttributes()
        {
            foreach (var v in base.ToDatabaseAttributes())
                yield return v;

            // Temporary fallback until a proper schema is assigned
            yield return (ATTRIB_ID_AIM, StarRating);
        }

        public override void FromDatabaseAttributes(IReadOnlyDictionary<int, double> values, IBeatmapOnlineInfo onlineInfo)
        {
            base.FromDatabaseAttributes(values, onlineInfo);

            StarRating = values[ATTRIB_ID_AIM];
        }
    }
}