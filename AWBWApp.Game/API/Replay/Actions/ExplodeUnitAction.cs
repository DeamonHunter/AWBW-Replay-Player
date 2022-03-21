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

        private List<ReplayUnit> originalUnits;

        private const int explosion_range = 3;
        private ReplayUnit originalExplodingUnit;

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
            MoveUnit?.SetupAndUpdate(controller, context);

            if (!context.Units.Remove(ExplodedUnitId, out var explodedUnit))
                throw new ReplayMissingUnitException(ExplodedUnitId);

            originalExplodingUnit = explodedUnit.Clone();

            originalUnits = new List<ReplayUnit>();

            var destroyedUnits = new HashSet<long>();
            var explodingPlayer = controller.Players[originalExplodingUnit.PlayerID!.Value];

            foreach (var unit in context.Units)
            {
                if (!unit.Value.PlayerID.HasValue || explodingPlayer.OnSameTeam(controller.Players[unit.Value.PlayerID!.Value]))
                    continue;

                if (unit.Value.BeingCarried.HasValue && unit.Value.BeingCarried.Value)
                    continue;

                var position = unit.Value.Position!.Value;
                var distance = (position - originalExplodingUnit.Position!.Value).ManhattonDistance();

                if (distance <= explosion_range)
                {
                    originalUnits.Add(unit.Value.Clone());

                    unit.Value.HitPoints = unit.Value.HitPoints!.Value + HPChange;

                    if (unit.Value.HitPoints <= 0)
                    {
                        destroyedUnits.Add(unit.Key);

                        if (unit.Value.CargoUnits != null && unit.Value.CargoUnits.Count > 0)
                        {
                            foreach (var cargoUnitID in unit.Value.CargoUnits)
                            {
                                if (!context.Units.TryGetValue(cargoUnitID, out var cargoUnit))
                                    throw new ReplayMissingUnitException(cargoUnitID);

                                originalUnits.Add(cargoUnit);
                                destroyedUnits.Add(cargoUnitID);
                            }
                        }
                    }
                }
            }

            foreach (var unit in destroyedUnits)
                context.Units.Remove(unit);
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
                        if (controller.ActivePlayer.OnSameTeam(owner))
                            continue;

                        var originalValue = ReplayActionHelper.CalculateUnitCost(unit, owner.ActiveCO.Value.CO.DayToDayPower, null);
                        unit.HealthPoints.Value += (int)HPChange;
                        owner.UnitValue.Value -= (originalValue - ReplayActionHelper.CalculateUnitCost(unit, owner.ActiveCO.Value.CO.DayToDayPower, null));

                        if (unit.HealthPoints.Value <= 0)
                            controller.Map.DeleteUnit(unit.UnitID, true);
                    }
                }
                yield return ReplayWait.WaitForMilliseconds(100);
            }
        }

        public void UndoAction(ReplayController controller)
        {
            foreach (var replayUnit in originalUnits)
            {
                var owner = controller.Players[replayUnit.PlayerID!.Value];
                var originalValue = ReplayActionHelper.CalculateUnitCost(replayUnit, owner.ActiveCO.Value.CO.DayToDayPower, null);

                if (controller.Map.TryGetDrawableUnit(replayUnit.Position!.Value, out var unit))
                {
                    owner.UnitValue.Value += (originalValue - ReplayActionHelper.CalculateUnitCost(unit, owner.ActiveCO.Value.CO.DayToDayPower, null));
                    unit.UpdateUnit(replayUnit);
                }
                else
                {
                    controller.Map.AddUnit(replayUnit);
                    owner.UnitValue.Value += originalValue;
                }
            }

            controller.Map.AddUnit(originalExplodingUnit);
            controller.ActivePlayer.UnitValue.Value += ReplayActionHelper.CalculateUnitCost(originalExplodingUnit, controller.ActivePlayer.ActiveCO.Value.CO.DayToDayPower, null);
            MoveUnit?.UndoAction(controller);
        }
    }
}
