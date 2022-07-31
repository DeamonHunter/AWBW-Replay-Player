using System.Collections.Generic;
using System.Linq;
using System.Text;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.API.Replay.Actions;
using AWBWApp.Game.Exceptions;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.COs;
using AWBWApp.Game.UI.Replay;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Game.Logic
{
    public class ReplaySetupContext
    {
        public Dictionary<long, ReplayUnit> Units = new Dictionary<long, ReplayUnit>();
        public Dictionary<Vector2I, ReplayBuilding> Buildings = new Dictionary<Vector2I, ReplayBuilding>();
        public Dictionary<Vector2I, Dictionary<string, BuildingTile>> BuildingKnowledge = new Dictionary<Vector2I, Dictionary<string, BuildingTile>>();

        public Dictionary<long, ReplayUser> PlayerInfos = new Dictionary<long, ReplayUser>();
        public Dictionary<long, ReplayUserTurn> PlayerTurns = new Dictionary<long, ReplayUserTurn>();

        //Todo: Are there other things we need to track here

        public Dictionary<long, PlayerStatsReadout> StatsReadouts = new Dictionary<long, PlayerStatsReadout>();
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
        public WeatherType WeatherType;

        private int fundsPerBuilding;

        public ReplaySetupContext(BuildingStorage buildingStorage, COStorage coStorage, Dictionary<long, ReplayUser> playerInfos, int fundsPerBuilding)
        {
            BuildingStorage = buildingStorage;
            this.coStorage = coStorage;
            PlayerInfos = playerInfos;
            this.fundsPerBuilding = fundsPerBuilding;
        }

        public void InitialSetup(StatsHandler statsReadout, ReplayData data)
        {
            //Correct the replay's initial funds per player. (Replays will always have funds at 0 until the player gets their first turn.)
            for (int i = 1; i < data.TurnData[0].Players.Count && i < data.TurnData.Count; i++)
            {
                var activePlayer = data.TurnData[i].ActivePlayerID;
                var activeFunds = data.TurnData[i].Players[activePlayer].Funds;
                for (int j = i - 1; j >= 0; j--)
                    data.TurnData[j].Players[activePlayer].Funds = activeFunds;
            }

            foreach (var player in data.TurnData[0].Players)
            {
                var readout = new PlayerStatsReadout
                {
                    GeneratedMoney = player.Value.Funds
                };

                StatsReadouts.Add(player.Key, readout);
            }

            foreach (var building in data.TurnData[0].Buildings)
            {
                var knowledge = new Dictionary<string, BuildingTile>();
                if (!BuildingStorage.TryGetBuildingByAWBWId(building.Value.TerrainID!.Value, out var tile))
                    continue;

                foreach (var player in data.ReplayInfo.Players)
                    knowledge[player.Value.TeamName] = tile;

                BuildingKnowledge.Add(building.Key, knowledge);
            }

            statsReadout.RegisterReadouts(StatsReadouts);

            var newReadouts = new Dictionary<long, PlayerStatsReadout>();
            foreach (var readout in StatsReadouts)
                newReadouts.Add(readout.Key, readout.Value.Clone());
            StatsReadouts = newReadouts;
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
            WeatherType = turn.StartWeather.Type;

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

        public void AddGameOverAction()
        {
            CurrentTurn.Actions ??= new List<IReplayAction>();

            if (CurrentTurn.Actions.Count != 0)
            {
                if (CurrentTurn.Actions[^1] is IActionCanEndGame lastAction && lastAction.EndsGame())
                    return;
            }

            //If the end does not have a game over action. Its likely a draw.
            var gameOverAction = new GameOverAction
            {
                FinishedDay = CurrentDay,
                GameEndDate = null,
                EndMessage = "Match ended in Draw!",
                Winners = PlayerInfos.Select(x => x.Key).ToList(),
                Draw = true,
                Losers = null
            };

            CurrentTurn.Actions.Add(gameOverAction);
        }

        public EndTurnDesync FinishTurnAndCheckForDesyncs(StatsHandler statsReadout, TurnData nextTurn)
        {
            StatsReadouts[nextTurn.ActivePlayerID].GeneratedMoney += nextTurn.Players[nextTurn.ActivePlayerID].Funds - FundsValuesForPlayers[nextTurn.ActivePlayerID];
            statsReadout.RegisterReadouts(StatsReadouts);

            var newReadouts = new Dictionary<long, PlayerStatsReadout>();
            foreach (var readout in StatsReadouts)
                newReadouts.Add(readout.Key, readout.Value.Clone());
            StatsReadouts = newReadouts;

            var desync = new EndTurnDesync
            {
                TurnIndex = CurrentTurnIndex,
                NextPlayerID = nextTurn.ActivePlayerID
            };

            foreach (var unit in Units)
            {
                if (!nextTurn.ReplayUnit.TryGetValue(unit.Key, out var nextTurnUnit))
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

        public ReplayUnit RemoveUnitFromSetupContext(long unitID, Dictionary<long, ReplayUnit> deletedUnits, out int value)
        {
            if (!Units.Remove(unitID, out var unitToRemove))
                throw new ReplayMissingUnitException(unitID);

            if (!deletedUnits.ContainsKey(unitID))
                deletedUnits.Add(unitID, unitToRemove.Clone());

            value = ReplayActionHelper.CalculateUnitCost(unitToRemove, coStorage.GetCOByAWBWId(PlayerTurns[unitToRemove.PlayerID!.Value].ActiveCOID).DayToDayPower, null);

            if (unitToRemove.CargoUnits != null && unitToRemove.CargoUnits.Count > 0)
            {
                foreach (var cargoUnitID in unitToRemove.CargoUnits)
                {
                    RemoveUnitFromSetupContext(cargoUnitID, deletedUnits, out var cargoValue);
                    value += cargoValue;
                }
            }

            return unitToRemove;
        }

        public void AdjustStatReadoutsFromUnitList(long ownerID, IEnumerable<ReplayUnit> units, long? skipUnitId = null)
        {
            foreach (var unit in units)
            {
                if (unit.ID == skipUnitId)
                    continue;

                var value = ReplayActionHelper.CalculateUnitCost(unit, coStorage.GetCOByAWBWId(PlayerTurns[unit.PlayerID!.Value].ActiveCOID).DayToDayPower, null);

                bool unitAlive = Units.TryGetValue(unit.ID, out var changedUnit);
                if (unitAlive)
                    value -= ReplayActionHelper.CalculateUnitCost(changedUnit, coStorage.GetCOByAWBWId(PlayerTurns[changedUnit.PlayerID!.Value].ActiveCOID).DayToDayPower, null);

                //Don't care if the unit change doesn't affect value. In repairing/resupplying units.
                if (value <= 0)
                    continue;

                StatsReadouts[unit.PlayerID!.Value].RegisterUnitStats(unitAlive ? UnitStatType.LostUnit : UnitStatType.LostUnit | UnitStatType.UnitCountChanged, unit.UnitName, unit.PlayerID!.Value, value);
                if (unit.PlayerID != ownerID)
                    StatsReadouts[ownerID].RegisterUnitStats(unitAlive ? UnitStatType.DamageUnit : UnitStatType.DamageUnit | UnitStatType.UnitCountChanged, unit.UnitName, unit.PlayerID!.Value, value);
            }
        }

        public void RegisterDiscoveryAndSetUndo(DiscoveryCollection collection)
        {
            collection.OriginalDiscovery.Clear();

            foreach (var id in collection.DiscoveryByID)
            {
                foreach (var discovered in id.Value.DiscoveredBuildings)
                {
                    if (!BuildingKnowledge.TryGetValue(discovered.Key, out var discoveries))
                        continue;

                    if (!collection.OriginalDiscovery.TryGetValue(discovered.Key, out var registered))
                    {
                        registered = new Dictionary<string, BuildingTile>();
                        collection.OriginalDiscovery.Add(discovered.Key, registered);
                    }

                    if (discoveries.TryGetValue(id.Key, out var before))
                        registered[id.Key] = before;

                    if (BuildingStorage.TryGetBuildingByAWBWId(discovered.Value.TerrainID!.Value, out var after))
                        discoveries[id.Key] = after;
                }
            }
        }

        public void UpdateBuildingAfterCapture(ReplayBuilding building, HashSet<string> teamsWhoSaw)
        {
            if (!BuildingKnowledge.TryGetValue(building.Position, out var discoveries))
                return;

            var buildingData = BuildingStorage.GetBuildingByAWBWId(building.TerrainID!.Value);

            discoveries[ActivePlayerTeam] = buildingData;

            foreach (var team in teamsWhoSaw)
                discoveries[team] = buildingData;
        }
    }

    public class EndTurnDesync
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
