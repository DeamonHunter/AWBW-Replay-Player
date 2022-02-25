using System.Collections.Generic;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.Tests.Visual
{
    [TestFixture]
    public class TestSceneShrinkingDrawable : AWBWAppTestScene
    {
        // Add visual tests to ensure correct behaviour of your game: https://github.com/ppy/osu-framework/wiki/Development-and-Testing
        // You can make changes to classes associated with the tests and they will recompile and update immediately.

        private List<ShrinkingCompositeDrawable> _shrinkerList = new List<ShrinkingCompositeDrawable>();
        private List<Sprite> _spriteList = new List<Sprite>();

        private Vector2 _originalSpriteSize;

        public TestSceneShrinkingDrawable()
        {
            var content = new Drawable[,]
            {
                {
                    AddBox(Anchor.TopLeft),
                    AddBox(Anchor.TopCentre),
                    AddBox(Anchor.TopRight)
                },
                {
                    AddBox(Anchor.CentreLeft),
                    AddBox(Anchor.Centre),
                    AddBox(Anchor.CentreRight)
                },
                {
                    AddBox(Anchor.BottomLeft),
                    AddBox(Anchor.BottomCentre),
                    AddBox(Anchor.BottomRight)
                },
            };

            Add(new TableContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(300),
                Content = content
            });
        }

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("Reset sprite Scale", () =>
            {
                foreach (var sprite in _spriteList)
                    sprite.ResizeTo(_originalSpriteSize);
                foreach (var shrinker in _shrinkerList)
                    shrinker.ScaleTo(1);
            });
        }

        [Test]
        public void TestGrowAllSprites()
        {
            AddStep("Grow all sprites", () =>
            {
                foreach (var sprite in _spriteList)
                    sprite.ResizeTo(_originalSpriteSize * 10, 500);
            });
            AddUntilStep("Wait For growth: ", () => _spriteList[0].LatestTransformEndTime == _spriteList[0].Time.Current);
        }

        [Test]
        public void TestGrowAllSpritesXOnly()
        {
            AddStep("Grow all sprites", () =>
            {
                foreach (var sprite in _spriteList)
                {
                    sprite.ResizeTo(new Vector2(_originalSpriteSize.X * 10, _originalSpriteSize.Y), 500);
                }
            });
            AddUntilStep("Wait For growth: ", () => _spriteList[0].LatestTransformEndTime == _spriteList[0].Time.Current);
        }

        [Test]
        public void TestGrowAllSpritesYOnly()
        {
            AddStep("Grow all sprites", () =>
            {
                foreach (var sprite in _spriteList)
                {
                    sprite.ResizeTo(new Vector2(_originalSpriteSize.X, _originalSpriteSize.Y * 10), 500);
                }
            });
            AddUntilStep("Wait For growth: ", () => _spriteList[0].LatestTransformEndTime == _spriteList[0].Time.Current);
        }

        [Test]
        public void TestGrowAllSpritesWhileScalingBoxes()
        {
            AddStep("Grow all sprites", () =>
            {
                foreach (var shrinker in _shrinkerList)
                    shrinker.ScaleTo(2, 500);
                foreach (var sprite in _spriteList)
                    sprite.ResizeTo(_originalSpriteSize * 10, 500);
            });
            AddUntilStep("Wait For growth: ", () => _spriteList[0].LatestTransformEndTime == _spriteList[0].Time.Current);
        }

        [BackgroundDependencyLoader]
        private void load(NearestNeighbourTextureStore store)
        {
            var texture = store.Get("UI/Team-A");

            _originalSpriteSize = texture.Size;

            foreach (var sprite in _spriteList)
            {
                sprite.Texture = texture;
                sprite.Size = texture.Size;
            }
        }

        private Drawable AddBox(Anchor anchor)
        {
            var box = new Sprite
            {
                Anchor = anchor,
                Origin = anchor
            };

            _spriteList.Add(box);

            ShrinkingCompositeDrawable shrinkingDrawable;
            var container = new Container()
            {
                Anchor = anchor,
                Origin = anchor,
                AutoSizeAxes = Axes.Both,
                BorderColour = Color4.White,
                BorderThickness = 2,
                Masking = true,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = ColourInfo.SingleColour(Color4.White.Opacity(0.5f))
                    },
                    shrinkingDrawable = new ShrinkingCompositeDrawable(box)
                    {
                        Anchor = anchor,
                        Origin = anchor,
                        Size = new Vector2(50)
                    }
                }
            };

            _shrinkerList.Add(shrinkingDrawable);

            return container;
        }
    }
}
