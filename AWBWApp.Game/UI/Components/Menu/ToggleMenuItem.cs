using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Localisation;

namespace AWBWApp.Game.UI.Components.Menu
{
    public class ToggleMenuItem : MenuItem
    {
        public readonly Bindable<bool> State;

        public ToggleMenuItem(LocalisableString text, Bindable<bool> bindable)
            : base(text)
        {
            State = bindable;
            Action.Value = () =>
            {
                State.Value = !State.Value;
            };
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
