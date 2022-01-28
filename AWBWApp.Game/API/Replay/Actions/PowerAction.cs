using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using Newtonsoft.Json.Linq;
using osu.Framework.Logging;

namespace AWBWApp.Game.API.Replay.Actions
{
    public class PowerActionBuilder : IReplayActionBuilder
    {
        public string Code => "Power";

        public ReplayActionDatabase Database { get; set; }

        //Todo: The Power action is a big thing and will likely need a bunch of testing.
        //This is a list of CO's who have been tested.
        private readonly HashSet<string> compatibleCOs = new HashSet<string>
        {
            "Sonja-S",
            "Drake-S"
        };

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var coName = (string)jObject["coName"];
            var coPower = (string)jObject["coPower"];

            if (!compatibleCOs.Contains($"{coName}-{coPower}"))
                throw new Exception($"Player executed an unknown power: {coName}-{coPower}");

            var action = new PowerAction();

            var globalEffects = (JObject)jObject["global"];

            if (globalEffects != null)
            {
                action.MovementRangeIncrease = (int)globalEffects["units_movement_points"];
                action.SightRangeIncrease = (int)globalEffects["units_vision"];

                if (coName == "Sonja")
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
                action.HPChanges = new Dictionary<int, PowerAction.HPChange>();

                var hpGainEntry = jObject["hpGain"];

                if (hpGainEntry is JObject hpGain)
                {
                    var change = new PowerAction.HPChange
                    {
                        ChangeAmount = (int)hpGain["hp"],
                        FuelPercentage = (double)hpGain["units_fuel"]
                    };

                    foreach (var player in (JArray)hpGain["players"])
                        action.HPChanges.Add((int)player, change);
                }

                var hpLossEntry = jObject["hpGain"];

                if (hpLossEntry is JObject hpLoss)
                {
                    var change = new PowerAction.HPChange
                    {
                        ChangeAmount = (int)hpLoss["hp"],
                        FuelPercentage = (double)hpLoss["units_fuel"]
                    };

                    foreach (var player in (JArray)hpLoss["players"])
                        action.HPChanges.Add((int)player, change);
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
        public int MovementRangeIncrease;
        public int SightRangeIncrease;
        public bool CanSeeIntoHiddenTiles;
        public bool ReverseAttackOrder;

        public string ChangeToWeather;

        public Dictionary<int, HPChange> HPChanges;

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            //Todo: Show power off
            Logger.Log("Power Action animation not implemented");

            //Todo: How much should this do?
            controller.AddPowerAction(this);

            if (ChangeToWeather.IsNullOrEmpty())
                Logger.Log("Weather Change not Implemented.");

            if (SightRangeIncrease != 0 || CanSeeIntoHiddenTiles)
            {
                controller.UpdateFogOfWar();
                yield return ReplayWait.WaitForMilliseconds(150);
            }

            if (HPChanges != null)
            {
                foreach (var change in HPChanges)
                {
                    foreach (var unit in controller.Map.GetDrawableUnitsFromPlayer(change.Key))
                    {
                        unit.HealthPoints.Value += change.Value.ChangeAmount;
                        unit.Fuel.Value = (int)Math.Floor(unit.Fuel.Value * change.Value.FuelPercentage);

                        //Todo: Play heal/damage animation

                        if (unit.HealthPoints.Value <= 0)
                            controller.Map.DestroyUnit(unit.UnitID);
                    }
                }
            }

            yield break;
        }

        public void UndoAction(ReplayController controller, bool immediate)
        {
            throw new NotImplementedException();
        }

        public class HPChange
        {
            public int ChangeAmount;
            public double FuelPercentage;
        }
    }
}
