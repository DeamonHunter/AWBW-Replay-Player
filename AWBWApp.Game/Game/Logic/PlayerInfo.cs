using AWBWApp.Game.API.Replay;

namespace AWBWApp.Game.Game.Logic
{
    public class PlayerInfo
    {
        public int TurnNumber;
        public int CountryID;
        public string CountryCode;

        public PlayerInfo(AWBWReplayPlayer player)
        {
            TurnNumber = player.TurnOrderIndex;
            CountryID = player.CountryId;
            CountryCode = player.CountryCode();
        }
    }
}
