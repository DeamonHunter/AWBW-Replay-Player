using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.API.Replay.Actions;
using Newtonsoft.Json.Linq;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Logging;

namespace AWBWApp.Game.API.New
{
    public class AWBWReplayParser
    {
        const string turn_start_text = "O:8:\"awbwGame\":";
        const string player_start_text = "O:10:\"awbwPlayer\":";
        const string building_start_text = "O:12:\"awbwBuilding\":";
        const string units_start_text = "O:8:\"awbwUnit\":";

        private static ReplayActionDatabase actionDatabase;

        public AWBWReplayParser()
        {
            actionDatabase = new ReplayActionDatabase();
        }

        public ReplayData ParseReplay(Stream archiveStream)
        {
            var zipArchive = new ZipArchive(archiveStream, ZipArchiveMode.Read);

            //As of 18/11/2021, replays contain 2 files. One is the gameID and the other has an 'a' appended to the start.
            if (zipArchive.Entries.Count != 2)
                throw new Exception("Cannot parse replay file, as it is either too old or invalid. (Archive does not contain 2 files.)");

            string gameStateFile = null;
            string replayFile = null;

            foreach (var entry in zipArchive.Entries)
            {
                string text;

                using (var entryStream = new GZipStream(entry.Open(), CompressionMode.Decompress))
                    using (var sr = new StreamReader(entryStream))
                        text = sr.ReadToEnd();

                if (entry.Name.StartsWith("a"))
                    replayFile = text;
                else
                    gameStateFile = text;
            }

            var state = ReadBaseReplayData(ref gameStateFile);
            ReadReplayActions(state, ref replayFile);
            return state;
        }

        ReplayData ReadBaseReplayData(ref string text)
        {
            var replayData = new ReplayData();

            var textIndex = 0;

            while (true)
            {
                ReadTurn(replayData, ref text, ref textIndex, textIndex == 0);

                if (text[textIndex++] != '\n' || textIndex >= text.Length)
                    break;
            }

            return replayData;
        }

