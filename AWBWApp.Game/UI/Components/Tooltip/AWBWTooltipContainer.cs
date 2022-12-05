using osu.Framework.Graphics.Cursor;

namespace AWBWApp.Game.UI.Components.Tooltip
{
    public partial class AWBWTooltipContainer : TooltipContainer
    {
        protected override ITooltip CreateTooltip() => new TextToolTip();
    }
}
