using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AWBWApp.Game.Game.Country;
using AWBWApp.Game.UI.Select;
using NUnit.Framework;
using OpenTabletDriver.Plugin.DependencyInjection;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Logging;
using osuTK;
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
        private Sprite _sprite;

        [Resolved]
        private IRenderer renderer { get; set; }

        private const string terrain_path = "https://awbw.amarriner.com/terrain/";

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
                    _sprite = new Sprite()
                    {
                        Width = 64,
                        Height = 64,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    }
                }
            };

            Add(container);
        }

        private void DownloadPressed()
        {
            _button.Enabled.Value = false;
            Task.Run(DownloadTask, CancellationToken.None);
        }

        private async Task DownloadTask()
        {
            try
            {
                string webUrl, fileLocation;
                //Todo: Non-standard setups will break this... but meh

                Dictionary<string, string> filesToDownload;

                if (_code.Current.Value == CountryCode.Terrain)
                {
                    filesToDownload = _terrainTiles;
                    webUrl = terrain_path + _terrainType.Current.Value.ToString().ToLowerInvariant() + "/";
                    fileLocation = Path.GetFullPath(Path.Combine(Environment.ProcessPath, "..", "..", "..", "..", "..", "AWBWApp.Resources", "Textures", "Map", _terrainType.Current.Value.ToString())) + "/";

                }
                else if (_code.Current.Value == CountryCode.Neutral)
                {
                    webUrl = terrain_path + _buildingType.Current.Value.ToString().ToLowerInvariant() + "/";
                    fileLocation = Path.GetFullPath(Path.Combine(Environment.ProcessPath, "..", "..", "..", "..", "..", "AWBWApp.Resources", "Textures", "Map", _buildingType.Current.Value.ToString())) + "/";

                    filesToDownload = new Dictionary<string, string>();

                    foreach (var buildingTile in _buildingTiles)
                    {
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
                    webUrl = terrain_path + _buildingType.Current.Value.ToString().ToLowerInvariant() + "/";
                    fileLocation = Path.GetFullPath(Path.Combine(Environment.ProcessPath, "..", "..", "..", "..", "..", "AWBWApp.Resources", "Textures", "Map", _buildingType.Current.Value.ToString())) + "/";

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

                foreach (var (webPart, filePart) in filesToDownload)
                {
                    await DownloadAndSave(webUrl + webPart + ".gif", fileLocation + filePart + ".png");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Download");
            }
            finally
            {
                Schedule(() =>
                {
                    _button.Enabled.Value = true;
                });
            }
        }

        private async Task DownloadAndSave(string webUrl, string fileLocation)
        {
            Logger.Log($"Downloading file {webUrl} to {fileLocation}");
            using (HttpClient client = new HttpClient())
            {
                using (var webStream = await client.GetStreamAsync(webUrl))
                {
                    using (var stream = new MemoryStream(await webStream.ReadAllRemainingBytesToArrayAsync()))
                    {
                        var gif = new GifDecoder().Decode(Configuration.Default, stream, CancellationToken.None);
                        await gif.SaveAsPngAsync(fileLocation);
                    }
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
        };

        private Dictionary<string, string> _buildingTiles = new Dictionary<string, string>()
        {
            { "airport", "Airport" },
            { "base", "Base" },
            { "city", "City" },
            { "comtower", "ComTower" },
            { "lab", "Lab" },
            { "port", "Port" },
        };

        private Dictionary<string, string> _neutralBuildings = new Dictionary<string, string>()
        {
            { "missilesilo", "Silo" },
            { "missilesiloempty", "SiloEmpty" },
            { "hpipeseam", "HSeam-0" },
            { "vpipeseam", "VSeam-0" },
        };
    }
}
