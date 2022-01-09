using System;

namespace AWBWApp.Game.API.Replay
{
    public class AWBWReplayPlayer
    {
        public int ID;
        public int UserId;

        public int CountryId;
        public int COId;
        public int TurnOrderIndex;
        public bool TakenTurn; //Todo: Figure out "turn"

        public string Email;
        public string UniqueId; //Todo: Figure out "uniq_id"
        public string EmailPress; //Todo: Figure out "emailpress"
        public string Signature; //Todo: Figure out "signature"

        public string LastRead; //Todo: Figure out "uniq_id"
        public string LastReadBroadcasts; //Todo: Figure out "uniq_id"

        public string CountryCode() =>
            CountryId switch
            {
                1 => "os",
                2 => "bm",
                3 => "ge",
                4 => "yc",
                5 => "bh",
                6 => "rf",
                7 => "gs",
                8 => "bd",
                9 => "ab",
                10 => "js",
                11 => "ci",
                12 => "pc",
                13 => "tg",
                14 => "pl",
                15 => "ar",
                16 => "wn",

                //These IDs are weird. Likely because of legacy stuff.
                17 => "pc",
                20 => "pl",
                _ => throw new InvalidOperationException("Country ID must be between 1 and 16 inclusively.")
            };
    }

    public class AWBWReplayPlayerTurn
    {
        public int ID; //Just to make sure things stay the same
        public int Funds;
        public int COPower;
        public string COPowerOn;
        public bool Eliminated;
    }
}
