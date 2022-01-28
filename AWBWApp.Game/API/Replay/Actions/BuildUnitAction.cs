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

            var unit = (JObject)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)jObject["newUnit"], turnData.ActiveTeam, turnData.ActivePlayerID);

            Logger.Log("Missing Fog Parse.");
            action.NewUnit = ReplayActionHelper.ParseJObjectIntoReplayUnit(unit);
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
            unit.CanMove.Value = false; //Todo: Is this always the case?

            if (controller.Map.TryGetDrawableBuilding(unit.MapPosition, out DrawableBuilding building))
                building.HasDoneAction.Value = true;

            controller.UpdateFogOfWar();
            yield break;
        }

        public void UndoAction(ReplayController controller, bool immediate)
        {
            Logger.Log("Undoing Build Action.");
            controller.Map.DestroyUnit(NewUnit.ID, false, immediate);
        }
    }
}
