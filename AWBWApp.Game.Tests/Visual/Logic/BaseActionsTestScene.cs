using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.Units;
using AWBWApp.Game.UI.Replay;
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
                    ReplayIndex = i + 1,
                    TeamName = i.ToString()
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
                ActiveTeam = activeId.TeamName,
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

        protected bool DoesStatsMatch(UnitStatType type, string unitName, int player1Count, long player1Value, int player2Count, int player2Value)
        {
            switch (type)
            {
                case UnitStatType.LostUnit:
                {
                    if (ReplayController.Stats.CurrentTurnStatsReadout[0].LostStats.Count > 0)
                    {
                        var player1Stat = ReplayController.Stats.CurrentTurnStatsReadout[0].LostStats[unitName];
                        if (player1Stat.Item1 != player1Count || player1Stat.Item2 != player1Value)
                            return false;
                    }
                    else if (player1Count != 0 || player1Value != 0)
                        return false;

                    if (ReplayController.Stats.CurrentTurnStatsReadout[1].LostStats.Count > 0)
                    {
                        var player2Stat = ReplayController.Stats.CurrentTurnStatsReadout[1].LostStats[unitName];
                        if (player2Stat.Item1 != player2Count || player2Stat.Item2 != player2Value)
                            return false;
                    }
                    else if (player2Count != 0 || player2Value != 0)
                        return false;

                    return true;
                }

                case UnitStatType.DamageUnit:
                {
                    if (ReplayController.Stats.CurrentTurnStatsReadout[0].DamageOtherStats.Count > 0)
                    {
                        var player1Stat = ReplayController.Stats.CurrentTurnStatsReadout[0].DamageOtherStats[1][unitName];
                        if (player1Stat.Item1 != player1Count || player1Stat.Item2 != player1Value)
                            return false;
                    }
                    else if (player1Count != 0 || player1Value != 0)
                        return false;

                    if (ReplayController.Stats.CurrentTurnStatsReadout[1].DamageOtherStats.Count > 0)
                    {
                        var player2Stat = ReplayController.Stats.CurrentTurnStatsReadout[1].DamageOtherStats[0][unitName];
                        if (player2Stat.Item1 != player2Count || player2Stat.Item2 != player2Value)
                            return false;
                    }
                    else if (player2Count != 0 || player2Value != 0)
                        return false;

                    return true;
                }

                case UnitStatType.BuildUnit:
                {
                    if (ReplayController.Stats.CurrentTurnStatsReadout[0].BuildStats.Count > 0)
                    {
                        var player1Stat = ReplayController.Stats.CurrentTurnStatsReadout[0].BuildStats[unitName];
                        if (player1Stat.Item1 != player1Count || player1Stat.Item2 != player1Value)
                            return false;
                    }
                    else if (player1Count != 0 || player1Value != 0)
                        return false;

                    if (ReplayController.Stats.CurrentTurnStatsReadout[1].BuildStats.Count > 0)
                    {
                        var player2Stat = ReplayController.Stats.CurrentTurnStatsReadout[1].BuildStats[unitName];
                        if (player2Stat.Item1 != player2Count || player2Stat.Item2 != player2Value)
                            return false;
                    }
                    else if (player2Count != 0 || player2Value != 0)
                        return false;

                    return true;
                }

                case UnitStatType.JoinUnit:
                {
                    if (ReplayController.Stats.CurrentTurnStatsReadout[0].JoinStats.Count > 0)
                    {
                        var player1Stat = ReplayController.Stats.CurrentTurnStatsReadout[0].JoinStats[unitName];
                        if (player1Stat.Item1 != player1Count || player1Stat.Item2 != player1Value)
                            return false;
                    }
                    else if (player1Count != 0 || player1Value != 0)
                        return false;

                    if (ReplayController.Stats.CurrentTurnStatsReadout[1].JoinStats.Count > 0)
                    {
                        var player2Stat = ReplayController.Stats.CurrentTurnStatsReadout[1].JoinStats[unitName];
                        if (player2Stat.Item1 != player2Count || player2Stat.Item2 != player2Value)
                            return false;
                    }
                    else if (player2Count != 0 || player2Value != 0)
                        return false;

                    return true;
                }
            }

            throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(UnitStatType));
        }
    }
}
