using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using Newtonsoft.Json.Linq;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Logging;

namespace AWBWApp.Game.API.Replay.Actions
{
    public class PowerActionBuilder : IReplayActionBuilder
    {
        public string Code => "Power";

        public ReplayActionDatabase Database { get; set; }

        //Todo: The Power action is a big thing and will likely need a bunch of testing.
        //This is a list of CO's who have been tested.
        //Todo: We don't get information about unit damage increases.
        private readonly HashSet<string> compatibleCOs = new HashSet<string>
        {
            "Sonja-Y",
            "Sonja-S",
            "Drake-Y",
            "Drake-S",
            "Jess-Y", //Missing Power Increase
            "Jess-S", //Missing Power Increase
            "Grimm-Y", //Missing Power Increase
            "Grimm-S" //Missing Power Increase
        };

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = new PowerAction();
            action.CombatOfficerName = (string)jObject["coName"];
            var coPower = (string)jObject["coPower"];

            if (coPower != "Y" && coPower != "S")
                throw new Exception($"CO Power is of type {coPower} which is not a type this program was made to handle.");

            if (!compatibleCOs.Contains($"{action.CombatOfficerName}-{coPower}"))
                throw new Exception($"Player executed an unknown power: {action.CombatOfficerName}-{coPower}");

            if ((int)jObject["playerID"] != turnData.ActivePlayerID)
                throw new Exception("Active player did not use the power. Is this supposed to be possible?");

            action.IsSuperPower = coPower == "S";
            action.PowerName = (string)jObject["powerName"];
            action.LeftOverPower = (int)jObject["playersCOP"];

            var globalEffects = (JObject)jObject["global"];

            if (globalEffects != null)
            {
                action.MovementRangeIncrease = (int)globalEffects["units_movement_points"];
                action.SightRangeIncrease = (int)globalEffects["units_vision"];

                if (action.CombatOfficerName == "Sonja")
                {
                    //AWBW doesn't specify these values in Json. So we need to create the data ourselves.
                    action.CanSeeIntoHiddenTiles = true;
                    if (coPower == "S")
                        action.ReverseAttackOrder = true;
                }
            }

            var hpChange = (JObject)jObject["hpChange"];

            if (hpChange != null)
            {
                action.PlayerWideChanges = new Dictionary<int, PowerAction.PlayerWideUnitChange>();

                var hpGainEntry = jObject["hpGain"];

                if (hpGainEntry is JObject hpGain)
                {
                    var change = new PowerAction.PlayerWideUnitChange
                    {
                        HPGain = (int)hpGain["hp"],
                        FuelGainPercentage = (double)hpGain["units_fuel"]
                    };

                    foreach (var player in (JArray)hpGain["players"])
                        action.PlayerWideChanges.Add((int)player, change);
                }

                var hpLossEntry = jObject["hpLoss"];

                if (hpLossEntry is JObject hpLoss)
                {
                    var change = new PowerAction.PlayerWideUnitChange
                    {
                        HPGain = (int)hpLoss["hp"],
                        FuelGainPercentage = (double)hpLoss["units_fuel"]
                    };

                    foreach (var player in (JArray)hpLoss["players"])
                        action.PlayerWideChanges.Add((int)player, change);
                }
            }

            var unitReplace = (JObject)jObject["unitReplace"];

            if (unitReplace != null)
            {
                action.UnitChanges = new Dictionary<int, PowerAction.UnitChange>();
                var activePlayerUnitReplace = (JObject)ReplayActionHelper.GetPlayerSpecificDataFromJObject(unitReplace, turnData.ActiveTeam, turnData.ActivePlayerID);

                foreach (JObject unit in (JArray)activePlayerUnitReplace["units"])
                {
                    var change = new PowerAction.UnitChange();

                    if (unit.TryGetValue("units_ammo", out JToken ammo))
                        change.Ammo = (int)ammo;

                    if (unit.TryGetValue("units_fuel", out JToken fuel))
                        change.Fuel = (int)fuel;

                    if (unit.TryGetValue("units_movement_points", out JToken movementPoints))
                        change.MovementPoints = (int)movementPoints;

                    action.UnitChanges.Add((int)unit["units_id"], change);
                }
            }

