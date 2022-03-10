using AWBWApp.Game.UI.Notifications;
using AWBWApp.Game.UI.Replay;
using AWBWApp.Game.UI.Replay.Toolbar;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Screens;

namespace AWBWApp.Game.Tests.Visual.Screens
{
    [TestFixture]
    public class TestSceneMainScreen : AWBWAppTestScene
    {
        protected MainScreen MainScreen;

        protected ScreenStack ScreenStack;

        [Cached]
        private NotificationOverlay notificationOverlay { get; set; }

        private AWBWMenuBar menuBar { get; set; }

        public TestSceneMainScreen()
        {
            Add(ScreenStack = new ScreenStack
            {
                RelativeSizeAxes = Axes.Both
            });
        }

        [BackgroundDependencyLoader]
        private void load(AWBWConfigManager localConfig)
        {
            Add(menuBar = new AWBWMenuBar(new MenuItem[]
            {
                new MenuItem("Settings")
                {
                    Items = new[]
                    {
                        new ToggleMenuItem("Show Grid", localConfig.GetBindable<bool>(AWBWSetting.ReplayShowGridOverMap)),
                        new ToggleMenuItem("Show Hidden Units", localConfig.GetBindable<bool>(AWBWSetting.ReplayShowHiddenUnits)),
                        new ToggleMenuItem("Skip End Turn", localConfig.GetBindable<bool>(AWBWSetting.ReplaySkipEndTurn))
                    }
                }
            }, notificationOverlay = new NotificationOverlay()));

            ScreenStack.Push(MainScreen = new MainScreen
            {
                RelativeSizeAxes = Axes.Both
            });
        }
    }
}
