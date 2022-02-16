using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.COs;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Replay;
using Newtonsoft.Json.Linq;
using osu.Framework.Graphics;
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
            "Jess-Y",
            "Jess-S",
            "Grimm-Y",
            "Grimm-S",
            "Kanbei-Y",
            "Kanbei-S",
            "Rachel-Y",
            "Rachel-S",
            "Max-Y",
            "Max-S",
            "Sturm-Y",
            "Sturm-S"
        };

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = new PowerAction();
            action.CombatOfficerName = (string)jObject["coName"];
            var coPower = (string)jObject["coPower"];

            if (coPower != "Y" && coPower != "S")
                throw new Exception($"CO Power is of type {coPower} which is not a type this program was made to handle.");

            if (!compatibleCOs.Contains($"{action.CombatOfficerName}-{coPower}"))
                //throw new Exception($"Player executed an unknown power: {action.CombatOfficerName}-{coPower}");
                return new EmptyAction();

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

                    foreach (var pair in unit)
                    {
                        switch (pair.Key)
                        {
                            case "units_ammo":
                                change.Ammo = (int)pair.Value;
                                break;

                            case "units_fuel":
                                change.Fuel = (int)pair.Value;
                                break;

                            case "units_hit_points":
                                change.HitPoints = (int)pair.Value;
                                break;

                            case "units_movement_points":
                                change.MovementPoints = (int)pair.Value;
                                break;

                            case "units_id":
                                break;

                            default:
                                throw new Exception("Unknown Unit Change: " + pair.Key);
                        }
                    }

                    action.UnitChanges.Add((int)unit["units_id"], change);
                }
            }

            var missileCoords = (JArray)jObject["missileCoords"];

            if (missileCoords != null)
            {
                action.MissileCoords = new List<Vector2I>();
                foreach (JObject coord in missileCoords)
                    action.MissileCoords.Add(new Vector2I((int)coord["x"], (int)coord["y"]));
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

        public COPower COPower;
        public int SightRangeIncrease;
        public int MovementRangeIncrease;

        public string ChangeToWeather;

        public Dictionary<int, PlayerWideUnitChange> PlayerWideChanges;
        public Dictionary<int, UnitChange> UnitChanges;
        public List<Vector2I> MissileCoords;

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            var co = controller.COStorage.GetCOByName(CombatOfficerName);
            COPower = IsSuperPower ? co.SuperPower : co.NormalPower;

            //Todo: Show power off
            Logger.Log("Power Action animation not implemented");

            yield return ReplayWait.WaitForTransformable(controller.PlayPowerAnimation(CombatOfficerName, PowerName, IsSuperPower));

            //Todo: How much should this do?
            controller.AddPowerAction(this);

            if (ChangeToWeather.IsNullOrEmpty())
                Logger.Log("Weather Change not Implemented.");

            if (SightRangeIncrease != 0 || COPower.SeeIntoHiddenTiles)
            {
                controller.UpdateFogOfWar();
                yield return ReplayWait.WaitForMilliseconds(150);
            }

            var coValue = controller.ActivePlayer.ActiveCO.Value;
            coValue.Power = LeftOverPower;
            if (co.NormalPower != null)
                coValue.PowerRequiredForNormal += 18000 * co.NormalPower.PowerStars;
            if (co.SuperPower != null)
                coValue.PowerRequiredForSuper += 18000 * co.SuperPower.PowerStars;
            controller.ActivePlayer.ActiveCO.Value = coValue;

            if (MissileCoords != null)
            {
                var waitForEffects = new List<EffectAnimation>();

                for (int i = 0; i < MissileCoords.Count; i++)
                {
                    var coord = MissileCoords[i];
                    var target = controller.Map.PlayEffect("Effects/Target", 1500, coord, i * 250, x =>
                    {
                        x.ScaleTo(10).ScaleTo(1, 1000, Easing.In)
                         .FadeTo(1, 500)
                         .RotateTo(0).RotateTo(90 * 4, 1200, Easing.Out).Then().Expire();
                    });

                    waitForEffects.Add(target);

                    var explosion = controller.Map.PlayEffect("Effects/Explosion/Explosion-Land", 500, coord + new Vector2I(0, -1), 1350 + i * 250, x => x.ScaleTo(3));
                    waitForEffects.Add(explosion);
                }

                foreach (var effect in waitForEffects)
                    yield return ReplayWait.WaitForTransformable(effect);
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
                        if (change.Value.HitPoints.HasValue)
                            unit.HealthPoints.Value = change.Value.HitPoints.Value;

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
            public int? HitPoints;
        }
    }
}
