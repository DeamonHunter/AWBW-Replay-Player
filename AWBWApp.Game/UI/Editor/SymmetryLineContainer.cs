using AWBWApp.Game.Editor;
using AWBWApp.Game.Game.Tile;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace AWBWApp.Game.UI.Editor
{
    public class SymmetryLineContainer : Container
    {
        private SymmetryMode symmetryMode;

        public SymmetryMode SymmetryMode
        {
            get => symmetryMode;
            set
            {
                if (symmetryMode == value)
                    return;

                symmetryMode = value;
                updateSymmetry();
            }
        }

        private SymmetryDirection symmetryDirection;

        public SymmetryDirection SymmetryDirection
        {
            get => symmetryDirection;
            set
            {
                if (symmetryDirection == value)
                    return;

                symmetryDirection = value;
                updateSymmetry();
            }
        }

        private Vector2I symmetryCenter;

        public Vector2I SymmetryCenter
        {
            get => symmetryCenter;
            set
            {
                if (symmetryCenter == value)
                    return;

                symmetryCenter = value;
                updateSymmetry();
            }
        }

        private Container lineContainer;
        private SpriteIcon arrowA;
        private SpriteIcon arrowB;

        public SymmetryLineContainer()
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
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.Centre,
                    Children = new Drawable[]
                    {
                        new Box()
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            RelativeSizeAxes = Axes.Y,
                            Colour = new Colour4(20, 50, 50, 255)
                        },
                        new Circle()
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(4, 4),
                            Colour = new Colour4(20, 50, 50, 255)
                        },
                        arrowA = new SpriteIcon()
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Position = new Vector2(6, 0),
                            Size = new Vector2(6, 4),
                            Icon = FontAwesome.Solid.LongArrowAltRight,
                            Colour = new Colour4(20, 50, 50, 255)
                        },
                        arrowB = new SpriteIcon()
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Position = new Vector2(-6, 0),
                            Size = new Vector2(6, 4),
                            Icon = FontAwesome.Solid.LongArrowAltLeft,
                            Colour = new Colour4(20, 50, 50, 255)
                        },
                    }
                },
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            updateSymmetry();
        }

        private void updateSymmetry()
        {
            if (symmetryMode == SymmetryMode.None)
            {
                lineContainer.Hide();
                return;
            }

            lineContainer.Show();

            var adjustedCenter = new Vector2((symmetryCenter.X + 1) / 2.0f, (symmetryCenter.Y + 1) / 2.0f) * DrawableTile.BASE_SIZE;
            adjustedCenter.Y += DrawableTile.BASE_SIZE.Y;

            lineContainer.Position = adjustedCenter;

            switch (SymmetryMode)
            {
                case SymmetryMode.MirrorInverted:
                    arrowA.Rotation = arrowB.Rotation = 90;
                    break;

                default:
                    arrowA.Rotation = arrowB.Rotation = 0;
                    break;
            }

            switch (SymmetryDirection)
            {
                case SymmetryDirection.Vertical:
                    lineContainer.Rotation = 0;
                    break;

                case SymmetryDirection.UpwardsDiagonal:
                    lineContainer.Rotation = 45;
                    break;

                case SymmetryDirection.Horizontal:
                    lineContainer.Rotation = 90;
                    break;

                case SymmetryDirection.DownwardsDiagonal:
                    lineContainer.Rotation = -45;
                    break;
            }
        }
    }
}
