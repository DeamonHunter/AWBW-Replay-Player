using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Localisation;

namespace AWBWApp.Game.UI.Replay.Toolbar
{
    public class ToggleMenuItem : MenuItem
    {
        public readonly BindableBool State = new BindableBool();

        public ToggleMenuItem(LocalisableString text, Bindable<bool> bindable)
            : base(text)
        {
            Action.Value = () =>
            {
                State.Value = !State.Value;
            };
            State.BindTo(bindable);
        }

        public ToggleMenuItem(LocalisableString text, Action<bool> action)
            : base(text)
        {
            Action.Value = () =>
            {
                State.Value = !State.Value;
                action?.Invoke(State.Value);
            };
        }

        public IconUsage? GetIconForState(bool state) => state ? (IconUsage?)FontAwesome.Solid.Check : null;
    }
}