        void ReadTurn(ReplayData replayData, ref string text, ref int textIndex, bool firstTurn)
        {
            if (text.Substring(textIndex, turn_start_text.Length) != turn_start_text)
                throw new Exception("Game State file does not start correctly.");
            textIndex += turn_start_text.Length;

            var entriesCount = ReadNextLength(ref text, ref textIndex);

            var newTurn = new TurnData();
            replayData.TurnData.Add(newTurn);

            if (text[textIndex++] != '{')
                throw new Exception("Game State file does not start correctly.");

            for (int i = 0; i < entriesCount; i++)
            {
                var entry = ReadString(ref text, ref textIndex);

                switch (entry)
                {
                    case "players":
                    {
                        ReadPlayers(ref text, ref textIndex, replayData, newTurn, firstTurn);
                        break;
                    }

                    case "buildings":
                    {
                        ReadBuildings(ref text, ref textIndex, newTurn, firstTurn);
                        break;
                    }

                    case "units":
                    {
                        ReadUnits(ref text, ref textIndex, newTurn, firstTurn);
                        break;
                    }

                    case "id":
                    {
                        var id = ReadInteger(ref text, ref textIndex);
                        if (!firstTurn && replayData.GameData.ID != id)
                            throw new Exception("Data 'ID' changed per turn when not expected.");
                        replayData.GameData.ID = id;
                        break;
                    }

                    case "name":
                    {
                        var name = ReadString(ref text, ref textIndex);
                        if (!firstTurn && replayData.GameData.Name != name)
                            throw new Exception("Data 'Name' changed per turn when not expected.");
                        replayData.GameData.Name = name;
                        break;
                    }

                    case "password":
                    {
                        var password = ReadString(ref text, ref textIndex);
                        if (!firstTurn && replayData.GameData.Password != password)
                            throw new Exception("Data 'Password' changed per turn when not expected.");
                        replayData.GameData.Password = password;
                        break;
                    }

                    case "creator":
                    {
                        var creator = ReadInteger(ref text, ref textIndex);
                        if (!firstTurn && replayData.GameData.CreatorId != creator)
                            throw new Exception("Data 'CreatorId' changed per turn when not expected.");
                        replayData.GameData.CreatorId = creator;
                        break;
                    }

                    case "maps_id":
                    {
                        var mapId = ReadInteger(ref text, ref textIndex);
                        if (!firstTurn && replayData.GameData.MapId != mapId)
                            throw new Exception("Data 'MapId' changed per turn when not expected.");
                        replayData.GameData.MapId = mapId;
                        break;
                    }

                    case "funds":
                    {
                        var funds = ReadInteger(ref text, ref textIndex);
                        if (!firstTurn && replayData.GameData.FundsPerBuilding != funds)
                            throw new Exception("Data 'FundsPerBuilding' changed per turn when not expected.");
                        replayData.GameData.FundsPerBuilding = funds;
                        break;
                    }

                    case "starting_funds":
                    {
                        var funds = ReadInteger(ref text, ref textIndex);
                        if (!firstTurn && replayData.GameData.StartingFunds != funds)
                            throw new Exception("Data 'StartingFunds' changed per turn when not expected.");
                        replayData.GameData.StartingFunds = funds;
                        break;
                    }

                    case "fog":
                    {
                        var fog = ReadBool(ref text, ref textIndex);
                        if (!firstTurn && replayData.GameData.Fog != fog)
                            throw new Exception("Data 'Fog' changed per turn when not expected.");
                        replayData.GameData.Fog = fog;
                        break;
                    }

                    case "use_powers":
                    {
                        var powersAvaliable = ReadBool(ref text, ref textIndex);
                        if (!firstTurn && replayData.GameData.PowersAllowed != powersAvaliable)
                            throw new Exception("Data 'PowersAllowed' changed per turn when not expected.");
                        replayData.GameData.PowersAllowed = powersAvaliable;
                        break;
                    }

                    case "official":
                    {
                        var official = ReadBool(ref text, ref textIndex);
                        if (!firstTurn && replayData.GameData.OfficialGame != official)
                            throw new Exception("Data 'OfficialGame' changed per turn when not expected.");
                        replayData.GameData.OfficialGame = official;
                        break;
                    }

                    case "league":
                    {
                        var leagueMatch = ReadString(ref text, ref textIndex);
                        if (!firstTurn && replayData.GameData.LeagueMatch != leagueMatch)
                            throw new Exception("Data 'LeagueMatch' changed per turn when not expected.");
                        replayData.GameData.LeagueMatch = leagueMatch;
                        break;
                    }

                    case "turn":
                    {
                        newTurn.PlayerID = ReadInteger(ref text, ref textIndex);
                        break;
                    }

                    case "day":
                    {
                        newTurn.Day = ReadInteger(ref text, ref textIndex);
                        break;
                    }

                    case "start_date":
                    {
                        var startDate = ReadString(ref text, ref textIndex);
                        if (!firstTurn && replayData.GameData.StartDate != startDate)
                            throw new Exception("Data 'StartDate' changed per turn when not expected.");
                        replayData.GameData.StartDate = startDate;
                        break;
                    }

                    case "end_date":
                    {
                        var startDate = ReadString(ref text, ref textIndex);
                        if (!firstTurn && replayData.GameData.EndDate != startDate)
                            throw new Exception("Data 'EndDate' changed per turn when not expected.");
                        replayData.GameData.EndDate = startDate;
                        break;
                    }

                    case "activity_date":
                    {
                        /*
                        var startDate = ReadString(ref text, ref textIndex);
                        if (!firstTurn && replayData.GameData.ActivityDate != startDate)
                            throw new Exception("Data 'ActivityDate' changed per turn when not expected.");
                        replayData.GameData.ActivityDate = startDate;
                        break;
                        */

                        //We likely don't care about the date things happened.
                        ReadString(ref text, ref textIndex);
                        break;
                    }

                    //Todo: Figure out how we will handle weather

                    case "weather_type":
                    case "weather_start":
                    case "weather_code":
                    case "win_condition":
                    case "active":
                    case "comment":
                    case "type":
                    case "max_rating":
                    case "team":
                    case "aet_date":
                    {
                        var value = ReadString(ref text, ref textIndex);
                        Logger.Log($"Replay contained known but incomplete string parameter: {entry}");
                        break;
                    }

                    case "boot_interval":
                    case "min_rating":
                    case "capture_win":
                    case "aet_interval":
                    case "timers_initial":
                    case "timers_increment":
                    case "timers_max_turn":
                    {
                        var value = ReadInteger(ref text, ref textIndex);
                        Logger.Log($"Replay contained known but incomplete int parameter: {entry}");
                        break;
                    }

                    default:
                        throw new Exception($"Replay contained unknown entry: {entry}");
                }
            }

            if (text[textIndex++] != '}')
                throw new Exception("Player data does not end correctly.");
        }

