using System.Collections.Generic;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Components;
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

namespace AWBWApp.Game.Tests.Visual.Components
{
    [TestFixture]
    public partial class TestSceneShrinkingDrawable : AWBWAppTestScene
    {
        // Add visual tests to ensure correct behaviour of your game: https://github.com/ppy/osu-framework/wiki/Development-and-Testing
        // You can make changes to classes associated with the tests and they will recompile and update immediately.

        private readonly List<ShrinkingCompositeDrawable> shrinkerList = new List<ShrinkingCompositeDrawable>();
        private readonly List<Sprite> spriteList = new List<Sprite>();

        private Vector2 originalSpriteSize;

        public TestSceneShrinkingDrawable()
        {
            var content = new[,]
            {
                {
                    addBox(Anchor.TopLeft),
                    addBox(Anchor.TopCentre),
                    addBox(Anchor.TopRight)
                },
                {
                    addBox(Anchor.CentreLeft),
                    addBox(Anchor.Centre),
                    addBox(Anchor.CentreRight)
                },
                {
                    addBox(Anchor.BottomLeft),
                    addBox(Anchor.BottomCentre),
                    addBox(Anchor.BottomRight)
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
                foreach (var sprite in spriteList)
                    sprite.ResizeTo(originalSpriteSize);
                foreach (var shrinker in shrinkerList)
                    shrinker.ScaleTo(1);
            });
        }

        [Test]
        public void TestGrowAllSprites()
        {
            AddStep("Grow all sprites", () =>
            {
                foreach (var sprite in spriteList)
                    sprite.ResizeTo(originalSpriteSize * 10, 500);
            });
            AddUntilStep("Wait For growth: ", () => spriteList[0].LatestTransformEndTime == spriteList[0].Time.Current);
        }

        [Test]
        public void TestGrowAllSpritesXOnly()
        {
            AddStep("Grow all sprites", () =>
            {
                foreach (var sprite in spriteList)
                {
                    sprite.ResizeTo(new Vector2(originalSpriteSize.X * 10, originalSpriteSize.Y), 500);
                }
            });
            AddUntilStep("Wait For growth: ", () => spriteList[0].LatestTransformEndTime == spriteList[0].Time.Current);
        }

        [Test]
        public void TestGrowAllSpritesYOnly()
        {
            AddStep("Grow all sprites", () =>
            {
                foreach (var sprite in spriteList)
                {
                    sprite.ResizeTo(new Vector2(originalSpriteSize.X, originalSpriteSize.Y * 10), 500);
                }
            });
            AddUntilStep("Wait For growth: ", () => spriteList[0].LatestTransformEndTime == spriteList[0].Time.Current);
        }

        [Test]
        public void TestGrowAllSpritesWhileScalingBoxes()
        {
            AddStep("Grow all sprites", () =>
            {
                foreach (var shrinker in shrinkerList)
                    shrinker.ScaleTo(2, 500);
                foreach (var sprite in spriteList)
                    sprite.ResizeTo(originalSpriteSize * 10, 500);
            });
            AddUntilStep("Wait For growth: ", () => spriteList[0].LatestTransformEndTime == spriteList[0].Time.Current);
        }

        [BackgroundDependencyLoader]
        private void load(NearestNeighbourTextureStore store)
        {
            var texture = store.Get("UI/Team-A");

            originalSpriteSize = texture.Size;

            foreach (var sprite in spriteList)
            {
                sprite.Texture = texture;
                sprite.Size = texture.Size;
            }
        }

        private Drawable addBox(Anchor anchor)
        {
            var box = new Sprite
            {
                Anchor = anchor,
                Origin = anchor
            };

            spriteList.Add(box);

            ShrinkingCompositeDrawable shrinkingDrawable;
            var container = new Container
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

            shrinkerList.Add(shrinkingDrawable);

            return container;
        }
    }
}
