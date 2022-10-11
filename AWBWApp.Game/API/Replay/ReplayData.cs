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
        public long ID;
        public string Name;
        public string UserDefinedName;

        public string Password;
        public long CreatorId;

        public long MapId;
        public Dictionary<long, ReplayUser> Players;

        public int FundsPerBuilding;
        public int StartingFunds;
        public bool Fog;
        public bool PowersAllowed;
        public string WeatherType;

        //Unnessecary data on the Game
        //Todo: Remove when double checked
        public bool OfficialGame;
        public MatchType Type;
        public string LeagueMatch;
        public bool TeamMatch;
        public int ReplayVersion;

        public int? CaptureWinBuildingNumber;

        //Unknown if this data is per turn or not
        public DateTime StartDate;
        public DateTime EndDate;

        public string GetDisplayName()
        {
            return UserDefinedName ?? Name;
        }
    }

    public enum MatchType
    {
        League,
        Normal,
        Tag
    }

    public class TurnData
    {
        public int Day;
        public long ActivePlayerID = -1;
        public string ActiveTeam;

        public bool Active; //Todo: "active" What is this?

        public ReplayWeather StartWeather;

        public Dictionary<long, ReplayUserTurn> Players;
        public List<IReplayAction> Actions;

        //Should these be ID based?
        public Dictionary<Vector2I, ReplayBuilding> Buildings;
        public Dictionary<long, ReplayUnit> ReplayUnit;

        //Todo: Do we need to handle this as a special case on turn start? Replay 554175
        public bool DrawWasAccepted;
    }
}
