using System.Collections.Generic;
using AWBWApp.Game.API.Replay;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Tests.Visual.Logic.Actions
{
    public abstract class BaseActionsTestScene : BaseGameMapTestScene
    {
        protected ReplayData CreateBasicReplayData(int playerCount)
        {
            var replayData = new ReplayData();

            //Create some basic players

            replayData.ReplayInfo.Players = new AWBWReplayPlayer[playerCount];
            replayData.ReplayInfo.PlayerIds = new Dictionary<int, int>();

            for (int i = 0; i < playerCount; i++)
            {
                var player = new AWBWReplayPlayer
                {
                    CountryId = i + 1,
                    UserId = i,
                    ID = i
                };

                replayData.ReplayInfo.PlayerIds.Add(i, i);
                replayData.ReplayInfo.Players[i] = player;
            }

            replayData.TurnData = new List<TurnData>();
            return replayData;
        }

        protected TurnData CreateBasicTurnData(int playerCount)
        {
            return new TurnData
            {
                Active = true,
                Actions = new List<IReplayAction>(),
                Buildings = new Dictionary<Vector2I, ReplayBuilding>(),
                ReplayUnit = new Dictionary<long, ReplayUnit>(),
                CoPowers = new Dictionary<int, int>(),
                Day = 0,
                ActivePlayerID = 0,
                Players = new AWBWReplayPlayerTurn[playerCount],
                Weather = new Weather()
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
