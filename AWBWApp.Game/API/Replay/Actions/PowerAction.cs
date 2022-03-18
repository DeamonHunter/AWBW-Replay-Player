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
            "Andy-Y", "Andy-S",
            "Hachi-Y", "Hachi-S",
            "Jake-Y", "Jake-S",
            "Max-Y", "Max-S",
            "Nell-Y", "Nell-S",
            "Rachel-Y", "Rachel-S",
            "Sami-Y", "Sami-S",
            "Colin-Y",
            "Grit-Y", "Grit-S",
            "Olaf-Y", "Olaf-S",
            "Sasha-Y", "Sasha-S",
            "Drake-Y", "Drake-S",
            "Eagle-Y", "Eagle-S",
            "Javier-Y", "Javier-S",
            "Jess-Y", "Jess-S",
            "Grimm-Y", "Grimm-S",
            "Kanbei-Y", "Kanbei-S",
            "Sensei-Y", "Sensei-S",
            "Sonja-Y", "Sonja-S",
            "Adder-Y", "Adder-S",
            "Flak-Y", "Flak-S",
            "Hawke-Y", "Hawke-S",
            "Jugger-Y", "Jugger-S",
            "Kindle-Y", "Kindle-S",
            "Koal-Y", "Koal-S",
            "Lash-Y", "Lash-S",
            "Sturm-Y", "Sturm-S",
            "Von Bolt-S"
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

            if ((long)jObject["playerID"] != turnData.ActivePlayerID)
                throw new Exception("Active player did not use the power. Is this supposed to be possible?");

            action.IsSuperPower = coPower == "S";
            action.PowerName = (string)jObject["powerName"];
            action.LeftOverPower = (int)jObject["playersCOP"];

            var globalEffects = (JObject)jObject["global"];

            if (globalEffects != null)
            {
                foreach (var entry in globalEffects)
                {
                    switch (entry.Key)
                    {
                        case "units_movement_points":
                            action.MovementRangeIncrease = (int)entry.Value;
                            break;

                        case "units_vision":
                            action.SightRangeIncrease = (int)entry.Value;
                            break;

                        default:
                            throw new Exception("Unknown global entry: " + entry.Key);
                    }
                }
            }

            var hpChange = (JObject)jObject["hpChange"];

            if (hpChange != null)
            {
                action.PlayerWideChanges = new Dictionary<long, PowerAction.PlayerWideUnitChange>();

                var hpGainEntry = jObject["hpGain"];

                if (hpGainEntry is JObject hpGain)
                {
                    var change = new PowerAction.PlayerWideUnitChange();

                    foreach (var entry in hpGain)
                    {
                        switch (entry.Key)
                        {
                            case "hp":
                                change.HPGain = (int)entry.Value;
                                break;

                            case "units_fuel":
                                change.FuelGainPercentage = (double)entry.Value;
                                break;

                            case "players":
                                break;

                            default:
                                throw new Exception("Unknown hpGain entry: " + entry.Key);
                        }
                    }

                    foreach (var player in (JArray)hpGain["players"])
                        action.PlayerWideChanges.Add((long)player, change);
                }

                var hpLossEntry = jObject["hpLoss"];

                if (hpLossEntry is JObject hpLoss)
                {
                    var change = new PowerAction.PlayerWideUnitChange();

                    foreach (var entry in hpLoss)
                    {
                        switch (entry.Key)
                        {
                            case "hp":
                                change.HPGain = (int)entry.Value;
                                break;

                            case "units_fuel":
                                change.FuelGainPercentage = (double)entry.Value;
                                break;

                            case "players":
                                break;

                            default:
                                throw new Exception("Unknown hpLoss entry: " + entry.Key);
                        }
                    }

                    foreach (var player in (JArray)hpLoss["players"])
                        action.PlayerWideChanges.Add((long)player, change);
                }
            }

            var unitReplace = (JObject)jObject["unitReplace"];

            if (unitReplace != null)
            {
                var activePlayerUnitReplace = (JObject)ReplayActionHelper.GetPlayerSpecificDataFromJObject(unitReplace, turnData.ActiveTeam, turnData.ActivePlayerID);

                //Occasionally this will be { "units": null }
                var unitReplaces = activePlayerUnitReplace["units"];

                if (unitReplaces.Type != JTokenType.Null)
                {
                    action.UnitChanges = new Dictionary<long, PowerAction.UnitChange>();

                    foreach (JObject unit in (JArray)unitReplaces)
                    {
                        var change = new PowerAction.UnitChange();

                        foreach (var entry in unit)
                        {
                            switch (entry.Key)
                            {
                                case "units_ammo":
                                    change.Ammo = (int)entry.Value;
                                    break;

                                case "units_fuel":
                                    change.Fuel = (int)entry.Value;
                                    break;

                                case "units_hit_points":
                                    change.HitPoints = (int)entry.Value;
                                    break;

                                case "units_movement_points":
                                    change.MovementPoints = (int)entry.Value;
                                    break;

                                case "units_long_range":
                                    change.Range = (int)entry.Value;
                                    break;

                                case "units_moved":
                                    change.UnitsMoved = (int)entry.Value;
                                    break;

                                case "units_id":
                                    break;

                                default:
                                    throw new Exception("Unknown Unit Change: " + entry.Key);
                            }
                        }

                        action.UnitChanges.Add((long)unit["units_id"], change);
                    }
                }
            }

            var playerReplace = (JObject)jObject["playerReplace"];

            if (playerReplace != null)
            {
                var details = ReplayActionHelper.GetPlayerSpecificDataFromJObject(playerReplace, turnData.ActiveTeam, turnData.ActivePlayerID);
                action.PlayerChanges = new Dictionary<long, PowerAction.PlayerChange>();

                foreach (var player in (JObject)details)
                {
                    var change = new PowerAction.PlayerChange();

                    foreach (var entry in (JObject)player.Value)
                    {
                        switch (entry.Key)
                        {
                            case "players_funds":
                                change.Money = (int)entry.Value;
                                break;

                            case "players_co_power":
                                change.COPower = (int)entry.Value;
                                break;

                            case "tags_co_power":
                                change.COPower = (int)entry.Value;
                                break;

                            default:
                                throw new Exception("Unknown PLayer Change: " + entry.Key);
                        }
                    }

                    action.PlayerChanges.Add(long.Parse(player.Key), change);
                }
            }

            var unitAdd = (JObject)jObject["unitAdd"];

            if (unitAdd != null)
            {
                var details = ReplayActionHelper.GetPlayerSpecificDataFromJObject(unitAdd, turnData.ActiveTeam, turnData.ActivePlayerID);

                var playerId = (long)details["playerId"];
                if (playerId != turnData.ActivePlayerID)
                    throw new Exception("Adding units for a non-active player?");

                action.CreatedUnits = new List<PowerAction.CreateUnit>();

                var unitName = (string)details["unitName"];

                foreach (var unit in (JArray)details["units"])
                {
                    var newUnit = new PowerAction.CreateUnit
                    {
                        UnitName = unitName,
                        HP = 9 //Hardcoded as it is not passed to us.
                    };

                    foreach (var entry in (JObject)unit)
                    {
                        switch (entry.Key)
                        {
                            case "units_id":
                                newUnit.UnitID = (long)entry.Value;
                                break;

                            case "units_x":
                                newUnit.Position.X = (int)entry.Value;
                                break;

                            case "units_y":
                                newUnit.Position.Y = (int)entry.Value;
                                break;

                            default:
                                throw new Exception("Unknown AddUnits entry: " + newUnit);
                        }
                    }

                    action.CreatedUnits.Add(newUnit);
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
                action.ChangeToWeather = WeatherHelper.ParseWeatherCode((string)weatherChange["weatherCode"]);

            return action;
        }
    }

    public class PowerAction : IReplayAction
    {
        public string ReadibleName => "Power";

        public string CombatOfficerName;
        public string PowerName;
        public bool IsSuperPower;
        public int LeftOverPower;

        public COPower COPower;
        public int SightRangeIncrease;
        public int MovementRangeIncrease;

        public Weather? ChangeToWeather;

        public Dictionary<long, PlayerChange> PlayerChanges;
        public Dictionary<long, PlayerWideUnitChange> PlayerWideChanges;
        public Dictionary<long, UnitChange> UnitChanges;
        public List<CreateUnit> CreatedUnits;
        public List<Vector2I> MissileCoords;

        public void Setup(ReplayController controller, ReplaySetupContext context)
        {
            controller.RegisterPower(this, context);
        }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            var co = controller.COStorage.GetCOByName(CombatOfficerName);
            COPower = IsSuperPower ? co.SuperPower : co.NormalPower;

            var powerAnimation = new PowerDisplay(CombatOfficerName, PowerName, IsSuperPower);
            controller.AddGenericActionAnimation(powerAnimation);
            yield return ReplayWait.WaitForTransformable(powerAnimation);

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
            controller.ActivePlayer.ActivePower.Value = IsSuperPower ? ActiveCOPower.Super : ActiveCOPower.Normal;

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

            if (ChangeToWeather.HasValue)
                controller.Map.CurrentWeather.Value = ChangeToWeather.Value;

            if (PlayerChanges != null)
            {
                foreach (var change in PlayerChanges)
                {
                    var player = controller.Players[change.Key];

                    if (change.Value.Money.HasValue)
                        player.Funds.Value = change.Value.Money.Value;

                    if (change.Value.COPower.HasValue)
                    {
                        var activeCO = player.ActiveCO.Value;
                        activeCO.Power = change.Value.COPower;
                        player.ActiveCO.Value = activeCO;
                    }

                    if (change.Value.TagCOPower.HasValue)
                    {
                        var activeCO = player.TagCO.Value;
                        activeCO.Power = change.Value.COPower;
                        player.TagCO.Value = activeCO;
                    }
                }
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
                            playEffectForUnitChange(controller, unit.MapPosition, change.Value);

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
                        if (change.Value.UnitsMoved.HasValue)
                            unit.CanMove.Value = change.Value.UnitsMoved.Value == 0;

                        if (change.Value.MovementPoints.HasValue)
                            Logger.Log("Unit Movement Change not implemented yet.");
                        if (change.Value.Range.HasValue)
                            Logger.Log("Unit Range Change not implemented yet.");
                        if (unit.HealthPoints.Value <= 0)
                            controller.Map.DeleteUnit(unit.UnitID, true);
                        else
                            playEffectForUnitChange(controller, unit.MapPosition, change.Value);
                    }
                    else
                        throw new Exception("Unable to find unit: " + change.Key);

                    yield return ReplayWait.WaitForMilliseconds(50);
                }
            }

            if (CreatedUnits != null)
            {
                foreach (var unit in CreatedUnits)
                {
                    var unitData = controller.Map.GetUnitDataForUnitName(unit.UnitName);
                    var newUnit = new ReplayUnit
                    {
                        ID = unit.UnitID,
                        PlayerID = controller.ActivePlayer.ID,
                        UnitName = unit.UnitName,
                        Position = unit.Position,
                        HitPoints = unit.HP,
                        Ammo = unitData.MaxAmmo,
                        BeingCarried = false,
                        Cost = unitData.Cost,
                        Fuel = unitData.MaxFuel,
                        FuelPerTurn = unitData.FuelUsagePerTurn,
                        MovementPoints = unitData.MovementRange,
                        Vision = unitData.Vision,
                        Range = unitData.AttackRange,
                        TimesMoved = 0,
                        MovementType = unitData.MovementType.ToString()
                    };

                    var drawable = controller.Map.AddUnit(newUnit);
                    controller.Map.PlaySelectionAnimation(drawable);
                    yield return ReplayWait.WaitForMilliseconds(50);
                }
            }

            yield break;
        }

        private void playEffectForUnitChange(ReplayController controller, Vector2I position, UnitChange change)
        {
        }

        private void playEffectForUnitChange(ReplayController controller, Vector2I position, PlayerWideUnitChange change)
        {
        }

        public void UndoAction(ReplayController controller, bool immediate)
        {
            throw new NotImplementedException("Undo Power Action is not complete");
        }

        public class PlayerChange
        {
            public int? Money;
            public int? COPower;
            public int? TagCOPower;
        }

        public class CreateUnit
        {
            public string UnitName;
            public long UnitID;
            public int HP;
            public Vector2I Position;
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
            public int? Range;
            public int? UnitsMoved;
        }
    }
}
