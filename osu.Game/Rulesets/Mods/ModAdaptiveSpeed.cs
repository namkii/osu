// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Audio;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Mods
{
    public class ModAdaptiveSpeed : Mod, IApplicableToRate, IApplicableToDrawableHitObject, IApplicableToBeatmap, IUpdatableByPlayfield
    {
        // use a wider range so there's still room for adjustment when the initial rate is extreme
        private const double fastest_rate = 2.5f;
        private const double slowest_rate = 0.4f;

        /// <summary>
        /// Adjust track rate using the average speed of the last x hits
        /// </summary>
        private const int average_count = 6;

        public override string Name => "Adaptive Speed";

        public override string Acronym => "AS";

        public override string Description => "Let track speed adapt to you.";

        public override ModType Type => ModType.Fun;

        public override double ScoreMultiplier => 1;

        public override Type[] IncompatibleMods => new[] { typeof(ModRateAdjust), typeof(ModTimeRamp) };

        [SettingSource("Initial rate", "The starting speed of the track")]
        public BindableNumber<double> InitialRate { get; } = new BindableDouble
        {
            MinValue = 0.5,
            MaxValue = 2,
            Default = 1,
            Value = 1,
            Precision = 0.01
        };

        [SettingSource("Adjust pitch", "Should pitch be adjusted with speed")]
        public BindableBool AdjustPitch { get; } = new BindableBool
        {
            Default = true,
            Value = true
        };

        public BindableNumber<double> SpeedChange { get; } = new BindableDouble
        {
            Default = 1,
            Value = 1
        };

        private ITrack track;
        private double targetRate = 1d;

        private readonly List<double> recentRates = Enumerable.Repeat(1d, average_count).ToList();

        /// <summary>
        /// Rate for a hit is calculated using the end time of another hit object earlier in time,
        /// caching them here for easy access
        /// </summary>
        private readonly Dictionary<HitObject, double> previousEndTimes = new Dictionary<HitObject, double>();

        /// <summary>
        /// Record the value removed from <see cref="recentRates"/> when an object is hit for rewind support
        /// </summary>
        private readonly Dictionary<HitObject, double> dequeuedRates = new Dictionary<HitObject, double>();

        public ModAdaptiveSpeed()
        {
            InitialRate.BindValueChanged(val =>
            {
                SpeedChange.Value = val.NewValue;
                targetRate = val.NewValue;
            });
            AdjustPitch.BindValueChanged(adjustPitchChanged);
        }

        public void ApplyToTrack(ITrack track)
        {
            this.track = track;

            InitialRate.TriggerChange();
            AdjustPitch.TriggerChange();
            recentRates.Clear();
            recentRates.AddRange(Enumerable.Repeat(InitialRate.Value, average_count));
        }

        public void ApplyToSample(DrawableSample sample)
        {
            sample.AddAdjustment(AdjustableProperty.Frequency, SpeedChange);
        }

        public void Update(Playfield playfield)
        {
            SpeedChange.Value = Interpolation.DampContinuously(SpeedChange.Value, targetRate, 50, playfield.Clock.ElapsedFrameTime);
        }

        public double ApplyToRate(double time, double rate = 1) => rate * InitialRate.Value;

        public void ApplyToDrawableHitObject(DrawableHitObject drawable)
        {
            drawable.OnNewResult += (o, result) =>
            {
                if (dequeuedRates.ContainsKey(result.HitObject)) return;
                if (!shouldProcessResult(result)) return;

                double prevEndTime = previousEndTimes[result.HitObject];

                recentRates.Add(Math.Clamp((result.HitObject.GetEndTime() - prevEndTime) / (result.TimeAbsolute - prevEndTime) * SpeedChange.Value, slowest_rate, fastest_rate));

                dequeuedRates.Add(result.HitObject, recentRates[0]);
                recentRates.RemoveAt(0);

                targetRate = recentRates.Average();
            };
            drawable.OnRevertResult += (o, result) =>
            {
                if (!dequeuedRates.ContainsKey(result.HitObject)) return;
                if (!shouldProcessResult(result)) return;

                recentRates.Insert(0, dequeuedRates[result.HitObject]);
                recentRates.RemoveAt(recentRates.Count - 1);
                dequeuedRates.Remove(result.HitObject);

                targetRate = recentRates.Average();
            };
        }

        public void ApplyToBeatmap(IBeatmap beatmap)
        {
            var hitObjects = getAllApplicableHitObjects(beatmap.HitObjects).ToList();
            var endTimes = hitObjects.Select(x => x.GetEndTime()).OrderBy(x => x).Distinct().ToList();

            foreach (HitObject hitObject in hitObjects)
            {
                int index = endTimes.BinarySearch(hitObject.GetEndTime());
                if (index < 0) index = ~index; // BinarySearch returns the next larger element in bitwise complement if there's no exact match
                index -= 1;

                if (index >= 0)
                    previousEndTimes.Add(hitObject, endTimes[index]);
            }
        }

        private void adjustPitchChanged(ValueChangedEvent<bool> adjustPitchSetting)
        {
            track?.RemoveAdjustment(adjustmentForPitchSetting(adjustPitchSetting.OldValue), SpeedChange);

            track?.AddAdjustment(adjustmentForPitchSetting(adjustPitchSetting.NewValue), SpeedChange);
        }

        private AdjustableProperty adjustmentForPitchSetting(bool adjustPitchSettingValue)
            => adjustPitchSettingValue ? AdjustableProperty.Frequency : AdjustableProperty.Tempo;

        private IEnumerable<HitObject> getAllApplicableHitObjects(IEnumerable<HitObject> hitObjects)
        {
            foreach (var hitObject in hitObjects)
            {
                if (!(hitObject.HitWindows is HitWindows.EmptyHitWindows))
                    yield return hitObject;

                foreach (HitObject nested in getAllApplicableHitObjects(hitObject.NestedHitObjects))
                    yield return nested;
            }
        }

        private bool shouldProcessResult(JudgementResult result)
        {
            if (!result.IsHit) return false;
            if (!result.Type.AffectsAccuracy()) return false;
            if (!previousEndTimes.ContainsKey(result.HitObject)) return false;

            return true;
        }
    }
}
