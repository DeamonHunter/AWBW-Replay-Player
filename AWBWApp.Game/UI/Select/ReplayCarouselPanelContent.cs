using System.Collections.Generic;
using AWBWApp.Game.Game.COs;
using AWBWApp.Game.Game.Country;
using AWBWApp.Game.UI.Components;
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
        private void load(CountryStorage countryStorage, COStorage coStorage)
        {
            var replayInfo = carouselReplay.ReplayInfo;

            var playersDrawables = new List<Drawable>();

            var index = 0;

            foreach (var player in replayInfo.Players)
            {
                var userText = player.Value.GetUIFriendlyUsername();

                if (player.Value.COsUsedByPlayer.Count >= 1 && replayInfo.Players.Count <= 8)
                {
                    userText += " (";

                    foreach (var coID in player.Value.COsUsedByPlayer)
                    {
                        if (coStorage.TryGetCOByAWBWId(coID, out var co))
                            userText += $"{co.Name}, ";
                    }
                    userText = userText[..^2] + ")";
                }

                playersDrawables.Add(new SpriteText
                {
                    Text = userText,
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Colour = Color4Extensions.FromHex(countryStorage.GetCountryByAWBWID(player.Value.CountryID).Colours["replayList"]).Lighten(0.5f), //Todo: Fix config
                    Font = FontUsage.Default.With(size: 20, italics: true)
                });

                if (index == replayInfo.Players.Count - 1)
                    continue;

                index++;
            }

            InternalChildren = new Drawable[]
            {
                new FillFlowContainer
                {
                    Direction = FillDirection.Vertical,
                    AutoSizeAxes = Axes.Y,
                    RelativeSizeAxes = Axes.X,
                    Padding = new MarginPadding { Top = 2, Left = 10, Right = 10, Bottom = 5 },
                    Spacing = new Vector2(0f, 3f),
                    Children = new Drawable[]
                    {
                        new FillFlowContainer
                        {
                            Direction = FillDirection.Horizontal,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            AutoSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Top = 5, Left = 10, Right = 10, Bottom = 5 },
                            Spacing = new Vector2(8f, 0),
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
                                    Padding = new MarginPadding { Top = 2 },
                                    Text = carouselReplay.MapName,
                                    Font = FontUsage.Default.With(weight: "SemiBold", size: 17, italics: true),
                                    Shadow = true,
                                    ShadowColour = new Color4(0, 0, 0, 200),
                                    Colour = new Color4(220, 220, 220, 255)
                                },
                            }
                        },
                        new SpriteText
                        {
                            Text = $"(Game ID: {replayInfo.ID})   {replayInfo.StartDate.ToShortDateString()} - {replayInfo.EndDate.ToShortDateString()}",
                            Font = FontUsage.Default.With(weight: "SemiBold", size: 17, italics: true),
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
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
                    Height = 0.4f,
                    Colour = ColourInfo.SingleColour(new Color4(15, 15, 15, 180))
                },
                new ShrinkingNamesContainer(
                    new FillFlowContainer
                    {
                        Direction = FillDirection.Full,
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Padding = new MarginPadding { Top = 5, Left = 10, Right = 10, Bottom = 5 },
                        Spacing = new Vector2(10, 0),
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Children = playersDrawables
                    })
                {
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    RelativeSizeAxes = Axes.Both,
                    Height = 0.4f,
                },
            };
        }
    }
}
