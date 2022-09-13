using System;
using System.Collections.Generic;
using System.Linq;
using AWBWApp.Game.UI.Components.Menu;
using AWBWApp.Game.UI.Interrupts;
using AWBWApp.Game.UI.Notifications;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Platform;

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
        private void load(AWBWConfigManager configManager, InterruptDialogueOverlay interrupts, FrameworkConfigManager frameworkConfig, GameHost host)
        {
            Menu.Items = new MenuItem[]
            {
                new MenuItem("Exit Screen", exitScreenAction),
                new MenuItem("Visual Settings")
                {
                    Items = new MenuItem[]
                    {
                        new EnumMenuItem<WindowMode>("Fullscreen Mode", frameworkConfig.GetBindable<WindowMode>(FrameworkSetting.WindowMode), host.Window?.SupportedWindowModes.ToList() ?? new List<WindowMode>() { WindowMode.Windowed }),
                        new MenuItem("Map Background Colour")
                        {
                            Items = new[] { new ColourPickerMenuItem(configManager.GetBindable<Colour4>(AWBWSetting.MapGridBaseColour)) }
                        },
                        new MenuItem("Map Background Grid Colour")
                        {
                            Items = new[] { new ColourPickerMenuItem(configManager.GetBindable<Colour4>(AWBWSetting.MapGridGridColour)) }
                        },
                        new ToggleMenuItem("Show Grid Overlay", configManager.GetBindable<bool>(AWBWSetting.ReplayShowGridOverMap)),
                        new ToggleMenuItem("Show Tile Cursor", configManager.GetBindable<bool>(AWBWSetting.ShowTileCursor)),
                        new ToggleMenuItem("Show Hidden Building/Units in Fog", configManager.GetBindable<bool>(AWBWSetting.ReplayOnlyShownKnownInfo)),
                        new ToggleMenuItem("Show Funds/Unit Count in Fog", configManager.GetBindable<bool>(AWBWSetting.ReplayShowPlayerDetailsInFog)),
                        new ToggleMenuItem("Show Weather Particles", configManager.GetBindable<bool>(AWBWSetting.ReplayShowWeather)),
                        new ToggleMenuItem("Movement Animations", configManager.GetBindable<bool>(AWBWSetting.ReplayMovementAnimations)),
                        new ToggleMenuItem("Show Movement Arrows", configManager.GetBindable<bool>(AWBWSetting.ReplayShowMovementArrows)),
                        new ToggleMenuItem("Show Animations for Hidden Actions", configManager.GetBindable<bool>(AWBWSetting.ShowAnimationsForHiddenActions)),
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
