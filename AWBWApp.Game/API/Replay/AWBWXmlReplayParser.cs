using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using AWBWApp.Game.Game.COs;
using AWBWApp.Game.Game.Logic;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Logging;

namespace AWBWApp.Game.API.Replay
{
    public class AWBWXmlReplayParser
    {
        //We don't really want to pass Country, CO, Building and Unit storages to this as that complicates a bunch of stuff.
        //So we are redefining some things here to avoid that.

        #region ParsingInformation

        private Dictionary<string, int> countryNameToAWBWID = new Dictionary<string, int>
        {
            { "Orange Star", 1 },
            { "Blue Moon", 2 },
            { "Green Earth", 3 },
            { "Yellow Comet", 4 },
            { "Black Hole", 5 },
            { "Red Fire", 6 },
            { "Grey Sky", 7 },
            { "Brown Desert", 8 },
            { "Amber Blaze", 9 },
            { "Jade Sun", 10 },
            { "Cobalt Ice", 16 },
            { "Pink Cosmos", 17 },
            { "Teal Galaxy", 19 },
            { "Purple Lightning", 20 },
            { "Acid Rain", 21 },
            { "White Nova", 22 },
        };

        private Dictionary<string, int> countryShortNameToAWBWID = new Dictionary<string, int>
        {
            { "os", 1 },
            { "bm", 2 },
            { "ge", 3 },
            { "yc", 4 },
            { "bh", 5 },
            { "rf", 6 },
            { "gs", 7 },
            { "bd", 8 },
            { "ab", 9 },
            { "js", 10 },
            { "ci", 16 },
            { "pc", 17 },
            { "tg", 19 },
            { "pl", 20 },
            { "ar", 21 },
            { "wn", 22 },
        };

        private Dictionary<string, int> coNameToAWBWId = new Dictionary<string, int>
        {
            { "Andy", 1 },
            { "Grit", 2 },
            { "Kanbei", 3 },

            { "Drake", 5 },

            { "Max", 7 },
            { "Sami", 8 },
            { "Olaf", 9 },
            { "Eagle", 10 },
            { "Adder", 11 },
            { "Hawke", 12 },
            { "Sensei", 13 },
            { "Jess", 14 },
            { "Colin", 15 },
            { "Lash", 16 },
            { "Hachi", 17 },
            { "Sonja", 18 },
            { "Sasha", 19 },
            { "Grimm", 20 },
            { "Koal", 21 },
            { "Jake", 22 },
            { "Kindle", 23 },
            { "Nell", 24 },
            { "Flak", 25 },
            { "Jugger", 26 },
            { "Javier", 27 },
            { "Rachel", 28 },
            { "Sturm", 29 },
            { "Von Bolt", 30 },
            { "No CO", 31 },
        };

