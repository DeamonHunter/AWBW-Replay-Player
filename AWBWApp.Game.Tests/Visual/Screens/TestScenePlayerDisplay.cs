using System;
using System.Collections.Generic;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.COs;
using AWBWApp.Game.Game.Country;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.UI;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.Tests.Visual.Screens
{
    [TestFixture]
    public class TestScenePlayerDisplay : AWBWAppTestScene
    {
        private PlayerInfo playerInfo;

        [Resolved]
        private COStorage coStorage { get; set; }

        [Resolved]
        private CountryStorage countryStorage { get; set; }

        [Test]
        public void TestCreateReplayPlayer()
        {
            AddStep("Create Replay Player", () => reset(false, false));
            addTests(false);
        }

        [Test]
        public void TestCreateReplayPlayerWithTeam()
        {
            AddStep("Create Replay Player", () => reset(true, false));
            addTests(false);
        }

        [Test]
        public void TestCreateReplayPlayerWithTag()
        {
            AddStep("Create Replay Player", () => reset(false, true));
            addTests(true);
        }

        [Test]
        public void TestCreateReplayPlayerWithTeamAndTag()
        {
            AddStep("Create Replay Player", () => reset(true, true));
            addTests(true);
        }

        private void reset(bool addTeam, bool addTag)
        {
            var replayPlayer = new ReplayUser
            {
                ID = 0,
                UserId = 0,
                Username = "This is a really long name",
                CountryID = 1,
                RoundOrder = 0,
                TeamName = addTeam ? "A" : null,
                COsUsedByPlayer = new HashSet<int>(1)
            };

            if (addTag)
                replayPlayer.COsUsedByPlayer.Add(3);

            playerInfo = new PlayerInfo(replayPlayer, countryStorage.GetCountryByAWBWID(1));

            var replayPlayerTurn = new ReplayUserTurn
            {
                ActiveCOID = 1,
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

            playerInfo.UpdateTurn(replayPlayerTurn, coStorage, 0, 0, 0, 0, ActiveCOPower.None);

            Child = new Container
            {
                AutoSizeAxes = Axes.Y,
                Width = 225,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Children = new Drawable[]
                {
                    new Box()
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(42, 91, 139, 255).Lighten(0.2f),
                        Size = new Vector2(2)
                    },
                    new ReplayPlayerList.DrawableReplayPlayer(playerInfo)
                }
            };
        }

        private void addTests(bool tag)
        {
            AddStep("Add money", () => updatePlayerInfo(gainMoney: 10000));
            AddStep("Add units", () => updatePlayerInfo(gainUnits: 10, gainUnitValue: 10000));
            AddStep("Add property value", () => updatePlayerInfo(gainPropertyValue: 10000));
            AddStep("Add power", () => updatePlayerInfo(gainPower: 100000));
            AddStep("Fill Power", () => updatePlayerInfo(gainPower: int.MaxValue));
            AddStep("Activate Normal Power", () => updatePlayerInfo(activatePower: ActiveCOPower.Normal));
            AddStep("Activate Super Power", () => updatePlayerInfo(activatePower: ActiveCOPower.Super));
            AddStep("Deactivate Power", () => updatePlayerInfo());

            if (tag)
            {
                AddStep("Swap Tag", () => updatePlayerInfo(swapTag: true));
                AddStep("Add power", () => updatePlayerInfo(gainPower: 10000));
                AddStep("Fill Power", () => updatePlayerInfo(gainPower: int.MaxValue));
                AddStep("Swap Tag", () => updatePlayerInfo(swapTag: true));
            }
        }

        private void updatePlayerInfo(bool swapTag = false, int? gainMoney = null, int? gainPower = null, int? newRequiredPower = null, int? newSuperPower = null, int? gainUnits = null, int? gainUnitValue = null, int? gainPropertyValue = null, ActiveCOPower activatePower = ActiveCOPower.None)
        {
            var replayPlayerTurn = new ReplayUserTurn
            {
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
                replayPlayerTurn.Power = (int)Math.Min(replayPlayerTurn.RequiredPowerForSuper ?? 0, (long)replayPlayerTurn.Power + gainPower.Value);

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

            if (activatePower != null)
                replayPlayerTurn.Power = 0;

            var unitCount = playerInfo.UnitCount.Value + (gainUnits ?? 0);
            var unitValue = playerInfo.UnitCount.Value + (gainUnitValue ?? 0);
            var propertyValue = playerInfo.UnitCount.Value + (gainPropertyValue ?? 0);

            playerInfo.UpdateTurn(replayPlayerTurn, coStorage, 0, unitCount, unitValue, propertyValue, activatePower);
        }
    }
}
