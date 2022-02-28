using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.API.Replay.Actions;
using AWBWApp.Game.Game.Logic;
using Newtonsoft.Json.Linq;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Logging;
using MatchType = AWBWApp.Game.API.Replay.MatchType;

namespace AWBWApp.Game.API.New
{
    //Todo: This would potentially be faster/cleaner using a memory stream or similar instead of reading everything into a string
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
            var stopWatch = new Stopwatch();
            stopWatch.Start();
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
            stopWatch.Stop();
            Logger.Log("Replay parsing took: " + stopWatch.Elapsed);
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

            var entriesCount = readNextLength(ref text, ref textIndex);

            var newTurn = new TurnData();
            replayData.TurnData.Add(newTurn);

            if (text[textIndex++] != '{')
                throw new Exception("Game State file does not start correctly.");

            #region TurnData parsing

            for (int i = 0; i < entriesCount; i++)
            {
                var entry = readStringWithoutUnicode(ref text, ref textIndex);

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
                        if (!firstTurn && replayData.ReplayInfo.ID != id)
                            throw new Exception("Data 'ID' changed per turn when not expected.");
                        replayData.ReplayInfo.ID = id;
                        break;
                    }

                    case "name":
                    {
                        var name = readString(ref text, ref textIndex);
                        if (!firstTurn && replayData.ReplayInfo.Name != name)
                            throw new Exception("Data 'Name' changed per turn when not expected.");
                        replayData.ReplayInfo.Name = name;
                        break;
                    }

                    case "password":
                    {
                        var password = readString(ref text, ref textIndex);
                        if (!firstTurn && replayData.ReplayInfo.Password != password)
                            throw new Exception("Data 'Password' changed per turn when not expected.");
                        replayData.ReplayInfo.Password = password;
                        break;
                    }

                    case "creator":
                    {
                        var creator = ReadInteger(ref text, ref textIndex);
                        if (!firstTurn && replayData.ReplayInfo.CreatorId != creator)
                            throw new Exception("Data 'CreatorId' changed per turn when not expected.");
                        replayData.ReplayInfo.CreatorId = creator;
                        break;
                    }

                    case "maps_id":
                    {
                        var mapId = ReadInteger(ref text, ref textIndex);
                        if (!firstTurn && replayData.ReplayInfo.MapId != mapId)
                            throw new Exception("Data 'MapId' changed per turn when not expected.");
                        replayData.ReplayInfo.MapId = mapId;
                        break;
                    }

                    case "funds":
                    {
                        var funds = ReadInteger(ref text, ref textIndex);
                        if (!firstTurn && replayData.ReplayInfo.FundsPerBuilding != funds)
                            throw new Exception("Data 'FundsPerBuilding' changed per turn when not expected.");
                        replayData.ReplayInfo.FundsPerBuilding = funds;
                        break;
                    }

                    case "starting_funds":
                    {
                        var funds = ReadInteger(ref text, ref textIndex);
                        if (!firstTurn && replayData.ReplayInfo.StartingFunds != funds)
                            throw new Exception("Data 'StartingFunds' changed per turn when not expected.");
                        replayData.ReplayInfo.StartingFunds = funds;
                        break;
                    }

                    case "weather_type":
                    {
                        var value = readString(ref text, ref textIndex);

                        if (!firstTurn && replayData.ReplayInfo.WeatherType != value)
                            throw new Exception("Data 'WeatherType' changed per turn when not expected.");

                        replayData.ReplayInfo.WeatherType = value;
                        break;
                    }

                    case "fog":
                    {
                        var fog = ReadBool(ref text, ref textIndex);
                        if (!firstTurn && replayData.ReplayInfo.Fog != fog)
                            throw new Exception("Data 'Fog' changed per turn when not expected.");
                        replayData.ReplayInfo.Fog = fog;
                        break;
                    }