        void ReadPlayers(ref string text, ref int textIndex, ReplayData data, TurnData turnData, bool firstTurn)
        {
            if (text[textIndex++] != 'a')
                throw new Exception("Expected an array declaration for player data.");
            if (text[textIndex++] != ':')
                throw new Exception("Expected an array declaration for player data.");

            var numberOfPlayers = ReadNextLength(ref text, ref textIndex);

            if (firstTurn)
            {
                data.GameData.Players = new AWBWReplayPlayer[numberOfPlayers];
                data.GameData.PlayerIds = new Dictionary<int, int>();
            }
            else if (data.GameData.Players.Length != numberOfPlayers)
                throw new Exception("Number of players changed?");

            turnData.Players = new AWBWReplayPlayerTurn[numberOfPlayers];

            if (text[textIndex++] != '{')
                throw new Exception("Expected an array start for player data.");

            for (int i = 0; i < numberOfPlayers; i++)
            {
                var playerIndex = ReadInteger(ref text, ref textIndex);

                if (text.Substring(textIndex, player_start_text.Length) != player_start_text)
                    throw new Exception("Player data does not start correctly.");
                textIndex += player_start_text.Length;

                var paramerterCount = ReadNextLength(ref text, ref textIndex);

                AWBWReplayPlayer playerData;

                if (data.GameData.Players[playerIndex] == null)
                {
                    playerData = new AWBWReplayPlayer();
                    data.GameData.Players[playerIndex] = playerData;
                }
                else
                    playerData = data.GameData.Players[playerIndex];

                var playerDataTurn = new AWBWReplayPlayerTurn();
                turnData.Players[playerIndex] = playerDataTurn;

                if (text[textIndex++] != '{')
                    throw new Exception("Player data does not start correctly.");

                for (int j = 0; j < paramerterCount; j++)
                {
                    var entry = ReadString(ref text, ref textIndex);

                    switch (entry)
                    {
                        case "id":
                        {
                            var id = ReadInteger(ref text, ref textIndex);
                            if (!firstTurn && playerData.ID != id)
                                throw new Exception("Player 'id' changed per turn when not expected.");
                            playerData.ID = id;
                            playerDataTurn.ID = id;
                            data.GameData.PlayerIds[id] = playerIndex;
                            break;
                        }

                        case "users_id":
                        {
                            var id = ReadInteger(ref text, ref textIndex);
                            if (!firstTurn && playerData.UserId != id)
                                throw new Exception("Player 'users_id' changed per turn when not expected.");
                            playerData.UserId = id;
                            break;
                        }

                        case "countries_id":
                        {
                            var id = ReadInteger(ref text, ref textIndex);
                            if (!firstTurn && playerData.CountryId != id)
                                throw new Exception("Player 'countries_id' changed per turn when not expected.");
                            playerData.CountryId = id;
                            break;
                        }

                        case "co_id":
                        {
                            var id = ReadInteger(ref text, ref textIndex);
                            if (!firstTurn && playerData.COId != id)
                                throw new Exception("Player 'co_id' changed per turn when not expected.");
                            playerData.COId = id;
                            break;
                        }

                        //These are likely turn by turn
                        case "funds":
                        {
                            var funds = ReadInteger(ref text, ref textIndex);
                            playerDataTurn.Funds = funds;
                            break;
                        }

                        case "eliminated":
                        {
                            var eliminated = ReadBool(ref text, ref textIndex);
                            playerDataTurn.Eliminated = eliminated;
                            break;
                        }

                        case "co_power":
                        {
                            var powerPoints = ReadInteger(ref text, ref textIndex);
                            playerDataTurn.COPower = powerPoints;
                            break;
                        }

                        case "co_power_on":
                        {
                            var powerActive = ReadString(ref text, ref textIndex);
                            playerDataTurn.COPowerOn = powerActive;
                            break;
                        }

                        case "order":
                        {
                            var turnIndex = ReadInteger(ref text, ref textIndex);
                            if (!firstTurn && playerData.TurnOrderIndex != turnIndex)
                                throw new Exception("Player 'order' changed per turn when not expected.");
                            playerData.TurnOrderIndex = turnIndex;
                            break;
                        }

                        case "turn":
                        case "email":
                        case "uniq_id":
                        case "last_read":
                        case "last_read_broadcasts":
                        case "emailpress":
                        case "signature":
                        case "accept_draw":
                        case "co_image":
                        case "team":
                        case "turn_start":
                        case "tags_co_id":
                        case "tags_co_power":
                        case "tags_co_max_power":
                        case "tags_co_max_spower":
                        case "interface":
                        {
                            var value = ReadString(ref text, ref textIndex);
                            Logger.Log($"Replay contained known but incomplete string parameter: {entry}");
                            break;
                        }

                        case "boot_interval":
                        case "co_max_power":
                        case "co_max_spower":
                        case "aet_count":
                        case "turn_clock":
                        {
                            var value = ReadInteger(ref text, ref textIndex);
                            Logger.Log($"Replay contained known but incomplete int parameter: {entry}");
                            break;
                        }

                        case "games_id":
                        {
                            ReadInteger(ref text, ref textIndex);
                            break;
                        }

                        default:
                            throw new Exception($"Replay player data contained unknown entry: {entry}");
                    }
                }

                if (text[textIndex++] != '}')
                    throw new Exception("Player data does not end correctly.");
            }

            if (text[textIndex++] != '}')
                throw new Exception("Player data does not end correctly.");
        }

