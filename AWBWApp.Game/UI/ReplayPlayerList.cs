using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI
{
    public class ReplayPlayerList : Container
    {
        private FillFlowContainer fillContainer;

        public ReplayPlayerList(List<ReplayPlayer> players)
        {
            Masking = true;
            EdgeEffect = new EdgeEffectParameters
            {
                Type = EdgeEffectType.Shadow,
                Colour = Color4.Black.Opacity(0.1f),
                Radius = 5
            };

            InternalChildren = new Drawable[]
            {
                new Box()
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Black,
                    Alpha = 0.3f
                },
                new BasicScrollContainer()
                {
                    RelativeSizeAxes = Axes.Both,
                    ScrollbarAnchor = Anchor.TopRight,
                    Children = new Drawable[]
                    {
                        fillContainer = new FillFlowContainer()
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical
                        }
                    }
                }
            };

            var first = true;

            foreach (var player in players)
            {
                if (first)
                {
                    first = false;
                    fillContainer.Add(new DrawableReplayPlayer());
                }
                else
                    fillContainer.Add(new DrawableReplayPlayer() { Width = 0.9f });
            }
        }

        private class DrawableReplayPlayer : Container
        {
            private Sprite coSprite;

            public DrawableReplayPlayer()
            {
                RelativeSizeAxes = Axes.X;
                Size = new Vector2(1, 60);
                Margin = new MarginPadding { Bottom = 2 };
                Anchor = Anchor.TopCentre;
                Origin = Anchor.TopCentre;
                InternalChildren = new Drawable[]
                {
                    new Container()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(0.6f, 1),
                        Margin = new MarginPadding(2),
                        EdgeEffect = new EdgeEffectParameters()
                        {
                            Colour = Color4.Black,
                            Type = EdgeEffectType.Shadow,
                            Radius = 2
                        },
                        Masking = true,
                        Children = new Drawable[]
                        {
                            new Box()
                            {
                                RelativeSizeAxes = Axes.Both,
                            },
                            new Box()
                            {
                                RelativeSizeAxes = Axes.X,
                                Colour = Color4.IndianRed,
                                Size = new Vector2(1, 20)
                            },
                            new SpriteText()
                            {
                                Text = "Username Too Long to Fit",
                                RelativeSizeAxes = Axes.Both,
                                Truncate = true
                            },
                            coSprite = new Sprite
                            {
                                FillMode = FillMode.Fit,
                                Anchor = Anchor.BottomLeft,
                                Origin = Anchor.BottomLeft,
                                Size = new Vector2(36),
                                Position = new Vector2(2, -2)
                            },
                            new Container()
                            {
                                RelativeSizeAxes = Axes.X,
                                Size = new Vector2(0.98f, 38),
                                Position = new Vector2(0, 20),
                                Padding = new MarginPadding { Left = 40 },
                                Children = new Drawable[]
                                {
                                    new ReplayPlayerInfo("UI/Clock"),
                                    new ReplayPlayerInfo("UI/Coin")
                                    {
                                        Position = new Vector2(0, 19)
                                    }
                                }
                            }
                        }
                    },
                    new Container()
                    {
                        RelativeSizeAxes = Axes.Both,
                        RelativePositionAxes = Axes.X,
                        Size = new Vector2(0.4f, 1),
                        Position = new Vector2(0.6f, 0),
                        Margin = new MarginPadding(2),
                        BorderColour = Color4.Black,
                        BorderThickness = 1,
                        EdgeEffect = new EdgeEffectParameters()
                        {
                            Colour = Color4.Black,
                            Type = EdgeEffectType.Shadow,
                            Radius = 2
                        },
                        Masking = true,
                        Children = new Drawable[]
                        {
                            new Box()
                            {
                                RelativeSizeAxes = Axes.Both,
                            },
                            new ReplayPlayerInfo("UI/Clock"),
                            new ReplayPlayerInfo("UI/Clock")
                            {
                                Position = new Vector2(0, 19)
                            },
                            new ReplayPlayerInfo("UI/BuildingsCaptured")
                            {
                                Position = new Vector2(0, 38)
                            }
                        }
                    }
                };
            }

            [BackgroundDependencyLoader]
            private void Load(TextureStore storage)
            {
                coSprite.Texture = storage.Get("CO/Olaf-DS");
            }

            private class ReplayPlayerInfo : Container
            {
                private Sprite infoIcon;
                private string infoIconTexture;

                public ReplayPlayerInfo(string icon)
                {
                    infoIconTexture = icon;
                    RelativeSizeAxes = Axes.X;
                    Height = 18;
                    Margin = new MarginPadding(1);
                    CornerRadius = 3;
                    Masking = true;
                    InternalChildren = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = new Color4(236, 236, 236, 255)
                        },
                        infoIcon = new Sprite()
                        {
                            Size = new Vector2(18),
                            Position = new Vector2(1),
                            FillMode = FillMode.Fit
                        }
                    };
                }

                [BackgroundDependencyLoader]
                private void Load(TextureStore storage)
                {
                    infoIcon.Texture = storage.Get(infoIconTexture);
                }
            }
        }
    }

    public class ReplayPlayer
    {
    }
}
