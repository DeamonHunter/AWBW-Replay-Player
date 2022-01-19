using System;
using AWBWApp.Game.API.Replay;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Select
{
    public class DrawableCarouselReplay : DrawableCarouselItem
    {
        public const float CAROUSEL_BEATMAP_SPACING = 5;

        public const float CAROUSEL_SELECTED_SCALE = 1.05f;

        public const float SELECTEDHEIGHT = (height * CAROUSEL_SELECTED_SCALE + CAROUSEL_BEATMAP_SPACING);

        private const float height = MAX_HEIGHT * 0.9f;

        private ReplayInfo replayInfo;

        private Action<ReplayInfo> startRequest;

        private Sprite background;

        public DrawableCarouselReplay() { }

        public DrawableCarouselReplay(CarouselReplay panel)
        {
            replayInfo = panel.ReplayInfo;
            Item = panel;
        }

        [BackgroundDependencyLoader(true)]
        private void load(ReplaySelectScreen replaySelect)
        {
            Header.Height = height;

            if (replaySelect != null)
            {
                startRequest = replaySelect.FinaliseSelection;
            }

            Anchor = Anchor.TopCentre;
            Origin = Anchor.TopCentre;

            MovementContainer.Anchor = Anchor.TopCentre;
            MovementContainer.Origin = Anchor.TopCentre;

            Header.Children = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both
                },
                new FillFlowContainer()
                {
                    Padding = new MarginPadding { Left = 5 },
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(4, 0),
                    AutoSizeAxes = Axes.Both,
                    Children = new[]
                    {
                        new SpriteText
                        {
                            Text = "Person A",
                            Font = FontUsage.Default.With(size: 20, italics: true),
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft
                        },
                        new SpriteText
                        {
                            Text = "vs",
                            Font = FontUsage.Default.With(size: 16),
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft
                        },
                        new SpriteText
                        {
                            Text = "Person B",
                            Font = FontUsage.Default.With(size: 20, italics: true),
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft
                        },
                        new SpriteText
                        {
                            Text = "on",
                            Font = FontUsage.Default.With(size: 16),
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft
                        },
                        new SpriteText
                        {
                            Text = "10/01/2022",
                            Font = FontUsage.Default.With(size: 16, italics: true),
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft
                        },
                    }
                },
                new FillFlowContainer()
                {
                    Padding = new MarginPadding { Left = 5 },
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(4, 0),
                    AutoSizeAxes = Axes.Both,
                    Children = new[]
                    {
                        new SpriteText
                        {
                            Text = "Person A",
                            Font = FontUsage.Default.With(size: 20, italics: true),
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft
                        },
                        new SpriteText
                        {
                            Text = "vs",
                            Font = FontUsage.Default.With(size: 16),
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft
                        },
                        new SpriteText
                        {
                            Text = "Person B",
                            Font = FontUsage.Default.With(size: 20, italics: true),
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft
                        },
                        new SpriteText
                        {
                            Text = "on",
                            Font = FontUsage.Default.With(size: 16),
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft
                        },
                        new SpriteText
                        {
                            Text = "10/01/2022",
                            Font = FontUsage.Default.With(size: 16, italics: true),
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft
                        },
                    }
                }
            };
        }

        protected override void Selected()
        {
            base.Selected();
            MovementContainer.ScaleTo(1.05f, 500, Easing.OutExpo);
            background.Colour = ColourInfo.GradientVertical(new Color4(20, 43, 51, 255), new Color4(40, 86, 102, 255));
        }

        protected override void Deselected()
        {
            base.Deselected();
            MovementContainer.ScaleTo(1f, 500, Easing.OutExpo);
            background.Colour = new Color4(20, 43, 51, 255);
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (Item.State.Value == CarouselItemState.Selected)
                startRequest?.Invoke(replayInfo);

            return base.OnClick(e);
        }

        protected override void UpdateItem()
        {
            base.UpdateItem();

            if (Item == null)
                return;

            replayInfo = ((CarouselReplay)Item).ReplayInfo;

            DelayedLoadWrapper background;
            DelayedLoadWrapper mainFlow;

            Header.Children = new Drawable[]
            {
                background = new DelayedLoadWrapper(() => new BufferedContainer
                {
                    RedrawOnScale = false,
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new Box()
                        {
                            Colour = replayInfo.LeagueMatch != null ? ColourInfo.SingleColour(Color4Extensions.FromHex("ed8f15")) : ColourInfo.SingleColour(Color4Extensions.FromHex("15a1ed")),
                            RelativeSizeAxes = Axes.Both,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            FillMode = FillMode.Fill
                        },
                        new FillFlowContainer()
                        {
                            Depth = -1,
                            RelativeSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Shear = new Vector2(0.8f, 0),
                            Alpha = 0.5f,
                            Children = new[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = Color4.Black,
                                    Width = 0.4f
                                },

                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = ColourInfo.GradientHorizontal(Color4.Black, new Color4(0f, 0f, 0f, 0.9f)),
                                    Width = 0.4f
                                },
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = ColourInfo.GradientHorizontal(new Color4(0f, 0f, 0f, 0.9f), new Color4(0f, 0f, 0f, 0.1f)),
                                    Width = 0.4f
                                },
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = ColourInfo.GradientHorizontal(new Color4(0f, 0f, 0f, 0.1f), new Color4(0, 0, 0, 0)),
                                    Width = 0.4f
                                }
                            }
                        }
                    }
                }, 100),
                mainFlow = new DelayedLoadWrapper(() => new ReplayCarouselPanelContent(((CarouselReplay)Item)), 100)
                {
                    RelativeSizeAxes = Axes.Both
                }
            };

            background.DelayedLoadComplete += fadeContentIn;
            mainFlow.DelayedLoadComplete += fadeContentIn;
        }

        private void fadeContentIn(Drawable d) => d.FadeInFromZero(750, Easing.OutQuint);
    }
}
