using System;
using System.Collections.Generic;
using System.IO;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Game.Tile;
using Newtonsoft.Json;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Primitives;
using osu.Framework.IO.Stores;

namespace AWBWApp.Game.Tests.Visual.Logic
{
    public class TestSceneTerrain : BaseGameMapTestScene
    {
        private const int max_awbw_id = 194;
        private const int grass_terrain_id = 1;

        private ResourceStore<byte[]> storage;

        private CustomShoalGenerator generator;

        [BackgroundDependencyLoader]
        private void load(ResourceStore<byte[]> store)
        {
            storage = store;

            AddLabel("Basic Rendering");
            AddStep("Render Small Map", () => RenderBasicMap(8, 8));
            AddStep("Render Tall Map", () => RenderBasicMap(8, 24));
            AddStep("Render Wide Map", () => RenderBasicMap(24, 8));
            AddStep("Render Large Map", () => RenderBasicMap(64, 64));

            AddLabel("Random Rendering");
            AddStep("Render Small Map", () => RenderRandomMap(8, 8));
            AddStep("Render Tall Map", () => RenderRandomMap(8, 24));
            AddStep("Render Wide Map", () => RenderRandomMap(24, 8));
            AddStep("Render Large Map", () => RenderRandomMap(64, 64));

            AddLabel("Building Test");
            AddStep("Render All IDs", () => RenderMapWithAllIDs(16, 16));
            AddStep("Change Weather", () => ReplayController.GoToNextTurn());
            AddStep("Change Weather", () => ReplayController.GoToNextTurn());

            AddLabel("Shoals Test");
            AddStep("Map: Sea Test", () => LoadMapFromFile("Json/Maps/SeaTest"));
            AddStep("Map: Shoal Test", () => LoadMapFromFile("Json/Maps/ShoalTest"));
            AddStep("Map: Custom Shoals", () => LoadMapFromFile("Json/Maps/98331"));

            generator = new CustomShoalGenerator(GetTileStorage(), GetBuildingStorage());
        }

        public void RenderBasicMap(int xSize, int ySize)
        {
            var replayData = createEmptyReplayWithWeatherChanges();
            ReplayController.LoadReplay(replayData, CreateBasicMap(xSize, ySize));
        }

        public void RenderRandomMap(int xSize, int ySize)
        {
            var replayData = createEmptyReplayWithWeatherChanges();
            var gameMap = new ReplayMap();
            gameMap.Size = new Vector2I(xSize, ySize);
            gameMap.Ids = new short[xSize * ySize];

            var random = new Random();

            var storage = GetTileStorage();

            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    var randomTile = storage.GetRandomTerrainTile(random);
                    gameMap.Ids[x * ySize + y] = (short)randomTile.AWBWId;
                }
            }

            ReplayController.LoadReplay(replayData, gameMap);
        }

        public void RenderMapWithAllIDs(int xSize, int ySize)
        {
            var replay = CreateEmptyReplay();
            var turn = replay.TurnData[0];
            replay.TurnData = new List<TurnData> { turn };

            var gameMap = new ReplayMap();
            gameMap.Size = new Vector2I(xSize, ySize);
            gameMap.Ids = new short[xSize * ySize];

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
                        gameMap.Ids[y * xSize + x] = (short)tile.AWBWId;
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

        public void LoadMapFromFile(string mapPath)
        {
            var replay = CreateEmptyReplay();
            var turn = replay.TurnData[0];

            ReplayMap gameMap;

            using (var stream = storage.GetStream(mapPath))
            {
                using (var sr = new StreamReader(stream))
                    gameMap = JsonConvert.DeserializeObject<ReplayMap>(sr.ReadToEnd());
            }

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