            var weatherChange = (JObject)jObject["weather"];

            if (weatherChange != null)
                action.ChangeToWeather = (string)weatherChange["weatherCode"];

            return action;
        }
    }

    public class PowerAction : IReplayAction
    {
        public string CombatOfficerName;
        public string PowerName;
        public bool IsSuperPower;

        public int LeftOverPower;

        public int MovementRangeIncrease;
        public int SightRangeIncrease;
        public bool CanSeeIntoHiddenTiles;
        public bool ReverseAttackOrder;

        public string ChangeToWeather;

        public Dictionary<int, PlayerWideUnitChange> PlayerWideChanges;
        public Dictionary<int, UnitChange> UnitChanges;

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            //Todo: Show power off
            Logger.Log("Power Action animation not implemented");

            yield return ReplayWait.WaitForTransformable(controller.PlayPowerAnimation(CombatOfficerName, PowerName, IsSuperPower));

            //Todo: How much should this do?
            controller.AddPowerAction(this);

            if (ChangeToWeather.IsNullOrEmpty())
                Logger.Log("Weather Change not Implemented.");

            if (SightRangeIncrease != 0 || CanSeeIntoHiddenTiles)
            {
                controller.UpdateFogOfWar();
                yield return ReplayWait.WaitForMilliseconds(150);
            }

            if (PlayerWideChanges != null)
            {
                foreach (var change in PlayerWideChanges)
                {
                    foreach (var unit in controller.Map.GetDrawableUnitsFromPlayer(change.Key))
                    {
                        if (change.Value.HPGain.HasValue)
                            unit.HealthPoints.Value += change.Value.HPGain.Value;
                        if (change.Value.FuelGainPercentage.HasValue)
                            unit.Fuel.Value = (int)Math.Floor(unit.Fuel.Value * change.Value.FuelGainPercentage.Value);

                        //Todo: Play heal/damage animation

                        if (unit.HealthPoints.Value <= 0)
                            controller.Map.DeleteUnit(unit.UnitID, true);
                        else
                            PlayEffectForUnitChange(controller, unit.MapPosition, change.Value);

                        yield return ReplayWait.WaitForMilliseconds(50);
                    }
                }
            }

            if (UnitChanges != null)
            {
                foreach (var change in UnitChanges)
                {
                    if (controller.Map.TryGetDrawableUnit(change.Key, out var unit))
                    {
                        if (change.Value.Ammo.HasValue)
                            unit.Ammo.Value = change.Value.Ammo.Value;
                        if (change.Value.Fuel.HasValue)
                            unit.Fuel.Value = change.Value.Fuel.Value;

                        if (unit.HealthPoints.Value <= 0)
                            controller.Map.DeleteUnit(unit.UnitID, true);
                        else
                            PlayEffectForUnitChange(controller, unit.MapPosition, change.Value);
                    }
                    else
                        throw new Exception("Unable to find unit: " + change.Key);

                    yield return ReplayWait.WaitForMilliseconds(50);
                }
            }

            yield break;
        }

        private void PlayEffectForUnitChange(ReplayController controller, Vector2I position, UnitChange change)
        {
        }

        private void PlayEffectForUnitChange(ReplayController controller, Vector2I position, PlayerWideUnitChange change)
        {
        }

        public void UndoAction(ReplayController controller, bool immediate)
        {
            throw new NotImplementedException();
        }

        public class PlayerWideUnitChange
        {
            public int? HPGain;
            public double? FuelGainPercentage;
        }

        public class UnitChange
        {
            public int? Fuel;
            public int? Ammo;
            public int? MovementPoints;
        }
    }
}
