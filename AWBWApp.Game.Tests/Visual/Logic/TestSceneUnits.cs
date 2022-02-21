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
        private List<int> UnitIds;

        private ReplayData baseData;

        [SetUpSteps]
        public void SetUpSteps()
        {
            UnitIds = GetUnitStorage().GetAllUnitIds();

            var countryIDs = GetCountryStorage().GetAllCountryIDs();

            baseData = CreateBasicReplayData(0);
            baseData.ReplayInfo.Players = new Dictionary<int, ReplayUser>(countryIDs.Count);

            for (int i = 0; i < countryIDs.Count; i++)
            {
                var countryID = countryIDs[i];
                baseData.ReplayInfo.Players[countryID] = new ReplayUser
                {
                    ID = countryID,
                    UserId = countryID,
                    CountryId = countryID
                };
            }
            baseData.TurnData.Add(CreateBasicTurnData(baseData));
            ReplayController.LoadReplay(baseData, CreateBasicMap(UnitIds.Count, countryIDs.Count));
        }

        [Test]
        public void TestDisplayAllUnits()
        {
            var countryIDs = GetCountryStorage().GetAllCountryIDs();

            var turn = CreateBasicTurnData(baseData);

            var unitStorage = GetUnitStorage();

            for (int x = 0; x < UnitIds.Count; x++)
            {
                for (int y = 0; y < countryIDs.Count; y++)
                {
                    var unit = CreateBasicReplayUnit(x * countryIDs.Count + y, countryIDs[y], unitStorage.GetUnitByAWBWId(UnitIds[x]).Name, new Vector2I(x, y));
                    turn.ReplayUnit.Add(unit.ID, unit);
                }
            }

            //Todo: Fix scheduling issues
            ScheduleAfterChildren(() => ReplayController.Map.ScheduleUpdateToGameState(turn, () => ReplayController.Map.ClearFog(false, true)));
        }
    }
}
