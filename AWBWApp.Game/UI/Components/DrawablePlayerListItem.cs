using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Components
{
    public class DrawablePlayerListItem : Container
    {
        private PlayerInfo playerInfo;
        private Sprite coSprite;
        private Sprite tagSprite;

        public DrawablePlayerListItem(PlayerInfo info, bool rightAligned, Color4 boxColor, Color4 borderColor, Color4 textColor)
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
                    Colour = boxColor
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
                            Colour = textColor
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
                    Colour = borderColor
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
