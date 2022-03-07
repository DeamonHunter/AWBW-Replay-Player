using System;
using AWBWApp.Game.IO;
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
        private ReplayManager replayManager { get; set; }

        [Test]
        public void TestItem()
        {
            if (!replayManager.TryGetReplayInfo(478996, out var info))
                throw new Exception("Failed to get the test replay.");

            var replayItem = new CarouselReplay(info);

            Clear();
            Add(new DrawableCarouselReplay(replayItem)
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(0.5f, DrawableCarouselItem.MAX_HEIGHT)
            });
        }
    }
}
