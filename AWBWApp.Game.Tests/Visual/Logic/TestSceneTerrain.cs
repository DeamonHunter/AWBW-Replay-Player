using System;
using System.Collections.Generic;
using AWBWApp.Game.API;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Tile;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Tests.Visual.Logic
{
    public class TestSceneTerrain : BaseGameMapTestScene
    {
        private const int max_awbw_id = 194;
        private const int grass_terrain_id = 1;

        [BackgroundDependencyLoader]
        private void load()
        {
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
        }

        public void RenderBasicMap(int xSize, int ySize)
        {
            var replayData = new ReplayData();
            ReplayController.LoadReplay(replayData, CreateBasicMap(xSize, ySize));
        }

        public void RenderRandomMap(int xSize, int ySize)
        {
            var replayData = new ReplayData();
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
            var replay = new ReplayData();
            var turn = new TurnData
            {
                Buildings = new Dictionary<Vector2I, ReplayBuilding>()
            };
            replay.TurnData = new List<TurnData> { turn };

            var gameMap = new ReplayMap();
            gameMap.Size = new Vector2I(xSize, ySize);
            gameMap.Ids = new short[xSize * ySize];

            var tileStorage = GetTileStorage();
            var buildingStorage = GetBuildingStorage();

            for (int x = 0; x < xSize; x++)
            {
                var row = new Dictionary<int, AWBWTile>();

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
                            ID = building.AWBWId,
                            TerrainID = building.AWBWId
                        };

                        turn.Buildings.Add(new Vector2I(x, y), replayBuilding);
                        gameMap.Ids[x * ySize + y] = grass_terrain_id;
                    }
                    else if (tileStorage.TryGetTileByAWBWId(id, out TerrainTile tile))
                        gameMap.Ids[x * ySize + y] = (short)tile.AWBWId;
                    else
                        throw new Exception($"Unknown AWBWID: {id}");
                }
            }

            ReplayController.LoadReplay(replay, gameMap);
        }
    }
}