                    case "use_powers":
                    {
                        var powersAvaliable = ReadBool(ref text, ref textIndex);
                        if (!firstTurn && replayData.ReplayInfo.PowersAllowed != powersAvaliable)
                            throw new Exception("Data 'PowersAllowed' changed per turn when not expected.");
                        replayData.ReplayInfo.PowersAllowed = powersAvaliable;
                        break;
                    }

                    case "official":
                    {
                        var official = ReadBool(ref text, ref textIndex);
                        if (!firstTurn && replayData.ReplayInfo.OfficialGame != official)
                            throw new Exception("Data 'OfficialGame' changed per turn when not expected.");
                        replayData.ReplayInfo.OfficialGame = official;
                        break;
                    }

                    case "league":
                    {
                        var leagueMatch = readString(ref text, ref textIndex);
                        if (!firstTurn && replayData.ReplayInfo.LeagueMatch != leagueMatch)
                            throw new Exception("Data 'LeagueMatch' changed per turn when not expected.");
                        replayData.ReplayInfo.LeagueMatch = leagueMatch;
                        break;
                    }

                    case "team":
                    {
                        var teamMatch = ReadBool(ref text, ref textIndex);
                        if (!firstTurn && replayData.ReplayInfo.TeamMatch != teamMatch)
                            throw new Exception("Data 'LeagueMatch' changed per turn when not expected.");

                        replayData.ReplayInfo.TeamMatch = teamMatch;
                        break;
                    }

                    case "turn":
                    {
                        newTurn.ActivePlayerID = ReadInteger(ref text, ref textIndex);
                        break;
                    }

                    case "day":
                    {
                        newTurn.Day = ReadInteger(ref text, ref textIndex);
                        break;
                    }

                    case "start_date":
                    {
                        var startDate = readString(ref text, ref textIndex);

                        var dateTime = DateTime.Parse(startDate);
                        if (!firstTurn && replayData.ReplayInfo.StartDate != dateTime)
                            throw new Exception("Data 'StartDate' changed per turn when not expected.");
                        replayData.ReplayInfo.StartDate = dateTime;
                        break;
                    }

                    case "activity_date":
                    {
                        //Describes the date at which the last activity was made during this turn.
                        //Is useful as an effective end date when 'end_date' doesn't specify a time.

                        var activityDate = readString(ref text, ref textIndex);

                        if (activityDate != null)
                        {
                            var dateTime = DateTime.Parse(activityDate);

                            if (dateTime > replayData.ReplayInfo.EndDate)
                                replayData.ReplayInfo.EndDate = dateTime;
                        }
                        break;
                    }

                    case "end_date":
                    {
                        //This is likely always null. But we will attempt to parse it incase.

                        var endDate = readString(ref text, ref textIndex);

                        if (endDate != null)
                        {
                            var dateTime = DateTime.Parse(endDate);

                            if (dateTime > replayData.ReplayInfo.EndDate)
                                replayData.ReplayInfo.EndDate = dateTime;
                        }
                        break;
                    }

                    case "weather_code":
                    {
                        var value = readString(ref text, ref textIndex);

                        if (newTurn.StartWeather == null)
                            newTurn.StartWeather = new ReplayWeather();
                        newTurn.StartWeather.Type = WeatherHelper.ParseWeatherCode(value);
                        break;
                    }

                    case "weather_start":
                    {
                        var value = ReadNullableInteger(ref text, ref textIndex);
                        if (newTurn.StartWeather == null)
                            newTurn.StartWeather = new ReplayWeather();
                        newTurn.StartWeather.TurnStartID = value;
                        break;
                    }

                    case "win_condition":
                    {
                        //Todo: Is this always null? Is this just a holdover?
                        var value = readString(ref text, ref textIndex);
                        break;
                    }

                    case "active":
                    {
                        //Todo: Is this always "Y"? Is this just a holdover?
                        var value = readString(ref text, ref textIndex);
                        break;
                    }

