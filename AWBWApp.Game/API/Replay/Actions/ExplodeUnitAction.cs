using System;
using System.Collections.Generic;
using AWBWApp.Game.Exceptions;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using Newtonsoft.Json.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Logging;

namespace AWBWApp.Game.API.Replay.Actions
{
    public class ExplodeUnitActionBuilder : IReplayActionBuilder
    {
        public string Code => "Explode";

        public ReplayActionDatabase Database { get; set; }

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = new ExplodeUnitAction();

            var moveObj = jObject["Move"];

            if (moveObj is JObject moveData)
            {
                var moveAction = Database.ParseJObjectIntoReplayAction(moveData, replayData, turnData);
                action.MoveUnit = moveAction as MoveUnitAction;
                if (moveAction == null)
                    throw new Exception("Capture action was expecting a movement action.");
            }

            var explodeData = (JObject)jObject["Explode"];
            if (explodeData == null)
                throw new Exception("Join Replay Action did not contain information about Join.");

            action.HPChange = (float)explodeData["hp"];
            action.ExplodedUnitId = (long)explodeData["unitId"];

            return action;
        }
    }

    public class ExplodeUnitAction : IReplayAction
    {
        public string ReadibleName => "Explode";

        public long ExplodedUnitId { get; set; }

        public float HPChange { get; set; }

        public MoveUnitAction MoveUnit;

        private readonly Dictionary<long, ReplayUnit> originalUnits = new Dictionary<long, ReplayUnit>();

        private const int explosion_range = 3;
        private ReplayUnit originalExplodingUnit;

        public string GetReadibleName(ReplayController controller, bool shortName)
        {
            if (shortName)
                return MoveUnit != null ? "Move + Explode" : "Explode";

            if (MoveUnit == null)
                return $"{originalExplodingUnit.UnitName} Explodes";

            return $"{originalExplodingUnit.UnitName} Moves + Explodes";
        }

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
            MoveUnit?.SetupAndUpdate(controller, context);

            if (!context.Units.Remove(ExplodedUnitId, out var explodedUnit))
                throw new ReplayMissingUnitException(ExplodedUnitId);

            originalExplodingUnit = explodedUnit.Clone();

            foreach (var unit in context.Units)
            {
                if (!unit.Value.PlayerID.HasValue)
                    continue;

                if (unit.Value.BeingCarried.HasValue && unit.Value.BeingCarried.Value)
                    continue;

                var position = unit.Value.Position!.Value;
                var distance = (position - originalExplodingUnit.Position!.Value).ManhattonDistance();

                if (distance <= explosion_range)
                {
                    originalUnits.Add(unit.Key, unit.Value.Clone());

                    unit.Value.HitPoints = unit.Value.HitPoints!.Value + HPChange;
                    if (unit.Value.HitPoints < 0.1f)
                        unit.Value.HitPoints = 0.1f;
                }
            }

            context.AdjustStatsToPlayerAction(context.ActivePlayerID, originalUnits.Values);
        }

        public bool HasVisibleAction(ReplayController controller)
        {
            if (MoveUnit != null && MoveUnit.HasVisibleAction(controller))
                return true;

            foreach (var unit in originalUnits)
            {
                if (controller.ShouldPlayerActionBeHidden(unit.Value))
                    continue;

                return true;
            }

            return false;
        }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Explode Action.");

            if (MoveUnit != null)
            {
                foreach (var transformable in MoveUnit.PerformAction(controller))
                    yield return transformable;
            }

            var explodingUnit = controller.Map.GetDrawableUnit(ExplodedUnitId);
            if (explodingUnit.OwnerID == null)
                throw new Exception("A bomb without an owner exploded?");

            //Todo: Bigger explosion?
            controller.Map.DeleteUnit(ExplodedUnitId, false);
            controller.Map.PlayEffect("Effects/Explosion/Explosion-Land", 500, explodingUnit.MapPosition + new Vector2I(0, -1), 0, x => x.ScaleTo(3));

            controller.ActivePlayer.UnitValue.Value -= ReplayActionHelper.CalculateUnitCost(explodingUnit, controller.ActivePlayer.ActiveCO.Value.CO.DayToDayPower, null);

            for (int i = 1; i <= explosion_range; i++)
            {
                foreach (var tile in Vec2IHelper.GetAllTilesWithDistance(explodingUnit.MapPosition, i))
                {
                    if (controller.Map.TryGetDrawableUnit(tile, out var unit))
                    {
                        if (!unit.OwnerID.HasValue)
                            continue;

                        var owner = controller.Players[unit.OwnerID.Value];
                        var originalValue = ReplayActionHelper.CalculateUnitCost(unit, owner.ActiveCO.Value.CO.DayToDayPower, null);
                        unit.HealthPoints.Value += (int)HPChange;
                        if (unit.HealthPoints.Value < 0.1f)
                            unit.HealthPoints.Value = 1;

                        owner.UnitValue.Value -= (originalValue - ReplayActionHelper.CalculateUnitCost(unit, owner.ActiveCO.Value.CO.DayToDayPower, null));
                    }
                }
                yield return ReplayWait.WaitForMilliseconds(100);
            }

            controller.UpdateFogOfWar();
            ReplayActionHelper.AdjustStatsToPlayerAction(controller, controller.ActivePlayer.ID, originalUnits.Values, false);
        }

        public void UndoAction(ReplayController controller)
        {
            Logger.Log("Undoing Explode Action.");

            ReplayActionHelper.AdjustStatsToPlayerAction(controller, controller.ActivePlayer.ID, originalUnits.Values, true);

            foreach (var replayUnit in originalUnits)
            {
                var owner = controller.Players[replayUnit.Value.PlayerID!.Value];
                var originalValue = ReplayActionHelper.CalculateUnitCost(replayUnit.Value, owner.ActiveCO.Value.CO.DayToDayPower, null);

                if (controller.Map.TryGetDrawableUnit(replayUnit.Key, out var unit))
                {
                    owner.UnitValue.Value += (originalValue - ReplayActionHelper.CalculateUnitCost(unit, owner.ActiveCO.Value.CO.DayToDayPower, null));
                    unit.UpdateUnit(replayUnit.Value);
                }
                else
                {
                    controller.Map.AddUnit(replayUnit.Value);
                    owner.UnitValue.Value += originalValue;
                }
            }

            controller.Map.AddUnit(originalExplodingUnit);
            controller.ActivePlayer.UnitValue.Value += ReplayActionHelper.CalculateUnitCost(originalExplodingUnit, controller.ActivePlayer.ActiveCO.Value.CO.DayToDayPower, null);

            controller.UpdateFogOfWar();
            MoveUnit?.UndoAction(controller);
        }
    }
}
