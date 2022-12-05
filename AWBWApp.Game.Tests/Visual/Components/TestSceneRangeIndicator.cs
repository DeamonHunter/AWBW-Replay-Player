using System.Collections.Generic;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Replay;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osuTK.Graphics;

namespace AWBWApp.Game.Tests.Visual.Components
{
    [TestFixture]
    public partial class TestSceneRangeIndicator : AWBWAppTestScene
    {
        private UnitRangeIndicator rangeIndicator;
        private Color4 colour = new Color4(200, 50, 50, 100);
        private Color4 outLineColor = new Color4(200, 100, 100, 255);

        public TestSceneRangeIndicator()
        {
            Add(rangeIndicator = new UnitRangeIndicator
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre
            });
        }

        [Test]
        public void TestSpecificTileLayouts()
        {
            AddStep("Change to 1 Tile", () =>
            {
                var tileList = new List<Vector2I> { new Vector2I(0, 0) };
                var center = new Vector2I(2, 2);
                rangeIndicator.ShowNewRange(tileList, center, colour, outLineColor);
            });
            AddStep("Change to 2 tiles", () =>
            {
                var tileList = new List<Vector2I> { new Vector2I(0, 0), new Vector2I(4, 4) };
                var center = new Vector2I(2, 2);
                rangeIndicator.ShowNewRange(tileList, center, colour, outLineColor);
            });
            AddStep("Change to 3 tiles in a row", () =>
            {
                var tileList = new List<Vector2I> { new Vector2I(0, 0), new Vector2I(1, 0), new Vector2I(2, 0) };
                var center = new Vector2I(2, 2);
                rangeIndicator.ShowNewRange(tileList, center, colour, outLineColor);
            });
            AddStep("Change to 3 tiles in a column", () =>
            {
                var tileList = new List<Vector2I> { new Vector2I(0, 0), new Vector2I(0, 1), new Vector2I(0, 2) };
                var center = new Vector2I(2, 2);
                rangeIndicator.ShowNewRange(tileList, center, colour, outLineColor);
            });
            AddStep("Change to 5 tiles in a L", () =>
            {
                var tileList = new List<Vector2I> { new Vector2I(0, 0), new Vector2I(0, 1), new Vector2I(0, 2), new Vector2I(1, 0), new Vector2I(2, 0) };
                var center = new Vector2I(2, 2);
                rangeIndicator.ShowNewRange(tileList, center, colour, outLineColor);
            });
        }

        [Test]
        public void TestStandardRange()
        {
            AddStep("0-0", () =>
            {
                rangeIndicator.ShowNewRange(createRangeBetweenXAndY(0, 0, Vector2I.Zero), Vector2I.Zero, colour, outLineColor);
            });
            AddStep("1-1", () =>
            {
                rangeIndicator.ShowNewRange(createRangeBetweenXAndY(1, 1, Vector2I.Zero), Vector2I.Zero, colour, outLineColor);
            });
            AddStep("2-3", () =>
            {
                rangeIndicator.ShowNewRange(createRangeBetweenXAndY(2, 3, Vector2I.Zero), Vector2I.Zero, colour, outLineColor);
            });
            AddStep("3-5", () =>
            {
                rangeIndicator.ShowNewRange(createRangeBetweenXAndY(3, 5, Vector2I.Zero), Vector2I.Zero, colour, outLineColor);
            });
        }

        private List<Vector2I> createRangeBetweenXAndY(int min, int max, Vector2I center)
        {
            var positions = new List<Vector2I>();
            for (var i = min; i <= max; i++)
                positions.AddRange(Vec2IHelper.GetAllTilesWithDistance(center, i));
            return positions;
        }
    }
}
