﻿using System.Collections.Generic;
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
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;
using osuTK.Graphics;
using osuTK.Graphics.ES30;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Configuration = SixLabors.ImageSharp.Configuration;
using Vector4 = System.Numerics.Vector4;

namespace AWBWApp.Game.UI.Select
{
    public class ReplayInfoWedge : VisibilityContainer
    {
        public ReplayInfo Replay
        {
            get => replay;
            set
            {
                if (replay == value) return;

                replay = value;

                updateDisplay();
            }
        }

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
                if (replay == null)
                {
                    removeDisplayedContent();
                    return;
                }

                LoadComponentAsync(loadingContent = new Container()
                {
                    RelativeSizeAxes = Axes.Both,
                    Depth = DisplayedContent?.Depth + 1 ?? 0,
                    Child = Info = new WedgeInfo(replay)
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
            State.Value = replay == null ? Visibility.Hidden : Visibility.Visible;

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
        private ReplayInfo replay;
        private SmallMapTexture map;

        public WedgeInfo(ReplayInfo info)
        {
            replay = info;
        }

        [BackgroundDependencyLoader]
        private void load(MapFileStorage storage, CountryStorage countryStorage)
        {
            RelativeSizeAxes = Axes.Both;

            SmallMapTexture map;
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
                        map = new SmallMapTexture(replay.MapId)
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
                        var username = player.Value.Username ?? "[Unknown Username: " + player.Value.UserId + "]";

                        var colour = Color4Extensions.FromHex(countryStorage.GetCountryByAWBWID(player.Value.CountryId).Colours["replayList"]).Lighten(0.5f);
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
                    var username = player.Value.Username ?? "[Unknown Username: " + player.Value.UserId + "]";

                    var colour = Color4Extensions.FromHex(countryStorage.GetCountryByAWBWID(player.Value.CountryId).Colours["replayList"]).Lighten(0.5f);
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
            private async void load(MapFileStorage mapStorage, TerrainTileStorage terrainStorage, BuildingStorage buildingStorage, CountryStorage countryStorage)
            {
                var map = await mapStorage.GetOrDownloadMap(mapID);

                mapName.Text = map.TerrainName;

                //Todo: This is mostly for testing, should probably create these images only once

                var image = new Image<Rgba32>(Configuration.Default, map.Size.X, map.Size.Y);
                Dictionary<short, Rgba32> mapColors = new Dictionary<short, Rgba32>();

                for (int y = 0; y < map.Size.Y; y++)
                {
                    for (int x = 0; x < map.Size.X; x++)
                    {
                        var tileId = map.Ids[y * map.Size.X + x];

                        if (mapColors.TryGetValue(tileId, out var pixel))
                        {
                            image[x, y] = pixel;
                            continue;
                        }

                        pixel = new Rgba32(0, 0, 0, 255);

                        if (terrainStorage.TryGetTileByAWBWId(tileId, out var tile))
                        {
                            var colour = Color4Extensions.FromHex(tile.Colour ?? "000000FF");
                            pixel = new Rgba32(new Vector4(colour.R, colour.G, colour.B, colour.A));
                        }
                        else if (buildingStorage.TryGetBuildingByAWBWId(tileId, out var building))
                        {
                            if (building.CountryID != 0)
                            {
                                var colour = Color4Extensions.FromHex(countryStorage.GetCountryByAWBWID(building.CountryID).Colours["playerList"]).Lighten(0.2f);
                                pixel = new Rgba32(new Vector4(colour.R, colour.G, colour.B, colour.A));
                            }
                            else
                            {
                                var colour = Color4Extensions.FromHex(building.Colour ?? "000000FF");
                                pixel = new Rgba32(new Vector4(colour.R, colour.G, colour.B, colour.A));
                            }
                        }

                        mapColors[tileId] = pixel;
                        image[x, y] = pixel;
                    }
                }

                mapSprite.Texture = new Texture(map.Size.X, map.Size.Y, true, All.Nearest);
                mapSprite.Texture.SetData(new TextureUpload(image));
                mapSprite.FillAspectRatio = (float)map.Size.X / map.Size.Y;

                Schedule(loadLayer.Hide);
            }
        }
    }
}