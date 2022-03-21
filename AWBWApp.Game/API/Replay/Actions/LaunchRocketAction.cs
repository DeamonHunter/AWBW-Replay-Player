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
        public string ReadibleName => "Launch";

        public Vector2I SiloPosition;
        public Vector2I TargetPosition;
        public float HPChange;

        public MoveUnitAction MoveUnit;

        private List<ReplayUnit> originalUnits;
        private ReplayBuilding originalBuilding;

        private const int explosion_range = 3;
        private const int used_silo_id = 112; //TODO: Remove this hardcoded value

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
            MoveUnit?.SetupAndUpdate(controller, context);

            if (!context.Buildings.Remove(SiloPosition, out var launchingBuilding))
                throw new ReplayMissingBuildingException(SiloPosition);

            var launchingUnit = context.Units.FirstOrDefault(x => x.Value.Position == SiloPosition).Value;
            if (launchingUnit != null)
                launchingUnit.TimesMoved = 1;

            originalBuilding = launchingBuilding.Clone();

            originalUnits = new List<ReplayUnit>();

            var destroyedUnits = new HashSet<long>();
            var explodingPlayer = controller.Players[context.ActivePlayerID];

            foreach (var unit in context.Units)
            {
                if (!unit.Value.PlayerID.HasValue || explodingPlayer.OnSameTeam(controller.Players[unit.Value.PlayerID!.Value]))
                    continue;

                if (unit.Value.BeingCarried.HasValue && unit.Value.BeingCarried.Value)
                    continue;

                var position = unit.Value.Position!.Value;
                var distance = (position - TargetPosition).ManhattonDistance();

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

            controller.Map.UpdateBuilding(originalBuilding, true);

            if (MoveUnit != null)
                MoveUnit?.UndoAction(controller);
            else if (controller.Map.TryGetDrawableUnit(SiloPosition, out var launchingUnit))
                launchingUnit.CanMove.Value = true;
        }
    }
}
