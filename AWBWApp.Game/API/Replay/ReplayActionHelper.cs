using System;
using System.Collections.Generic;
using System.Diagnostics;
using AWBWApp.Game.Game.COs;
using AWBWApp.Game.Game.Unit;
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
            var unit = new ReplayUnit();
            unit.ID = (long)jObject["units_id"];

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

            float priceMultiplier = 1;
            if (activePower != null && activePower.UnitPriceMultiplier != 1)
                priceMultiplier = activePower.UnitPriceMultiplier;
            else if (dayToDay.UnitPriceMultiplier != 1)
                priceMultiplier = dayToDay.UnitPriceMultiplier;

            return (int)(unit.Cost.Value * priceMultiplier * (unit.HitPoints.HasValue ? Math.Ceiling(unit.HitPoints.Value) * 0.1 : 1));
        }

        public static int CalculateUnitCost(DrawableUnit unit, COPower dayToDay, COPower activePower)
        {
            float priceMultiplier = 1;
            if (activePower != null && activePower.UnitPriceMultiplier != 1)
                priceMultiplier = activePower.UnitPriceMultiplier;
            else if (dayToDay.UnitPriceMultiplier != 1)
                priceMultiplier = dayToDay.UnitPriceMultiplier;

            return (int)(unit.UnitData.Cost * priceMultiplier * (unit.HealthPoints.Value * 0.1));
        }
    }
}
