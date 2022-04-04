using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Game.Tile;
using Newtonsoft.Json;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Primitives;
using osu.Framework.IO.Stores;

namespace AWBWApp.Game.Tests.Visual.Logic
{
    [TestFixture]
    public class TestSceneTerrain : BaseGameMapTestScene
    {
        private const int max_awbw_id = 194;
        private const int grass_terrain_id = 1;

        private const int orange_star_hq_id = 42;
        private const int pipe_seam_id = 113;
        private const int pipe_rubble_id = 115;

        private ResourceStore<byte[]> storage;

        private CustomShoalGenerator generator;

        [BackgroundDependencyLoader]
        private void load(ResourceStore<byte[]> store)
        {
            storage = store;
            generator = new CustomShoalGenerator(GetTileStorage(), GetBuildingStorage());
        }

        [Test]
        public void TestBasicMaps()
        {
            AddStep("Render Small Map", () => renderBasicMap(8, 8));
            AddStep("Render Tall Map", () => renderBasicMap(8, 24));
            AddStep("Render Wide Map", () => renderBasicMap(24, 8));
            AddStep("Render Large Map", () => renderBasicMap(64, 64));
        }

        [Test]
        public void TestRandomMaps()
        {
            AddStep("Render Small Map", () => renderRandomMap(8, 8));
            AddStep("Render Tall Map", () => renderRandomMap(8, 24));
            AddStep("Render Wide Map", () => renderRandomMap(24, 8));
            AddStep("Render Large Map", () => renderRandomMap(64, 64));
        }

        [Test]
        public void TestBuildings()
        {
            AddStep("Render All IDs", () => renderMapWithAllIDs(16, 16));
            AddStep("Change Weather", () => ReplayController.GoToNextTurn());
            AddStep("Change Weather", () => ReplayController.GoToNextTurn());
        }

        [Test]
        public void TestCustomShoalRendering()
        {
            AddStep("Map: Sea Test", () => loadMapFromFile("Json/Maps/SeaTest"));
            AddStep("Map: Shoal Test", () => loadMapFromFile("Json/Maps/ShoalTest"));
            AddStep("Map: Shoal Alt Test A", () => loadMapFromFile("Json/Maps/ShoalAltTestA"));
            AddStep("Map: Shoal Alt Test B", () => loadMapFromFile("Json/Maps/ShoalAltTestB"));
            AddStep("Map: Custom Shoals", () => loadMapFromFile("Json/Maps/98331"));
        }

        [Test]
        public void TestOrderOfDrawablesWhenChanging()
        {
            AddStep("Setup", createBuildingChangeOrderingTest);
            AddStep("Change To Rubble", () => ReplayController.Map.UpdateBuilding(new ReplayBuilding { Position = new Vector2I(2, 1), TerrainID = pipe_rubble_id }, false));
        }

        private void renderBasicMap(int xSize, int ySize)
        {
            var replayData = createEmptyReplayWithWeatherChanges();
            ReplayController.LoadReplay(replayData, CreateBasicMap(xSize, ySize));
        }

        private void renderRandomMap(int xSize, int ySize)
        {
            var replayData = createEmptyReplayWithWeatherChanges();
            var gameMap = new ReplayMap
            {
                Size = new Vector2I(xSize, ySize),
                Ids = new short[xSize * ySize]
            };

            var random = new Random();

            var terrainTileStorage = GetTileStorage();

            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    var randomTile = terrainTileStorage.GetRandomTerrainTile(random);
                    gameMap.Ids[x * ySize + y] = (short)randomTile.AWBWID;
                }
            }

