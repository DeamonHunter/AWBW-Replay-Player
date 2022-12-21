using AWBWApp.Game.Editor;
using AWBWApp.Game.Game.Tile;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace AWBWApp.Game.UI.Editor
{
    public partial class CaptureOverlayContainer : Container
    {
        [Resolved]
        private Bindable<bool> showCaptureOverlay { get; set; }

        private Container lineContainer;

        private IconUsage[] dice = {
            FontAwesome.Solid.Dice,
            FontAwesome.Solid.DiceOne,
            FontAwesome.Solid.DiceTwo,
            FontAwesome.Solid.DiceThree,
            FontAwesome.Solid.DiceFour,
            FontAwesome.Solid.DiceFive,
            FontAwesome.Solid.DiceSix,
            };

        public CaptureOverlayContainer()
        {
            RelativeSizeAxes = Axes.Both;

            Masking = true;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            InternalChildren = new Drawable[]
            {
                lineContainer = new Container()
                {
                    RelativeSizeAxes = Axes.Y,
                    Size = new Vector2(20, 2),
                    Position = new Vector2(0, DrawableTile.BASE_SIZE.Y),
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.Centre,
                    Children = new Drawable[]
                    {
                        new Circle()
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(4, 4),
                            Colour = new Colour4(20, 50, 50, 255)
                        },
                    }
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            showCaptureOverlay.BindValueChanged(_ => calcAndShowCaptures(), true);

            // calcAndShowCaptures();
            lineContainer.Show();
        }

        public void calcAndShowCaptures()
        {
            if (!showCaptureOverlay.Value)
            {
                lineContainer.Hide();
                return;
            }
            lineContainer.Clear();

            // var adjustedCenter = new Vector2((symmetryCenter.X + 1) / 2.0f, (symmetryCenter.Y + 1) / 2.0f) * DrawableTile.BASE_SIZE;
            // adjustedCenter.Y += DrawableTile.BASE_SIZE.Y;
            // lineContainer.Position = adjustedCenter;
            // arrowA.Rotation = (arrowA.Rotation + 90) % 180;
            for (int i = 0; i < 42; i++)
            {
                int coord = (int)((i + 0.5) * DrawableTile.BASE_SIZE.X);
                lineContainer.Add(new SpriteIcon()
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Position = new Vector2(coord, coord),
                                Size = new Vector2(6, 4),
                                Icon = dice[i%dice.Length],
                                Colour = new Colour4(20, 50, 50, 255)
                            });
            }
            lineContainer.Show();
        }
    }
}
