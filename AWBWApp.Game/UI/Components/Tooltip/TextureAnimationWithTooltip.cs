using osu.Framework.Graphics.Animations;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Localisation;

namespace AWBWApp.Game.UI.Components.Tooltip
{
    public class TextureAnimationWithTooltip : TextureAnimation, IHasTooltip
    {
        private readonly string tooltip;

        public TextureAnimationWithTooltip(string tooltip)
        {
            this.tooltip = tooltip;
        }

        public LocalisableString TooltipText => tooltip;
    }
}
