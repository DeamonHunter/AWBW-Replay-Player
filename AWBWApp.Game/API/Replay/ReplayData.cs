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
        public Dictionary<int, int> PlayerIds;
        public AWBWReplayPlayer[] Players;

        public int FundsPerBuilding;
        public int StartingFunds;
        public bool Fog;
        public bool PowersAllowed;

        //Unnessecary data on the Game
        //Todo: Remove when double checked
        public bool OfficialGame;
        public string LeagueMatch;
        public bool TeamMatch;

        public int? MinimumRating;
        public int? MaximumRating;

        public int StartingTimer;
        public int AdditionalTimerPerTurn;
        public int MaximumTurnTime;

        public string Comment;
        public int BootInterval;

        public int AETInterval; //Todo: "aet_interval" What is this?
        public string AETDate; //Todo: "aet_date" What is this?

        //Unknown if this data is per turn or not
        public string StartDate;
        public string EndDate;
        //public string ActivityDate; //Todo: "activity_date" What is this?

        public int CaptureWin; //Todo: "capture_win" What is this?
        public bool Type; //Todo: "type" What is this?
    }

    public class TurnData
    {
        public int Day;
        public int ActivePlayerID;
        public string ActiveTeam;

        public bool Active; //Todo: "active" What is this?

        public ReplayWeather Weather;

        public AWBWReplayPlayerTurn[] Players;
        public List<IReplayAction> Actions;

        public Dictionary<int, int> CoPowers;

        //Should these be ID based?
        public Dictionary<Vector2I, ReplayBuilding> Buildings;
        public Dictionary<long, ReplayUnit> ReplayUnit;
    }

    public class Weather
    {
    }
}
