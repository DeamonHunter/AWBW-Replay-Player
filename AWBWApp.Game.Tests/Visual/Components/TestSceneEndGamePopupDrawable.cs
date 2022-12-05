using System.Collections.Generic;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.COs;
using AWBWApp.Game.Game.Country;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.UI.Replay;
using NUnit.Framework;
using osu.Framework.Allocation;

namespace AWBWApp.Game.Tests.Visual.Components
{
    [TestFixture]
    public partial class TestSceneEndGamePopupDrawable : AWBWAppTestScene
    {
        [Resolved]
        private CountryStorage countryStorage { get; set; }

        [Resolved]
        private COStorage coStorage { get; set; }

        private EndGamePopupDrawable endGamePopup;

        [Test]
        public void TestItem()
        {
            const bool handle_tag_cos = true;

            var players = new Dictionary<long, PlayerInfo>();

            var countries = countryStorage.GetAllCountryIDs();
            var cos = coStorage.GetAllCOIDs();

            var winningIds = new List<long>();
            var losingIds = new List<long>();

            for (int i = 0; i < 16; i++)
            {
                var user = new ReplayUser
                {
                    CountryID = countries[i],
                    ID = i,
                    UserId = i,
                    Username = "DeamonHunter",
                    EliminatedOn = i != 0 ? 0 : null
                };

                var turn = new ReplayUserTurn
                {
                    ID = i,
                    Funds = 1000,
                    Eliminated = i != 0,

                    ActiveCOID = handle_tag_cos ? cos[(i * 2) % cos.Count] : cos[i],
                    Power = 45000,
                    RequiredPowerForNormal = 90000,
                    RequiredPowerForSuper = 180000,
                };

                if (handle_tag_cos)
                {
                    turn.TagCOID = cos[(i * 2 + 1) % cos.Count];
                    turn.TagPower = 45000;
                    turn.TagRequiredPowerForNormal = 90000;
                    turn.TagRequiredPowerForSuper = 180000;
                }

                var playerInfo = new PlayerInfo(user, countryStorage.GetCountryByAWBWID(user.CountryID));
                playerInfo.UpdateTurn(turn, coStorage, 1, 1, 1000, 2000, ActiveCOPower.None);
                players.Add(i, playerInfo);

                if (i % 2 == 0)
                    winningIds.Add(i);
                else
                    losingIds.Add(i);
            }

            AddStep("Create", () =>
            {
                Clear();
                endGamePopup = new EndGamePopupDrawable(players, winningIds, losingIds, "The game is over! DeamonHunter is the winner!", false, null);

                Add(endGamePopup);
            });
        }
    }
}
