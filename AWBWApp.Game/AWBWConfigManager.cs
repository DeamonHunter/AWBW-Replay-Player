using System.Collections.Generic;
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
        }
    }

    public enum AWBWSetting
    {
        Version,
        ReplaySkipEndTurn,
        ReplayShowHiddenUnits,
        ReplayShowGridOverMap,
        ReplayShortenActionToolTips
    }
}