                    case "capture_win":
                    {
                        var value = ReadInteger(ref text, ref textIndex);

                        int? trueValue;
                        if (value >= 1000)
                            trueValue = null;
                        else
                            trueValue = value + 2; //Todo: Is this always two? Or is it differing if a player has more than 2 buildings to start with.

                        if (!firstTurn && replayData.ReplayInfo.CaptureWinBuildingNumber != trueValue)
                            throw new Exception("Data 'CaptureWinBuildingNumber' changed per turn when not expected.");

                        replayData.ReplayInfo.CaptureWinBuildingNumber = trueValue;
                        break;
                    }

                    case "comment":
                    {
                        //Todo: Is this always null? Is this just a holdover?
                        //This may always be null do to comments not being displayed on finished matches?
                        var value = readString(ref text, ref textIndex);
                        break;
                    }

                    case "type":
                    {
                        var value = readString(ref text, ref textIndex);

                        MatchType type;

                        switch (value)
                        {
                            case "L":
                                type = MatchType.League;
                                break;

                            case "N":
                                type = MatchType.Normal;
                                break;

                            case "A":
                                type = MatchType.Normal;
                                break;

                            default:
                                throw new Exception("Unknown Match Type: " + value);
                        }

                        if (!firstTurn && replayData.ReplayInfo.Type != type)
                            throw new Exception("Data 'Type' changed per turn when not expected.");

                        replayData.ReplayInfo.Type = type;
                        break;
                    }

                    //Todo: Do we want to display these values as part of extended match info?

                    #region Useless values

                    case "aet_date":
                    {
                        //Describes the date at which the Auto End Turn would have finished the players turn.
                        //We do not need this as we do not care when the players would have been booted.
                        readString(ref text, ref textIndex);
                        break;
                    }

                    case "aet_interval":
                    {
                        //Describes the interval at which the Auto End Turn would have finished the players turn.
                        //We do not need this as we do not care when the players would have been booted.
                        var value = ReadInteger(ref text, ref textIndex);
                        break;
                    }

                    case "boot_interval":
                    {
                        //Todo: Is this always -1? Is this just a holdover?
                        var value = ReadInteger(ref text, ref textIndex);
                        break;
                    }

                    case "max_rating":
                    {
                        //Todo: Is this always null? Is this just a holdover?
                        var value = ReadNullableInteger(ref text, ref textIndex);
                        break;
                    }

                    case "min_rating":
                    {
                        //Todo: Is this always 0? Is this just a holdover?
                        var value = ReadInteger(ref text, ref textIndex);
                        break;
                    }

                    case "timers_initial":
                    {
                        //Describes the initial amount of time the player has per turn in seconds
                        //Not useful for replaying the game.
                        ReadInteger(ref text, ref textIndex);
                        break;
                    }

                    case "timers_increment":
                    {
                        //Describes the extra amount of time the player has per turn in seconds
                        //Not useful for replaying the game.
                        ReadInteger(ref text, ref textIndex);
                        break;
                    }

                    case "timers_max_turn":
                    {
                        //Describes the max amount of time a single turn can take up.
                        //Not useful for replaying the game.
                        ReadInteger(ref text, ref textIndex);
                        break;
                    }

                    #endregion

