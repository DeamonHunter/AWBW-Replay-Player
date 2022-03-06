using osu.Framework.Bindables;

namespace AWBWApp.Game.UI.Replay.Toolbar
{
    public class ReplaySettings
    {
        public Bindable<bool> ShowHiddenUnits = new Bindable<bool>(true);
        public Bindable<bool> ShowGridOverMap = new Bindable<bool>();
    }
}
