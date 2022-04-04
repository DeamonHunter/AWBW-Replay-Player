using osu.Framework.Graphics.Cursor;

namespace AWBWApp.Game.UI.Components.Tooltip
{
    public class AWBWTooltipContainer : TooltipContainer
    {
        protected override ITooltip CreateTooltip() => new TextToolTip();
    }
}
