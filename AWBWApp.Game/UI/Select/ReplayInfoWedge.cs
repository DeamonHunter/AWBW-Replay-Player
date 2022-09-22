using System;
using System.Linq;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Country;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.IO;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Logging;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Select
{
    public class ReplayInfoWedge : VisibilityContainer
    {
        public ReplayInfo Replay
        {
            get => replay;
            set
            {
                if (replay == value && DisplayedContent != null) return;

                replay = value;

                updateDisplay();
            }
        }

        public bool HasReplays;

        private ReplayInfo replay;

        protected WedgeInfo Info { get; private set; }

        protected Container DisplayedContent { get; private set; }
        private Container loadingContent;

        public override bool IsPresent => base.IsPresent || DisplayedContent == null;

        private void updateDisplay()
        {
            Scheduler.AddOnce(loadInfo);

            void loadInfo()
            {
                LoadComponentAsync(loadingContent = new Container()
                {
                    RelativeSizeAxes = Axes.Both,
                    Depth = DisplayedContent?.Depth + 1 ?? 0,
                    Child = Info = new WedgeInfo(replay, HasReplays)
                }, loaded =>
                {
                    if (loaded != loadingContent) return;

                    removeDisplayedContent();
                    Add(DisplayedContent = loaded);
                    DisplayedContent.FadeInFromZero(250, Easing.Out);
                });
            }
        }

        private void removeDisplayedContent()
        {
            State.Value = Visibility.Visible;

            DisplayedContent?.FadeOut(250);
            DisplayedContent?.Expire();
            DisplayedContent = null;
        }

        protected override void PopIn()
        {
            this.MoveToX(0, 800, Easing.OutQuint);
            this.FadeIn(800);
        }

        protected override void PopOut()
        {
            this.MoveToX(-100, 800, Easing.In);
            this.FadeOut(1600, Easing.In);
        }
    }

    public class WedgeInfo : Container
    {
        private readonly ReplayInfo replay;
        private readonly bool hasReplays;

        public WedgeInfo(ReplayInfo info, bool hasReplays)
        {
            replay = info;
            this.hasReplays = hasReplays;
        }

        [BackgroundDependencyLoader]
        private void load(MapFileStorage storage, CountryStorage countryStorage)
        {
            RelativeSizeAxes = Axes.Both;

            if (replay == null)
            {
                Children = new Drawable[]
                {
                    new Container()
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        RelativeSizeAxes = Axes.Both,
                        Masking = true,
                        BorderColour = new Color4(50, 50, 200, 255).Darken(0.4f),
                        BorderThickness = 5,
                        Size = new Vector2(1, 1.05f),
                        Child = new Box()
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = ColourInfo.GradientVertical(new Color4(50, 50, 200, 175), new Color4(25, 25, 175, 125))
                        }
                    },
                    new Container()
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Masking = true,
                        CornerRadius = 15,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Children = new Drawable[]
                        {
                            new Box()
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.Black.Opacity(0.5f)
                            },
                            createMissingReplaysContainer(hasReplays)
                        }
                    },
                };
                return;
            }

            Children = new Drawable[]
            {
                new Container()
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    BorderColour = new Color4(50, 50, 200, 255).Darken(0.4f),
                    BorderThickness = 5,
                    Size = new Vector2(1, 1.05f),
                    Child = new Box()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = ColourInfo.GradientVertical(new Color4(50, 50, 200, 175), new Color4(25, 25, 175, 125))
                    }
                },
                new FillFlowContainer()
                {
                    Direction = FillDirection.Vertical,
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Children = new Drawable[]
                    {
                        createTextArea(countryStorage),
                        new SmallMapTexture(replay.MapId)
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Size = new Vector2(280)
                        }
                    }
                }
            };
        }

        private Drawable createTextArea(CountryStorage countryStorage)
        {
            var titleFlow = new TextFlowContainer()
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Padding = new MarginPadding(10)
            };

            titleFlow.AddText(replay.Name, text => text.Font = new FontUsage("Roboto", weight: "Bold", size: 26f));

            var textFlow = new TextFlowContainer()
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Padding = new MarginPadding(10)
            };

            textFlow.AddText("Players\n", text => text.Font = new FontUsage("Roboto", weight: "Bold", size: 22f));

            if (replay.TeamMatch)
            {
                var teams = new string[] { "A", "B", "C", "D", "E", "F", "G", "H" };

                ITextPart lastPart = null;

                foreach (var team in teams)
                {
                    var players = replay.Players.Where(p => p.Value.TeamName == team);
                    if (!players.Any())
                        continue;

                    textFlow.AddText($"Team {team}:  ", text => text.Font = new FontUsage("Roboto", weight: "Bold"));

                    foreach (var player in players)
                    {
                        var username = player.Value.Username ?? "[Unknown Username:" + player.Value.UserId + "]";

                        var colour = Color4Extensions.FromHex(countryStorage.GetCountryByAWBWID(player.Value.CountryID).Colours["replayList"]).Lighten(0.5f);
                        textFlow.AddText(username + "  ", text =>
                        {
                            text.Colour = colour;
                            text.Shadow = true;
                        });
                    }
                    lastPart = textFlow.AddText("\n\n");
                }
                if (lastPart != null)
                    textFlow.RemovePart(lastPart);
            }
            else
            {
                foreach (var player in replay.Players)
                {
                    var username = player.Value.Username ?? "[Unknown Username:" + player.Value.UserId + "]";

                    var colour = Color4Extensions.FromHex(countryStorage.GetCountryByAWBWID(player.Value.CountryID).Colours["replayList"]).Lighten(0.5f);
                    textFlow.AddText(username + "  ", text =>
                    {
                        text.Colour = colour;
                        text.Shadow = true;
                    });
                }
            }

            return new FillFlowContainer()
            {
                Direction = FillDirection.Vertical,
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Children = new Drawable[]
                {
                    new Container()
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.Black.Opacity(0.6f)
                            },
                            titleFlow
                        }
                    },
                    new Container()
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.Black.Opacity(0.4f)
                            },
                            textFlow
                        }
                    },
                    createGameStats()
                }
            };
        }

        private Drawable createGameStats()
        {
            var statsFlow = new TextFlowContainer()
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Padding = new MarginPadding(10),
                Spacing = new Vector2(0, 2)
            };

            addStat(statsFlow, "Game ID", replay.ID.ToString());
            addStat(statsFlow, "Fog", replay.Fog ? "On" : "Off");
            addStat(statsFlow, "Tag COs", replay.Type == MatchType.Tag ? "On" : "Off");
            addStat(statsFlow, "CO Powers", replay.PowersAllowed ? "On" : "Off");

            addStat(statsFlow, "Weather", replay.WeatherType);
            addStat(statsFlow, "Starting Funds", replay.StartingFunds.ToString());
            addStat(statsFlow, "Funds Per Building", replay.FundsPerBuilding.ToString());

            var lastNewLine = addStat(statsFlow, "Capture Win", replay.CaptureWinBuildingNumber.HasValue ? replay.CaptureWinBuildingNumber.ToString() : "Off");
            statsFlow.RemovePart(lastNewLine);

            return new Container()
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black.Opacity(0.3f)
                    },
                    statsFlow
                }
            };
        }

        private ITextPart addStat(TextFlowContainer text, string name, string value)
        {
            text.AddText(name + ":  ", createHeader);
            text.AddText(value);
            return text.AddText("\n");
        }

        private void createHeader(SpriteText text)
        {
            text.Font = new FontUsage("Roboto", weight: "Bold");
        }

        private TextFlowContainer createMissingReplaysContainer(bool hasReplays)
        {
            var textFlow = new TextFlowContainer()
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                TextAnchor = Anchor.Centre,
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Spacing = new Vector2(0, 5),
                Padding = new MarginPadding { Vertical = 5, Horizontal = 2 }
            };

            if (hasReplays)
            {
                textFlow.AddText("No Replays match the current search text.", text => text.Font = new FontUsage("Roboto", weight: "Bold", size: 24));
                return textFlow;
            }

            textFlow.AddText("No Replays have been added.\n\n\n", text => text.Font = new FontUsage("Roboto", weight: "Bold", size: 24));
            textFlow.AddText("You can add more replays by doing the following:\n\n", text => text.Font = new FontUsage("Roboto", weight: "Bold", size: 18));

            textFlow.AddText("Select \"Import a Replay\" and follow the prompts.", text => text.Font = new FontUsage("Roboto", size: 18));
            textFlow.AddText("\nOR\n");
            textFlow.AddText("Dragging a replay on top of this player.", text => text.Font = new FontUsage("Roboto", size: 18));

            return textFlow;
        }

        private class SmallMapTexture : Container
        {
            private Sprite mapSprite;
            private SpriteText mapName;
            private LoadingSpinner loadLayer;
            private long mapID;

            public SmallMapTexture(long mapID)
            {
                this.mapID = mapID;

                Children = new Drawable[]
                {
                    new Box()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black.Opacity(0.2f)
                    },
                    mapSprite = new Sprite()
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.Both,
                        FillMode = FillMode.Fit
                    },
                    new Container()
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.Black.Opacity(0.6f)
                            },
                            mapName = new SpriteText
                            {
                                Anchor = Anchor.BottomCentre,
                                Origin = Anchor.BottomCentre,
                                Shadow = true,
                                ShadowColour = Color4.Black,
                                Font = new FontUsage("Roboto", weight: "Bold"),
                                Margin = new MarginPadding { Bottom = 5 }
                            },
                        }
                    },
                    loadLayer = new LoadingSpinner()
                };
                loadLayer.Show();
            }

            [BackgroundDependencyLoader]
            private async void load(IRenderer renderer, MapFileStorage mapStorage, TerrainTileStorage terrainStorage, BuildingStorage buildingStorage, CountryStorage countryStorage)
            {
                string name;
                Texture map;

                try
                {
                    (name, map) = await mapStorage.GetTextureForMap(mapID, renderer, terrainStorage, buildingStorage, countryStorage);
                }
                catch (Exception e)
                {
                    Logger.Log($"Failed to get map for texture: {mapID}. {e.Message}");
                    mapName.Text = $"[Missing Map: {mapID}";
                    return;
                }

                mapName.Text = name;

                if (map != null)
                {
                    mapSprite.Texture = map;
                    mapSprite.FillAspectRatio = map.Size.X / map.Size.Y;
                }

                Schedule(loadLayer.Hide);
            }
        }
    }
}
