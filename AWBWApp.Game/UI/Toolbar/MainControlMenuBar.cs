using System;
using AWBWApp.Game.UI.Components.Menu;
using AWBWApp.Game.UI.Notifications;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
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
        private void load(AWBWConfigManager configManager)
        {
            var scaleItems = createPlayerListScaleItems(configManager);

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
                new MenuItem("UI Settings")
                {
                    Items = new[]
                    {
                        new MenuItem("Player List Scale")
                        {
                            Items = scaleItems
                        },
                        new ToggleMenuItem("Right Side Player List", configManager.GetBindable<bool>(AWBWSetting.PlayerListRightSide)),
                    }
                }
            };
        }

        private MenuItem[] createPlayerListScaleItems(AWBWConfigManager configManager)
        {
            var playerListScale = configManager.GetBindable<float>(AWBWSetting.PlayerListScale);
            var genericBindable = new Bindable<object>(1f);

            playerListScale.BindValueChanged(x =>
            {
                genericBindable.Value = x.NewValue;
            }, true);

            genericBindable.BindValueChanged(x =>
            {
                playerListScale.Value = (float)x.NewValue;
            });

            return new MenuItem[]
            {
                new StatefulMenuItem("1.0x", genericBindable, 1f),
                new StatefulMenuItem("1.05x", genericBindable, 1.05f),
                new StatefulMenuItem("1.1x", genericBindable, 1.1f),
                new StatefulMenuItem("1.15x", genericBindable, 1.15f),
                new StatefulMenuItem("1.2x", genericBindable, 1.2f),
                new StatefulMenuItem("1.25x", genericBindable, 1.25f),
            };
        }
    }
}
