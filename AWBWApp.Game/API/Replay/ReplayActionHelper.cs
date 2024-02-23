﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using AWBWApp.Game.Exceptions;
using AWBWApp.Game.Game.COs;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Game.Units;
using AWBWApp.Game.UI.Replay;
using Newtonsoft.Json.Linq;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.API.Replay
{
    public static class ReplayActionHelper
    {
        public static JToken GetPlayerSpecificDataFromJObject(JObject jObject, string teamName, long playerId)
        {
            JToken data;
            var playerString = playerId.ToString();
            if (jObject.ContainsKey(playerString))
                data = jObject[playerString];
            else if (teamName != null && jObject.ContainsKey(teamName))
                data = jObject[teamName];
            else
                data = jObject["global"];

            Debug.Assert(data != null);

            return data;
        }

        public static ReplayUnit ParseJObjectIntoReplayUnit(JObject jObject)
        {
            var unit = new ReplayUnit
            {
                ID = (long)jObject["units_id"]
            };

            if (jObject.TryGetValue("units_players_id", out JToken playerId))
                unit.PlayerID = (long)playerId;

            if (jObject.TryGetValue("units_name", out JToken unitName))
                unit.UnitName = (string)unitName;

            if (jObject.TryGetValue("units_movement_points", out JToken movementPoints))
                unit.MovementPoints = (int)movementPoints;

            if (jObject.TryGetValue("units_vision", out JToken vision))
                unit.Vision = (int)vision;

            if (jObject.TryGetValue("units_fuel", out JToken fuel))
                unit.Fuel = (int)fuel;

            if (jObject.TryGetValue("units_fuel_per_turn", out JToken fuelPerTurn))
                unit.FuelPerTurn = (int)fuelPerTurn;

            if (jObject.TryGetValue("units_sub_dive", out JToken subDived))
                unit.SubHasDived = ParseSubHasDived((string)subDived);

            if (jObject.TryGetValue("units_ammo", out JToken ammo))
                unit.Ammo = (int)ammo;

            if (jObject.TryGetValue("units_short_range", out JToken shortRange) && jObject.TryGetValue("units_long_range", out JToken longRange))
                unit.Range = new Vector2I((int)shortRange, (int)longRange);

            if (jObject.TryGetValue("units_second_weapon", out JToken secondWeapon))
                unit.SecondWeapon = ParseReplayBool((string)secondWeapon);

            if (jObject.TryGetValue("units_cost", out JToken cost))
                unit.Cost = (int)cost;

            if (jObject.TryGetValue("units_movement_type", out JToken movementType))
                unit.MovementType = (string)movementType;

            if (jObject.TryGetValue("units_x", out JToken posX) && jObject.TryGetValue("units_y", out JToken posY))
                unit.Position = new Vector2I((int)posX, (int)posY);

            if (jObject.TryGetValue("units_moved", out JToken moved))
                unit.TimesMoved = (int)moved;

            if (jObject.TryGetValue("units_capture", out JToken captured))
                unit.TimesCaptured = (int)captured;

            if (jObject.TryGetValue("units_fired", out JToken fired))
                unit.TimesFired = (int)fired;

            if (jObject.TryGetValue("units_hit_points", out JToken hitPoints))
            {
                if (hitPoints.Type == JTokenType.String) // May be given '?' as a value
                    unit.HitPoints = null;
                else
                    unit.HitPoints = (float)hitPoints;
            }

            if (jObject.TryGetValue("units_carried", out JToken beingCarried))
                unit.BeingCarried = ParseReplayBool((string)beingCarried);

            if (jObject.TryGetValue("units_cargo1_units_id", out JToken carriedId1))
            {
                var id = (long)carriedId1;

                if (id != 0)
                {
                    if (unit.CargoUnits == null)
                        unit.CargoUnits = new List<long>();
                    unit.CargoUnits.Add(id);
                }
            }

            if (jObject.TryGetValue("units_cargo2_units_id", out JToken carriedId2))
            {
                var id = (long)carriedId2;

                if (id != 0)
                {
                    if (unit.CargoUnits == null)
                        unit.CargoUnits = new List<long>();
                    unit.CargoUnits.Add(id);
                }
            }

            return unit;
        }

        public static bool ParseSubHasDived(string dived)
        {
            switch (dived)
            {
                case "y":
                case "Y":
                case "D":
                case "d":
                    return true;

                case "n":
                case "N":
                case "r":
                case "R":
                    return false;

                default:
                    throw new Exception("Unknown Sub has Dived: " + dived);
            }
        }

        public static ReplayBuilding ParseJObjectIntoReplayBuilding(JObject jObject)
        {
            var building = new ReplayBuilding
            {
                ID = (long)jObject["buildings_id"],
                TerrainID = (int?)jObject["terrain_id"],
                Capture = (int)jObject["buildings_capture"],
                Position = new Vector2I((int)jObject["buildings_x"], (int)jObject["buildings_y"]),
                Team = (string)jObject["buildings_team"]
            };

            return building;
        }
        public static ReplayBuilding ParseJObjectIntoReplaySeam(JObject jObject)
        {
            var building = new ReplayBuilding
            {
                // ID = buildings_ID,   //Action Attack Seam does not get the ID of the Seam, so i guess they dont have any
                TerrainID = (int?)jObject["buildings_terrain_id"],
                Capture = (int)jObject["buildings_hit_points"],
                Position = new Vector2I((int)jObject["seamX"], (int)jObject["seamY"])
            };

            return building;
        }

        public static bool ParseReplayBool(string boolean)
        {
            if (boolean == "Y" || boolean == "y")
                return true;
            if (boolean == "N" || boolean == "n")
                return false;

            throw new Exception($"Unknown boolean value {boolean}");
        }

        public static int CalculateUnitCost(ReplayUnit unit, COPower dayToDay, COPower activePower)
        {
            if (unit.Cost == null)
                throw new ArgumentException("Provided unit does not have any cost.", nameof(unit));
            if (unit.HitPoints == null)
                throw new ArgumentException("Provided unit does not have HP.", nameof(unit));

            if (unit.HitPoints.Value <= 0)
                return 0;

            float priceMultiplier = 1;
            if (activePower != null && activePower.UnitPriceMultiplier != 1)
                priceMultiplier = activePower.UnitPriceMultiplier;
            else if (dayToDay.UnitPriceMultiplier != 1)
                priceMultiplier = dayToDay.UnitPriceMultiplier;

            return (int)(unit.Cost.Value * priceMultiplier * Math.Ceiling(unit.HitPoints.Value) * 0.1);
        }

        public static int CalculateUnitCost(DrawableUnit unit, COPower dayToDay, COPower activePower)
        {
            if (unit.HealthPoints.Value <= 0)
                return 0;

            float priceMultiplier = 1;
            if (activePower != null && activePower.UnitPriceMultiplier != 1)
                priceMultiplier = activePower.UnitPriceMultiplier;
            else if (dayToDay != null && dayToDay.UnitPriceMultiplier != 1)
                priceMultiplier = dayToDay.UnitPriceMultiplier;

            return (int)(unit.UnitData.Cost * priceMultiplier * (unit.HealthPoints.Value * 0.1));
        }

        public static void UpdateUnitCargoPositions(ReplaySetupContext context, ReplayUnit unit, Vector2I position)
        {
            if (unit.CargoUnits == null || unit.CargoUnits.Count == 0)
                return;

            foreach (var cargoID in unit.CargoUnits)
            {
                if (!context.Units.TryGetValue(cargoID, out var cargoUnit))
                    throw new ReplayMissingUnitException(cargoID);

                cargoUnit.Position = position;
                UpdateUnitCargoPositions(context, cargoUnit, position);
            }
        }

        public static void AdjustStatsToAttack(ReplayController controller, IEnumerable<ReplayUnit> units, long player1, long player2, bool undo)
        {
            foreach (var unit in units)
            {
                if (unit.PlayerID != player1 && unit.PlayerID != player2)
                    continue;

                var co = controller.Players[unit.PlayerID!.Value].ActiveCO.Value.CO;
                var value = ReplayActionHelper.CalculateUnitCost(unit, co.DayToDayPower, null);

                bool unitAlive = controller.Map.TryGetDrawableUnit(unit.ID, out var changedUnit);
                if (unitAlive)
                    value -= ReplayActionHelper.CalculateUnitCost(changedUnit, co.DayToDayPower, null);

                //Don't care if the unit change doesn't affect value. In repairing/resupplying units.
                if (value <= 0)
                    continue;

                var stats = undo ? UnitStatType.Undo : UnitStatType.None;
                stats |= unitAlive ? UnitStatType.None : UnitStatType.UnitCountChanged;

                if (unit.PlayerID == player1)
                {
                    controller.Stats.CurrentTurnStatsReadout[player1].RegisterUnitStats(stats | UnitStatType.LostUnit, unit.UnitName, unit.PlayerID!.Value, value);
                    controller.Stats.CurrentTurnStatsReadout[player2].RegisterUnitStats(stats | UnitStatType.DamageUnit, unit.UnitName, unit.PlayerID!.Value, value);
                }
                else
                {
                    controller.Stats.CurrentTurnStatsReadout[player2].RegisterUnitStats(stats | UnitStatType.LostUnit, unit.UnitName, unit.PlayerID!.Value, value);
                    controller.Stats.CurrentTurnStatsReadout[player1].RegisterUnitStats(stats | UnitStatType.DamageUnit, unit.UnitName, unit.PlayerID!.Value, value);
                }
            }
        }

        public static void AdjustStatsToPlayerAction(ReplayController controller, long causedBy, IEnumerable<ReplayUnit> units, bool undo)
        {
            foreach (var unit in units)
            {
                var co = controller.Players[unit.PlayerID!.Value].ActiveCO.Value.CO;
                var value = ReplayActionHelper.CalculateUnitCost(unit, co.DayToDayPower, null);

                bool unitAlive = controller.Map.TryGetDrawableUnit(unit.ID, out var changedUnit);
                if (unitAlive)
                    value -= ReplayActionHelper.CalculateUnitCost(changedUnit, co.DayToDayPower, null);

                //Don't care if the unit change doesn't affect value. In repairing/resupplying units.
                if (value <= 0)
                    continue;

                var stats = undo ? UnitStatType.Undo : UnitStatType.None;
                stats |= unitAlive ? UnitStatType.None : UnitStatType.UnitCountChanged;

                controller.Stats.CurrentTurnStatsReadout[unit.PlayerID!.Value].RegisterUnitStats(stats | UnitStatType.LostUnit, unit.UnitName, unit.PlayerID!.Value, value);
                if (unit.PlayerID != causedBy)
                    controller.Stats.CurrentTurnStatsReadout[causedBy].RegisterUnitStats(stats | UnitStatType.DamageUnit, unit.UnitName, unit.PlayerID!.Value, value);
            }
        }
    }
}