        private Dictionary<string, (bool, int)> buildingNames = new Dictionary<string, (bool, int)>()
        {
            { "neutralcity", (true, 34) },
            { "neutralbase", (true, 35) },
            { "neutralairport", (true, 36) },
            { "neutralport", (true, 37) },
            { "neutralcomtower", (true, 133) },
            { "neutrallab", (true, 145) },

            { "missilesilo", (true, 111) },
            { "missilesiloempty", (false, 112) },

            { "orangestarcity", (true, 38) },
            { "orangestarbase", (true, 39) },
            { "orangestarairport", (true, 40) },
            { "orangestarport", (true, 41) },
            { "orangestarhq", (true, 42) },
            { "orangestarcomtower", (true, 134) },
            { "orangestarlab", (true, 146) },

            { "bluemooncity", (true, 43) },
            { "bluemoonbase", (true, 44) },
            { "bluemoonairport", (true, 45) },
            { "bluemoonport", (true, 46) },
            { "bluemoonhq", (true, 47) },
            { "bluemooncomtower", (true, 129) },
            { "bluemoonlab", (true, 140) },

            { "greenearthcity", (true, 48) },
            { "greenearthbase", (true, 49) },
            { "greenearthairport", (true, 50) },
            { "greenearthport", (true, 51) },
            { "greenearthhq", (true, 52) },
            { "greenearthcomtower", (true, 131) },
            { "greenearthlab", (true, 142) },

            { "yellowcometcity", (true, 53) },
            { "yellowcometbase", (true, 54) },
            { "yellowcometairport", (true, 55) },
            { "yellowcometport", (true, 56) },
            { "yellowcomethq", (true, 57) },
            { "yellowcometcomtower", (true, 136) },
            { "yellowcometlab", (true, 148) },

            { "redfirecity", (true, 81) },
            { "redfirebase", (true, 82) },
            { "redfireairport", (true, 83) },
            { "redfireport", (true, 84) },
            { "redfirehq", (true, 85) },
            { "redfirecomtower", (true, 135) },
            { "redfirelab", (true, 147) },

            { "greyskycity", (true, 86) },
            { "greyskybase", (true, 87) },
            { "greyskyairport", (true, 88) },
            { "greyskyport", (true, 89) },
            { "greyskyhq", (true, 90) },
            { "greyskycomtower", (true, 137) },
            { "greyskylab", (true, 143) },

            { "blackholecity", (true, 91) },
            { "blackholebase", (true, 92) },
            { "blackholeairport", (true, 93) },
            { "blackholeport", (true, 94) },
            { "blackholehq", (true, 95) },
            { "blackholecomtower", (true, 128) },
            { "blackholelab", (true, 139) },

            { "browndesertcity", (true, 96) },
            { "browndesertbase", (true, 97) },
            { "browndesertairport", (true, 98) },
            { "browndesertport", (true, 99) },
            { "browndeserthq", (true, 100) },
            { "browndesertcomtower", (true, 130) },
            { "browndesertlab", (true, 141) },

            { "amberblazecity", (true, 119) },
            { "amberblazebase", (true, 118) },
            { "amberblazeairport", (true, 117) },
            { "amberblazeport", (true, 121) },
            { "amberblazehq", (true, 120) },
            { "amberblazecomtower", (true, 127) },
            { "amberblazelab", (true, 138) },

            { "jadesuncity", (true, 124) },
            { "jadesunbase", (true, 123) },
            { "jadesunairport", (true, 122) },
            { "jadesunport", (true, 126) },
            { "jadesunhq", (true, 125) },
            { "jadesuncomtower", (true, 132) },
            { "jadesunlab", (true, 144) },

            { "cobalticecity", (true, 151) },
            { "cobalticebase", (true, 150) },
            { "cobalticeairport", (true, 149) },
            { "cobalticeport", (true, 155) },
            { "cobalticehq", (true, 153) },
            { "cobalticecomtower", (true, 152) },
            { "cobalticelab", (true, 154) },

            { "pinkcosmoscity", (true, 158) },
            { "pinkcosmosbase", (true, 157) },
            { "pinkcosmosairport", (true, 156) },
            { "pinkcosmosport", (true, 162) },
            { "pinkcosmoshq", (true, 160) },
            { "pinkcosmoscomtower", (true, 159) },
            { "pinkcosmoslab", (true, 161) },

            { "tealgalaxycity", (true, 165) },
            { "tealgalaxybase", (true, 164) },
            { "tealgalaxyairport", (true, 163) },
            { "tealgalaxyport", (true, 169) },
            { "tealgalaxyhq", (true, 167) },
            { "tealgalaxycomtower", (true, 166) },
            { "tealgalaxylab", (true, 168) },

            { "purplelightningcity", (true, 172) },
            { "purplelightningbase", (true, 171) },
            { "purplelightningairport", (true, 170) },
            { "purplelightningport", (true, 176) },
            { "purplelightninghq", (true, 174) },
            { "purplelightningcomtower", (true, 173) },
            { "purplelightninglab", (true, 175) },

            { "acidraincity", (true, 183) },
            { "acidrainbase", (true, 182) },
            { "acidrainairport", (true, 181) },
            { "acidrainport", (true, 187) },
            { "acidrainhq", (true, 185) },
            { "acidraincomtower", (true, 184) },
            { "acidrainlab", (true, 186) },

            { "whitenovacity", (true, 190) },
            { "whitenovabase", (true, 189) },
            { "whitenovaairport", (true, 188) },
            { "whitenovaport", (true, 194) },
            { "whitenovahq", (true, 192) },
            { "whitenovacomtower", (true, 191) },
            { "whitenovalab", (true, 193) },
        };

