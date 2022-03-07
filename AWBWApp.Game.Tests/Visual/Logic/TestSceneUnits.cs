using System.Collections.Generic;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Tests.Visual.Logic.Actions;
using NUnit.Framework;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Tests.Visual.Logic
{
    [TestFixture]
    public class TestSceneUnits : BaseActionsTestScene
    {
        private List<int> UnitIds;

        private ReplayData baseData;

        [Test]
        public void TestDisplayAllUnits()
        {
            AddStep("Show All units", () =>
            {
                UnitIds = GetUnitStorage().GetAllUnitIds();

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
                        CountryId = countryID
                    };
                }

                var turn = CreateBasicTurnData(baseData);
                baseData.TurnData.Add(turn);

                var unitStorage = GetUnitStorage();

                for (int x = 0; x < UnitIds.Count; x++)
                {
                    for (int y = 0; y < countryIDs.Count; y++)
                    {
                        var unit = CreateBasicReplayUnit(x * countryIDs.Count + y, countryIDs[y], unitStorage.GetUnitByAWBWId(UnitIds[x]).Name, new Vector2I(x, y));
                        turn.ReplayUnit.Add(unit.ID, unit);
                    }
                }

                ReplayController.LoadReplay(baseData, CreateBasicMap(UnitIds.Count, countryIDs.Count));
            });
        }
    }
}