                    default:
                        throw new Exception($"Replay contained unknown entry: {entry}");
                }
            }

            #endregion

            if (text[textIndex++] != '}')
                throw new Exception("Player data does not end correctly.");

            newTurn.ActiveTeam = replayData.ReplayInfo.Players[newTurn.ActivePlayerID].TeamName;
        }

        void ReadPlayers(ref string text, ref int textIndex, ReplayData data, TurnData turnData, bool firstTurn)
        {
            if (text[textIndex++] != 'a')
                throw new Exception("Expected an array declaration for player data.");
            if (text[textIndex++] != ':')
                throw new Exception("Expected an array declaration for player data.");

            var numberOfPlayers = readNextLength(ref text, ref textIndex);

            data.ReplayInfo.Players ??= new Dictionary<int, ReplayUser>(numberOfPlayers);
            turnData.Players = new Dictionary<int, AWBWReplayPlayerTurn>();

            if (text[textIndex++] != '{')
                throw new Exception("Expected an array start for player data.");

            for (int i = 0; i < numberOfPlayers; i++)
            {
                var playerIndex = ReadInteger(ref text, ref textIndex);

                if (text.Substring(textIndex, player_start_text.Length) != player_start_text)
                    throw new Exception("Player data does not start correctly.");
                textIndex += player_start_text.Length;

                var paramerterCount = readNextLength(ref text, ref textIndex);

                ReplayUser playerData;

                //This makes this slightly awkward but the benefits of using a dictionary tend to outway this awkwardness.
                if (!firstTurn)
                    playerData = Enumerable.First(data.ReplayInfo.Players, x => x.Value.ReplayIndex == playerIndex).Value;
                else
                    playerData = new ReplayUser { ReplayIndex = playerIndex };
                var playerDataTurn = new AWBWReplayPlayerTurn();

                if (text[textIndex++] != '{')
                    throw new Exception("Player data does not start correctly.");

                for (int j = 0; j < paramerterCount; j++)
                {
                    var entry = readStringWithoutUnicode(ref text, ref textIndex);

                    switch (entry)
                    {
                        case "id":
                        {
                            var id = ReadInteger(ref text, ref textIndex);
                            if (!firstTurn && playerData.ID != id)
                                throw new Exception("Player 'id' changed per turn when not expected.");
                            playerData.ID = id;
                            playerDataTurn.ID = id;
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

                        case "team":
                        {
                            var teamName = readString(ref text, ref textIndex);
                            if (!firstTurn && playerData.TeamName != teamName)
                                throw new Exception("Player 'teamName' changed per turn when not expected.");
                            playerData.TeamName = teamName;
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
                            if (firstTurn)
                                playerData.COsUsedByPlayer.Add(id);
                            else if (!playerData.COsUsedByPlayer.Contains(id))
                                throw new Exception("Player's COs changed mid match?");

                            playerDataTurn.ActiveCOID = id;
                            break;
                        }

                        case "tags_co_id":
                        {
                            var id = ReadNullableInteger(ref text, ref textIndex);

                            if (id != null)
                            {
                                if (firstTurn)
                                    playerData.COsUsedByPlayer.Add(id.Value);
                                else if (!playerData.COsUsedByPlayer.Contains(id.Value))
                                    throw new Exception("Player's COs changed mid match?");

                                playerDataTurn.TagCOID = id;
                            }
                            break;
                        }

                        case "co_max_power":
                        {
                            var powerRequired = ReadNullableInteger(ref text, ref textIndex);
                            playerDataTurn.RequiredPowerForNormal = powerRequired;
                            break;
                        }

                        case "co_max_spower":
                        {
                            var powerRequired = ReadNullableInteger(ref text, ref textIndex);
                            playerDataTurn.RequiredPowerForSuper = powerRequired;
                            break;
                        }

                        case "tags_co_max_power":
                        {
                            var powerRequired = ReadNullableInteger(ref text, ref textIndex);
                            playerDataTurn.TagRequiredPowerForNormal = powerRequired;
                            break;
                        }

                        case "tags_co_max_spower":
                        {
                            var powerRequired = ReadNullableInteger(ref text, ref textIndex);
                            playerDataTurn.TagRequiredPowerForSuper = powerRequired;
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
                            if (playerDataTurn.Eliminated && playerData.EliminatedOn == null)
                                playerData.EliminatedOn = data.TurnData.Count - 1;

                            break;
                        }

                        case "co_power":
                        {
                            var powerPoints = ReadInteger(ref text, ref textIndex);
                            playerDataTurn.Power = powerPoints;
                            break;
                        }

                        case "tags_co_power":
                        {
                            var powerPoints = ReadNullableInteger(ref text, ref textIndex);
                            playerDataTurn.TagPower = powerPoints;
                            break;
                        }

                        case "co_power_on":
                        {
                            var powerActive = readString(ref text, ref textIndex);

                            //Todo: See if this changes for Tag
                            switch (powerActive)
                            {
                                case "N":
                                    playerDataTurn.ActiveCOPowers = ActiveCOPowers.None;
                                    break;

                                case "Y":
                                    playerDataTurn.ActiveCOPowers = ActiveCOPowers.Normal;
                                    break;

                                case "S":
                                    playerDataTurn.ActiveCOPowers = ActiveCOPowers.Super;
                                    break;

                                default:
                                    throw new Exception("Unknown power: " + powerActive); //Todo: Do Tag CO's trigger this.
                            }

                            break;
                        }

                        case "order":
                        {
                            var turnIndex = ReadInteger(ref text, ref textIndex);
                            if (!firstTurn && playerData.RoundOrder != turnIndex)
                                throw new Exception("Player 'order' changed per turn when not expected.");
                            playerData.RoundOrder = turnIndex;
                            break;
                        }

                        case "accept_draw":
                        {
                            turnData.DrawWasAccepted = ReadBool(ref text, ref textIndex);
                            break;
                        }

                        #region Unneeded Values

                        case "co_image":
                        {
                            //Specifies the image used to show the CO.
                            //Not really useful for us, as we keep our own images, unless AWBW starts making skins or something.
                            readString(ref text, ref textIndex);
                            break;
                        }

                        case "email":
                        {
                            //Specifies the email used by this player. This may always be null.
                            //Not useful for us and is probably breaking some privacy stuff if we were to show this.
                            var value = readString(ref text, ref textIndex);
                            break;
                        }

                        case "emailpress":
                        {
                            //This date is likely to do with the player, and not important to the replay. This may always be null.
                            //Not useful for us and is probably breaking some privacy stuff if we were to show this.
                            var value = readString(ref text, ref textIndex);
                            break;
                        }

                        case "last_read":
                        {
                            //This date is likely to do with the player, and not important to the replay.
                            //Not useful for us and is probably breaking some privacy stuff if we were to show this.
                            readString(ref text, ref textIndex);
                            break;
                        }

                        case "last_read_broadcasts":
                        {
                            //This date is likely to do with the player, and not important to the replay. This may always be null.
                            //Not useful for us and is probably breaking some privacy stuff if we were to show this.
                            readString(ref text, ref textIndex);
                            break;
                        }

                        case "games_id":
                        {
                            //Describes which game/replay id this player info belongs to.
                            //Not useful in our condition as this information is redundent.
                            ReadInteger(ref text, ref textIndex);
                            break;
                        }

                        case "signature":
                        {
                            //Specifies the signature of the player. This may always be null.
                            var value = readString(ref text, ref textIndex);
                            break;
                        }

                        case "turn":
                        {
                            //Unsure what this value is meant to represent. But seems to always be null.
                            //Likely to be a leftover value and not useful
                            //Todo: Is this always null?
                            var value = readString(ref text, ref textIndex);
                            break;
                        }

                        case "turn_start":
                        {
                            //Describes when the turn started
                            //Not useful for us as we don't really care when turns begin and end.
                            readString(ref text, ref textIndex);
                            break;
                        }

                        case "turn_clock":
                        {
                            //Describes how much time this player had at the start of the turn.
                            //Not useful for us as we don't really care when turns begin and end.
                            ReadInteger(ref text, ref textIndex);
                            break;
                        }

                        case "aet_count":
                        {
                            //Todo: Maybe this may show some light on how a turn ended. Like does this change per turn, or is it always the same
                            //Likely describes how many times a player has had the turn auto ended.
                            //Not useful as we really don't care about the turns ending like this.
                            var value = ReadInteger(ref text, ref textIndex);
                            break;
                        }

                        case "uniq_id":
                        {
                            //Likely was a different type of player id? This seems to be null in 90% of cases and unsure of the rest of the cases.
                            readString(ref text, ref textIndex);
                            break;
                        }

                        case "interface":
                        {
                            //Likely describes which interface the player was using.
                            //This probably doesn't matter too much?
                            var value = readString(ref text, ref textIndex);
                            break;
                        }

                        #endregion

                        default:
                        {
                            //Todo: Add a way to throw a notification but not crash entirely when not in debug
                            throw new Exception($"Replay player data contained unknown entry: {entry}");
                        }
                    }
                }

                if (text[textIndex++] != '}')
                    throw new Exception("Player data does not end correctly.");

                if (firstTurn)
                    data.ReplayInfo.Players.Add(playerData.ID, playerData);
                turnData.Players.Add(playerData.ID, playerDataTurn);
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

            var numberOfBuildings = readNextLength(ref text, ref textIndex);

            data.Buildings = new Dictionary<Vector2I, ReplayBuilding>();
            if (text[textIndex++] != '{')
                throw new Exception("Expected an array start for building data.");

            for (int i = 0; i < numberOfBuildings; i++)
            {
                var buildingIndex = ReadInteger(ref text, ref textIndex); //Likely not needed
                if (text.Substring(textIndex, building_start_text.Length) != building_start_text)
                    throw new Exception("Building data does not start correctly.");
                textIndex += building_start_text.Length;

                var parameterCount = readNextLength(ref text, ref textIndex);

                if (text[textIndex++] != '{')
                    throw new Exception("Building data does not start correctly.");

                var building = new ReplayBuilding();

                for (int j = 0; j < parameterCount; j++)
                {
                    var entry = readStringWithoutUnicode(ref text, ref textIndex);

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
                            readString(ref text, ref textIndex);
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

            var numberOfUnits = readNextLength(ref text, ref textIndex);

            data.ReplayUnit = new Dictionary<long, ReplayUnit>();
            if (text[textIndex++] != '{')
                throw new Exception("Expected an array start for unit data.");

            for (int i = 0; i < numberOfUnits; i++)
            {
                var unitIndex = ReadInteger(ref text, ref textIndex); //Likely not needed
                if (text.Substring(textIndex, units_start_text.Length) != units_start_text)
                    throw new Exception("Unit data does not start correctly.");
                textIndex += units_start_text.Length;

                var parameterCount = readNextLength(ref text, ref textIndex);

                if (text[textIndex++] != '{')
                    throw new Exception("Unit data does not start correctly.");

                var unit = new ReplayUnit();

                for (int j = 0; j < parameterCount; j++)
                {
                    var entry = readStringWithoutUnicode(ref text, ref textIndex);

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
                            var name = readString(ref text, ref textIndex);
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
                            var dived = readString(ref text, ref textIndex);
                            unit.SubHasDived = ReplayActionHelper.ParseSubHasDived(dived);
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
                            var type = readString(ref text, ref textIndex);
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
                            readString(ref text, ref textIndex);
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
            var turnIdx = -1;

            foreach (var turn in replayData.TurnData)
            {
                turnIdx++;
                if (turn.ActivePlayerID != playerID || turn.Day != day)
                    continue;

                turnData = turn;
                break;
            }

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

            var actionCount = readNextLength(ref text, ref textIndex);

            if (text[textIndex++] != '{')
                throw new Exception("Expected an array declaration for unit data.");

            if (turnData != null)
            {
                turnData.Actions = new List<IReplayAction>();

                for (int i = 0; i < actionCount; i++)
                {
                    var index = ReadInteger(ref text, ref textIndex);
                    var actionString = readString(ref text, ref textIndex);

                    if (actionString == "Array")
                    {
                        Logger.Log("Replay contained action 'Array' which is not an action.");
                        turnData.Actions.Add(new EmptyAction());
                        continue;
                    }

                    var jsonObject = JObject.Parse(actionString);

                    if (index != i)
                        throw new Exception("Out of Order actions");

                    try
                    {
                        var action = actionDatabase.ParseJObjectIntoReplayAction(jsonObject, replayData, turnData);
                        if (action != null)
                            turnData.Actions.Add(action);
                    }
                    catch (Exception e)
                    {
                        throw new AggregateException($"Failed to parse Replay Action #{i} on Turn {turnIdx}. Day {day}, Active Player {playerID}", e);
                    }
                }
            }
            else
            {
                //Tag battles with more than 2 players seem to occasionally add these for dead? players.
                if (actionCount > 1)
                    throw new Exception("Replay actions contained an unknown turn id.");

                if (actionCount == 1)
                {
                    var index = ReadInteger(ref text, ref textIndex);
                    var actionString = readString(ref text, ref textIndex);
                    var jsonObject = JObject.Parse(actionString);
                    var action = actionDatabase.ParseJObjectIntoReplayAction(jsonObject, replayData, turnData);

                    if (!(action is EndTurnAction || action is TagAction))
                        throw new Exception("Replay actions contained an unknown turn id.");
                }
            }

            if (text[textIndex++] != '}')
                throw new Exception("Expected an array declaration for unit data.");
            if (text[textIndex++] != '}')
                throw new Exception("Expected an array declaration for unit data.");
        }

        //Todo: Read long maybe?
        private int readNextLength(ref string text, ref int index)
        {
            var startIndex = index;

            while (true)
            {
                var character = text[index++];
                if (character == ':')
                    break;
            }
            return int.Parse(text[startIndex..(index - 1)]);
        }

        //Todo: May fail if a unicode character appears near end of file.
        private string readString(ref string text, ref int index)
        {
            var type = text[index];
            index += 2;

            switch (type)
            {
                case 's':
                {
                    if (text[index - 1] != ':')
                        throw new Exception("String was badly formatted.");
                    var entryLength = readNextLength(ref text, ref index);
                    if (text[index] != '"')
                        throw new Exception("String was badly formatted.");

                    var textEntry = text[(index + 1)..(index + entryLength + 1)];

                    var byteCount = Encoding.UTF8.GetByteCount(textEntry);

                    if (byteCount != entryLength)
                    {
                        var textCount = 0;

                        while (byteCount > entryLength)
                        {
                            textCount++;
                            byteCount -= Encoding.UTF8.GetByteCount(textEntry, entryLength - textCount, 1);
                        }

                        textEntry = textEntry[0..^textCount];
                    }

                    index += textEntry.Length + 3;

                    if (text[index - 2] != '"')
                        throw new Exception("String was badly formatted.");
                    if (text[index - 1] != ';')
                        throw new Exception("String was badly formatted.");

                    return textEntry;
                }

                case 'N':
                    return null;

                default:
                    throw new Exception("Unknown string kind: " + type);
            }
        }

        private string readStringWithoutUnicode(ref string text, ref int index)
        {
            var type = text[index];
            index += 2;

            switch (type)
            {
                case 's':
                {
                    if (text[index - 1] != ':')
                        throw new Exception("String was badly formatted.");
                    var entryLength = readNextLength(ref text, ref index);
                    if (text[index] != '"')
                        throw new Exception("String was badly formatted.");

                    var textEntry = text[(index + 1)..(index + entryLength + 1)];
                    index += textEntry.Length + 3;

                    if (text[index - 2] != '"')
                        throw new Exception("String was badly formatted.");
                    if (text[index - 1] != ';')
                        throw new Exception("String was badly formatted.");

                    return textEntry;
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

        int? ReadNullableInteger(ref string text, ref int index)
        {
            if (text[index++] != 'i')
            {
                if (text[index - 1] == 'N')
                {
                    if (text[index++] != ';')
                        throw new Exception("Null was badly formatted.");
                    return null;
                }

                throw new Exception("Was expecting a integer or null.");
            }
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
            var stringData = readString(ref text, ref index);

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
