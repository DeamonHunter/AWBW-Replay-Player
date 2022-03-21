using System;
using System.Collections.Generic;
using AWBWApp.Game.Exceptions;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using Newtonsoft.Json.Linq;
using osu.Framework.Logging;

namespace AWBWApp.Game.API.Replay.Actions
{
    public class DeleteUnitActionBuilder : IReplayActionBuilder
    {
        public string Code => "Delete";

        public ReplayActionDatabase Database { get; set; }

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = new DeleteUnitAction();

            var deleteData = (JObject)jObject["Delete"];
            if (deleteData == null)
                throw new Exception("Join Replay Action did not contain information about Join.");

            var moveObj = jObject["Move"];

            if (moveObj is JObject moveData)
            {
                var moveAction = Database.ParseJObjectIntoReplayAction(moveData, replayData, turnData);
                action.MoveUnit = moveAction as MoveUnitAction;
                if (moveAction == null)
                    throw new Exception("Delete action was expecting a movement action.");
            }

            action.DeletedUnitId = (long)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)deleteData["unitId"], turnData.ActiveTeam, turnData.ActivePlayerID);
            return action;
        }
    }

    public class DeleteUnitAction : IReplayAction
    {
        public string ReadibleName => "Delete";

        public MoveUnitAction MoveUnit;

        public long DeletedUnitId { get; set; }

        private ReplayUnit originalUnit;
        private int unitValue;

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
            MoveUnit?.SetupAndUpdate(controller, context);

            if (!context.Units.Remove(DeletedUnitId, out var unit))
                throw new ReplayMissingUnitException(DeletedUnitId);

            originalUnit = unit.Clone();

            unitValue = ReplayActionHelper.CalculateUnitCost(unit, controller.COStorage.GetCOByAWBWId(context.PlayerTurns[context.ActivePlayerID].ActiveCOID).DayToDayPower, null); //Unit funds does not care about the current active powers
        }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Delete Action.");

            if (MoveUnit != null)
            {
                foreach (var transformable in MoveUnit.PerformAction(controller))
                    yield return transformable;
            }

            controller.Map.DeleteUnit(DeletedUnitId, true);
            controller.ActivePlayer.UnitValue.Value -= unitValue;
        }

        public void UndoAction(ReplayController controller)
        {
            Logger.Log("Undoing Delete Action.");
            controller.Map.AddUnit(originalUnit);
            controller.ActivePlayer.UnitValue.Value += unitValue;

            MoveUnit?.UndoAction(controller);
        }
    }
}
