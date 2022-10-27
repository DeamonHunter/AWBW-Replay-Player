using AWBWApp.Game.UI.Select;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osuTK;

namespace AWBWApp.Game.Tests.Visual.Components
{
    [TestFixture]
    public class TestSceneReplayCarouselItem : AWBWAppTestScene
    {
        [Resolved]
        private TestReplayDecoder replayStorageManager { get; set; }

        [Test]
        public void TestItem()
        {
            AddStep("Create Normal Replay", () =>
            {
                var info = replayStorageManager.GetReplayInStorage("Json/Replays/478996");

                var replayItem = new CarouselReplay(info.ReplayInfo, "Test Map");

                Schedule(() =>
                {
                    Clear();
                    Add(new DrawableCarouselReplay(replayItem)
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(0.5f, DrawableCarouselItem.MAX_HEIGHT)
                    });
                });
            });
        }
    }
}
