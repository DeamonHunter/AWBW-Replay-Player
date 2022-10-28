using System.Linq;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.API.Replay.Actions;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Testing;

namespace AWBWApp.Game.Tests.Visual.Logic.Actions
{
    [TestFixture]
    public class TestSceneBuildUnitAction : BaseActionsTestScene
    {
        private static Vector2I unitPosition = new Vector2I(2, 2);
        private ReplayUnit createdUnit;

        private AWBWConfigManager config;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
            dependencies.Cache(config = new AWBWConfigManager(LocalStorage));
            return dependencies;
        }

        [Test]
        public void TestCreateUnit()
        {
            AddStep("Setup", () => createTest(false));
            AddStep("Create Unit", ReplayController.GoToNextAction);
            AddAssert("Unit was created", () => HasUnit(0));
            AddAssert("Building is done", () => ReplayController.Map.TryGetDrawableBuilding(unitPosition, out var building) && building.HasDoneAction.Value);
            AddAssert("Funds is 0", () => ReplayController.Players[0].Funds.Value == 0);
            AddAssert("Unit Value is 1000", () => ReplayController.Players[0].UnitValue.Value == 1000);
            AddStep("Undo", ReplayController.GoToPreviousAction);
            AddAssert("Unit doesn't exist", () => !HasUnit(0));
            AddAssert("Building is not done", () => ReplayController.Map.TryGetDrawableBuilding(unitPosition, out var building) && !building.HasDoneAction.Value);
            AddAssert("Funds is 1000", () => ReplayController.Players[0].Funds.Value == 1000);
            AddAssert("Unit Value is 0", () => ReplayController.Players[0].UnitValue.Value == 0);
        }

        [Test]
        public void TestSceneCreateUnitWhileHiddenInFog()
        {
            AddStep("Setup", () => createTest(true));
            AddStep("Swap to Opponent and set hidden", () => swapToOpponentAndSetUnitsHidden(false));
            AddStep("Create Unit", ReplayController.GoToNextAction);
            AddAssert("Unit was created", () => HasUnit(0));
            AddAssert("Building is done", () => ReplayController.Map.TryGetDrawableBuilding(unitPosition, out var building) && building.HasDoneAction.Value);
            AddAssert("Unit is Hidden", () => ReplayController.Map.TryGetDrawableUnit(0, out var unit) && unit.ChildrenOfType<Container>().First().Alpha <= 0);
            AddStep("Reset back to normal", () => swapToOpponentAndSetUnitsHidden(true));
        }

        private void createTest(bool foggy)
        {
            var replayData = CreateBasicReplayData(2);
            replayData.ReplayInfo.Fog = foggy;

            var turn = CreateBasicTurnData(replayData);
            turn.Players[0].Funds = 1000;
            replayData.TurnData.Add(turn);

            var building = CreateBasicReplayBuilding(0, unitPosition, 39);
            turn.Buildings.Add(building.Position, building);

            createdUnit = CreateBasicReplayUnit(0, 0, "Infantry", unitPosition);
            createdUnit.TimesMoved = 1;

            var createUnitAction = new BuildUnitAction
            {
                NewUnit = createdUnit
            };
            turn.Actions.Add(createUnitAction);

            ReplayController.LoadReplay(replayData, CreateBasicMap(5, 5));
        }

        private void swapToOpponentAndSetUnitsHidden(bool reverse)
        {
            ReplayController.CurrentFogView.Value = reverse ? "" : 1L;
            config.SetValue(AWBWSetting.ReplayOnlyShownKnownInfo, reverse);
        }
    }
}
