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
        public ReplayUnit NewUnit;

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Build Action.");
            var unit = controller.Map.AddUnit(NewUnit);
            unit.CanMove.Value = false;

            if (controller.Map.TryGetDrawableBuilding(unit.MapPosition, out DrawableBuilding building))
                building.HasDoneAction.Value = true;

            controller.UpdateFogOfWar();
            controller.Map.PlaySelectionAnimation(unit);
            yield break;
        }

        public void UndoAction(ReplayController controller, bool immediate)
        {
            throw new NotImplementedException("Undo Build Action is not complete");
        }
    }
}
