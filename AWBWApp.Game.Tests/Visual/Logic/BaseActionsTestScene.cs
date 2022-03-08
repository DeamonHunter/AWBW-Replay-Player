using System.Collections.Generic;
using System.Linq;
using AWBWApp.Game.API.Replay;
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
                    Players = new Dictionary<long, ReplayUser>(playerCount)
                }
            };

            for (int i = 0; i < playerCount; i++)
            {
                var player = new ReplayUser
                {
                    CountryId = i + 1,
                    UserId = i,
                    ID = i
                };

                replayData.ReplayInfo.Players.Add(player.ID, player);
            }

            replayData.TurnData = new List<TurnData>();
            return replayData;
        }

        protected TurnData CreateBasicTurnData(ReplayData data)
        {
            var players = new Dictionary<long, ReplayUserTurn>();

            foreach (var player in data.ReplayInfo.Players)
            {
                var playerData = new ReplayUserTurn
                {
                    ActiveCOID = player.Value.CountryId == 4 || player.Value.CountryId == 6 ? 17 : player.Value.CountryId, //Skip over 4/6 as those are not valid
                    RequiredPowerForNormal = 90000,
                    RequiredPowerForSuper = 180000
                };

                players.Add(player.Key, playerData);
            }

            return new TurnData
            {
                Active = true,
                Actions = new List<IReplayAction>(),
                Buildings = new Dictionary<Vector2I, ReplayBuilding>(),
                ReplayUnit = new Dictionary<long, ReplayUnit>(),
                Day = 0,
                ActivePlayerID = data.ReplayInfo.Players.First().Key,
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
            };
        }
    }
}
