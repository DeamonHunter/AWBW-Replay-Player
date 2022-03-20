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

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
            context.Units.Add(NewUnit.ID, NewUnit.Clone());
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

            var dayToDay = controller.ActivePlayer.ActiveCO.Value.CO.DayToDayPower;
            var currentPower = controller.GetActivePowerForPlayer(unit.OwnerID!.Value);

            controller.ActivePlayer.Funds.Value -= ReplayActionHelper.CalculateUnitCost(NewUnit, dayToDay, currentPower?.COPower);

            controller.UpdateFogOfWar();
            controller.Map.PlaySelectionAnimation(unit);
            yield break;
        }

        public void UndoAction(ReplayController controller)
        {
            var unit = controller.Map.DeleteUnit(NewUnit.ID, false);

            if (controller.Map.TryGetDrawableBuilding(unit.MapPosition, out DrawableBuilding building))
                building.HasDoneAction.Value = false;
        }
    }
}