        void ReadBuildings(ref string text, ref int textIndex, TurnData data, bool firstTurn)
        {
            if (text[textIndex++] != 'a')
                throw new Exception("Expected an array declaration for building data.");
            if (text[textIndex++] != ':')
                throw new Exception("Expected an array declaration for building data.");

            var numberOfBuildings = ReadNextLength(ref text, ref textIndex);

            data.Buildings = new Dictionary<Vector2I, ReplayBuilding>();
            if (text[textIndex++] != '{')
                throw new Exception("Expected an array start for building data.");

            for (int i = 0; i < numberOfBuildings; i++)
            {
                var buildingIndex = ReadInteger(ref text, ref textIndex); //Likely not needed
                if (text.Substring(textIndex, building_start_text.Length) != building_start_text)
                    throw new Exception("Building data does not start correctly.");
                textIndex += building_start_text.Length;

                var parameterCount = ReadNextLength(ref text, ref textIndex);

                if (text[textIndex++] != '{')
                    throw new Exception("Building data does not start correctly.");

                var building = new ReplayBuilding();

                for (int j = 0; j < parameterCount; j++)
                {
                    var entry = ReadString(ref text, ref textIndex);

                    switch (entry)
                    {
                        case "id":
                        {
                            var id = ReadInteger(ref text, ref textIndex);
                            building.ID = id;
                            break;
                        }

                        case "terrain_id":
                        {
                            var id = ReadInteger(ref text, ref textIndex);
                            building.TerrainID = id;
                            break;
                        }

                        case "x":
                        {
                            var posX = ReadInteger(ref text, ref textIndex);
                            building.Position.X = posX;
                            break;
                        }

                        case "y":
                        {
                            var posY = ReadInteger(ref text, ref textIndex);
                            building.Position.Y = posY;
                            break;
                        }

                        case "capture":
                        {
                            var capture = ReadInteger(ref text, ref textIndex);
                            building.Capture = capture;
                            break;
                        }

                        case "last_capture":
                        {
                            var capture = ReadInteger(ref text, ref textIndex);
                            building.LastCapture = capture;
                            break;
                        }

                        case "games_id":
                        {
                            //We do not need this value, this was likely added to make database reading easier for AWBW
                            ReadInteger(ref text, ref textIndex);
                            break;
                        }

                        case "last_updated":
                        {
                            //Unneeded data containing the time at which the building was last updated.
                            ReadString(ref text, ref textIndex);
                            break;
                        }

                        default:
                            throw new Exception($"Replay building data contained unknown entry: {entry}");
                    }
                }

                data.Buildings.Add(building.Position, building);
                if (text[textIndex++] != '}')
                    throw new Exception("Player data does not end correctly.");
            }
            if (text[textIndex++] != '}')
                throw new Exception("Player data does not end correctly.");
        }

