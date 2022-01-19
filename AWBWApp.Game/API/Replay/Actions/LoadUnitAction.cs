using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using Newtonsoft.Json.Linq;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Logging;

namespace AWBWApp.Game.API.Replay.Actions
{
    public class LoadUnitActionBuilder : IReplayActionBuilder
    {
        public string Code => "Load";

        public ReplayActionDatabase Database { get; set; }

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = new LoadUnitAction();

            var moveObj = jObject["Move"];

            if (moveObj is JObject moveData)
            {
                var moveAction = Database.ParseJObjectIntoReplayAction(moveData, replayData, turnData);
                action.MoveUnit = moveAction as MoveUnitAction;
                if (moveAction == null)
                    throw new Exception("Capture action was expecting a movement action.");
            }

            var attackData = (JObject)jObject["Load"];
            if (attackData == null)
                throw new Exception("Capture Replay Action did not contain information about Capture.");

            action.LoadedId = (int)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)attackData["loaded"], turnData.ActiveTeam, turnData.ActivePlayerID);
            action.TransportID = (int)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)attackData["transport"], turnData.ActiveTeam, turnData.ActivePlayerID);
            return action;
        }
    }

    public class LoadUnitAction : IReplayAction
    {
        public long LoadedId { get; set; }
        public long TransportID { get; set; }

        public MoveUnitAction MoveUnit;

        public List<Transformable> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Supply Action.");
            Logger.Log("Load animation not completed.");

            List<Transformable> transformables;

            if (MoveUnit != null)
            {
                transformables = MoveUnit.PerformAction(controller);
            }
            else
            {
                transformables = new List<Transformable>();
            }

            var loadingUnit = controller.Map.GetDrawableUnit(LoadedId);
            var transportUnit = controller.Map.GetDrawableUnit(TransportID);

            var sequence = transportUnit.WaitForTransformationToComplete(loadingUnit);
            sequence.OnComplete(x =>
            {
                loadingUnit.BeingCarried.Value = true;
                transportUnit.Cargo.Add(loadingUnit.UnitID);
            });

            return transformables;
        }

        public void UndoAction(ReplayController controller, bool immediate)
        {
            Logger.Log("Undoing Capture Action.");
            throw new NotImplementedException("Undo Action for Capture Building is not complete");
            //controller.Map.DestroyUnit(NewUnit.ID, false, immediate);
        }
    }
}
