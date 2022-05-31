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
        public long UserID { get; }
        public int RoundOrder { get; }
        public string Team { get; }
        public int? EliminatedOn { get; }
        public int OriginalCountryID { get; }

        public Bindable<CountryData> Country = new Bindable<CountryData>();
        public Bindable<FaceDirection> UnitFaceDirection = new Bindable<FaceDirection>();

        public BindableBool Eliminated = new BindableBool();
        public Bindable<COInfo> ActiveCO = new Bindable<COInfo>();
        public Bindable<COInfo> TagCO = new Bindable<COInfo>();
        public Bindable<ActiveCOPower> ActivePower = new Bindable<ActiveCOPower>();

        public BindableDouble PowerPercentage = new BindableDouble(); //Only used for old style replays
        public BindableInt Funds = new BindableInt();
        public BindableInt UnitCount = new BindableInt();
        public BindableInt UnitValue = new BindableInt();
        public BindableInt PropertyValue = new BindableInt();

        public PlayerInfo(ReplayUser player, CountryData country)
        {
            ID = player.ID;
            Username = player.Username;
            UserID = player.UserId;

            Team = player.TeamName;
            RoundOrder = player.RoundOrder;

            OriginalCountryID = country.AWBWID;
            Country.Value = country;
            UnitFaceDirection.Value = country.FaceDirection;

            EliminatedOn = player.EliminatedOn;
        }

        public void UpdateTurn(ReplayUserTurn turn, COStorage coStorage, int turnNumber, int unitCount, int unitValue, int propertyValue, ActiveCOPower activePower)
        {
            Eliminated.Value = EliminatedOn.HasValue && turnNumber >= EliminatedOn;

            Funds.Value = turn.Funds;
            UnitCount.Value = unitCount;
            UnitValue.Value = unitValue;
            PropertyValue.Value = propertyValue;
            PowerPercentage.Value = turn.PowerPercentage;

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

            ActivePower.Value = activePower;
        }

        public void UpdateUndo(ReplayUserTurn turn, COStorage coStorage, int turnNumber, int unitCount, int unitValue, int propertyValue, ActiveCOPower activePower)
        {
            Eliminated.Value = EliminatedOn.HasValue && turnNumber >= EliminatedOn;

            UnitCount.Value = unitCount;
            UnitValue.Value = unitValue;
            PropertyValue.Value = propertyValue;

            if (ActiveCO.Value.CO.AWBWID != turn.ActiveCOID)
                (ActiveCO.Value, TagCO.Value) = (TagCO.Value, ActiveCO.Value);

            ActivePower.Value = activePower;
        }

        public bool OnSameTeam(PlayerInfo info)
        {
            if (Team != null && info.Team != null)
                return Team == info.Team;

            return ID == info.ID;
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