        void ReadUnits(ref string text, ref int textIndex, TurnData data, bool firstTurn)
        {
            if (text[textIndex++] != 'a')
                throw new Exception("Expected an array declaration for unit data.");
            if (text[textIndex++] != ':')
                throw new Exception("Expected an array declaration for unit data.");

            var numberOfUnits = ReadNextLength(ref text, ref textIndex);

            data.ReplayUnit = new Dictionary<long, ReplayUnit>();
            if (text[textIndex++] != '{')
                throw new Exception("Expected an array start for unit data.");

            for (int i = 0; i < numberOfUnits; i++)
            {
                var unitIndex = ReadInteger(ref text, ref textIndex); //Likely not needed
                if (text.Substring(textIndex, units_start_text.Length) != units_start_text)
                    throw new Exception("Unit data does not start correctly.");
                textIndex += units_start_text.Length;

                var parameterCount = ReadNextLength(ref text, ref textIndex);

                if (text[textIndex++] != '{')
                    throw new Exception("Unit data does not start correctly.");

                var unit = new ReplayUnit();

                for (int j = 0; j < parameterCount; j++)
                {
                    var entry = ReadString(ref text, ref textIndex);

                    switch (entry)
                    {
                        case "id":
                        {
                            var id = ReadInteger(ref text, ref textIndex);
                            unit.ID = id;
                            break;
                        }

                        case "players_id":
                        {
                            var id = ReadInteger(ref text, ref textIndex);
                            unit.PlayerID = id;
                            break;
                        }

                        case "name":
                        {
                            var name = ReadString(ref text, ref textIndex);
                            unit.UnitName = name;
                            break;
                        }

                        case "movement_points":
                        {
                            var points = ReadInteger(ref text, ref textIndex);
                            unit.MovementPoints = points;
                            break;
                        }

                        case "vision":
                        {
                            var range = ReadInteger(ref text, ref textIndex);
                            unit.Vision = range;
                            break;
                        }

                        case "fuel":
                        {
                            var range = ReadInteger(ref text, ref textIndex);
                            unit.Fuel = range;
                            break;
                        }

                        case "fuel_per_turn":
                        {
                            var usage = ReadInteger(ref text, ref textIndex);
                            unit.FuelPerTurn = usage;
                            break;
                        }

                        case "sub_dive":
                        {
                            var dived = ReadBool(ref text, ref textIndex);
                            unit.SubHasDived = dived;
                            break;
                        }

                        case "ammo":
                        {
                            var count = ReadInteger(ref text, ref textIndex);
                            unit.Ammo = count;
                            break;
                        }

                        case "short_range":
                        {
                            var range = ReadInteger(ref text, ref textIndex);
                            var value = unit.Range.GetValueOrDefault();
                            value.X = range;
                            unit.Range = value;
                            break;
                        }

                        case "long_range":
                        {
                            var range = ReadInteger(ref text, ref textIndex);
                            var value = unit.Range.GetValueOrDefault();
                            value.Y = range;
                            unit.Range = value;
                            break;
                        }

                        case "second_weapon":
                        {
                            var hasSecondWeapon = ReadBool(ref text, ref textIndex);
                            unit.SecondWeapon = hasSecondWeapon;
                            break;
                        }

                        case "cost":
                        {
                            var cost = ReadInteger(ref text, ref textIndex);
                            unit.Cost = cost;
                            break;
                        }

                        case "movement_type":
                        {
                            var type = ReadString(ref text, ref textIndex);
                            unit.MovementType = type;
                            break;
                        }

                        case "x":
                        {
                            var posX = ReadInteger(ref text, ref textIndex);
                            var value = unit.Position.GetValueOrDefault();
                            value.X = posX;
                            unit.Position = value;
                            break;
                        }

                        case "y":
                        {
                            var posY = ReadInteger(ref text, ref textIndex);
                            var value = unit.Position.GetValueOrDefault();
                            value.Y = posY;
                            unit.Position = value;
                            break;
                        }

                        case "moved":
                        {
                            var moved = ReadInteger(ref text, ref textIndex);
                            unit.TimesMoved = moved;
                            break;
                        }

                        case "capture":
                        {
                            var captured = ReadInteger(ref text, ref textIndex);
                            unit.TimesCaptured = captured;
                            break;
                        }

                        case "fired":
                        {
                            var fired = ReadInteger(ref text, ref textIndex);
                            unit.TimesFired = fired;
                            break;
                        }

                        case "hit_points":
                        {
                            var hp = ReadFloat(ref text, ref textIndex);
                            unit.HitPoints = hp;
                            break;
                        }

                        case "cargo1_units_id":
                        {
                            var carriedUnit = ReadInteger(ref text, ref textIndex);
                            if (carriedUnit == 0)
                                break;

                            if (unit.CargoUnits == null)
                                unit.CargoUnits = new List<int>();
                            unit.CargoUnits.Add(carriedUnit);
                            break;
                        }

                        case "cargo2_units_id":
                        {
                            var carriedUnit = ReadInteger(ref text, ref textIndex);
                            if (carriedUnit == 0)
                                break;

                            if (unit.CargoUnits == null)
                                unit.CargoUnits = new List<int>();
                            unit.CargoUnits.Add(carriedUnit);
                            break;
                        }

                        case "carried":
                        {
                            var carried = ReadBool(ref text, ref textIndex);
                            unit.BeingCarried = carried;
                            break;
                        }

                        case "games_id":
                        {
                            //We do not need this value, this was likely added to make database reading easier for AWBW
                            ReadInteger(ref text, ref textIndex);
                            break;
                        }

                        case "symbol":
                        {
                            //We do not need this value, this is basically a secondary code for a unit. (Alongside Unit name)
                            ReadString(ref text, ref textIndex);
                            break;
                        }

                        default:
                            throw new Exception($"Replay unit data contained unknown entry: {entry}");
                    }
                }
                if (!unit.Position.HasValue)
                    throw new Exception("improperly specified unit. Did not contain position data in turn sync update.");

                data.ReplayUnit.Add(unit.ID, unit);
                if (text[textIndex++] != '}')
                    throw new Exception("Player data does not end correctly.");
            }
            if (text[textIndex++] != '}')
                throw new Exception("Player data does not end correctly.");
        }

