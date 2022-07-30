using System.Collections.Generic;
using AWBWApp.Game.API.Replay;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Tests.Visual.Logic
{
    [TestFixture]
    public class TestSceneUnits : BaseActionsTestScene
    {
        private List<int> unitIds;

        private ReplayData baseData;

        private AWBWConfigManager config;

        [Test]
        public void TestDisplayAllUnits()
        {
            AddStep("Show All units", () =>
            {
                unitIds = GetUnitStorage().GetAllUnitIds();

                var countryIDs = GetCountryStorage().GetAllCountryIDs();

                baseData = CreateBasicReplayData(0);
                baseData.ReplayInfo.Players = new Dictionary<long, ReplayUser>(countryIDs.Count);

                for (int i = 0; i < countryIDs.Count; i++)
                {
                    var countryID = countryIDs[i];
                    baseData.ReplayInfo.Players[countryID] = new ReplayUser
                    {
                        ID = countryID,
                        UserId = countryID,
                        CountryID = countryID
                    };
                }

                var turn = CreateBasicTurnData(baseData);
                baseData.TurnData.Add(turn);

                var unitStorage = GetUnitStorage();

                for (int x = 0; x < unitIds.Count; x++)
                {
                    for (int y = 0; y < countryIDs.Count; y++)
                    {
                        var unit = CreateBasicReplayUnit(x * countryIDs.Count + y, countryIDs[y], unitStorage.GetUnitByAWBWId(unitIds[x]).Name, new Vector2I(x, y));
                        turn.ReplayUnit.Add(unit.ID, unit);
                    }
                }

                ReplayController.LoadReplay(baseData, CreateBasicMap(unitIds.Count, countryIDs.Count));
            });

            AddStep("Toggle Wait Status", () =>
            {
                foreach (var unit in baseData.TurnData[0].ReplayUnit)
                {
                    unit.Value.TimesMoved = unit.Value.TimesMoved == 0 ? 1 : 0;
                }

                ReplayController.RestartTurn();
            });

            AddStep("Toggle Dived Status", () =>
            {
                foreach (var unit in baseData.TurnData[0].ReplayUnit)
                {
                    if (unit.Value.UnitName == "Stealth" || unit.Value.UnitName == "Sub")
                        unit.Value.SubHasDived = !unit.Value.SubHasDived;
                }

                ReplayController.RestartTurn();
            });
        }

        [TestCase("Infantry")]
        [TestCase("Tank")]
        public void TestShowAllStatus(string unit)
        {
            AddStep("Setup", () =>
            {
                var data = CreateBasicReplayData(2);
                data.ReplayInfo.Fog = true;
                config.SetValue(AWBWSetting.ReplayOnlyShownKnownInfo, true);

                var turn = CreateBasicTurnData(data);
                data.TurnData.Add(turn);

                var turn2 = CreateBasicTurnData(data, 1);
                data.TurnData.Add(turn2);

                for (int i = 0; i < 10; i++)
                {
                    var healthOnly = CreateBasicReplayUnit(i * 6, 0, unit, new Vector2I(i, 0));
                    healthOnly.HitPoints = i + 1;
                    healthOnly.Ammo = 99;
                    healthOnly.Fuel = 99;
                    turn.ReplayUnit.Add(healthOnly.ID, healthOnly);
                    turn2.ReplayUnit.Add(healthOnly.ID, healthOnly);

                    var healthAndAmmo = CreateBasicReplayUnit(i * 6 + 1, 0, unit, new Vector2I(i, 1));
                    healthAndAmmo.HitPoints = i + 1;
                    healthAndAmmo.Ammo = 0;
                    healthAndAmmo.Fuel = 99;
                    turn.ReplayUnit.Add(healthAndAmmo.ID, healthAndAmmo);
                    turn2.ReplayUnit.Add(healthAndAmmo.ID, healthAndAmmo);

                    var healthAndFuel = CreateBasicReplayUnit(i * 6 + 2, 0, unit, new Vector2I(i, 2));
                    healthAndFuel.HitPoints = i + 1;
                    healthAndFuel.Ammo = 99;
                    healthAndFuel.Fuel = 0;
                    turn.ReplayUnit.Add(healthAndFuel.ID, healthAndFuel);
                    turn2.ReplayUnit.Add(healthAndFuel.ID, healthAndFuel);

                    var healthAndLoaded = CreateBasicReplayUnit(i * 6 + 3, 0, unit, new Vector2I(i, 3));
                    healthAndLoaded.HitPoints = i + 1;
                    healthAndLoaded.Ammo = 99;
                    healthAndLoaded.Fuel = 99;
                    healthAndLoaded.CargoUnits = new List<long> { 0 };
                    turn.ReplayUnit.Add(healthAndLoaded.ID, healthAndLoaded);
                    turn2.ReplayUnit.Add(healthAndLoaded.ID, healthAndLoaded);

                    var healthAndCapturing = CreateBasicReplayUnit(i * 6 + 4, 0, unit, new Vector2I(i, 4));
                    healthAndCapturing.HitPoints = i + 1;
                    healthAndCapturing.Ammo = 99;
                    healthAndCapturing.Fuel = 99;
                    turn.ReplayUnit.Add(healthAndCapturing.ID, healthAndCapturing);
                    turn2.ReplayUnit.Add(healthAndCapturing.ID, healthAndCapturing);

                    var building = CreateBasicReplayBuilding(i * 2, new Vector2I(i, 4), 34);
                    building.Capture = 10;
                    building.LastCapture = 20;
                    turn.Buildings.Add(building.Position, building);
                    turn2.Buildings.Add(building.Position, building);

                    var all = CreateBasicReplayUnit(i * 6 + 5, 0, unit, new Vector2I(i, 5));
                    all.HitPoints = i + 1;
                    all.Ammo = 0;
                    all.Fuel = 0;
                    all.CargoUnits = new List<long> { 0 };
                    all.TimesCaptured = 1;
                    turn.ReplayUnit.Add(all.ID, all);
                    turn2.ReplayUnit.Add(all.ID, all);

                    building = CreateBasicReplayBuilding(i * 2 + 1, new Vector2I(i, 5), 34);
                    building.Capture = 10;
                    building.LastCapture = 20;
                    turn.Buildings.Add(building.Position, building);
                    turn2.Buildings.Add(building.Position, building);
                }

                ReplayController.LoadReplay(data, CreateBasicMap(10, 6));
            });

            AddStep("Go to Opponent Turn", () => ReplayController.GoToNextTurn());
            AddStep("Toggle Fog Visibility", () => config.SetValue(AWBWSetting.ReplayOnlyShownKnownInfo, !config.Get<bool>(AWBWSetting.ReplayOnlyShownKnownInfo)));
        }

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
            dependencies.Cache(config = new AWBWConfigManager(LocalStorage));
            return dependencies;
        }
    }
}
