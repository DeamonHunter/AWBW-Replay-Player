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
    public partial class TestSceneEndTurnPopupDrawable : AWBWAppTestScene
    {
        [Resolved]
        private CountryStorage countryStorage { get; set; }

        [Resolved]
        private COStorage coStorage { get; set; }

        private EndTurnPopupDrawable endTurnPopup;

        [Test]
        public void TestItem()
        {
            var user = new ReplayUser
            {
                CountryID = 1,
                ID = 1,
                UserId = 1,
                Username = "DeamonHunter"
            };

            var turn = new ReplayUserTurn
            {
                ID = 1,
                Funds = 1000,

                ActiveCOID = 1,
                Power = 45000,
                RequiredPowerForNormal = 90000,
                RequiredPowerForSuper = 180000,

                TagCOID = 2,
                TagPower = 45000,
                TagRequiredPowerForNormal = 90000,
                TagRequiredPowerForSuper = 180000
            };

            var playerInfo = new PlayerInfo(user, countryStorage.GetCountryByAWBWID(user.CountryID));
            playerInfo.UpdateTurn(turn, coStorage, 1, 1, 1000, 2000, ActiveCOPower.None);

            AddStep("Create", () =>
            {
                Clear();
                endTurnPopup = new EndTurnPopupDrawable(playerInfo, 1);

                Add(endTurnPopup);
            });
        }
    }
}
