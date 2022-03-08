using AWBWApp.Game.UI.Select;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Screens;

namespace AWBWApp.Game.Tests.Visual.Screens
{
    [TestFixture]
    public class TestSceneReplaySelect : AWBWAppTestScene
    {
        protected ReplaySelectScreen Screen;

        protected ScreenStack ScreenStack;

        public TestSceneReplaySelect()
        {
            Add(ScreenStack = new ScreenStack
            {
                RelativeSizeAxes = Axes.Both
            });

            ScreenStack.Push(Screen = new ReplaySelectScreen
            {
                RelativeSizeAxes = Axes.Both
            });
        }
    }
}
