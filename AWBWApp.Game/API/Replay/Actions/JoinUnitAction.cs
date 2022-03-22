using System;
using System.Collections.Generic;
using AWBWApp.Game.Exceptions;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using Newtonsoft.Json.Linq;
using osu.Framework.Logging;

namespace AWBWApp.Game.API.Replay.Actions
{
    public class JoinUnitActionBuilder : IReplayActionBuilder
    {
        public string Code => "Join";

        public ReplayActionDatabase Database { get; set; }

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = new JoinUnitAction();

            var moveObj = jObject["Move"];

            if (moveObj is JObject moveData)
            {
                var moveAction = Database.ParseJObjectIntoReplayAction(moveData, replayData, turnData);
                action.MoveUnit = moveAction as MoveUnitAction;
                if (moveAction == null)
                    throw new Exception("Capture action was expecting a movement action.");
            }

            var joinData = (JObject)jObject["Join"];
            if (joinData == null)
                throw new Exception("Join Replay Action did not contain information about Join.");

            action.JoiningUnitId = (long)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)joinData["joinID"], turnData.ActiveTeam, turnData.ActivePlayerID);
            action.JoinedUnit = ReplayActionHelper.ParseJObjectIntoReplayUnit((JObject)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)joinData["unit"], turnData.ActiveTeam, turnData.ActivePlayerID));
            action.FundsAfterJoin = (int)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)joinData["newFunds"], turnData.ActiveTeam, turnData.ActivePlayerID);
            return action;
        }
    }

    public class JoinUnitAction : IReplayAction
    {
        public string ReadibleName => "Join";

        public MoveUnitAction MoveUnit;

        public long JoiningUnitId { get; set; }

        public ReplayUnit JoinedUnit { get; set; }

        public int FundsAfterJoin { get; set; }

        //Variables for Undoing
        private ReplayUnit originalJoiningUnit;
        private ReplayUnit originalJoinedUnit;
        private int valueChange;
        private int fundsChange;

        //Todo: Track funds
        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
            MoveUnit?.SetupAndUpdate(controller, context);

            if (!context.Units.Remove(JoiningUnitId, out var unit))
                throw new ReplayMissingUnitException(JoiningUnitId);

            originalJoiningUnit = unit.Clone();

            if (!context.Units.TryGetValue(JoinedUnit.ID, out unit))
                throw new ReplayMissingUnitException(JoinedUnit.ID);

            originalJoinedUnit = unit.Clone();
            unit.Overwrite(JoinedUnit);

            fundsChange = FundsAfterJoin - context.FundsValuesForPlayers[context.ActivePlayerID];
            context.FundsValuesForPlayers[context.ActivePlayerID] = FundsAfterJoin;

            var dayToDay = controller.COStorage.GetCOByAWBWId(context.PlayerTurns[context.ActivePlayerID].ActiveCOID).DayToDayPower;
            var joinedUnitValueChange = ReplayActionHelper.CalculateUnitCost(JoinedUnit, dayToDay, null) - ReplayActionHelper.CalculateUnitCost(originalJoinedUnit, dayToDay, null);
            valueChange = joinedUnitValueChange - ReplayActionHelper.CalculateUnitCost(originalJoiningUnit, dayToDay, null);
        }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Join Action.");
            Logger.Log("Join animation not completed.");

            if (MoveUnit != null)
            {
                foreach (var transformable in MoveUnit.PerformAction(controller))
                    yield return transformable;
            }

            controller.ActivePlayer.Funds.Value = FundsAfterJoin;

            controller.Map.DeleteUnit(JoiningUnitId, false);
            var joinedUnit = controller.Map.GetDrawableUnit(JoinedUnit.ID);

            controller.ActivePlayer.UnitValue.Value += valueChange;

            joinedUnit.UpdateUnit(JoinedUnit);
        }

        public void UndoAction(ReplayController controller)
        {
            controller.Map.AddUnit(originalJoiningUnit);
            controller.Map.GetDrawableUnit(originalJoinedUnit.ID).UpdateUnit(originalJoinedUnit);

            controller.ActivePlayer.UnitValue.Value -= valueChange;
            controller.ActivePlayer.Funds.Value += fundsChange;

            MoveUnit?.UndoAction(controller);
        }
    }
}
