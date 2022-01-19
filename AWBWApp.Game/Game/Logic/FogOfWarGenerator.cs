using System;
using AWBWApp.Game.Game.Building;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Game.Logic
{
    public class FogOfWarGenerator
    {
        public Bindable<bool[,]> FogOfWar;

        private GameMap gameMap;

        public FogOfWarGenerator(GameMap map)
        {
            gameMap = map;
            FogOfWar = new Bindable<bool[,]>
            {
                Value = new bool[gameMap.MapSize.X, gameMap.MapSize.Y]
            };
        }

        public void ResetFog()
        {
            var fogArray = FogOfWar.Value;

            for (int x = 0; x < gameMap.MapSize.X; x++)
            {
                for (int y = 0; y < gameMap.MapSize.Y; y++)
                    fogArray[x, y] = true;
            }

            FogOfWar.TriggerChange();
        }

        public void GenerateFogForPlayer(int player)
        {
            var fogArray = FogOfWar.Value;
            Array.Clear(fogArray, 0, fogArray.Length);

            //All the buildings the player owns shows its own tile.
            foreach (var drawableBuilding in gameMap.GetDrawableBuildingsForPlayer(player))
                fogArray[drawableBuilding.TilePosition.X, drawableBuilding.TilePosition.Y] = true;

            foreach (var drawableUnit in gameMap.GetDrawableUnitsFromPlayer(player))
            {
                var visionRange = drawableUnit.UnitData.Vision;

                for (int x = -visionRange; x <= visionRange; x++)
                {
                    for (int y = -visionRange; y <= visionRange; y++)
                    {
                        var tilePosition = drawableUnit.MapPosition + new Vector2I(x, y);
                        if (tilePosition.X < 0 || tilePosition.X >= gameMap.MapSize.X || tilePosition.Y < 0 || tilePosition.Y >= gameMap.MapSize.Y)
                            continue;

                        var distance = Math.Abs(x) + Math.Abs(y);
                        if (distance > visionRange)
                            continue;

                        if (gameMap.TryGetDrawableBuilding(tilePosition, out DrawableBuilding building))
                        {
                            if (building.BuildingTile.LimitFogOfWarSightDistance <= 0 || distance <= building.BuildingTile.LimitFogOfWarSightDistance)
                                fogArray[tilePosition.X, tilePosition.Y] = true;
                        }
                        else
                        {
                            var tile = gameMap.GetDrawableTile(tilePosition);
                            if (tile.TerrainTile.LimitFogOfWarSightDistance <= 0 || distance <= tile.TerrainTile.LimitFogOfWarSightDistance)
                                fogArray[tilePosition.X, tilePosition.Y] = true;
                        }
                    }
                }
            }

            FogOfWar.TriggerChange();
        }
    }
}
