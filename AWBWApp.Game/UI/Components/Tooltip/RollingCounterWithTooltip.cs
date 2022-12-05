using System;
using AWBWApp.Game.UI.Replay;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Localisation;

namespace AWBWApp.Game.UI.Components.Tooltip
{
    public partial class RollingCounterWithTooltip<T> : RollingCounter<T>, IHasTooltip where T : struct, IEquatable<T>
    {
        public string Tooltip { get; set; }

        public LocalisableString TooltipText => Tooltip;
    }
}
