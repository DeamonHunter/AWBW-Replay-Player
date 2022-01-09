using System.Collections.Generic;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.API.Replay.Actions;
using NUnit.Framework;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Tests.Visual.Logic.Actions
{
    //Todo: Add trapping
    [TestFixture]
    public class TestSceneBuildUnitAction : BaseActionsTestScene
    {
        [Test]
        public void TestCreateUnit()
        {
            AddStep("Setup", createTest);
            AddStep("Create Unit", ReplayController.GoToNextAction);
            AddAssert("Unit was created", () => ReplayController.Map.TryGetDrawableUnit(0, out _));
        }

        private void createTest()
        {
            var replayData = CreateBasicReplayData(2);

            var turn = new TurnData();
            turn.ReplayUnit = new Dictionary<long, ReplayUnit>();
            turn.Buildings = new Dictionary<Vector2I, ReplayBuilding>();
            turn.Actions = new List<IReplayAction>();

            replayData.TurnData.Add(turn);

            var building = CreateBasicReplayBuilding(0, new Vector2I(2, 2), 39);
            turn.Buildings.Add(building.Position, building);

            var createdUnit = CreateBasicReplayUnit(0, 1, "Infantry", new Vector2I(2, 2));

            var createUnitAction = new BuildUnitAction();
            createUnitAction.NewUnit = createdUnit;
            turn.Actions.Add(createUnitAction);

            ReplayController.LoadReplay(replayData, CreateBasicMap(5, 5));
        }
    }
}
