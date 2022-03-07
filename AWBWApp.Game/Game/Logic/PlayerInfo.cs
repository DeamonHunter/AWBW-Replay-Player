using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.COs;
using AWBWApp.Game.Game.Country;
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

        public Bindable<CountryData> Country = new Bindable<CountryData>();

        public BindableBool Eliminated = new BindableBool();
        public Bindable<COInfo> ActiveCO = new Bindable<COInfo>();
        public Bindable<COInfo> TagCO = new Bindable<COInfo>();

        public BindableInt Funds = new BindableInt();
        public BindableInt UnitCount = new BindableInt();
        public BindableInt UnitValue = new BindableInt();
        public BindableInt PropertyValue = new BindableInt();

        public PlayerInfo(ReplayUser player, CountryData country)
        {
            ID = player.ID;
            Username = player.Username;
            Team = player.TeamName;
            RoundOrder = player.RoundOrder;

            Country.Value = country;

            EliminatedOn = player.EliminatedOn;
        }

        public void UpdateTurn(ReplayUserTurn turn, COStorage coStorage, int turnNumber, int unitCount, int unitValue, int propertyValue)
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
    }

    public struct COInfo
    {
        public COData CO;
        public int? Power;
        public int? PowerRequiredForNormal;
        public int? PowerRequiredForSuper;
    }
}