        private Dictionary<string, string> unitNames = new Dictionary<string, string>
        {
            { "infantry", "Infantry" },
            { "mech", "Mech" },
            { "md.tank", "Md.Tank" },
            { "tank", "Tank" },
            { "recon", "Recon" },
            { "apc", "APC" },
            { "artillery", "Artillery" },
            { "anti-air", "Anti-Air" },
            { "missile", "Missile" },
            { "fighter", "Fighter" },
            { "bomber", "Bomber" },
            { "b-copter", "B-Copter" },
            { "t-copter", "T-Copter" },
            { "battleship", "Battleship" },
            { "cruiser", "Cruiser" },
            { "lander", "Lander" },
            { "sub", "Sub" },
            { "blackboat", "Black Boat" },
            { "carrier", "Carrier" },
            { "stealth", "Stealth" },
            { "neotank", "Neotank" },
            { "piperunner", "Piperunner" },
            { "blackbomb", "Black Bomb" },
            { "megatank", "Mega Tank" },
        };

        #endregion

        public ReplayData ParseReplayFile(Stream fileStream)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            string gameStateFile = null;

            try
            {
                using (var entryStream = new GZipStream(fileStream, CompressionMode.Decompress))
                {
                    using (var sr = new StreamReader(entryStream))
                        gameStateFile = sr.ReadToEnd();
                }
            }
            catch (InvalidDataException)
            {
                throw new Exception("Failed to open replay as a GZip file. Are you sure this is a replay?");
            }

            var xml = new XmlDocument();
            xml.LoadXml(gameStateFile);

            var state = readReplayData(xml);
            state.ReplayInfo.ReplayVersion = 0;

            stopWatch.Stop();
            Logger.Log("Replay parsing took: " + stopWatch.Elapsed);
            return state;
        }

        private ReplayData readReplayData(XmlDocument document)
        {
            if (document?.DocumentElement == null)
                throw new Exception("Improperly defined xml document.");

            var replayData = new ReplayData();

            foreach (XmlNode node in document.DocumentElement.ChildNodes)
            {
                switch (node.Name)
                {
                    default:
                        throw new Exception($"Unknown base xml entry: {node.Name}");

                    case "FormatVersion":
                    {
                        if (!int.TryParse(node.InnerText, out var version) || version != 3)
                            throw new Exception($"Unknown format version for xml: {node.Value}");

                        break;
                    }

                    case "Replay":
                    {
                        foreach (XmlNode replayNode in node.ChildNodes)
                        {
                            switch (replayNode.Name)
                            {
                                default:
                                    throw new Exception($"Unknown Replay xml entry: {replayNode.Name}");

                                case "Game":
                                    readXMLGameData(replayData, replayNode);
                                    break;

                                case "Turns":
                                    readXMLTurnData(replayData, replayNode);
                                    break;
                            }
                        }

                        break;
                    }
                }
            }

            return replayData;
        }

