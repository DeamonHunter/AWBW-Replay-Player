using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.COs;
using osu.Framework.Bindables;

namespace AWBWApp.Game.Game.Logic
{
    public class PlayerInfo
    {
        public long ID { get; }
        public string Username { get; }
        public int RoundOrder { get; }
        public string Team { get; }
        public int? EliminatedOn { get; }

        public BindableInt CountryID = new BindableInt();
        public Bindable<string> CountryCode = new Bindable<string>();
        public Bindable<string> CountryPath = new Bindable<string>();

        public BindableBool Eliminated = new BindableBool();
        public Bindable<COInfo> ActiveCO = new Bindable<COInfo>();
        public Bindable<COInfo> TagCO = new Bindable<COInfo>();

        public BindableInt Funds = new BindableInt();
        public BindableInt UnitCount = new BindableInt();
        public BindableInt UnitValue = new BindableInt();
        public BindableInt PropertyValue = new BindableInt();

        public PlayerInfo(ReplayUser player)
        {
            ID = player.ID;
            Username = player.Username;
            Team = player.TeamName;
            RoundOrder = player.RoundOrder;

            CountryID.Value = player.CountryId;
            CountryCode.Value = player.CountryCode();
            CountryPath.Value = player.CountryPathName();

            EliminatedOn = player.EliminatedOn;
        }

        public void UpdateTurn(AWBWReplayPlayerTurn turn, COStorage coStorage, int turnNumber, int unitCount, int unitValue, int propertyValue)
        {
            Eliminated.Value = EliminatedOn.HasValue && turnNumber >= EliminatedOn;

            Funds.Value = turn.Funds;
            UnitCount.Value = unitCount;
            UnitValue.Value = unitValue;
            PropertyValue.Value = propertyValue;

            ActiveCO.Value = new COInfo
            {
                CO = coStorage.GetCOByAWBWId(turn.ActiveCOID),
                Power = turn.Power,
                PowerRequiredForNormal = turn.RequiredPowerForNormal,
                PowerRequiredForSuper = turn.RequiredPowerForSuper
            };

            TagCO.Value = new COInfo
            {
                CO = turn.TagCOID.HasValue ? coStorage.GetCOByAWBWId(turn.TagCOID.Value) : null,
                Power = turn.TagPower,
                PowerRequiredForNormal = turn.TagRequiredPowerForNormal,
                PowerRequiredForSuper = turn.TagRequiredPowerForSuper
            };
        }
        /*
        private static string GetCOName(int id) =>
            id switch
            {
                1 => "Andy",
                2 => "Grit",
                3 => "Kanbei",
                5 => "Drake",
                7 => "Max",
                8 => "Sami",
                9 => "Olaf",
                10 => "Eagle",
                11 => "Adder",
                12 => "Hawke",
                13 => "Sensei",
                14 => "Jess",
                15 => "Colin",
                16 => "Lash",
                17 => "Hachi",
                18 => "Sonja",
                19 => "Sasha",
                20 => "Grimm",
                21 => "Koal",
                22 => "Jake",
                23 => "Kindle",
                24 => "Nell",
                25 => "Flak",
                26 => "Jugger",
                27 => "Javier",
                28 => "Rachel",
                29 => "Sturm",
                30 => "Von Bolt",
                _ => throw new Exception("Unknown CO ID: " + id)
            };
        */
    }

    public struct COInfo
    {
        public COData CO;
        public int? Power;
        public int? PowerRequiredForNormal;
        public int? PowerRequiredForSuper;
    }
}
