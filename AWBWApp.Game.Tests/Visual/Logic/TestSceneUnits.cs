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

            var replayData = CreateBasicReplayData(0);
            replayData.ReplayInfo.Players = new Dictionary<int, ReplayUser>(TeamIDs.Length);

            for (int i = 0; i < TeamIDs.Length; i++)
            {
                replayData.ReplayInfo.Players[i] = new ReplayUser
                {
                    ID = TeamIDs[i],
                    CountryId = TeamIDs[i]
                };
            }
            replayData.TurnData.Add(CreateBasicTurnData(replayData));
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
                Weather = new ReplayWeather(),
                Players = new Dictionary<int, AWBWReplayPlayerTurn>(TeamIDs.Length)
            };

            var players = new ReplayUser[TeamIDs.Length];
            var playersIndex = new Dictionary<int, int>();

            for (int i = 0; i < TeamIDs.Length; i++)
            {
                var id = TeamIDs[i];

                players[i] = new ReplayUser
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
            ReplayController.Map.ScheduleUpdateToGameState(turn, () => ReplayController.Map.ClearFog(false, true));
        }
    }
}
