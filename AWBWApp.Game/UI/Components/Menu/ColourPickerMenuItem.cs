using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;

namespace AWBWApp.Game.UI.Components.Menu
{
    public class ColourPickerMenuItem : MenuItem
    {
        public readonly Bindable<Colour4> State;

        public ColourPickerMenuItem(Bindable<Colour4> bindable)
            : base(string.Empty)
        {
            State = bindable;
        }
    }
}
