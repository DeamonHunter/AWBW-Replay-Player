using System;
using AWBWApp.Game.API.Replay;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AWBWApp.Game.API
{
    /*
    public class LoadReplayAction : IReplayAction
    {
        [JsonProperty]
        public long LoadedId;

        [JsonProperty]
        public long TransportId;

        public void PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Load Action.");
            var loadedUnit = controller.Map.GetDrawableUnit(LoadedId);
            var transportUnit = controller.Map.GetDrawableUnit(TransportId);

            var path = new List<UnitPosition>
            {
                new UnitPosition { X = loadedUnit.MapPosition.X, Y = loadedUnit.MapPosition.Y },
                new UnitPosition { X = transportUnit.MapPosition.X, Y = transportUnit.MapPosition.Y },
            };

            loadedUnit.FollowPath(path).Then().FadeOut();
            transportUnit.LoadUnit(loadedUnit);
        }
    }

    public class UnloadReplayAction : IReplayAction
    {
        [JsonProperty]
        public AWBWUnit UnloadedUnit;

        [JsonProperty]
        public long TransportId;

        public void PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Load Action.");
            var unloadedUnit = controller.Map.GetDrawableUnit(UnloadedUnit.ID);
            var transportUnit = controller.Map.GetDrawableUnit(TransportId);

            unloadedUnit.UpdateUnit(UnloadedUnit);

            var path = new List<UnitPosition>
            {
                new UnitPosition { X = transportUnit.MapPosition.X, Y = transportUnit.MapPosition.Y },
                new UnitPosition { X = unloadedUnit.MapPosition.X, Y = unloadedUnit.MapPosition.Y },
            };

            transportUnit.MoveToPosition(transportUnit.MapPosition); //Force transport to be at the right place
            transportUnit.UnloadUnit(unloadedUnit);
            unloadedUnit.FadeIn();
            unloadedUnit.FollowPath(path);
        }
    }

    public class SupplyReplayAction : IReplayAction
    {
        [JsonProperty]
        public List<long> SuppliedIds;

        public void PerformAction(ReplayController controller)
        {
            //Todo: Supplied Animation
            foreach (var suppliedUnitId in SuppliedIds)
            {
                var suppliedUnit = controller.Map.GetDrawableUnit(suppliedUnitId);
                suppliedUnit.Ammo.Value = suppliedUnit.UnitData.MaxAmmo;
                suppliedUnit.Fuel.Value = suppliedUnit.UnitData.MaxFuel;
            }
        }
    }

    public class RepairReplayAction : IReplayAction
    {
        [JsonProperty]
        public long Funds;

        [JsonProperty]
        public RepairUnit RepairedUnit;

        [JsonProperty("unitId")]
        public long? BlackBoatId;

        [JsonProperty]
        public List<long> SuppliedIds;

        // {"action": "Repair", "funds": 18900, "repairedUnit": {"units_id":87204364, "units_hit_points": 10}, "unitId": "87125679" }
        public void PerformAction(ReplayController controller)
        {
            var repairedUnit = controller.Map.GetDrawableUnit(RepairedUnit.UnitId);
            repairedUnit.HealthPoints.Value = RepairedUnit.HitPoints;
            repairedUnit.Ammo.Value = repairedUnit.UnitData.MaxAmmo;
            repairedUnit.Fuel.Value = repairedUnit.UnitData.MaxFuel;

            //Todo: Funds
            //Todo: Repair Animation

            if (BlackBoatId.HasValue)
            {
                var blackBoat = controller.Map.GetDrawableUnit(RepairedUnit.UnitId);
                blackBoat.CanMove.Value = false;
            }
        }

        public class RepairUnit
        {
            [JsonProperty("units_id")]
            public long UnitId;
            [JsonProperty("units_hit_points")]
            public int HitPoints;
        }
    }

    public class CaptureReplayAction : IReplayAction
    {
        [JsonProperty]
        public AWBWBuilding BuildingInfo { get; set; }

        public void PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Capture Action.");
            controller.Map.UpdateBuilding(BuildingInfo, false);
            var unit = controller.Map.GetDrawableUnit(new Vector2I(BuildingInfo.X, BuildingInfo.Y));
            if (unit != null)
                unit.HasCaptured.Value = true;
        }
    }
    
    public class AttackReplayAction : IReplayAction
    {
        [JsonProperty]
        public AWBWUnit Attacker { get; set; }

        [JsonProperty]
        public AWBWUnit Defender { get; set; }

        [JsonProperty]
        public AWBWAttackCOP COPValues { get; set; }

        [JsonProperty]
        public long? GainedFunds { get; set; }

        public void PerformAction(ReplayController controller)
        {
            var attacker = controller.Map.GetDrawableUnit(Attacker.ID);
            attacker.UpdateUnit(Attacker);
            var defender = controller.Map.GetDrawableUnit(Defender.ID);
            defender.UpdateUnit(Defender);

            if (attacker.HealthPoints.Value <= 0)
                controller.Map.DestroyUnit(attacker.UnitID);
            if (defender.HealthPoints.Value <= 0)
                controller.Map.DestroyUnit(defender.UnitID);

            //Todo: Adjust funds
            //Todo: Adjust COP
        }
    }

    public class JoinUnitsAction : IReplayAction
    {
        // {"action": "Join", 87127649, "joinedUnit": *unitData*, "newFunds": 21000, "playerId": 1045088}

        [JsonProperty]
        public long PlayerId { get; set; }

        [JsonProperty]
        public long JoinId { get; set; }

        [JsonProperty]
        public AWBWUnit JoinedUnit { get; set; }

        [JsonProperty]
        public long? NewFunds { get; set; }

        public void PerformAction(ReplayController controller)
        {
            var joiningUnit = controller.Map.GetDrawableUnit(JoinId);
            var joinedUnit = controller.Map.GetDrawableUnit(JoinedUnit.ID);

            if (joiningUnit.MapPosition != joinedUnit.MapPosition)
            {
                var path = new List<UnitPosition>()
                {
                    new UnitPosition { X = joiningUnit.MapPosition.X, Y = joiningUnit.MapPosition.Y },
                    new UnitPosition { X = joinedUnit.MapPosition.X, Y = joinedUnit.MapPosition.Y }
                };
                joiningUnit.FollowPath(path);
            }
            controller.Map.DestroyUnit(JoinId, false);
            joinedUnit.UpdateUnit(JoinedUnit);

            //Todo: Add funds
        }
    }

    public class DeleteUnitAction : IReplayAction
    {
        [JsonProperty("deletedId")]
        public long DeletedUnitId;

        public void PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Next Turn Action.");
            var unit = controller.Map.GetDrawableUnit(DeletedUnitId);
            controller.Map.DestroyUnit(DeletedUnitId);

            //Todo: Add funds
        }
    }

    public class HideUnitAction : IReplayAction
    {
        [JsonProperty("unitId")]
        public long HiddenUnit;

        [JsonProperty("unitId")]
        public bool Vision;

        public void PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Next Turn Action.");
            var unit = controller.Map.GetDrawableUnit(HiddenUnit);

            unit.CanMove.Value = false;
            unit.Dived.Value = true;

            //Todo: Update Fog
            //Todo: Update Visibility of unit
        }
    }

    public class UnhideUnitAction : IReplayAction
    {
        [JsonProperty("unitId")]
        public long UnhiddenUnit;

        public void PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Next Turn Action.");
            var unit = controller.Map.GetDrawableUnit(UnhiddenUnit);

            unit.CanMove.Value = false;
            unit.Dived.Value = false;

            //Todo: Update Fog
            //Todo: Update Visibility of unit
        }
    }

    public class ExplodeUnitAction : IReplayAction
    {
        [JsonProperty("unitId")]
        public long ExplodedUnit;

        [JsonProperty]
        public int ExplosionX;
        [JsonProperty]
        public int ExplosionY;

        public void PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Next Turn Action.");
            controller.Map.DestroyUnit(ExplodedUnit);
            //Todo: Play bigger explosion animation

            var units = controller.Map.GetUnitsWithDistance(new Vector2I(ExplosionX, ExplosionY), 3);

            foreach (var unit in units)
            {
                if (unit.HealthPoints.Value <= 0)
                    continue;

                unit.HealthPoints.Value = Math.Max(unit.HealthPoints.Value - 5, 1);
            }

            //Todo: Update Fog
        }
    }

    public class NextTurnAction : IReplayAction
    {
        [JsonProperty]
        public int Day;

        [JsonProperty("nextPId")]
        public long NextPlayerId;

        public void PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Next Turn Action.");
            controller.AdvanceToNextTurn(Day, NextPlayerId);
            //Todo: Pull up info overlay
        }
    }
*/

    public class ReplayActionConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            JToken jObject = JToken.ReadFrom(reader);

            ReplayActionType type = jObject["action"].ToObject<ReplayActionType>();

            IReplayAction result;

            switch (type)
            {
                default:
                    throw new ArgumentOutOfRangeException();
            }
            serializer.Populate(jObject.CreateReader(), result);
            return result;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.IsInstanceOfType(typeof(IReplayAction));
        }
    }

    public enum ReplayActionType
    {
        Move,
        NextTurn,
        Build,
        Capt,
        Fire,
        Load,
        Unload,
        Supply,
        Join,
        Repair,
        Delete,
        Hide,
        Unhide,
        Explode,

        Launch,
        /*
         * Pulls up info overlay
         * Sami Power: {"action": "Power", "coName": "Sami", "coPower": "S", "copValue": 0, "playerId": 1045087, "powerName": "Victory March", "unitReplace": {"units": [{"units_id": 87202054, "units_movement_points": 5}]}
         * Andy Power: {"action": "Power", "coName": "Andy", "coPower": "S", "copValue": 0, "playerId": 1045088, "powerName": "Hyper Upgrade", "global": {"units_movement_points": 1, "units_vision": 0}, "hpChange": {"hpGain": {"players":[1045088], "hp": 5, "units_fuel": 1}, "hpLoss": ""]}
         */
        Power,
        Resign, // {"action": "Resign", "playerId": 1045089, "message": "muncher21 has resigned!", "GameOver": {"action": "GameOver", "day":20, "losers": [1045089], "winners": [1045088], "message": "The game is over! zagghov is the winner!"}

        //Todo: Add elimination event to Capt https://awbw.amarriner.com/2030.php?games_id=351247&ndx=88
        Elimination, // {"action": "Resign", "eliminatedByPId": 1045089, "playerId": 1045089, "message": "Colin1234 was eliminated by capture!", "GameOver": {"action": "GameOver", "day":20, "losers": [1045089], "winners": [1045088], "message": "The game is over! zagghov is the winner!"}

        SetDraw,

        //Unneeded
        AttackSeam,
    }

    public class UnitPosition
    {
        [JsonProperty]
        public bool Unit_Visible { get; set; }

        [JsonProperty]
        public int X { get; set; }

        [JsonProperty]
        public int Y { get; set; }
    }
}
