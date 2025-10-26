using System.Collections.Generic;
using AWBWApp.Game.Game.COs;
using AWBWApp.Game.IO;
using AWBWApp.Game.UI.Select;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Platform;

namespace AWBWApp.Game
{
    public class AWBWConfigManager : IniConfigManager<AWBWSetting>
    {
        public AWBWConfigManager(Storage storage, IDictionary<AWBWSetting, object> defaultOverrides = null)
            : base(storage, defaultOverrides)
        {
        }

        private void setColourBindable(AWBWSetting setting)
        {
            if (ConfigStore.TryGetValue(setting, out var bindable))
            {
                if (!(bindable is BindableColour))
                {
                    var other = (IBindable<Colour4>)bindable;
                    var newBindable = new BindableColour();
                    newBindable.Value = other.Value;

                    ConfigStore[setting] = newBindable;
                }
            }
            else
                ConfigStore.Add(setting, new BindableColour());
        }

        protected override void InitialiseDefaults()
        {
            setColourBindable(AWBWSetting.MapGridBaseColour);
            setColourBindable(AWBWSetting.MapGridGridColour);

            SetDefault(AWBWSetting.Version, string.Empty);

            SetDefault(AWBWSetting.ReplaySkipEndTurn, false);
            SetDefault(AWBWSetting.ReplayOnlyShownKnownInfo, true);
            SetDefault(AWBWSetting.ReplayShowPlayerDetailsInFog, true);
            SetDefault(AWBWSetting.ReplayShowGridOverMap, false);
            SetDefault(AWBWSetting.ReplayShortenActionToolTips, false);
            SetDefault(AWBWSetting.ReplayShowMovementArrows, true);
            SetDefault(AWBWSetting.PlayerListScale, 1f);
            SetDefault(AWBWSetting.PlayerListLeftSide, false);
            SetDefault(AWBWSetting.TileInfoPopupAnchor, Anchor.BottomLeft);
            SetDefault(AWBWSetting.ReplayBarControlScale, 1f);
            SetDefault(AWBWSetting.ReplayBarControlPositionX, 0f);
            SetDefault(AWBWSetting.ReplayBarControlPositionY, 0f);
            SetDefault(AWBWSetting.ReplayListSort, CarouselSort.EndDate);
            SetDefault(AWBWSetting.PlayerListKeepOrderStatic, false);
            SetDefault(AWBWSetting.ReplayCombineReplayListAndControlBar, false);
            SetDefault(AWBWSetting.ReplayMovementAnimations, true);
            SetDefault(AWBWSetting.ReplayShowWeather, true);
            SetDefault(AWBWSetting.ReplayAllowLeftMouseToDragMap, true);
            SetDefault(AWBWSetting.MapSkin, MapSkin.Classic);
            SetDefault(AWBWSetting.BuildingSkin, BuildingSkin.AW2);
            SetDefault(AWBWSetting.ShowTileCursor, true);
            SetDefault(AWBWSetting.ShowAnimationsForHiddenActions, true);
            SetDefault(AWBWSetting.SonjaHPVisiblity, SonjaHPVisibility.AlwaysVisible);
            SetDefault(AWBWSetting.ShowClock, true);
            SetDefault(AWBWSetting.LockMapPosition, false);

            SetDefault(AWBWSetting.MapGridBaseColour, new Colour4(42, 91, 139, 255).Lighten(0.2f));
            SetDefault(AWBWSetting.MapGridGridColour, new Colour4(42, 91, 139, 255).Darken(0.8f));
        }
    }

    public enum AWBWSetting
    {
        Version,
        ReplaySkipEndTurn,
        ReplayOnlyShownKnownInfo,
        ReplayShowPlayerDetailsInFog,
        ReplayShowGridOverMap,
        ReplayShortenActionToolTips,
        ReplayShowMovementArrows,
        PlayerListScale,
        PlayerListLeftSide,
        TileInfoPopupAnchor,
        ReplayBarControlScale,
        ReplayBarControlPositionX,
        ReplayBarControlPositionY,
        ReplayListSort,
        PlayerListKeepOrderStatic,
        ReplayCombineReplayListAndControlBar,
        ReplayMovementAnimations,
        ReplayShowWeather,
        ReplayAllowLeftMouseToDragMap,
        MapSkin,
        BuildingSkin,
        ShowTileCursor,
        ShowAnimationsForHiddenActions,
        MapGridBaseColour,
        MapGridGridColour,
        SonjaHPVisiblity,
        ShowClock,
        LockMapPosition
    }

    public enum MapSkin
    {
        Classic,
        Desert,
        DoR
    }

    public static class MapSkinHelper
    {
        public static string ToFolder(this MapSkin skin, BuildingSkin buildingSkin)
        {
            if (skin == MapSkin.Classic && buildingSkin == BuildingSkin.AW1)
                return "ClassicAW1";

            return skin.ToString();
        }
    }

    public enum BuildingSkin
    {
        AW1,
        AW2
    }
}
