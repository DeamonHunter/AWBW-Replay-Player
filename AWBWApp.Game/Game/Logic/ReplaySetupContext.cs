using System.Collections.Generic;
using System.Text;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.COs;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Game.Logic
{
    public class ReplaySetupContext
    {
        public Dictionary<long, ReplayUnit> Units = new Dictionary<long, ReplayUnit>();
        public Dictionary<Vector2I, ReplayBuilding> Buildings = new Dictionary<Vector2I, ReplayBuilding>();
        public Dictionary<long, ReplayUser> PlayerInfos = new Dictionary<long, ReplayUser>();
        public Dictionary<long, ReplayUserTurn> PlayerTurns = new Dictionary<long, ReplayUserTurn>();

        //Todo: Are there other things we need to track here
        public Dictionary<long, int> PropertyValuesForPlayers = new Dictionary<long, int>();
        public Dictionary<long, int> FundsValuesForPlayers = new Dictionary<long, int>();
        public Dictionary<long, int> PowerValuesForPlayers = new Dictionary<long, int>();

        public TurnData CurrentTurn;
        public int CurrentTurnIndex;
        public int CurrentActionIndex;
        public int CurrentDay;

        public long ActivePlayerID;
        public string ActivePlayerTeam;

        //Todo: Possibly change how we calculate funds
        public BuildingStorage BuildingStorage;
        private COStorage coStorage;
        public Weather Weather;

        private int fundsPerBuilding;

        public ReplaySetupContext(BuildingStorage buildingStorage, COStorage coStorage, Dictionary<long, ReplayUser> playerInfos, int fundsPerBuilding)
        {
            BuildingStorage = buildingStorage;
            this.coStorage = coStorage;
            PlayerInfos = playerInfos;
            this.fundsPerBuilding = fundsPerBuilding;
        }

        public DesyncTurn MakeDesync(TurnData nextTurn)
        {
            var desync = new DesyncTurn();
            desync.TurnIndex = CurrentTurnIndex;
            desync.NextPlayerID = nextTurn.ActivePlayerID;

            foreach (var unit in Units)
            {
                ReplayUnit nextTurnUnit;

                if (!nextTurn.ReplayUnit.TryGetValue(unit.Key, out nextTurnUnit))
                {
                    desync.ChangedUnits.Add(unit.Key, unit.Value.Clone());
                    desync.DesyncRemovedUnitCount++;
                    continue;
                }

                if (!unit.Value.DoesUnitMatch(nextTurnUnit))
                {
                    desync.ChangedUnits.Add(unit.Key, unit.Value.Clone());
                    desync.DesyncChangedUnitCount++;
                }
            }

            foreach (var nextTurnUnit in nextTurn.ReplayUnit)
            {
                if (!Units.ContainsKey(nextTurnUnit.Key))
                    desync.AddedUnits.Add(nextTurnUnit.Key);
            }

            foreach (var building in Buildings)
            {
                if (!nextTurn.Buildings.TryGetValue(building.Key, out var nextTurnBuilding))
                {
                    desync.DesyncedBuildings.Add(building.Key, building.Value);
                    continue;
                }

                if (!building.Value.DoesBuildingMatch(nextTurnBuilding))
                    desync.DesyncedBuildings.Add(building.Key, building.Value);
            }

            foreach (var player in nextTurn.Players)
            {
                var nextPropertyValue = getPropertyValueForPlayer(player.Key, nextTurn);
                if (PropertyValuesForPlayers[player.Key] != nextPropertyValue)
                    desync.DesyncedProperty.Add(player.Key, PropertyValuesForPlayers[player.Key]);

                if (PowerValuesForPlayers[player.Key] != nextTurn.Players[player.Key].Power)
                    desync.DesyncedPowers.Add(player.Key, PowerValuesForPlayers[player.Key]);

                if (player.Key != nextTurn.ActivePlayerID)
                {
                    if (FundsValuesForPlayers[player.Key] != nextTurn.Players[player.Key].Funds)
                        desync.DesyncedFunds.Add(player.Key, FundsValuesForPlayers[player.Key]);
                }
                else
                {
                    desync.DesyncedFunds.Add(player.Key, FundsValuesForPlayers[player.Key]);

                    var nextFunds = nextTurn.Players[player.Key].Funds;
                    if (FundsValuesForPlayers[player.Key] + nextPropertyValue - nextFunds != 0)
                        desync.NextPlayerFundsComplaint = FundsValuesForPlayers[player.Key];
                }
            }

            return desync;
        }

        public void SetupForTurn(TurnData turn, int turnIndex)
        {
            CurrentTurn = turn;
            CurrentTurnIndex = turnIndex;
            CurrentDay = turn.Day;

            ActivePlayerID = turn.ActivePlayerID;
            ActivePlayerTeam = turn.ActiveTeam;

            PlayerTurns.Clear();

            PropertyValuesForPlayers.Clear();
            FundsValuesForPlayers.Clear();
            Weather = turn.StartWeather.Type;

            foreach (var player in turn.Players)
            {
                PlayerTurns.Add(player.Key, player.Value.Clone());
                PropertyValuesForPlayers[player.Key] = getPropertyValueForPlayer(player.Key, turn);
                FundsValuesForPlayers[player.Key] = player.Value.Funds;
                PowerValuesForPlayers[player.Key] = player.Value.Power;
            }

            Units.Clear();
            foreach (var unit in turn.ReplayUnit)
                Units.Add(unit.Key, unit.Value.Clone());

            Buildings.Clear();

            foreach (var building in turn.Buildings)
                Buildings.Add(building.Key, building.Value.Clone());
        }

        private int getPropertyValueForPlayer(long playerID, TurnData turn)
        {
            var playerCountry = PlayerInfos[playerID].CountryID;

            var perBuilding = fundsPerBuilding + coStorage.GetCOByAWBWId(PlayerTurns[playerID].ActiveCOID).DayToDayPower.PropertyFundIncrease;

            var funds = 0;

            foreach (var building in turn.Buildings)
            {
                if (!BuildingStorage.TryGetBuildingByAWBWId(building.Value.TerrainID!.Value, out var buildingData))
                    continue;

                if (buildingData.CountryID != playerCountry || !buildingData.GivesMoneyWhenCaptured)
                    continue;

                funds += perBuilding;
            }

            return funds;
        }
    }

    public class DesyncTurn
    {
        public long DesyncChangedUnitCount;
        public long DesyncRemovedUnitCount;

        public HashSet<long> AddedUnits = new HashSet<long>();
        public Dictionary<long, ReplayUnit> ChangedUnits = new Dictionary<long, ReplayUnit>();

        public Dictionary<Vector2I, ReplayBuilding> DesyncedBuildings = new Dictionary<Vector2I, ReplayBuilding>();

        public Dictionary<long, int> DesyncedFunds = new Dictionary<long, int>();
        public Dictionary<long, int> DesyncedProperty = new Dictionary<long, int>();
        public Dictionary<long, int> DesyncedPowers = new Dictionary<long, int>();

        public int TurnIndex;
        public long NextPlayerID;
        public long? NextPlayerFundsComplaint;

        public string WriteDesyncReport()
        {
            var noDesyncedFunds = DesyncedFunds.Count <= 0 || (DesyncedFunds.Count == 1 && DesyncedFunds.ContainsKey(NextPlayerID) && !NextPlayerFundsComplaint.HasValue);

            if (noDesyncedFunds && AddedUnits.Count <= 0 && DesyncChangedUnitCount <= 0 && DesyncRemovedUnitCount <= 0 && DesyncedBuildings.Count <= 0 && DesyncedProperty.Count <= 0 && DesyncedPowers.Count <= 0)
                return null;

            var sb = new StringBuilder();
            sb.AppendLine($"Desync occured on turn {TurnIndex}:");
            if (AddedUnits.Count > 0)
                sb.AppendLine($"{AddedUnits.Count} additional units.");
            if (DesyncChangedUnitCount > 0)
                sb.AppendLine($"{DesyncChangedUnitCount} units with changes");
            if (DesyncRemovedUnitCount > 0)
                sb.AppendLine($"{DesyncRemovedUnitCount} missing units.");
            if (DesyncedBuildings.Count > 0)
                sb.AppendLine($"{DesyncedBuildings.Count} buildings");
            if (DesyncedProperty.Count > 0)
                sb.AppendLine($"{DesyncedProperty.Count} property values");
            if (DesyncedPowers.Count > 0)
                sb.AppendLine($"{DesyncedPowers.Count} power values");

            if (!noDesyncedFunds)
            {
                if (!DesyncedFunds.ContainsKey(NextPlayerID) || NextPlayerFundsComplaint.HasValue)
                    sb.AppendLine($"{DesyncedFunds.Count} funds values");
                else
                    sb.AppendLine($"{DesyncedFunds.Count - 1} funds values");
            }

            return sb.ToString();
        }

        public void UndoDesync(ReplayController controller)
        {
            foreach (var unit in ChangedUnits)
            {
                if (controller.Map.TryGetDrawableUnit(unit.Key, out var drawable))
                    drawable.UpdateUnit(unit.Value);
                else
                    controller.Map.AddUnit(unit.Value);
            }

            foreach (var unit in AddedUnits)
                controller.Map.DeleteUnit(unit, false);

            foreach (var building in DesyncedBuildings)
                controller.Map.UpdateBuilding(building.Value, false);

            foreach (var funds in DesyncedFunds)
                controller.Players[funds.Key].Funds.Value = funds.Value;

            foreach (var funds in DesyncedProperty)
                controller.Players[funds.Key].PropertyValue.Value = funds.Value;

            foreach (var power in DesyncedPowers)
            {
                var co = controller.Players[power.Key].ActiveCO.Value;
                co.Power = power.Value;
                controller.Players[power.Key].ActiveCO.Value = co;
            }
        }
    }
}
