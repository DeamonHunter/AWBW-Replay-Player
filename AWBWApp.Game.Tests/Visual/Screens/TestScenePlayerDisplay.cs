using System.Collections.Generic;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.COs;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.UI;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace AWBWApp.Game.Tests.Visual.Screens
{
    [TestFixture]
    public class TestScenePlayerDisplay : AWBWAppTestScene
    {
        private PlayerInfo playerInfo;

        [Resolved]
        private COStorage coStorage { get; set; }

        [Test]
        public void TestCreateReplayPlayer()
        {
            AddStep("Create Replay Player", () => reset(false, false));
            AddTests(false);
        }

        [Test]
        public void TestCreateReplayPlayerWithTeam()
        {
            AddStep("Create Replay Player", () => reset(true, false));
            AddTests(false);
        }

        [Test]
        public void TestCreateReplayPlayerWithTag()
        {
            AddStep("Create Replay Player", () => reset(false, true));
            AddTests(true);
        }

        [Test]
        public void TestCreateReplayPlayerWithTeamAndTag()
        {
            AddStep("Create Replay Player", () => reset(true, true));
            AddTests(true);
        }

        private void reset(bool addTeam, bool addTag)
        {
            var replayPlayer = new AWBWReplayPlayer
            {
                ID = 0,
                UserId = 0,
                Username = "This is a really long name",
                CountryId = 1,
                RoundOrder = 0,
                TeamName = addTeam ? "A" : null,
                COsUsedByPlayer = new HashSet<int>(1)
            };

            if (addTag)
                replayPlayer.COsUsedByPlayer.Add(3);

            playerInfo = new PlayerInfo(replayPlayer);

            var replayPlayerTurn = new AWBWReplayPlayerTurn
            {
                ActiveCOID = 1,
                ActiveCOPowers = ActiveCOPowers.None,
                Eliminated = false,
                Funds = 0,
                Power = 0,
                RequiredPowerForNormal = 180000,
                RequiredPowerForSuper = 450000,
            };

            if (addTag)
            {
                replayPlayerTurn.TagCOID = 3;
                replayPlayerTurn.TagPower = 0;
                replayPlayerTurn.TagRequiredPowerForNormal = 270000;
                replayPlayerTurn.TagRequiredPowerForSuper = 720000;
            }

            playerInfo.UpdateTurn(replayPlayerTurn, coStorage, 0, 0, 0, 0);

            Child = new Container()
            {
                AutoSizeAxes = Axes.Y,
                Width = 225,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Child = new ReplayPlayerList.DrawableReplayPlayer(playerInfo)
            };
        }

        private void AddTests(bool tag)
        {
            AddStep("Add money", () => UpdatePlayerInfo(gainMoney: 10000));
            AddStep("Add units", () => UpdatePlayerInfo(gainUnits: 10, gainUnitValue: 10000));
            AddStep("Add property value", () => UpdatePlayerInfo(gainPropertyValue: 10000));
            AddStep("Add power", () => UpdatePlayerInfo(gainPower: 10000));
            AddStep("Set Power to next value", () => UpdatePlayerInfo(gainPower: 10000));

            if (tag)
            {
                AddStep("Swap Tag", () => UpdatePlayerInfo(swapTag: true));
                AddStep("Add power", () => UpdatePlayerInfo(gainPower: 10000));
                AddStep("Set Power to next value", () => UpdatePlayerInfo(gainPower: 10000));
                AddStep("Swap Tag", () => UpdatePlayerInfo(swapTag: true));
            }
        }

        private void UpdatePlayerInfo(bool swapTag = false, int? gainMoney = null, int? gainPower = null, int? newRequiredPower = null, int? newSuperPower = null, int? gainUnits = null, int? gainUnitValue = null, int? gainPropertyValue = null)
        {
            var replayPlayerTurn = new AWBWReplayPlayerTurn
            {
                ActiveCOPowers = ActiveCOPowers.None, //Todo: Show
                Eliminated = playerInfo.Eliminated.Value,
                Funds = playerInfo.Funds.Value,

                ActiveCOID = playerInfo.ActiveCO.Value.CO?.AWBWId ?? 1,
                Power = playerInfo.ActiveCO.Value.Power ?? 0,
                RequiredPowerForNormal = playerInfo.ActiveCO.Value.PowerRequiredForNormal,
                RequiredPowerForSuper = playerInfo.ActiveCO.Value.PowerRequiredForSuper,

                TagCOID = playerInfo.TagCO.Value.CO?.AWBWId,
                TagPower = playerInfo.TagCO.Value.Power,
                TagRequiredPowerForNormal = playerInfo.TagCO.Value.PowerRequiredForNormal,
                TagRequiredPowerForSuper = playerInfo.TagCO.Value.PowerRequiredForSuper
            };

            if (gainMoney.HasValue)
                replayPlayerTurn.Funds += gainMoney.Value;

            if (gainPower.HasValue)
                replayPlayerTurn.Power += gainPower.Value;

            if (newRequiredPower.HasValue || newSuperPower.HasValue)
            {
                replayPlayerTurn.RequiredPowerForNormal = newRequiredPower;
                replayPlayerTurn.RequiredPowerForSuper = newSuperPower;
            }

            if (swapTag)
            {
                (replayPlayerTurn.ActiveCOID, replayPlayerTurn.TagCOID) = (replayPlayerTurn.TagCOID ?? 1, replayPlayerTurn.ActiveCOID);
                (replayPlayerTurn.Power, replayPlayerTurn.TagPower) = (replayPlayerTurn.TagPower ?? 0, replayPlayerTurn.Power);
                (replayPlayerTurn.RequiredPowerForNormal, replayPlayerTurn.TagRequiredPowerForNormal) = (replayPlayerTurn.TagRequiredPowerForNormal, replayPlayerTurn.RequiredPowerForNormal);
                (replayPlayerTurn.RequiredPowerForSuper, replayPlayerTurn.TagRequiredPowerForSuper) = (replayPlayerTurn.TagRequiredPowerForSuper, replayPlayerTurn.RequiredPowerForSuper);
            }

            var unitCount = playerInfo.UnitCount.Value + (gainUnits ?? 0);
            var unitValue = playerInfo.UnitCount.Value + (gainUnitValue ?? 0);
            var propertyValue = playerInfo.UnitCount.Value + (gainPropertyValue ?? 0);

            playerInfo.UpdateTurn(replayPlayerTurn, coStorage, 0, unitCount, unitValue, propertyValue);
        }
    }
}
