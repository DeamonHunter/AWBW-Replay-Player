using System.Collections.Generic;
using AWBWApp.Game.UI.Select;
using osu.Framework.Configuration;
using osu.Framework.Platform;

namespace AWBWApp.Game
{
    public class AWBWConfigManager : IniConfigManager<AWBWSetting>
    {
        public AWBWConfigManager(Storage storage, IDictionary<AWBWSetting, object> defaultOverrides = null)
            : base(storage, defaultOverrides)
        {
        }

        protected override void InitialiseDefaults()
        {
            SetDefault(AWBWSetting.Version, string.Empty);

            SetDefault(AWBWSetting.ReplaySkipEndTurn, false);
            SetDefault(AWBWSetting.ReplayShowHiddenUnits, true);
            SetDefault(AWBWSetting.ReplayShowGridOverMap, false);
            SetDefault(AWBWSetting.ReplayShortenActionToolTips, false);
            SetDefault(AWBWSetting.ReplayShowMovementArrows, true);
            SetDefault(AWBWSetting.PlayerListScale, 1f);
            SetDefault(AWBWSetting.PlayerListLeftSide, false);
            SetDefault(AWBWSetting.ReplayBarControlScale, 1f);
            SetDefault(AWBWSetting.ReplayBarControlPositionX, 0f);
            SetDefault(AWBWSetting.ReplayBarControlPositionY, 0f);
            SetDefault(AWBWSetting.ReplayListSort, CarouselSort.EndDate);
            SetDefault(AWBWSetting.PlayerListKeepOrderStatic, false);
            SetDefault(AWBWSetting.ReplayCombineReplayListAndControlBar, false);
            SetDefault(AWBWSetting.ReplayMovementAnimations, true);
            SetDefault(AWBWSetting.ReplayShowWeather, true);
            SetDefault(AWBWSetting.ReplayAllowLeftMouseToDragMap, true);
        }
    }

    public enum AWBWSetting
    {
        Version,
        ReplaySkipEndTurn,
        ReplayShowHiddenUnits,
        ReplayShowGridOverMap,
        ReplayShortenActionToolTips,
        ReplayShowMovementArrows,
        PlayerListScale,
        PlayerListLeftSide,
        ReplayBarControlScale,
        ReplayBarControlPositionX,
        ReplayBarControlPositionY,
        ReplayListSort,
        PlayerListKeepOrderStatic,
        ReplayCombineReplayListAndControlBar,
        ReplayMovementAnimations,
        ReplayShowWeather,
        ReplayAllowLeftMouseToDragMap
    }
}
