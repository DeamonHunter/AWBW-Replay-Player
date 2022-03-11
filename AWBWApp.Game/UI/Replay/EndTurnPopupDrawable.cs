using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Replay
{
    public class EndTurnPopupDrawable : CompositeDrawable
    {
        private PlayerInfo playerInfo;
        private Sprite characterSprite;
        private Sprite tagCharacterSprite;

        public EndTurnPopupDrawable(PlayerInfo playerInfo, int day)
        {
            this.playerInfo = playerInfo;

            Width = 200;
            AutoSizeAxes = Axes.Y;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            var borderColour = Color4Extensions.FromHex(playerInfo.Country.Value.Colours["playerList"]).Darken(0.1f);

            var dayContainer = createDayContainer(day, borderColour);

            GridContainer grid;
            Container spriteContainer;
            InternalChildren = new Drawable[]
            {
                new Container()
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
                        new FillFlowContainer()
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Direction = FillDirection.Vertical,
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Spacing = new Vector2(0, 3),
                            Children = new Drawable[]
                            {
                                grid = new GridContainer()
                                {
                                    Origin = Anchor.TopCentre,
                                    Anchor = Anchor.TopCentre,
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    RowDimensions = new Dimension[] { new Dimension(mode: GridSizeMode.AutoSize) },
                                    ColumnDimensions = new Dimension[] { new Dimension(mode: GridSizeMode.Distributed), new Dimension(mode: GridSizeMode.AutoSize), new Dimension(mode: GridSizeMode.AutoSize) },
                                    Content = new Drawable[][]
                                    {
                                        new Drawable[]
                                        {
                                            dayContainer,
                                            spriteContainer = new Container()
                                            {
                                                Margin = new MarginPadding { Right = 10, Top = 5 },
                                                Size = new Vector2(40, 40),
                                                Child = characterSprite = new Sprite()
                                                {
                                                    Anchor = Anchor.CentreRight,
                                                    Origin = Anchor.CentreRight,
                                                    Size = new Vector2(40, 40)
                                                },
                                            },
                                            null
                                        }
                                    }
                                },
                                new Container()
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                    Size = new Vector2(0.9f, 5f),
                                    Position = new Vector2(0, 20),
                                    Masking = true,
                                    CornerRadius = 3,
                                    Child = new Box()
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = borderColour,
                                    },
                                },
                                new TextFlowContainer
                                {
                                    TextAnchor = Anchor.TopCentre,
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Padding = new MarginPadding { Bottom = 5 },
                                    Text = $"{playerInfo.Username}'s Turn",
                                    Colour = new Color4(10, 10, 10, 255)
                                }
                            }
                        }
                    }
                }
            };

            if (playerInfo.TagCO.Value.CO != null)
            {
                spriteContainer.Padding = new MarginPadding { Top = 5 };
                grid.Content[0][2] = new Container()
                {
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                    Margin = new MarginPadding { Right = 10 },
                    Size = new Vector2(25),
                    Child = tagCharacterSprite = new Sprite()
                    {
                        Anchor = Anchor.BottomRight,
                        Origin = Anchor.BottomRight,
                        Size = new Vector2(25)
                    }
                };
            }

            this.FadeOut();
        }

        private Drawable createDayContainer(int day, Color4 borderColour)
        {
            return new SpriteText()
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Text = $"Day {day}",
                Colour = new Color4(0, 0, 0, 255),
                Font = new FontUsage("Roboto", size: 32, weight: "Bold")
            };
        }

        [BackgroundDependencyLoader]
        private void load(NearestNeighbourTextureStore textureStore)
        {
            characterSprite.Texture = textureStore.Get($"CO/{playerInfo.ActiveCO.Value.CO.Name}-Small");
            if (tagCharacterSprite != null)
                tagCharacterSprite.Texture = textureStore.Get($"CO/{playerInfo.TagCO.Value.CO.Name}-Small");

            Animate();
        }

        public void Animate()
        {
            this.FadeOut().FadeIn(300, Easing.OutQuint);
            this.ScaleTo(new Vector2(0, 0.5f)).ScaleTo(1, 300, Easing.OutQuint);

            using (BeginDelayedSequence(1250))
            {
                this.FadeIn(300, Easing.OutQuint);
                this.ScaleTo(new Vector2(0, 0.5f), 300, Easing.InQuint).Then().Expire();
            }
        }
    }
}
