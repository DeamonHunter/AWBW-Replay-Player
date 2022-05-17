using System;
using AWBWApp.Game.UI.Components.Menu;
using AWBWApp.Game.UI.Interrupts;
using AWBWApp.Game.UI.Notifications;
using osu.Framework.Allocation;
using osu.Framework.Graphics.UserInterface;

namespace AWBWApp.Game.UI.Toolbar
{
    public class MainControlMenuBar : AWBWMenuBar
    {
        private readonly Action exitScreenAction;

        public MainControlMenuBar(Action exitScreenAction, NotificationOverlay overlay)
            : base(null, overlay)
        {
            this.exitScreenAction = exitScreenAction;
        }

        [BackgroundDependencyLoader]
        private void load(AWBWConfigManager configManager, InterruptDialogueOverlay interrupts)
        {
            Menu.Items = new MenuItem[]
            {
                new MenuItem("Exit Screen", exitScreenAction),
                new MenuItem("Unit Settings")
                {
                    Items = new[]
                    {
                        new ToggleMenuItem("Show Hidden Units", configManager.GetBindable<bool>(AWBWSetting.ReplayShowHiddenUnits)),
                        new ToggleMenuItem("Show Movement Arrows", configManager.GetBindable<bool>(AWBWSetting.ReplayShowMovementArrows))
                    }
                },
                new MenuItem("Menu Settings")
                {
                    Items = new[]
                    {
                        new ToggleMenuItem("Show Grid", configManager.GetBindable<bool>(AWBWSetting.ReplayShowGridOverMap)),
                        new ToggleMenuItem("Skip End Turn", configManager.GetBindable<bool>(AWBWSetting.ReplaySkipEndTurn)),
                        new ToggleMenuItem("Shorten Action Tooltips", configManager.GetBindable<bool>(AWBWSetting.ReplayShortenActionToolTips))
                    }
                },
                new MenuItem("Rebind Keys", () => interrupts.Push(new KeyRebindingInterrupt()))
            };
        }
    }
}
