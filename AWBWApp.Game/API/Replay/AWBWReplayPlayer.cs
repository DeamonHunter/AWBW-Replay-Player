using System;
using osuTK.Graphics;

namespace AWBWApp.Game.API.Replay
{
    public class AWBWReplayPlayer
    {
        public int ID;
        public int UserId;

        public string TeamName;
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

        public Color4 CountryColour() =>
            CountryId switch
            {
                1 => new Color4(240, 0, 8, 255),
                2 => new Color4(0, 152, 248, 255),
                3 => new Color4(0, 192, 16, 255),
                4 => new Color4(208, 128, 0, 255),
                5 => new Color4(101, 59, 165, 255),
                6 => new Color4(193, 70, 61, 255),
                7 => new Color4(93, 93, 93, 255),
                8 => new Color4(188, 130, 72, 255),
                9 => new Color4(231, 135, 22, 255),
                10 => new Color4(133, 146, 123, 255),
                11 => new Color4(35, 66, 186, 255),
                12 => new Color4(255, 51, 204, 255),
                13 => new Color4(27, 162, 152, 255),
                14 => new Color4(204, 0, 255, 255),
                15 => new Color4(82, 105, 12, 255),
                16 => new Color4(226, 174, 147, 255),

                //These IDs are weird. Likely because of legacy stuff.
                17 => new Color4(255, 51, 204, 255),
                20 => new Color4(204, 0, 255, 255),
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
