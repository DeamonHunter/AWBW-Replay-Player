using System;
using System.Collections.Generic;
using AWBWApp.Game.API;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Tile;
using osu.Framework.Allocation;

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
            var state = AWBWGameState.GenerateBlankState();
            state.Terrain.Clear();

            for (int x = 0; x < xSize; x++)
            {
                var row = new Dictionary<int, AWBWTile>();

                for (int y = 0; y < ySize; y++)
                {
                    var tile = new AWBWTile { Terrain_Id = grass_terrain_id };
                    row.Add(y, tile);
                }
                state.Terrain.Add(x, row);
            }

            ReplayController.ShowGameState(state);
        }

        public void RenderRandomMap(int xSize, int ySize)
        {
            var state = AWBWGameState.GenerateBlankState();
            state.Terrain.Clear();

            var random = new Random();

            var storage = GetTileStorage();

            for (int x = 0; x < xSize; x++)
            {
                var row = new Dictionary<int, AWBWTile>();

                for (int y = 0; y < ySize; y++)
                {
                    var randomTile = storage.GetRandomTerrainTile(random);

                    var tile = new AWBWTile { Terrain_Id = randomTile.AWBWId };
                    row.Add(y, tile);
                }
                state.Terrain.Add(x, row);
            }

            ReplayController.ShowGameState(state);
        }

        public void RenderMapWithAllIDs(int xSize, int ySize)
        {
            var state = AWBWGameState.GenerateBlankState();
            state.Terrain.Clear();
            state.Buildings.Clear();

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
                        row.Add(y, new AWBWTile { Terrain_Id = grass_terrain_id });

                        if (!state.Buildings.TryGetValue(x, out Dictionary<int, AWBWBuilding> buildingsRow))
                        {
                            buildingsRow = new Dictionary<int, AWBWBuilding>();
                            state.Buildings.Add(x, buildingsRow);
                        }
                        buildingsRow.Add(y, new AWBWBuilding { Terrain_Id = id });
                    }
                    else if (tileStorage.TryGetTileByAWBWId(id, out TerrainTile tile))
                    {
                        row.Add(y, new AWBWTile { Terrain_Id = tile.AWBWId });
                    }
                    else
                        throw new Exception($"Unknown AWBWID: {id}");
                }
                state.Terrain.Add(x, row);
            }

            ReplayController.ShowGameState(state);
        }
    }
}