        private void readXMLGameData(ReplayData data, XmlNode gameNode)
        {
            data.ReplayInfo.Players = new Dictionary<long, ReplayUser>();

            foreach (XmlNode node in gameNode.ChildNodes)
            {
                switch (node.Name)
                {
                    default:
                        throw new Exception($"Unknown Game xml entry: {node.Name}");

                    case "Id":
                        data.ReplayInfo.ID = long.Parse(node.InnerText);
                        break;

                    case "Name":
                        data.ReplayInfo.Name = node.InnerText;
                        break;

                    case "Map":
                        //Todo: Does this map exist.

                        data.ReplayInfo.MapId = long.Parse(node.SelectSingleNode("Id")!.InnerText);
                        break;

                    case "PlayerInfos":
                    {
                        foreach (XmlNode playerNode in node.ChildNodes)
                        {
                            var user = new ReplayUser();

                            // Ids are not provided
                            user.ID = data.ReplayInfo.Players.Count;
                            user.UserId = -1;

                            user.Username = playerNode.SelectSingleNode("Player")!.InnerText;

                            user.CountryID = countryNameToAWBWID[playerNode.SelectSingleNode("Country")!.InnerText];
                            user.RoundOrder = user.CountryID;
                            user.COsUsedByPlayer.Add(coNameToAWBWId[playerNode.SelectSingleNode("CO")!.InnerText]);

                            data.ReplayInfo.Players.Add(user.ID, user);
                        }

                        break;
                    }

                    case "GameStats":
                        break; //Game Stats is kinda not necessary
                }
            }
        }

