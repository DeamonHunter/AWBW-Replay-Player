using AWBWApp.Game.UI.Battle;
using NUnit.Framework;
using osu.Framework.Graphics;
using osuTK;

namespace AWBWApp.Game.Tests.Visual.Components
{
    public class TestSceneBattleWindow : AWBWAppTestScene
    {
        private BattleWindow window;

        public TestSceneBattleWindow()
        {
            Child = window = new BattleWindow()
            {
                Scale = new Vector2(2, 2),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre
            };
        }

        [Test]
        public void TestFire()
        {
            AddStep("Start Animation", () => window.AnimateMove(false));
        }
    }
}
