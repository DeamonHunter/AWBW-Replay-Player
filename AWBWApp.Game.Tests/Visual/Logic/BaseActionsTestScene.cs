using System;
using System.Collections.Generic;
using System.Linq;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.Units;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Tests.Visual.Logic
{
    public abstract class BaseActionsTestScene : BaseGameMapTestScene
    {
        protected ReplayData CreateBasicReplayData(int playerCount)
        {
            var replayData = new ReplayData
            {
                ReplayInfo = new ReplayInfo
                {
                    //Create some basic players
                    Players = new Dictionary<long, ReplayUser>(playerCount),
                    FundsPerBuilding = 1000
                }
            };

            for (int i = 0; i < playerCount; i++)
            {
                var player = new ReplayUser
                {
                    CountryID = i + 1,
                    RoundOrder = i + 1,
                    UserId = i,
                    ID = i,
                    ReplayIndex = i + 1
                };

                replayData.ReplayInfo.Players.Add(player.ID, player);
            }

            replayData.TurnData = new List<TurnData>();
            return replayData;
        }

        protected TurnData CreateBasicTurnData(ReplayData data, int playerIdx = 0)
        {
            var players = new Dictionary<long, ReplayUserTurn>();

            foreach (var player in data.ReplayInfo.Players)
            {
                var playerData = new ReplayUserTurn
                {
                    ActiveCOID = player.Value.CountryID == 4 || player.Value.CountryID == 6 ? 17 : player.Value.CountryID, //Skip over 4/6 as those are not valid
                    RequiredPowerForNormal = 90000,
                    RequiredPowerForSuper = 180000
                };

                players.Add(player.Key, playerData);
            }

            var playersInOrder = data.ReplayInfo.Players.Values.ToList();
            playersInOrder.Sort((x, y) => x.RoundOrder.CompareTo(y.RoundOrder));

            var activeId = playersInOrder[playerIdx];

            return new TurnData
            {
                Active = true,
                Actions = new List<IReplayAction>(),
                Buildings = new Dictionary<Vector2I, ReplayBuilding>(),
                ReplayUnit = new Dictionary<long, ReplayUnit>(),
                Day = 0,
                ActivePlayerID = activeId.ID,
                Players = players,
                StartWeather = new ReplayWeather()
            };
        }

        protected ReplayUnit CreateBasicReplayUnit(int id, int? playerId, string unitName, Vector2I position)
        {
            var storage = GetUnitStorage();
            var baseUnitData = storage.GetUnitByCode(unitName);
            return new ReplayUnit
            {
                ID = id,
                PlayerID = playerId,
                UnitName = unitName,

                HitPoints = 10,
                Position = position,
                Fuel = baseUnitData.MaxFuel,
                FuelPerTurn = baseUnitData.FuelUsagePerTurn,
                Ammo = baseUnitData.MaxAmmo,

                Cost = baseUnitData.Cost,
                MovementPoints = baseUnitData.MovementRange,
                Vision = baseUnitData.Vision,
                Range = baseUnitData.AttackRange,
                MovementType = baseUnitData.MovementType.ToString(), //Todo: Work out unit type stuff

                TimesMoved = 0,
                TimesCaptured = 0,
                TimesFired = 0,

                SubHasDived = false,
                SecondWeapon = null,

                BeingCarried = false,
                CargoUnits = null
            };
        }

        protected ReplayBuilding CreateBasicReplayBuilding(int id, Vector2I position, int terrainId)
        {
            //Todo: Fix team
            return new ReplayBuilding
            {
                ID = id,
                Position = position,
                TerrainID = terrainId,
                Capture = 20,
                LastCapture = 20
            };
        }

        protected bool HasUnit(long unitID) => ReplayController.Map.TryGetDrawableUnit(unitID, out _);

        protected bool DoesUnitMatchData(long unitID, ReplayUnit unit)
        {
            if (!ReplayController.Map.TryGetDrawableUnit(unitID, out var drawableUnit))
                return false;

            return unit.DoesDrawableUnitMatch(drawableUnit);
        }

        protected bool DoesUnitPassTest(long unitID, Func<DrawableUnit, bool> test)
        {
            if (!ReplayController.Map.TryGetDrawableUnit(unitID, out var drawableUnit))
                return false;

            return test(drawableUnit);
        }
    }
}
