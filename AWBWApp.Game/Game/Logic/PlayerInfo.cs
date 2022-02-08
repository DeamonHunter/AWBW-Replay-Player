using AWBWApp.Game.API.Replay;

namespace AWBWApp.Game.Game.Logic
{
    public class PlayerInfo
    {
        public int TurnNumber;
        public int CountryID;
        public string CountryCode;
        public string Team;

        public PlayerInfo(AWBWReplayPlayer player)
        {
            TurnNumber = player.TurnOrder;
            CountryID = player.CountryId;
            CountryCode = player.CountryCode();
            Team = player.TeamName;
        }
    }
}
