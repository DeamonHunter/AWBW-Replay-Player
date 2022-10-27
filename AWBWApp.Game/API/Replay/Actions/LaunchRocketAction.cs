using System;
using System.Collections.Generic;
using System.Linq;
using AWBWApp.Game.Exceptions;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using Newtonsoft.Json.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Logging;

namespace AWBWApp.Game.API.Replay.Actions
{
    public class LaunchUnitActionBuilder : IReplayActionBuilder
    {
        public string Code => "Launch";

        public ReplayActionDatabase Database { get; set; }

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = new LaunchRocketAction();

            var moveObj = jObject["Move"];

            if (moveObj is JObject moveData)
            {
                var moveAction = Database.ParseJObjectIntoReplayAction(moveData, replayData, turnData);
                action.MoveUnit = moveAction as MoveUnitAction;
                if (moveAction == null)
                    throw new Exception("Capture action was expecting a movement action.");
            }

            var launchData = (JObject)jObject["Launch"];
            if (launchData == null)
                throw new Exception("Join Replay Action did not contain information about Join.");

            action.SiloPosition = new Vector2I((int)launchData["siloX"], (int)launchData["siloY"]);
            action.TargetPosition = new Vector2I((int)launchData["targetX"], (int)launchData["targetY"]);
            action.HPChange = (float)launchData["hp"];
            return action;
        }
    }

    public class LaunchRocketAction : IReplayAction
    {
        public Vector2I SiloPosition;
        public Vector2I TargetPosition;
        public float HPChange;

        public MoveUnitAction MoveUnit;

        private readonly Dictionary<long, ReplayUnit> originalUnits = new Dictionary<long, ReplayUnit>();
        private ReplayBuilding originalBuilding;

        private const int explosion_range = 3;
        private const int used_silo_id = 112; //TODO: Remove this hardcoded value

        public string GetReadibleName(ReplayController controller, bool shortName)
        {
            if (shortName)
                return MoveUnit != null ? "Move + Launch" : "Launch";

            if (MoveUnit == null)
            {
                if (controller.Map.TryGetDrawableUnit(SiloPosition, out var launchUnit))
                    return $"{launchUnit.UnitData.Name} Launches Rocket";

                return "Launches Rocket";
            }

            var moveUnit = controller.Map.GetDrawableUnit(MoveUnit.Unit.ID);
            return $"{moveUnit.UnitData.Name} Moves + Launches Rocket";
        }

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
            MoveUnit?.SetupAndUpdate(controller, context);

            if (!context.Buildings.Remove(SiloPosition, out var launchingBuilding))
                throw new ReplayMissingBuildingException(SiloPosition);

            var launchingUnit = context.Units.FirstOrDefault(x => x.Value.Position == SiloPosition).Value;
            if (launchingUnit != null)
                launchingUnit.TimesMoved = 1;

            originalBuilding = launchingBuilding.Clone();

            foreach (var unit in context.Units)
            {
                if (!unit.Value.PlayerID.HasValue)
                    continue;

                if (unit.Value.BeingCarried.HasValue && unit.Value.BeingCarried.Value)
                    continue;

                var position = unit.Value.Position!.Value;
                var distance = (position - TargetPosition).ManhattonDistance();

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

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Launch Action.");

            if (MoveUnit != null)
            {
                foreach (var transformable in MoveUnit.PerformAction(controller))
                    yield return transformable;
            }

            Logger.Log("Todo: Initial launch animation.");

            if (controller.Map.TryGetDrawableBuilding(SiloPosition, out _))
            {
                var replayBuilding = new ReplayBuilding
                {
                    Position = SiloPosition,
                    TerrainID = used_silo_id
                };

                controller.Map.UpdateBuilding(replayBuilding, false);
            }

            controller.Map.PlayEffect("Effects/Target", 1500, TargetPosition, 0, x =>
            {
                x.ScaleTo(10).ScaleTo(1, 1000, Easing.In)
                 .FadeTo(1, 500)
                 .RotateTo(0).RotateTo(90 * 4, 1200, Easing.Out).Then().Expire();
            });

            var explosion = controller.Map.PlayEffect("Effects/Explosion/Explosion-Land", 500, TargetPosition + new Vector2I(0, -1), 1350, x => x.ScaleTo(3));

            yield return ReplayWait.WaitForTransformable(explosion);

            if (controller.Map.TryGetDrawableUnit(SiloPosition, out var launchingUnit))
                launchingUnit.CanMove.Value = false;

            for (int i = 0; i <= explosion_range; i++)
            {
                foreach (var tile in Vec2IHelper.GetAllTilesWithDistance(TargetPosition, i))
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

        public void UndoAction(ReplayController controller)
        {
            Logger.Log("Undoing Launch Action.");

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

            controller.Map.UpdateBuilding(originalBuilding, true);

            controller.UpdateFogOfWar();
            if (MoveUnit != null)
                MoveUnit?.UndoAction(controller);
            else if (controller.Map.TryGetDrawableUnit(SiloPosition, out var launchingUnit))
                launchingUnit.CanMove.Value = true;
        }
    }
}
