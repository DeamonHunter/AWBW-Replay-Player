using AWBWApp.Game.API.Replay;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Select
{
    public class ReplayCarouselPanelContent : CompositeDrawable
    {
        private readonly CarouselReplay carouselReplay;

        public ReplayCarouselPanelContent(CarouselReplay replay)
        {
            carouselReplay = replay;

            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            var replayInfo = carouselReplay.ReplayInfo;

            var playersDrawables = new Drawable[replayInfo.Players.Count * 2 - 1];

            for (int i = 0; i < replayInfo.Players.Count; i++)
            {
                var player = replayInfo.Players[i];

                playersDrawables[i * 2] = new SpriteText
                {
                    Text = player.UniqueId,
                    Colour = ReplayUser.CountryColour(player.CountryId).Lighten(0.5f),
                    Font = FontUsage.Default.With(size: 15, italics: true)
                };

                if (i == replayInfo.Players.Count - 1)
                    break;

                playersDrawables[i * 2 + 1] = new SpriteText
                {
                    Text = ", ",
                    Font = FontUsage.Default.With(size: 15),
                    Shadow = true
                };
            }

            InternalChildren = new Drawable[]
            {
                new FillFlowContainer
                {
                    Direction = FillDirection.Horizontal,
                    AutoSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Top = 5, Left = 10, Right = 10, Bottom = 5 },
                    Spacing = new Vector2(5f, 0),
                    Children = new Drawable[]
                    {
                        new SpriteText
                        {
                            Text = replayInfo.Name,
                            Font = FontUsage.Default.With(weight: "Bold", size: 22, italics: true),
                            Shadow = true,
                            ShadowColour = new Color4(0, 0, 0, 200)
                        },
                        new SpriteText
                        {
                            Text = $"({replayInfo.StartDate} - {replayInfo.EndDate})",
                            Font = FontUsage.Default.With(weight: "SemiBold", size: 17, italics: true),
                            Shadow = true,
                            ShadowColour = new Color4(0, 0, 0, 200)
                        },
                    }
                },
                new Box
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    RelativeSizeAxes = Axes.Both,
                    Height = 0.5f,
                    Colour = ColourInfo.SingleColour(new Color4(15, 15, 15, 180))
                },
                new FillFlowContainer
                {
                    Direction = FillDirection.Full,
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    Padding = new MarginPadding { Top = 5, Left = 10, Right = 10, Bottom = 5 },
                    RelativeSizeAxes = Axes.Both,
                    Height = 0.5f,
                    Children = playersDrawables
                },
            };
        }
    }
}
