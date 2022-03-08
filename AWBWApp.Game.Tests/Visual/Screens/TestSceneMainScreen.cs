using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Screens;

namespace AWBWApp.Game.Tests.Visual.Screens
{
    [TestFixture]
    public class TestSceneMainScreen : AWBWAppTestScene
    {
        protected MainScreen MainScreen;

        protected ScreenStack ScreenStack;

        public TestSceneMainScreen()
        {
            Add(ScreenStack = new ScreenStack
            {
                RelativeSizeAxes = Axes.Both
            });

            ScreenStack.Push(MainScreen = new MainScreen
            {
                RelativeSizeAxes = Axes.Both
            });
        }
    }
}