        void ReadReplayActions(ReplayData replayData, ref string text)
        {
            var textIndex = 0;

            while (true)
            {
                ReadReplayActionTurn(replayData, ref text, ref textIndex);

                if (text[textIndex++] != '\n' || textIndex >= text.Length)
                    break;
            }
        }

        void ReadReplayActionTurn(ReplayData replayData, ref string text, ref int textIndex)
        {
            if (text.Substring(textIndex, 2) != "p:")
                throw new Exception("Improper action turn start. Turn indicator misconfigured.");
            textIndex += 2;

            var startIndex = textIndex;

            while (true)
            {
                var character = text[textIndex++];
                if (character == ';')
                    break;
            }
            var number = text.Substring(startIndex, textIndex - startIndex - 1);
            var playerID = int.Parse(number);

            //Todo: Does this have any use?
            if (text.Substring(textIndex, 2) != "d:")
                throw new Exception("Improper action turn start. Day indicator misconfigured.");
            textIndex += 2;

            startIndex = textIndex;

            while (true)
            {
                var character = text[textIndex++];
                if (character == ';')
                    break;
            }
            number = text.Substring(startIndex, textIndex - startIndex - 1);
            var day = int.Parse(number);

            TurnData turnData = null;

            foreach (var turn in replayData.TurnData)
            {
                if (turn.PlayerID != playerID || turn.Day != day)
                    continue;

                turnData = turn;
                break;
            }

            if (turnData == null)
                throw new Exception("Replay actions contained an unknown turn id.");

            if (text.Substring(textIndex, 7) != "a:a:3:{")
                throw new Exception("Improper action turn start. Turn Array indicator misconfigured.");
            textIndex += 7;

            if (ReadInteger(ref text, ref textIndex) != 0)
                throw new Exception("Improper action turn start. 1st array member not in right format.");

            if (ReadInteger(ref text, ref textIndex) != playerID)
                throw new Exception("Inner array indicated the wrong player id.");

            if (ReadInteger(ref text, ref textIndex) != 1)
                throw new Exception("Improper action turn start. 2nd array member not in right format.");

            var playerTurnNumber = ReadInteger(ref text, ref textIndex);

            if (ReadInteger(ref text, ref textIndex) != 2)
                throw new Exception("Improper action turn start. 3rd array member not in right format.");

            if (text[textIndex++] != 'a')
                throw new Exception("Expected an array declaration for unit data.");
            if (text[textIndex++] != ':')
                throw new Exception("Expected an array declaration for unit data.");

            var actionCount = ReadNextLength(ref text, ref textIndex);
            turnData.Actions = new List<IReplayAction>();

            if (text[textIndex++] != '{')
                throw new Exception("Expected an array declaration for unit data.");

            for (int i = 0; i < actionCount; i++)
            {
                var index = ReadInteger(ref text, ref textIndex);
                var actionString = ReadString(ref text, ref textIndex);

                if (actionString == "Array")
                {
                    Logger.Log("Replay contained action 'Array' which is not an action.");
                    turnData.Actions.Add(new EmptyAction());
                    continue;
                }

                var jsonObject = JObject.Parse(actionString);

                if (index != i)
                    throw new Exception("Out of Order actions");

                turnData.Actions.Add(actionDatabase.ParseJObjectIntoReplayAction(jsonObject, replayData, turnData));
            }
            if (text[textIndex++] != '}')
                throw new Exception("Expected an array declaration for unit data.");
            if (text[textIndex++] != '}')
                throw new Exception("Expected an array declaration for unit data.");
        }

