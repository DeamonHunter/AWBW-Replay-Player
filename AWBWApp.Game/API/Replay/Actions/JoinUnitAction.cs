using System;
using System.Collections.Generic;
using AWBWApp.Game.Exceptions;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Replay;
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

            action.JoiningUnitID = (long)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)joinData["joinID"], turnData.ActiveTeam, turnData.ActivePlayerID);
            action.JoinedUnit = ReplayActionHelper.ParseJObjectIntoReplayUnit((JObject)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)joinData["unit"], turnData.ActiveTeam, turnData.ActivePlayerID));
            action.FundsAfterJoin = (int)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)joinData["newFunds"], turnData.ActiveTeam, turnData.ActivePlayerID);
            return action;
        }
    }

    public class JoinUnitAction : IReplayAction
    {
        public MoveUnitAction MoveUnit;

        public long JoiningUnitID { get; set; }

        public ReplayUnit JoinedUnit { get; set; }

        public int FundsAfterJoin { get; set; }

        //Variables for Undoing
        private ReplayUnit originalJoiningUnit;
        private ReplayUnit originalJoinedUnit;
        private int valueChange;
        private int fundsChange;

        public string GetReadibleName(ReplayController controller, bool shortName)
        {
            if (shortName)
                return MoveUnit != null ? "Move + Join" : "Join";

            if (MoveUnit == null)
                return $"{originalJoiningUnit.UnitName} Joins {originalJoinedUnit.UnitName}";

            return $"{originalJoiningUnit.UnitName} Moves + Joins {originalJoinedUnit.UnitName}";
        }

        //Todo: Track funds
        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
            MoveUnit?.SetupAndUpdate(controller, context);

            if (!context.Units.Remove(JoiningUnitID, out var unit))
                throw new ReplayMissingUnitException(JoiningUnitID);

            originalJoiningUnit = unit.Clone();

            if (!context.Units.TryGetValue(JoinedUnit.ID, out unit))
                throw new ReplayMissingUnitException(JoinedUnit.ID);

            originalJoinedUnit = unit.Clone();
            unit.Overwrite(JoinedUnit);

            fundsChange = FundsAfterJoin - context.FundsValuesForPlayers[context.ActivePlayerID];
            context.FundsValuesForPlayers[context.ActivePlayerID] = FundsAfterJoin;

            var dayToDay = controller.COStorage.GetCOByAWBWId(context.PlayerTurns[context.ActivePlayerID].ActiveCOID).DayToDayPower;
            var originalJoiningUnitValue = ReplayActionHelper.CalculateUnitCost(originalJoiningUnit, dayToDay, null);
            var originalJoinedUnitValue = ReplayActionHelper.CalculateUnitCost(originalJoinedUnit, dayToDay, null);
            var joinedUnitValueChange = ReplayActionHelper.CalculateUnitCost(JoinedUnit, dayToDay, null);
            valueChange = joinedUnitValueChange - (originalJoinedUnitValue + originalJoiningUnitValue);

            var valueLost = Math.Max(-valueChange, 0);
            context.StatsReadouts[originalJoinedUnit.PlayerID!.Value].RegisterUnitStats(UnitStatType.JoinUnit | UnitStatType.UnitCountChanged, originalJoiningUnit.UnitName, originalJoiningUnit.PlayerID!.Value, valueLost);
        }

        public bool HasVisibleAction(ReplayController controller)
        {
            if (MoveUnit != null && MoveUnit.HasVisibleAction(controller))
                return true;

            return !controller.ShouldPlayerActionBeHidden(JoinedUnit.Position!.Value);
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

            controller.Map.DeleteUnit(JoiningUnitID, false);
            var joinedUnit = controller.Map.GetDrawableUnit(JoinedUnit.ID);

            controller.ActivePlayer.UnitValue.Value += valueChange;
            controller.Stats.CurrentTurnStatsReadout[originalJoinedUnit.PlayerID!.Value].RegisterUnitStats(UnitStatType.JoinUnit | UnitStatType.UnitCountChanged, originalJoiningUnit.UnitName, originalJoiningUnit.PlayerID!.Value, Math.Max(-valueChange, 0));

            joinedUnit.UpdateUnit(JoinedUnit);
            controller.UpdateFogOfWar();
        }

        public void UndoAction(ReplayController controller)
        {
            Logger.Log("Undoing Join Action.");

            controller.Map.AddUnit(originalJoiningUnit);
            controller.Map.GetDrawableUnit(originalJoinedUnit.ID).UpdateUnit(originalJoinedUnit);
            controller.Stats.CurrentTurnStatsReadout[originalJoinedUnit.PlayerID!.Value].RegisterUnitStats(UnitStatType.JoinUnit | UnitStatType.UnitCountChanged | UnitStatType.Undo, originalJoiningUnit.UnitName, originalJoiningUnit.PlayerID!.Value, Math.Max(-valueChange, 0));

            controller.ActivePlayer.UnitValue.Value -= valueChange;
            controller.ActivePlayer.Funds.Value -= fundsChange;

            controller.UpdateFogOfWar();
            MoveUnit?.UndoAction(controller);
        }
    }
}
