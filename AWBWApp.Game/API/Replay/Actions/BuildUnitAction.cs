using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
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
            return action;
        }
    }

    public class BuildUnitAction : IReplayAction
    {
        public string ReadibleName => "Build";

        public ReplayUnit NewUnit;
        private int unitCost;

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
            var activePlayer = context.Players[context.ActivePlayerID];

            context.Units.Add(NewUnit.ID, NewUnit.Clone());
            var dayToDay = controller.COStorage.GetCOByAWBWId(activePlayer.ActiveCOID).DayToDayPower;
            var currentPower = controller.GetActivePowerForPlayer(NewUnit.PlayerID!.Value);

            unitCost = ReplayActionHelper.CalculateUnitCost(NewUnit, dayToDay, currentPower?.COPower);

            activePlayer.Funds -= unitCost;
        }

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

            controller.UpdateFogOfWar();
            controller.Map.PlaySelectionAnimation(unit);
            yield break;
        }

        public void UndoAction(ReplayController controller)
        {
            Logger.Log("Undoing Build Action.");
            var unit = controller.Map.DeleteUnit(NewUnit.ID, false);

            if (controller.Map.TryGetDrawableBuilding(unit.MapPosition, out DrawableBuilding building))
                building.HasDoneAction.Value = false;

            controller.ActivePlayer.Funds.Value += unitCost;
            controller.UpdateFogOfWar();
        }
    }
}
