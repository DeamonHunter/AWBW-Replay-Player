using AWBWApp.Game.UI.Notifications;
using AWBWApp.Game.UI.Toolbar;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Screens;

namespace AWBWApp.Game.Tests.Visual.Screens
{
    [TestFixture]
    public class TestSceneMainScreen : AWBWAppTestScene
    {
        protected MainScreen MainScreen;

        protected ScreenStack ScreenStack;

        [Cached]
        private NotificationOverlay notificationOverlay { get; set; } = new NotificationOverlay();

        private AWBWMenuBar menuBar { get; set; }

        public TestSceneMainScreen()
        {
            Add(ScreenStack = new ScreenStack
            {
                RelativeSizeAxes = Axes.Both
            });
        }

        [SetUp]
        public void Setup()
        {
            ScreenStack.Push(MainScreen = new MainScreen());
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(menuBar = new MainControlMenuBar(ScreenStack.Exit, notificationOverlay));
        }
    }
}
