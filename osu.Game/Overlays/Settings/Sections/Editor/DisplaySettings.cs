// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.
#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Input.Handlers.Mouse;
using osu.Framework.Localisation;
using osu.Game.Configuration;

namespace osu.Game.Overlays.Settings.Sections.Editor
{
    public partial class DisplaySettings : SettingsSubsection
    {
        private readonly MouseHandler mouseHandler;
        protected override LocalisableString Header => new LocalisableString("Display settings");
        private SettingsCheckbox displayInEditor;
        private SettingsCheckbox useLegacyTicks;
        private SettingsCheckbox showWaveform;
        // private Bindable<bool> relativeMode;

        public DisplaySettings(MouseHandler mouseHandler)
        {
            this.mouseHandler = mouseHandler;
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager osuConfig, FrameworkConfigManager config)
        {
            // relativeMode = mouseHandler;

            Children = new Drawable[]
            {
                displayInEditor = new SettingsCheckbox
                {
                    LabelText = new LocalisableString("Show cursor in editor"),
                    TooltipText = new LocalisableString("Show cursor in editor while placing objects."),
                    Current = osuConfig.GetBindable<bool>(OsuSetting.EditorMouse),
                    Keywords = new[] { @"show", @"editor", @"cursor" }
                },
                useLegacyTicks = new SettingsCheckbox
                {
                    LabelText = new LocalisableString("Use legacy timeline"),
                    TooltipText = new LocalisableString("Use more compact legacy timeline instead of a newer one"),
                    Current = osuConfig.GetBindable<bool>(OsuSetting.EditorTimeline),
                    Keywords = new[] { @"show", @"editor", @"timeline", @"ticks" }
                },
                showWaveform = new SettingsCheckbox
                {
                    LabelText = new LocalisableString("Show waveform"),
                    TooltipText = new LocalisableString("Show waveform as a default in Editor"),
                    Current = osuConfig.GetBindable<bool>(OsuSetting.EditorWaveform),
                    Keywords = new[] { @"show", @"editor", @"waveform" }
                },
            };
        }

        // protected override void LoadComplete()
        // {
        //     base.LoadComplete();
        //
        //     useLegacyTicks.Current.BindValueChanged(value =>
        //     {
        //         base.
        //     });
        // }
    }
}
