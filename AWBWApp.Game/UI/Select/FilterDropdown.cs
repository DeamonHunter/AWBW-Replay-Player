using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;

namespace AWBWApp.Game.UI.Select
{
    public partial class FilterDropdown : BasicDropdown<CarouselFilter>
    {
        public FilterDropdown()
        {
            AutoSizeAxes = Axes.None;
            Items = Enum.GetValues<CarouselFilter>();
        }
    }
}
