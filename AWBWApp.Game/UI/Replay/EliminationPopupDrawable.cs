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
    public class EliminationPopupDrawable : CompositeDrawable
    {
        private PlayerInfo playerInfo;
        private Sprite characterSprite;
        private SpriteIcon characterSpriteIcon;
        private Sprite tagCharacterSprite;
        private SpriteIcon tagCharacterSpriteIcon;

        public EliminationPopupDrawable(PlayerInfo playerInfo, string eliminationMessage, bool resigned)
        {
            this.playerInfo = playerInfo;

            Width = 200;
            AutoSizeAxes = Axes.Y;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            var borderColour = Color4Extensions.FromHex(playerInfo.Country.Value.Colours["playerList"]).Darken(0.1f);

            FillFlowContainer spriteFillFlow;
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
                                new TextFlowContainer(x => x.Font = new FontUsage("Roboto", weight: "Bold", size: 23))
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    TextAnchor = Anchor.TopCentre,
                                    Colour = new Color4(30, 30, 30, 255),
                                    Text = resigned ? $"{playerInfo.Username}\nhas Resigned!" : $"{playerInfo.Username}\nwas Eliminated!"
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
                                new Container()
                                {
                                    Origin = Anchor.TopCentre,
                                    Anchor = Anchor.TopCentre,
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Children = new Drawable[]
                                    {
                                        new Box()
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = borderColour.LightenAndFade(0.8f)
                                        },
                                        spriteFillFlow = new FillFlowContainer()
                                        {
                                            Origin = Anchor.TopCentre,
                                            Anchor = Anchor.TopCentre,
                                            Direction = FillDirection.Horizontal,
                                            RelativeSizeAxes = Axes.X,
                                            Width = 0.4f,
                                            AutoSizeAxes = Axes.Y,
                                            Spacing = new Vector2(0, 10),
                                            Children = new Drawable[]
                                            {
                                                spriteContainer = new Container()
                                                {
                                                    Anchor = Anchor.BottomCentre,
                                                    Origin = Anchor.BottomCentre,
                                                    Margin = new MarginPadding { Bottom = 2 },
                                                    Size = new Vector2(40),
                                                    Children = new Drawable[]
                                                    {
                                                        characterSprite = new Sprite()
                                                        {
                                                            Anchor = Anchor.Centre,
                                                            Origin = Anchor.Centre,
                                                            Size = new Vector2(40)
                                                        },
                                                        characterSpriteIcon = new SpriteIcon()
                                                        {
                                                            Anchor = Anchor.Centre,
                                                            Origin = Anchor.Centre,
                                                            Size = new Vector2(40),
                                                            Colour = new Colour4(120, 20, 0, 255),
                                                            Icon = FontAwesome.Solid.Times,
                                                            Alpha = 0
                                                        }
                                                    }
                                                }
                                            }
                                        },
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
                                    Text = eliminationMessage,
                                    Colour = new Color4(10, 10, 10, 255)
                                }
                            }
                        }
                    }
                }
            };

            if (playerInfo.TagCO.Value.CO != null)
            {
                spriteFillFlow.Add(new Container()
                {
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Size = new Vector2(25),
                    Children = new Drawable[]
                    {
                        tagCharacterSprite = new Sprite()
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(25)
                        },
                        tagCharacterSpriteIcon = new SpriteIcon()
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(25),
                            Colour = new Colour4(120, 20, 0, 255),
                            Icon = FontAwesome.Solid.Times,
                            Alpha = 0
                        }
                    }
                });
            }

            this.FadeOut();
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
            this.ScaleTo(new Vector2(0.75f, 0f)).ScaleTo(1, 300, Easing.OutQuint);

            using (BeginDelayedSequence(400))
            {
                characterSpriteIcon.FadeIn(100).RotateTo(180, 400, Easing.OutCubic).ScaleTo(0.5f).ScaleTo(1f, 400, Easing.OutBounce);
                tagCharacterSpriteIcon?.Delay(200).FadeIn(100).RotateTo(180, 400, Easing.OutCubic).ScaleTo(0.5f).ScaleTo(1f, 400, Easing.OutBounce);

                using (BeginDelayedSequence(2750))
                {
                    this.FadeIn(300, Easing.OutQuint);
                    this.ScaleTo(new Vector2(0.75f, 0f), 300, Easing.InQuint).Then().Expire();
                }
            }
        }
    }
}
