using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Replay;
using Newtonsoft.Json.Linq;
using osu.Framework.Logging;

namespace AWBWApp.Game.API.Replay.Actions
{
    public class BuildUnitActionBuilder : IReplayActionBuilder
    {
        public string Code => "Build";

        public ReplayActionDatabase Database { get; set; }

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = new BuildUnitAction();

            var unit = ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)jObject["newUnit"], turnData.ActiveTeam, turnData.ActivePlayerID);

            if (unit.Type == JTokenType.Null)
                return null;

            action.NewUnit = ReplayActionHelper.ParseJObjectIntoReplayUnit((JObject)unit);

            if (jObject.TryGetValue("discovered", out var discovered))
            {
                var collection = new DiscoveryCollection(discovered);
                if (!collection.IsEmpty())
                    action.Discovered = collection;
            }

            return action;
        }
    }

    public class BuildUnitAction : IReplayAction
    {
        public ReplayUnit NewUnit;
        private int unitCost;
        private int unitValue;

        public DiscoveryCollection Discovered;

        public string GetReadibleName(ReplayController controller, bool shortName)
        {
            if (shortName)
                return "Build";

            return $"Build {NewUnit.UnitName}";
        }

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
            var activePlayer = context.PlayerTurns[context.ActivePlayerID];

            context.Units.Add(NewUnit.ID, NewUnit.Clone());
            var dayToDay = controller.COStorage.GetCOByAWBWId(activePlayer.ActiveCOID).DayToDayPower;
            var currentPower = controller.GetActivePowerForPlayer(NewUnit.PlayerID!.Value);

            unitCost = ReplayActionHelper.CalculateUnitCost(NewUnit, dayToDay, currentPower?.COPower);
            context.FundsValuesForPlayers[context.ActivePlayerID] -= unitCost;
            unitValue = ReplayActionHelper.CalculateUnitCost(NewUnit, dayToDay, null); //unitValue doesn't care about active powers

            activePlayer.Funds -= unitCost;

            context.StatsReadouts[NewUnit.PlayerID!.Value].RegisterUnitStats(UnitStatType.BuildUnit | UnitStatType.UnitCountChanged, NewUnit.UnitName, NewUnit.PlayerID!.Value, unitValue);
            context.StatsReadouts[NewUnit.PlayerID!.Value].MoneySpentOnBuildingUnits += unitCost;
            if (Discovered != null)
                context.RegisterDiscoveryAndSetUndo(Discovered);
        }

        public bool HasVisibleAction(ReplayController controller) => !controller.ShouldPlayerActionBeHidden(NewUnit);

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Build Action.");

            if (!NewUnit.PlayerID.HasValue || !NewUnit.Cost.HasValue)
                throw new Exception("The unit being built was not set up correctly?");

            if (!NewUnit.HitPoints.HasValue || NewUnit.HitPoints != 10)
                throw new Exception("Created unit didn't have 10 hp?");

            var unit = controller.Map.AddUnit(NewUnit);
            unit.CanMove.Value = false;

            if (controller.Map.TryGetDrawableBuilding(unit.MapPosition, out DrawableBuilding building))
                building.HasDoneAction.Value = true;

            controller.ActivePlayer.Funds.Value -= unitCost;
            controller.ActivePlayer.UnitValue.Value += unitValue;

            controller.UpdateFogOfWar();
            if (Discovered != null)
                controller.Map.RegisterDiscovery(Discovered);

            if (controller.ShowAnimationsWhenUnitsHidden.Value || !controller.ShouldPlayerActionBeHidden(unit.MapPosition, unit.UnitData.MovementType == MovementType.Air))
                controller.Map.PlaySelectionAnimation(unit);

            controller.Stats.CurrentTurnStatsReadout[unit.OwnerID!.Value].RegisterUnitStats(UnitStatType.BuildUnit | UnitStatType.UnitCountChanged, NewUnit.UnitName, NewUnit.PlayerID!.Value, unitValue);
            controller.Stats.CurrentTurnStatsReadout[unit.OwnerID!.Value].MoneySpentOnBuildingUnits += unitCost;
            yield break;
        }

        public void UndoAction(ReplayController controller)
        {
            Logger.Log("Undoing Build Action.");
            var unit = controller.Map.DeleteUnit(NewUnit.ID, false);

            controller.Stats.CurrentTurnStatsReadout[unit.OwnerID!.Value].RegisterUnitStats(UnitStatType.BuildUnit | UnitStatType.UnitCountChanged | UnitStatType.Undo, NewUnit.UnitName, NewUnit.PlayerID!.Value, unitValue);
            controller.Stats.CurrentTurnStatsReadout[unit.OwnerID!.Value].MoneySpentOnBuildingUnits -= unitCost;

            if (controller.Map.TryGetDrawableBuilding(unit.MapPosition, out DrawableBuilding building))
                building.HasDoneAction.Value = false;

            if (Discovered != null)
                controller.Map.UndoDiscovery(Discovered);

            controller.ActivePlayer.Funds.Value += unitCost;
            controller.ActivePlayer.UnitValue.Value -= unitValue;
            controller.UpdateFogOfWar();
        }
    }
}
