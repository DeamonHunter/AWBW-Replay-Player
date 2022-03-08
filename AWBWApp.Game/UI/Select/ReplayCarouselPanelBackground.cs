using AWBWApp.Game.API.Replay;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Select
{
    public class ReplayCarouselPanelBackground : BufferedContainer
    {
        public ReplayCarouselPanelBackground(ReplayInfo replayInfo)
        {
            RedrawOnScale = false;
            RelativeSizeAxes = Axes.Both;
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
                    Shear = new Vector2(0.2f, 0),
                    Alpha = 0.5f,
                    Children = new[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = ColourInfo.GradientHorizontal(new Color4(0, 0, 0, 1f), new Color4(0, 0, 0, 0.8f)),
                            Width = 0.1f
                        },
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = ColourInfo.GradientHorizontal(new Color4(0, 0, 0, 0.8f), new Color4(0f, 0f, 0f, 0.4f)),
                            Width = 0.4f
                        },
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = ColourInfo.GradientHorizontal(new Color4(0f, 0f, 0f, 0.4f), new Color4(0f, 0f, 0f, 0.8f)),
                            Width = 0.4f
                        },
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = ColourInfo.GradientHorizontal(new Color4(0f, 0f, 0f, 0.8f), new Color4(0, 0, 0, 1f)),
                            Width = 0.2f
                        },
                    }
                }
            };
        }
    }
}
