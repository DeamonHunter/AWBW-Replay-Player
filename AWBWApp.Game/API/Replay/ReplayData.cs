using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.API.Replay
{
    public class ReplayData
    {
        public ReplayInfo ReplayInfo = new ReplayInfo();
        public List<TurnData> TurnData = new List<TurnData>();
    }

    public class ReplayInfo
    {
        public int ID;
        public string Name;
        public string Password;
        public int CreatorId;

        public int MapId;
        public Dictionary<int, ReplayUser> Players;

        public int FundsPerBuilding;
        public int StartingFunds;
        public bool Fog;
        public bool PowersAllowed;

        //Unnessecary data on the Game
        //Todo: Remove when double checked
        public bool OfficialGame;
        public MatchType Type;
        public string LeagueMatch;
        public bool TeamMatch;

        public int? CaptureWinBuildingNumber;

        //Unknown if this data is per turn or not
        public DateTime StartDate;
        public DateTime EndDate;
    }

    public enum MatchType
    {
        League,
        Normal
    }

    public class TurnData
    {
        public int Day;
        public int ActivePlayerID = -1;
        public string ActiveTeam;

        public bool Active; //Todo: "active" What is this?

        public ReplayWeather Weather;

        public Dictionary<int, AWBWReplayPlayerTurn> Players;
        public List<IReplayAction> Actions;

        public Dictionary<int, int> CoPowers;

        //Should these be ID based?
        public Dictionary<Vector2I, ReplayBuilding> Buildings;
        public Dictionary<long, ReplayUnit> ReplayUnit;

        //Todo: Do we need to handle this as a special case on turn start? Replay 554175
        public bool DrawWasAccepted;
    }

    public class Weather
    {
    }
}
