using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using OpenTabletDriver.Plugin.DependencyInjection;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Logging;
using osuTK;
using osuTK.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using Configuration = SixLabors.ImageSharp.Configuration;

namespace AWBWApp.Game.Tests.Visual.Tools
{
    [TestFixture]
    public partial class TestDownloadSprites : AWBWAppTestScene
    {
        private Dropdown<MapSkin> _terrainType;
        private Dropdown<BuildingSkin> _buildingType;
        private Dropdown<CountryCode> _code;
        private BasicButton _button;
        private BasicButton _unitButton;
        private Sprite _sprite;
        private BasicSliderBar<double> _progress;
        private SpriteText _progressText;

        [Resolved]
        private IRenderer renderer { get; set; }

        private const string terrain_path = "https://awbw.amarriner.com/terrain/";
        private const string unit_path = "https://awbw.amarriner.com/terrain/ani/";
        private const string movement_path = "https://awbw.amarriner.com/terrain/aw2/movement/";

        public TestDownloadSprites()
        {
            var container = new FillFlowContainer()
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 10),
                Children = new Drawable[]
                {
                    _terrainType = new BasicDropdown<MapSkin>()
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Width = 400,
                        Items = Enum.GetValues<MapSkin>()
                    },
                    _buildingType = new BasicDropdown<BuildingSkin>()
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Width = 400,
                        Items = Enum.GetValues<BuildingSkin>()
                    },
                    _code = new BasicDropdown<CountryCode>()
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Width = 400,
                        Items = Enum.GetValues<CountryCode>()
                    },
                    _button = new BasicButton()
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Width = 400,
                        Height = 30,
                        Text = "Download",
                        Action = DownloadPressed
                    },
                    _unitButton = new BasicButton()
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Width = 400,
                        Height = 30,
                        Text = "Unit Download",
                        Action = UnitDownloadPressed
                    },
                    new Container()
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Width = 400,
                        Height = 30,
                        Children = new Drawable[]
                        {
                            _progress = new BasicSliderBar<double>()
                            {
                                RelativeSizeAxes = Axes.Both,
                                BackgroundColour = new Color4(50, 75, 50, 255),
                                SelectionColour = new Color4(100, 150, 100, 255),
                                Current = new BindableDouble()
                                {
                                    MinValue = 0f,
                                    MaxValue = 1f,
                                    Default = 0.5f,
                                }
                            }
                        }
                    },
                    _sprite = new Sprite()
                    {
                        Width = 64,
                        Height = 64,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    }
                }
            };

            _progress.Add(_progressText = new SpriteText()
            {
                Text = "Waiting for Input.",
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            });

            Add(container);
        }

        private void DownloadPressed()
        {
            SetEnabled(false);
            Task.Run(DownloadTask, CancellationToken.None);
        }

        private void UnitDownloadPressed()
        {
            SetEnabled(false);
            Task.Run(UnitDownloadTask, CancellationToken.None);
        }

        private void SetEnabled(bool enabled)
        {
            _button.Enabled.Value = enabled;
            _unitButton.Enabled.Value = enabled;
            _terrainType.Current.Disabled = !enabled;
            _buildingType.Current.Disabled = !enabled;
            _code.Current.Disabled = !enabled;
        }

        private async Task DownloadTask()
        {
            try
            {
                string webUrl, fileLocation;
                //Todo: Non-standard setups will break this... but meh

                Dictionary<string, string> filesToDownload;
                bool animated;

                if (_code.Current.Value == CountryCode.Terrain)
                {
                    filesToDownload = new Dictionary<string, string>();
                    animated = false;

                    foreach (var terrainTile in _terrainTiles)
                    {
                        filesToDownload.Add(terrainTile.Key, terrainTile.Value);
                        if (terrainTile.Key == "teleporter")
                            continue;

                        filesToDownload.Add(terrainTile.Key + "_snow", terrainTile.Value + "_Snow");
                    }

                    if (_terrainType.Current.Value == MapSkin.Desert)
                    {
                        webUrl = terrain_path + "desert/";
                        fileLocation = Path.GetFullPath(Path.Combine(Environment.ProcessPath, "..", "..", "..", "..", "..", "AWBWApp.Resources", "Textures", "Map", "Desert")) + "/";
                        filesToDownload.Remove("teleporter");
                    }
                    else if (_terrainType.Current.Value == MapSkin.DoR)
                    {
                        webUrl = terrain_path + "dor/";
                        fileLocation = Path.GetFullPath(Path.Combine(Environment.ProcessPath, "..", "..", "..", "..", "..", "AWBWApp.Resources", "Textures", "Map", "DoR")) + "/";
                        filesToDownload.Remove("teleporter");
                    }
                    else if (_buildingType.Current.Value == BuildingSkin.AW1)
                    {
                        webUrl = terrain_path + (_buildingType.Current.Value == BuildingSkin.AW2 ? "ani" : "aw1") + "/";
                        fileLocation = Path.GetFullPath(Path.Combine(Environment.ProcessPath, "..", "..", "..", "..", "..", "AWBWApp.Resources", "Textures", "Map", "ClassicAW1")) + "/";
                    }
                    else
                    {
                        webUrl = terrain_path + (_buildingType.Current.Value == BuildingSkin.AW2 ? "ani" : "aw1") + "/";
                        fileLocation = Path.GetFullPath(Path.Combine(Environment.ProcessPath, "..", "..", "..", "..", "..", "AWBWApp.Resources", "Textures", "Map", _terrainType.Current.Value.ToString())) + "/";
                    }
                }
                else if (_code.Current.Value == CountryCode.Neutral)
                {
                    webUrl = terrain_path + (_buildingType.Current.Value == BuildingSkin.AW2 ? "ani" : "aw1") + "/";
                    fileLocation = Path.GetFullPath(Path.Combine(Environment.ProcessPath, "..", "..", "..", "..", "..", "AWBWApp.Resources", "Textures", "Map", _buildingType.Current.Value.ToString())) + "/";

                    filesToDownload = new Dictionary<string, string>();
                    animated = _buildingType.Current.Value == BuildingSkin.AW2;
                    foreach (var buildingTile in _buildingTiles)
                    {
                        if (buildingTile.Key.Contains("hq"))
                            continue;
                        filesToDownload.Add("neutral" + buildingTile.Key, "Neutral/" + buildingTile.Value + "-0");
                        filesToDownload.Add("neutral" + buildingTile.Key + "_rain", "Neutral/" + buildingTile.Value + "_Rain-0");
                        filesToDownload.Add("neutral" + buildingTile.Key + "_snow", "Neutral/" + buildingTile.Value + "_Snow-0");
                    }

                    foreach (var buildingTile in _neutralBuildings)
                    {
                        filesToDownload.Add(buildingTile.Key, "Neutral/" + buildingTile.Value);
                        filesToDownload.Add(buildingTile.Key + "_rain", "Neutral/" + buildingTile.Value + "_Rain");
                        filesToDownload.Add(buildingTile.Key + "_snow", "Neutral/" + buildingTile.Value + "_Snow");
                    }
                }
                else
                {
                    webUrl = terrain_path + (_buildingType.Current.Value == BuildingSkin.AW2 ? "ani" : "aw1") + "/";
                    fileLocation = Path.GetFullPath(Path.Combine(Environment.ProcessPath, "..", "..", "..", "..", "..", "AWBWApp.Resources", "Textures", "Map", _buildingType.Current.Value.ToString())) + "/";

                    animated = _buildingType.Current.Value == BuildingSkin.AW2;
                    var countryPath = _codeToFolder[_code.Current.Value];
                    filesToDownload = new Dictionary<string, string>();
                    foreach (var buildingTile in _buildingTiles)
                    {
                        filesToDownload.Add(countryPath.ToLower() + buildingTile.Key, countryPath + "/" + buildingTile.Value + "-0");
                        filesToDownload.Add(countryPath.ToLower() + buildingTile.Key + "_rain", countryPath + "/" + buildingTile.Value + "_Rain-0");
                        filesToDownload.Add(countryPath.ToLower() + buildingTile.Key + "_snow", countryPath + "/" + buildingTile.Value + "_Snow-0");
                    }
                }

                Logger.Log($"Input Web: {webUrl}", level: LogLevel.Important);
                Logger.Log($"Output Location: {fileLocation}", level: LogLevel.Important);

                var count = 0;
                _progress.Current.Value = 0;

                using (HttpClient client = new HttpClient())
                {
                    foreach (var (webPart, filePart) in filesToDownload)
                    {
                        _progressText.Text = $"{count}/{filesToDownload.Count}";
                        await DownloadAndSave(webUrl + webPart + ".gif", fileLocation + filePart + ".png", animated, false, client);
                        count++;
                        _progress.Current.Value = (float)count / filesToDownload.Count;
                    }
                }

                _progressText.Text = "Done";
            }
            catch (Exception e)
            {
                Logger.Error(e, "Download");
            }
            finally
            {
                Schedule(() =>
                {
                    SetEnabled(true);
                });
            }
        }

        private async Task UnitDownloadTask()
        {
            try
            {
                //Todo: Non-standard setups will break this... but meh

                Dictionary<string, string> filesToDownload;

                switch (_code.Current.Value)
                {
                    case CountryCode.Terrain:
                    case CountryCode.Neutral:
                        return;
                }

                var fileLocation = Path.GetFullPath(Path.Combine(Environment.ProcessPath, "..", "..", "..", "..", "..", "AWBWApp.Resources", "Textures", "Units")) + "/";

                var countryPath = _codeToFolder[_code.Current.Value];
                filesToDownload = new Dictionary<string, string>();
                var code = _code.Current.Value.ToString().ToLowerInvariant();

                foreach (var unit in _units)
                {
                    filesToDownload.Add(unit_path + code + unit.Key, countryPath + "/" + unit.Value);
                    filesToDownload.Add(movement_path + code + "/" + code + unit.Key.Replace(".", "") + "_mside", countryPath + "/" + unit.Value + "_MSide");
                    filesToDownload.Add(movement_path + code + "/" + code + unit.Key.Replace(".", "") + "_mup", countryPath + "/" + unit.Value + "_MUp");
                    filesToDownload.Add(movement_path + code + "/" + code + unit.Key.Replace(".", "") + "_mdown", countryPath + "/" + unit.Value + "_MDown");
                }

                Logger.Log($"Output Location: {fileLocation}", level: LogLevel.Important);

                var count = 0;
                _progress.Current.Value = 0;

                using (HttpClient client = new HttpClient())
                {
                    foreach (var (webPart, filePart) in filesToDownload)
                    {
                        _progressText.Text = $"{count}/{filesToDownload.Count}";
                        await DownloadAndSave(webPart + ".gif", fileLocation + filePart + ".png", true, true, client);
                        count++;
                        _progress.Current.Value = (float)count / filesToDownload.Count;
                    }
                }

                _progressText.Text = "Done";
            }
            catch (Exception e)
            {
                Logger.Error(e, "Download");
            }
            finally
            {
                Schedule(() =>
                {
                    SetEnabled(true);
                });
            }
        }

        private async Task DownloadAndSave(string webUrl, string fileLocation, bool animated, bool appendFrame, HttpClient client)
        {
            Logger.Log($"Downloading file {webUrl} to {fileLocation}");
            using (var webStream = await client.GetStreamAsync(webUrl))
            {
                using (var stream = new MemoryStream(await webStream.ReadAllRemainingBytesToArrayAsync()))
                {
                    var gif = new GifDecoder().Decode(Configuration.Default, stream, CancellationToken.None);

                    if (animated)
                    {
                        for (var i = 0; i < gif.Frames.Count; i++)
                        {
                            if (appendFrame)
                            {
                                await gif.Frames.CloneFrame(i).SaveAsPngAsync(fileLocation.Replace(".png", $"-{i}.png"));
                            }
                            else
                            {
                                var newLocation = fileLocation.Replace("-0", $"-{i}");
                                await gif.Frames.CloneFrame(i).SaveAsPngAsync(newLocation);
                            }
                        }
                    }
                    else
                        await gif.SaveAsPngAsync(fileLocation);
                }
            }
        }

        private enum CountryCode
        {
            Neutral,
            Terrain,
            OS,
            BM,
            GE,
            YC,
            BH,
            RF,
            GS,
            BD,
            AB,
            JS,
            CI,
            PC,
            TG,
            PL,
            AR,
            WN,
            AA,
            NE,
            SC,
            UW
        }

        private Dictionary<CountryCode, string> _codeToFolder = new Dictionary<CountryCode, string>()
        {
            { CountryCode.OS, "OrangeStar" },
            { CountryCode.BM, "BlueMoon" },
            { CountryCode.GE, "GreenEarth" },
            { CountryCode.YC, "YellowComet" },
            { CountryCode.BH, "BlackHole" },
            { CountryCode.RF, "RedFire" },
            { CountryCode.GS, "GreySky" },
            { CountryCode.BD, "BrownDesert" },
            { CountryCode.AB, "AmberBlossom" },
            { CountryCode.JS, "JadeSun" },
            { CountryCode.CI, "CobaltIce" },
            { CountryCode.PC, "PinkCosmos" },
            { CountryCode.TG, "TealGalaxy" },
            { CountryCode.PL, "PurpleLightning" },
            { CountryCode.AR, "AcidRain" },
            { CountryCode.WN, "WhiteNova" },
            { CountryCode.AA, "AzureAsteroid" },
            { CountryCode.NE, "NoirEclipse" },
            { CountryCode.SC, "SilverClaw" },
            { CountryCode.UW, "UmberWilds" },
        };

        private Dictionary<string, string> _terrainTiles = new Dictionary<string, string>()
        {
            { "plain", "Plain" },
            { "mountain", "Mountain" },
            { "wood", "Wood" },
            { "reef", "Reef" },
            { "teleporter", "Black" },
            { "hroad", "Road/H" },
            { "vroad", "Road/V" },
            { "croad", "Road/C" },
            { "esroad", "Road/ES" },
            { "swroad", "Road/SW" },
            { "wnroad", "Road/WN" },
            { "neroad", "Road/NE" },
            { "eswroad", "Road/ESW" },
            { "swnroad", "Road/SWN" },
            { "wneroad", "Road/WNE" },
            { "nesroad", "Road/NES" },
            { "hbridge", "Road/HBridge" },
            { "vbridge", "Road/VBridge" },
            { "hriver", "River/H" },
            { "vriver", "River/V" },
            { "criver", "River/C" },
            { "esriver", "River/ES" },
            { "swriver", "River/SW" },
            { "wnriver", "River/WN" },
            { "neriver", "River/NE" },
            { "eswriver", "River/ESW" },
            { "swnriver", "River/SWN" },
            { "wneriver", "River/WNE" },
            { "nesriver", "River/NES" },
        };

        private Dictionary<string, string> _buildingTiles = new Dictionary<string, string>()
        {
            { "airport", "Airport" },
            { "base", "Base" },
            { "city", "City" },
            { "comtower", "ComTower" },
            { "lab", "Lab" },
            { "port", "Port" },
            { "hq", "HQ" },
        };

        private Dictionary<string, string> _neutralBuildings = new Dictionary<string, string>()
        {
            { "missilesilo", "Silo" },
            { "missilesiloempty", "SiloEmpty" },
            { "hpipeseam", "HSeam-0" },
            { "vpipeseam", "VSeam-0" },
            { "hpiperubble", "HRubble-0" },
            { "vpiperubble", "VRubble-0" },
        };

        private Dictionary<string, string> _units = new Dictionary<string, string>()
        {
            { "anti-air", "Anti-Air" },
            { "apc", "APC" },
            { "artillery", "Artillery" },
            { "b-copter", "B-Copter" },
            { "battleship", "Battleship" },
            { "blackboat", "BlackBoat" },
            { "blackbomb", "BlackBomb" },
            { "bomber", "Bomber" },
            { "carrier", "Carrier" },
            { "cruiser", "Cruiser" },
            { "fighter", "Fighter" },
            { "infantry", "Infantry" },
            { "lander", "Lander" },
            { "md.tank", "MdTank" },
            { "mech", "Mech" },
            { "megatank", "MegaTank" },
            { "missile", "Missile" },
            { "neotank", "NeoTank" },
            { "piperunner", "PipeRunner" },
            { "recon", "Recon" },
            { "rocket", "Rocket" },
            { "stealth", "Stealth" },
            { "sub", "Sub" },
            { "tank", "Tank" },
            { "t-copter", "T-Copter" },
        };
    }
}
