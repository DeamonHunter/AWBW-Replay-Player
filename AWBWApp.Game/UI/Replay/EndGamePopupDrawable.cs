using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Components;
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

        public EndGamePopupDrawable(Dictionary<long, PlayerInfo> players, List<long> winners, List<long> losers, string gameOverMessage, Action<long> openStatsAction)
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
                    new StandardCloseButton()
                    {
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                        Padding = new MarginPadding { Top = 5, Right = 5 },
                        Action = () =>
                        {
                            FinishTransforms(true);
                            this.FadeOut(300, Easing.OutQuint).Expire();
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
                        Children = createFillFlowChildren(gameOverMessage, players, winners, losers, borderColour, openStatsAction)
                    }
                }
            };

            this.FadeOut();
        }

        private Drawable[] createFillFlowChildren(string gameOverMessage, Dictionary<long, PlayerInfo> players, List<long> winners, List<long> losers, Color4 borderColour, Action<long> openStatsAction)
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

            drawables[2] = winnersContainer = createPlayersFlow(players, winners, false, openStatsAction);

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

            drawables[4] = losersContainer = createPlayersFlow(players, losers, true, openStatsAction);

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

        private FillFlowContainer createPlayersFlow(Dictionary<long, PlayerInfo> players, List<long> playersToShow, bool rightAligned, Action<long> openStatsAction)
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

            var boxColor = (rightAligned ? new Color4(142, 19, 19, 255) : new Color4(20, 125, 20, 255)).LightenAndFade(0.6f);
            var borderColor = (rightAligned ? new Color4(142, 19, 19, 255) : new Color4(20, 125, 20, 255)).LightenAndFade(0.4f);

            foreach (var player in playersToShow)
            {
                var info = players[player];
                flow.Add(new EndGamePlayerListItem(info, rightAligned, boxColor, borderColor, new Color4(20, 20, 20, 255), true)
                {
                    Action = () => openStatsAction?.Invoke(player)
                });
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

        protected override bool OnClick(ClickEvent e)
        {
            return true;
        }

        protected override bool OnHover(HoverEvent e)
        {
            return true;
        }
    }
}