        private void readXMLTurnData(ReplayData data, XmlNode turnsNode)
        {
            var unitId = 0;

            foreach (XmlNode node in turnsNode.ChildNodes)
            {
                int turnIndex = -1;

                var turnData = new TurnData();
                turnData.ReplayUnit = new Dictionary<long, ReplayUnit>();
                turnData.Buildings = new Dictionary<Vector2I, ReplayBuilding>();
                turnData.Players = new Dictionary<long, ReplayUserTurn>();

                foreach (XmlNode innerNode in node.ChildNodes)
                {
                    switch (innerNode.Name)
                    {
                        default:
                            throw new Exception($"Unknown Game xml entry: {innerNode.Name}");

                        case "TurnIndex":
                            turnIndex = int.Parse(innerNode.InnerText);
                            break;

                        case "Day":
                            turnData.Day = int.Parse(innerNode.InnerText);
                            break;

                        case "Weather":
                            turnData.StartWeather = new ReplayWeather { Type = Enum.Parse<Weather>(innerNode.InnerText) };
                            break;

                        case "EntityInfos":
                        {
                            var lastTurn = turnIndex - 1 < data.TurnData.Count && turnIndex > 0 ? data.TurnData[turnIndex - 1] : null;

                            foreach (XmlNode entityNode in innerNode.ChildNodes)
                            {
                                var x = int.Parse(entityNode.SelectSingleNode("X")!.InnerText);
                                var y = int.Parse(entityNode.SelectSingleNode("Y")!.InnerText);

                                var position = new Vector2I(x, y);

                                var name = entityNode.SelectSingleNode("Name");

                                if (name!.InnerText == "capture")
                                {
                                    if (turnData.Buildings[position].Capture == 0)
                                        turnData.Buildings[position].Capture = 20;
                                    else
                                        turnData.Buildings[position].Capture = 10;
                                    continue;
                                }

                                if (int.TryParse(name!.InnerText, out var health))
                                {
                                    var unit = turnData.ReplayUnit.First(unit => unit.Value.Position == position && (!unit.Value.BeingCarried.HasValue || unit.Value.BeingCarried == false));
                                    unit.Value.HitPoints = health;
                                    continue;
                                }

                                //Figure out if this is a unit or a building.

                                var code = name.InnerText[..2];

                                if (countryShortNameToAWBWID.TryGetValue(code, out var countryID))
                                {
                                    var replayUnit = new ReplayUnit();
                                    replayUnit.ID = unitId++;
                                    replayUnit.UnitName = unitNames[name.InnerText[2..]];
                                    replayUnit.HitPoints = 10;
                                    replayUnit.Position = position;
                                    replayUnit.PlayerID = data.ReplayInfo.Players.First(p => p.Value.CountryID == countryID).Value.ID;

                                    replayUnit.TimesMoved = 0;
                                    replayUnit.Fuel = 99;
                                    replayUnit.Ammo = 99;

                                    turnData.ReplayUnit.Add(replayUnit.ID, replayUnit);
                                    continue;
                                }

                                if (buildingNames.TryGetValue(name.InnerText, out var buildingID))
                                {
                                    //Is this a true building or a fake one.
                                    if (buildingID.Item1)
                                    {
                                        var replayBuilding = new ReplayBuilding();
                                        replayBuilding.ID = position.Y * 1000 + position.X;
                                        replayBuilding.Capture = 20;
                                        replayBuilding.LastCapture = 20;
                                        replayBuilding.TerrainID = buildingID.Item2;
                                        replayBuilding.Position = position;
                                        turnData.Buildings.Add(position, replayBuilding);
                                    }
                                    continue;
                                }

                                throw new Exception($"Unknown name: {name.InnerText}");
                            }

                            if (lastTurn != null)
                            {
                                foreach (var building in turnData.Buildings)
                                {
                                    var lastTurnBuilding = lastTurn.Buildings[building.Key];

                                    if (lastTurnBuilding.Capture != 20)
                                    {
                                        if (lastTurnBuilding.TerrainID != building.Value.TerrainID)
                                            building.Value.Capture = 20;
                                        else
                                        {
                                            var unitAbove = turnData.ReplayUnit.FirstOrDefault(unit => unit.Value.Position == building.Value.Position);
                                            var lastUnitAbove = lastTurn.ReplayUnit.FirstOrDefault(unit => unit.Value.Position == building.Value.Position);

                                            if (unitAbove.Value?.UnitName != lastUnitAbove.Value?.UnitName || unitAbove.Value?.PlayerID != lastUnitAbove.Value?.PlayerID)
                                                building.Value.Capture = 20;
                                            else
                                                building.Value.Capture = 10;
                                        }
                                    }
                                }
                            }

                            break;
                        }

                        case "PlayerInfos":
                        {
                            foreach (XmlNode playerNode in innerNode.ChildNodes)
                            {
                                var playerName = playerNode.SelectSingleNode("Player")!.InnerText;

                                var playerData = data.ReplayInfo.Players.First(p => p.Value.Username == playerName);

                                var playerTurn = new ReplayUserTurn();
                                playerTurn.ActiveCOID = playerData.Value.COsUsedByPlayer.First();

                                foreach (XmlNode playerInfoNode in playerNode.SelectSingleNode("TurnInfo"))
                                {
                                    switch (playerInfoNode.Name)
                                    {
                                        case "Funds":
                                            playerTurn.Funds = int.Parse(playerInfoNode.InnerText);
                                            break;

                                        case "HasLost":
                                            playerTurn.Eliminated = bool.Parse(playerInfoNode.InnerText);

                                            if (playerTurn.Eliminated && !playerData.Value.EliminatedOn.HasValue)
                                                playerData.Value.EliminatedOn = turnIndex;

                                            break;

                                        case "IsActive":
                                            if (bool.Parse(playerInfoNode.InnerText))
                                                turnData.ActivePlayerID = playerData.Value.ID;
                                            break;

                                        case "PowerBarChargePercentage":
                                            playerTurn.PowerPercentage = double.Parse(playerInfoNode.InnerText) / 100;
                                            break;

                                        case "PowerType":
                                            if (playerInfoNode.InnerText.ToLower() == "scop")
                                                playerTurn.COPowerOn = ActiveCOPower.Super;
                                            else if (playerInfoNode.InnerText.ToLower() == "cop")
                                                playerTurn.COPowerOn = ActiveCOPower.Normal;
                                            else
                                                playerTurn.COPowerOn = ActiveCOPower.None;

                                            break;

                                        case "Income":
                                            break; //Auto calculated
                                    }
                                }

                                turnData.Players.Add(playerData.Key, playerTurn);
                            }

                            break;
                        }

                        case "DisplayedStuff":
                            break; //DisplayedStuff is not necessary as it is telling where to render things.

                        case "LoggedActions":
                            break; //logged actions doesn't really help us as we can't recreate the actions from this.
                    }
                }

                for (int i = data.TurnData.Count; i <= turnIndex; i++)
                    data.TurnData.Add(null);

                data.TurnData[turnIndex] = turnData;
            }
        }
    }
}