        //Todo: Read long maybe?
        int ReadNextLength(ref string text, ref int index)
        {
            var startIndex = index;

            while (true)
            {
                var character = text[index++];
                if (character == ':')
                    break;
            }
            var number = text.Substring(startIndex, index - startIndex - 1);
            return int.Parse(number);
        }

        string ReadString(ref string text, ref int index)
        {
            var type = text[index];
            index += 2;

            switch (type)
            {
                case 's':
                {
#if DEBUG
                    if (text[index - 1] != ':')
                        throw new Exception("String was badly formatted.");
#endif
                    var entryLength = ReadNextLength(ref text, ref index);
#if DEBUG
                    if (text[index] != '"')
                        throw new Exception("String was badly formatted.");
                    if (text[index + entryLength + 1] != '"')
                        throw new Exception("String was badly formatted.");
                    if (text[index + entryLength + 2] != ';')
                        throw new Exception("String was badly formatted.");
#endif
                    var entry = text.Substring(index + 1, entryLength);
                    index += entryLength + 3;
                    return entry;
                }

                case 'N':
                    return null;

                default:
                    throw new Exception("Unknown string kind: " + type);
            }
        }

        int ReadInteger(ref string text, ref int index)
        {
            if (text[index++] != 'i')
                throw new Exception("Was expecting a integer.");
            if (text[index++] != ':')
                throw new Exception("Integer was badly formatted.");

            var startIndex = index;

            while (true)
            {
                var character = text[index++];
                if (character == ';')
                    break;
            }
            var number = text.Substring(startIndex, index - startIndex - 1);
            return int.Parse(number);
        }

        float ReadFloat(ref string text, ref int index)
        {
            if (text[index++] != 'd')
                throw new Exception("Was expecting a integer.");
            if (text[index++] != ':')
                throw new Exception("Float was badly formatted.");

            var startIndex = index;

            while (true)
            {
                var character = text[index++];
                if (character == ';')
                    break;
            }
            var number = text.Substring(startIndex, index - startIndex - 1);
            return float.Parse(number);
        }

        bool ReadBool(ref string text, ref int index)
        {
            var stringData = ReadString(ref text, ref index);

            if (stringData == null)
                throw new Exception("Unable to handle null bool."); //Is this possible?

            if (stringData == "Y" || stringData == "y")
                return true;

            if (stringData == "N" || stringData == "n")
                return false;

            throw new Exception($"Unknown bool value: {stringData}");
        }
    }
}
