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
        }
    }

    public enum AWBWSetting
    {
        Version
    }
}
