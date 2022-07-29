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
                new MenuItem("Visual Settings")
                {
                    Items = new[]
                    {
                        new ToggleMenuItem("Show Grid", configManager.GetBindable<bool>(AWBWSetting.ReplayShowGridOverMap)),
                        new ToggleMenuItem("Show Tile Cursor", configManager.GetBindable<bool>(AWBWSetting.ShowTileCursor)),
                        new ToggleMenuItem("Show Hidden Units", configManager.GetBindable<bool>(AWBWSetting.ReplayShowHiddenUnits)),
                        new ToggleMenuItem("Show Weather", configManager.GetBindable<bool>(AWBWSetting.ReplayShowWeather)),
                        new ToggleMenuItem("Movement Animations", configManager.GetBindable<bool>(AWBWSetting.ReplayMovementAnimations)),
                        new ToggleMenuItem("Show Movement Arrows", configManager.GetBindable<bool>(AWBWSetting.ReplayShowMovementArrows)),
                    }
                },
                new MenuItem("Control Settings")
                {
                    Items = new[]
                    {
                        new MenuItem("Rebind Keys", () => interrupts.Push(new KeyRebindingInterrupt())),
                        new ToggleMenuItem("Allow Left Mouse to Drag Map", configManager.GetBindable<bool>(AWBWSetting.ReplayAllowLeftMouseToDragMap)),
                        new ToggleMenuItem("Skip End Turn", configManager.GetBindable<bool>(AWBWSetting.ReplaySkipEndTurn)),
                        new ToggleMenuItem("Shorten Action Tooltips", configManager.GetBindable<bool>(AWBWSetting.ReplayShortenActionToolTips)),
                        new ToggleMenuItem("Move Controls Bar to Player List", configManager.GetBindable<bool>(AWBWSetting.ReplayCombineReplayListAndControlBar)),
                    }
                },
            };
        }
    }
}