            ReplayController.LoadReplay(replayData, gameMap);
        }

        private void renderMapWithAllIDs(int xSize, int ySize)
        {
            var replay = CreateEmptyReplay();
            var turn = replay.TurnData[0];
            replay.TurnData = new List<TurnData> { turn };

            var gameMap = new ReplayMap
            {
                Size = new Vector2I(xSize, ySize),
                Ids = new short[xSize * ySize]
            };

            var tileStorage = GetTileStorage();
            var buildingStorage = GetBuildingStorage();

            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    var id = x * ySize + y + 1; //+1 to skip id 0
                    if (id > max_awbw_id)
                        id = grass_terrain_id;

                    if ((id > 57 && id < 81) || (id > 176 && id < 181))
                        id = grass_terrain_id; //There is a gap in id's here for some reason.

                    if (buildingStorage.TryGetBuildingByAWBWId(id, out BuildingTile building))
                    {
                        var replayBuilding = new ReplayBuilding
                        {
                            ID = building.AWBWID,
                            TerrainID = building.AWBWID,
                            Position = new Vector2I(x, y)
                        };

                        turn.Buildings.Add(new Vector2I(x, y), replayBuilding);
                        gameMap.Ids[y * xSize + x] = grass_terrain_id;
                    }
                    else if (tileStorage.TryGetTileByAWBWId(id, out TerrainTile tile))
                        gameMap.Ids[y * xSize + x] = (short)tile.AWBWID;
                    else
                        throw new Exception($"Unknown AWBWID: {id}");
                }
            }

            replay.TurnData.Add(new TurnData
            {
                ActivePlayerID = 0,
                Players = new Dictionary<long, ReplayUserTurn> { { 0, new ReplayUserTurn { ActiveCOID = 1, RequiredPowerForNormal = 90000, RequiredPowerForSuper = 180000 } } },
                ReplayUnit = turn.ReplayUnit,
                Buildings = turn.Buildings,
                StartWeather = new ReplayWeather { Type = Weather.Rain }
            });
            replay.TurnData.Add(new TurnData
            {
                ActivePlayerID = 0,
                Players = new Dictionary<long, ReplayUserTurn> { { 0, new ReplayUserTurn { ActiveCOID = 1, RequiredPowerForNormal = 90000, RequiredPowerForSuper = 180000 } } },
                ReplayUnit = turn.ReplayUnit,
                Buildings = turn.Buildings,
                StartWeather = new ReplayWeather { Type = Weather.Snow }
            });

            ReplayController.LoadReplay(replay, gameMap);
        }

        private void loadMapFromFile(string mapPath)
        {
            var replay = CreateEmptyReplay();
            var turn = replay.TurnData[0];

            ReplayMap gameMap;

            using (var stream = storage.GetStream(mapPath))
            {
                using (var sr = new StreamReader(stream))
                    gameMap = JsonConvert.DeserializeObject<ReplayMap>(sr.ReadToEnd());
            }

            Debug.Assert(gameMap != null, "Game Map was Null.");

            var buildingStorage = GetBuildingStorage();

            for (int i = 0; i < gameMap.Ids.Length; i++)
            {
                var tileId = gameMap.Ids[i];

                if (!buildingStorage.TryGetBuildingByAWBWId(tileId, out _))
                    continue;

                var position = new Vector2I(i % gameMap.Size.X, i / gameMap.Size.X);
                turn.Buildings.Add(position, new ReplayBuilding { ID = i, TerrainID = tileId, Position = position });
            }

            var shoal = generator.CreateCustomShoalVersion(gameMap);

            ReplayController.LoadReplay(replay, shoal);
        }

        private void createBuildingChangeOrderingTest()
        {
            var replayData = createEmptyReplayWithWeatherChanges();

            var map = CreateBasicMap(5, 5);

            var buildings = replayData.TurnData[0].Buildings;
            buildings.Add(new Vector2I(2, 2), new ReplayBuilding
            {
                ID = 0,
                TerrainID = orange_star_hq_id,
                Position = new Vector2I(2, 2)
            });
            buildings.Add(new Vector2I(2, 1), new ReplayBuilding
            {
                ID = 1,
                TerrainID = pipe_seam_id,
                Position = new Vector2I(2, 1)
            });

            ReplayController.LoadReplay(replayData, map);
        }

        private ReplayData createEmptyReplayWithWeatherChanges()
        {
            var replay = CreateEmptyReplay();
            replay.TurnData.Add(new TurnData
            {
                ActivePlayerID = 0,
                Players = new Dictionary<long, ReplayUserTurn> { { 0, new ReplayUserTurn { ActiveCOID = 1, RequiredPowerForNormal = 90000, RequiredPowerForSuper = 180000 } } },
                ReplayUnit = new Dictionary<long, ReplayUnit>(),
                Buildings = new Dictionary<Vector2I, ReplayBuilding>(),
                StartWeather = new ReplayWeather { Type = Weather.Rain }
            });
            replay.TurnData.Add(new TurnData
            {
                ActivePlayerID = 0,
                Players = new Dictionary<long, ReplayUserTurn> { { 0, new ReplayUserTurn { ActiveCOID = 1, RequiredPowerForNormal = 90000, RequiredPowerForSuper = 180000 } } },
                ReplayUnit = new Dictionary<long, ReplayUnit>(),
                Buildings = new Dictionary<Vector2I, ReplayBuilding>(),
                StartWeather = new ReplayWeather { Type = Weather.Snow }
            });

            return replay;
        }
    }
}
