// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Handlers.Mouse;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Game.Overlays.Settings.Sections.Editor;

namespace osu.Game.Overlays.Settings.Sections
{
    public partial class EditorSection : SettingsSection
    {
        private MouseHandler mh;

        public EditorSection()
        {
            Children = new Drawable[]
            {
                new DisplaySettings(mh)
            };
        }

        public override LocalisableString Header => new LocalisableString("Editor");

        public override Drawable CreateIcon() => new SpriteIcon()
        {
            Icon = FontAwesome.Solid.Edit
        };

        [BackgroundDependencyLoader]
        private void load(GameHost host, OsuGameBase game)
        {
            foreach (var handler in host.AvailableInputHandlers)
            {
                switch (handler)
                {
                    case MouseHandler mh:
                        this.mh = mh;
                        break;
                }
            }
        }
    }
}
