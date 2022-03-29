using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Replay
{
    public class EndGamePopupDrawable : CompositeDrawable
    {
        private FillFlowContainer winnersContainer;
        private FillFlowContainer losersContainer;

        public EndGamePopupDrawable(Dictionary<long, PlayerInfo> players, List<long> winners, List<long> losers, string gameOverMessage)
        {
            Width = 400;
            AutoSizeAxes = Axes.Y;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            var borderColour = new Color4(20, 20, 20, 255);

            InternalChild = new Container()
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Masking = true,
                BorderColour = borderColour,
                BorderThickness = 4,
                Children = new Drawable[]
                {
                    new Box()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(240, 240, 240, 255)
                    },
                    new CloseButton()
                    {
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                        Padding = new MarginPadding { Top = 5, Right = 5 },
                        Action = () =>
                        {
                            FinishTransforms(true);
                            Expire();
                        }
                    },
                    new FillFlowContainer()
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Direction = FillDirection.Vertical,
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Spacing = new Vector2(0, 3),
                        Padding = new MarginPadding { Bottom = 5 },
                        Children = createFillFlowChildren(gameOverMessage, players, winners, losers, borderColour)
                    }
                }
            };

            this.FadeOut();
        }

        private Drawable[] createFillFlowChildren(string gameOverMessage, Dictionary<long, PlayerInfo> players, List<long> winners, List<long> losers, Color4 borderColour)
        {
            if (winners == null || winners.Count == 0)
            {
                winners = losers;
                losers = null;
            }

            var drawables = new Drawable[losers == null || losers.Count <= 0 ? 3 : 5];
            drawables[0] = createTitleTextFlow(gameOverMessage);

            drawables[1] = new Container()
            {
                RelativeSizeAxes = Axes.X,
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                Size = new Vector2(0.9f, 30f),
                Position = new Vector2(0, 20),
                Masking = true,
                CornerRadius = 3,
                Children = new Drawable[]
                {
                    new Box()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = borderColour.Opacity(0.35f),
                    },
                    new SpriteText()
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Padding = new MarginPadding { Left = 30 },
                        Colour = borderColour,
                        Font = new FontUsage("Roboto", weight: "Bold"),
                        Text = "Victory"
                    },
                    new SpriteIcon()
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Position = new Vector2(90, 0),
                        Icon = FontAwesome.Solid.Check,
                        Colour = new Color4(20, 125, 20, 255),
                        Size = new Vector2(20)
                    }
                }
            };

            drawables[2] = winnersContainer = createPlayersFlow(players, winners, false);

            if (losers == null || losers.Count <= 0)
                return drawables;

            drawables[3] = new Container()
            {
                RelativeSizeAxes = Axes.X,
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                Size = new Vector2(0.9f, 30f),
                Position = new Vector2(0, 20),
                Masking = true,
                CornerRadius = 3,
                Children = new Drawable[]
                {
                    new Box()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = borderColour.Opacity(0.35f),
                    },
                    new SpriteText()
                    {
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                        Padding = new MarginPadding { Right = 30 },
                        Colour = borderColour,
                        Font = new FontUsage("Roboto", weight: "Bold"),
                        Text = "Defeat"
                    },
                    new SpriteIcon()
                    {
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                        Position = new Vector2(-90, 0),
                        Icon = FontAwesome.Solid.Times,
                        Colour = new Color4(142, 19, 19, 255),
                        Size = new Vector2(20)
                    }
                }
            };

            drawables[4] = losersContainer = createPlayersFlow(players, losers, true);

            return drawables;
        }

        private TextFlowContainer createTitleTextFlow(string gameOverMessage)
        {
            var container = new TextFlowContainer()
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                TextAnchor = Anchor.TopCentre,
                Colour = new Color4(20, 20, 20, 255)
            };

            container.AddText("Game Over!\n", text => text.Font = new FontUsage("Roboto", size: 26, weight: "Bold"));
            container.AddText(gameOverMessage);

            return container;
        }

        private FillFlowContainer createPlayersFlow(Dictionary<long, PlayerInfo> players, List<long> playersToShow, bool rightAligned)
        {
            var flow = new FillFlowContainer()
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                Direction = FillDirection.Vertical,
                Padding = new MarginPadding { Horizontal = 25 }
            };

            foreach (var player in playersToShow)
            {
                var info = players[player];

                flow.Add(new PlayerDrawable(info, rightAligned));
            }

            return flow;
        }

        [BackgroundDependencyLoader]
        private void load(NearestNeighbourTextureStore textureStore)
        {
            Animate();
        }

        public void Animate()
        {
            this.FadeOut().FadeIn(500, Easing.OutQuint);
            this.ScaleTo(new Vector2(0.75f, 0f)).ScaleTo(1, 500, Easing.OutCubic);

            var index = 0;

            foreach (var child in winnersContainer.Children)
            {
                child.FadeOut().ScaleTo(new Vector2(0, 1)).Delay(index * 100).FadeIn(200).ScaleTo(1, 400, Easing.OutQuint);
                index++;
            }

            if (losersContainer != null)
            {
                foreach (var child in losersContainer.Children)
                {
                    child.FadeOut().ScaleTo(new Vector2(0, 1)).Delay(index * 100).FadeIn(200).ScaleTo(1, 400, Easing.OutQuint);
                    index++;
                }
            }
        }

        private class CloseButton : ClickableContainer
        {
            public CloseButton()
            {
                Colour = new Color4(40, 40, 40, 255);
                AutoSizeAxes = Axes.Both;

                Children = new Drawable[]
                {
                    new SpriteIcon()
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Icon = FontAwesome.Solid.TimesCircle,
                        Size = new Vector2(20)
                    }
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                this.FadeColour(new Color4(239, 155, 20, 255), 200);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                this.FadeColour(new Color4(40, 40, 40, 255), 200);
                base.OnHoverLost(e);
            }
        }

        private class PlayerDrawable : Container
        {
            private PlayerInfo playerInfo;
            private Sprite coSprite;
            private Sprite tagSprite;

            public PlayerDrawable(PlayerInfo info, bool rightAligned)
            {
                playerInfo = info;

                Masking = true;
                CornerRadius = 4;
                AlwaysPresent = true;

                RelativeSizeAxes = Axes.X;
                Size = new Vector2(0.8f, 30f);
                Position = new Vector2(rightAligned ? -25 : 25, 0);
                Anchor = rightAligned ? Anchor.TopRight : Anchor.TopLeft;
                Origin = rightAligned ? Anchor.TopRight : Anchor.TopLeft;

                Children = new Drawable[]
                {
                    new Box()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = (rightAligned ? new Color4(142, 19, 19, 255) : new Color4(20, 125, 20, 255)).LightenAndFade(0.6f)
                    },
                    new FillFlowContainer()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Anchor = rightAligned ? Anchor.TopRight : Anchor.TopLeft,
                        Origin = rightAligned ? Anchor.TopRight : Anchor.TopLeft,
                        Padding = new MarginPadding
                        {
                            Left = rightAligned ? 30 : 5,
                            Right = rightAligned ? 5 : 30
                        },
                        Spacing = new Vector2(2, 0),
                        Children = new Drawable[]
                        {
                            coSprite = new Sprite()
                            {
                                Size = new Vector2(30),
                                Anchor = rightAligned ? Anchor.CentreRight : Anchor.CentreLeft,
                                Origin = rightAligned ? Anchor.CentreRight : Anchor.CentreLeft
                            },
                            tagSprite = new Sprite()
                            {
                                Size = new Vector2(0),
                                Anchor = rightAligned ? Anchor.CentreRight : Anchor.CentreLeft,
                                Origin = rightAligned ? Anchor.CentreRight : Anchor.CentreLeft
                            },
                            new SpriteText()
                            {
                                Anchor = rightAligned ? Anchor.CentreRight : Anchor.CentreLeft,
                                Origin = rightAligned ? Anchor.CentreRight : Anchor.CentreLeft,
                                Text = playerInfo.Username,
                                Font = new FontUsage("Roboto", weight: "Bold", size: 18),
                                Colour = new Color4(20, 20, 20, 255)
                            }
                        }
                    },
                    new SpriteIcon()
                    {
                        Anchor = rightAligned ? Anchor.CentreLeft : Anchor.CentreRight,
                        Origin = rightAligned ? Anchor.CentreLeft : Anchor.CentreRight,
                        Size = new Vector2(20),
                        Position = new Vector2(rightAligned ? 5 : -5, 0),
                        Icon = FontAwesome.Solid.SkullCrossbones,
                        Alpha = playerInfo.Eliminated.Value ? 1 : 0,
                        Colour = new Color4(20, 20, 20, 255)
                    },
                    new Box()
                    {
                        RelativeSizeAxes = Axes.X,
                        Size = new Vector2(1, 5),
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre,
                        Colour = (rightAligned ? new Color4(142, 19, 19, 255) : new Color4(20, 125, 20, 255)).LightenAndFade(0.4f)
                    }
                };
            }

            [BackgroundDependencyLoader]
            private void load(NearestNeighbourTextureStore textureStore)
            {
                coSprite.Texture = textureStore.Get($"CO/{playerInfo.ActiveCO.Value.CO.Name}-Small");

                if (playerInfo.TagCO.Value.CO != null)
                {
                    tagSprite.Texture = textureStore.Get($"CO/{playerInfo.TagCO.Value.CO.Name}-Small");
                    tagSprite.Size = new Vector2(20);
                }
            }
        }
    }
}
