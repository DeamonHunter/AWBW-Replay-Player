using System.Collections.Generic;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Tests.Visual.Logic.Actions;
using NUnit.Framework;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Testing;

namespace AWBWApp.Game.Tests.Visual.Logic
{
    [TestFixture]
    public class TestSceneUnits : BaseActionsTestScene
    {
        private int[] TeamIDs = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 }; //Todo: Update with actual ids 
        private List<int> UnitIds;

        [SetUpSteps]
        public void SetUpSteps()
        {
            UnitIds = GetUnitStorage().GetAllUnitIds();

            var turnData = CreateBasicTurnData(TeamIDs.Length);
            var replayData = new ReplayData
            {
                TurnData = new List<TurnData> { turnData }
            };
            replayData.ReplayInfo.Players = new AWBWReplayPlayer[TeamIDs.Length];

            for (int i = 0; i < TeamIDs.Length; i++)
            {
                replayData.ReplayInfo.Players[i] = new AWBWReplayPlayer
                {
                    ID = TeamIDs[i],
                    CountryId = TeamIDs[i]
                };
            }
            ReplayController.LoadReplay(replayData, CreateBasicMap(UnitIds.Count, TeamIDs.Length));
        }

        [Test]
        public void TestDisplayAllUnits()
        {
            var turn = new TurnData
            {
                Actions = new List<IReplayAction>(),
                Active = false,
                Buildings = new Dictionary<Vector2I, ReplayBuilding>(),
                CoPowers = new Dictionary<int, int>(),
                Day = 0,
                ActivePlayerID = TeamIDs[0],
                ReplayUnit = new Dictionary<long, ReplayUnit>(),
                Weather = new Weather(),
                Players = new AWBWReplayPlayerTurn[TeamIDs.Length]
            };

            var players = new AWBWReplayPlayer[TeamIDs.Length];
            var playersIndex = new Dictionary<int, int>();

            for (int i = 0; i < TeamIDs.Length; i++)
            {
                var id = TeamIDs[i];

                players[i] = new AWBWReplayPlayer
                {
                    ID = id,
                    CountryId = id
                };
                playersIndex.Add(id, i);
            }

            var unitStorage = GetUnitStorage();

            for (int x = 0; x < UnitIds.Count; x++)
            {
                for (int y = 0; y < TeamIDs.Length; y++)
                {
                    var unit = CreateBasicReplayUnit(x * TeamIDs.Length + y, TeamIDs[y], unitStorage.GetUnitByAWBWId(UnitIds[x]).Name, new Vector2I(x, y));
                    turn.ReplayUnit.Add(unit.ID, unit);
                }
            }
            ReplayController.Map.ScheduleUpdateToGameState(turn);
        }
    }
}
