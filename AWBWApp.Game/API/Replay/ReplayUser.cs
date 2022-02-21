using System;
using System.Collections.Generic;
using osuTK.Graphics;

namespace AWBWApp.Game.API.Replay
{
    public class ReplayUser
    {
        public int ID;
        public int UserId;
        public string Username; //This needs to be filled in by asking AWBW

        public string TeamName;
        public int CountryId;

        public HashSet<int> COsUsedByPlayer = new HashSet<int>();

        public int ReplayIndex;
        public int RoundOrder;
        public int? EliminatedOn;

        public string UniqueId; //Todo: Figure out "uniq_id"

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

                //These IDs are weird. Likely because of legacy stuff.

                16 => "ci",
                17 => "pc",
                19 => "tg",
                20 => "pl",
                21 => "ar",
                22 => "wn",
                _ => throw new InvalidOperationException("Country ID must be between 1 and 16 inclusively.")
            };

        public string CountryPathName() =>
            CountryId switch
            {
                1 => "OrangeStar",
                2 => "BlueMoon",
                3 => "GreenEarth",
                4 => "YellowComet",
                5 => "BlackHole",
                6 => "RedFire",
                7 => "GreySky",
                8 => "BrownDesert",
                9 => "AmberBlaze",
                10 => "JadeSun",

                //These IDs are weird. Likely because of legacy stuff.

                16 => "CobaltIce",
                17 => "PinkCosmos",
                19 => "TealGalaxy",
                20 => "PurpleLightning",
                21 => "AcidRain",
                22 => "WhiteNova",
                _ => throw new InvalidOperationException("Country ID must be between 1 and 16 inclusively.")
            };

        public static Color4 CountryColour(int countryId) =>
            countryId switch
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

                16 => new Color4(35, 66, 186, 255),
                17 => new Color4(255, 51, 204, 255),
                19 => new Color4(27, 162, 152, 255),
                20 => new Color4(204, 0, 255, 255),
                21 => new Color4(82, 105, 12, 255),
                22 => new Color4(226, 174, 147, 255),

                _ => throw new InvalidOperationException("Country ID must be between 1 and 16 inclusively.")
            };
    }

    public class AWBWReplayPlayerTurn
    {
        public int ID; //Just to make sure things stay the same
        public int Funds;

        public int ActiveCOID;
        public int Power;
        public int? RequiredPowerForNormal; //This changes every time we use a power.
        public int? RequiredPowerForSuper; //This changes every time we use a power.

        public int? TagCOID;
        public int? TagPower;
        public int? TagRequiredPowerForNormal; //This changes every time we use a power.
        public int? TagRequiredPowerForSuper; //This changes every time we use a power.

        public ActiveCOPowers ActiveCOPowers;
        public bool Eliminated;
    }

    [Flags]
    public enum ActiveCOPowers
    {
        None = 0,
        Normal = 1 << 0,
        Super = 1 << 1,
        TagNormal = 1 << 2,
        TagSuper = 1 << 3,
    }
}
