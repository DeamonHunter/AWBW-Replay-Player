using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Units;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Game.Logic
{
    public class FogOfWarGenerator
    {
        public Bindable<FogOfWarState[,]> FogOfWar;

        private GameMap gameMap;

        public FogOfWarGenerator(GameMap map)
        {
            gameMap = map;
            FogOfWar = new Bindable<FogOfWarState[,]>
            {
                Value = new FogOfWarState[gameMap.MapSize.X, gameMap.MapSize.Y]
            };
        }

        public void ClearFog(bool makeFoggy, bool triggerChange)
        {
            var fogArray = FogOfWar.Value;

            var fogValue = makeFoggy ? FogOfWarState.Hidden : FogOfWarState.AllVisible;

            for (int x = 0; x < gameMap.MapSize.X; x++)
            {
                for (int y = 0; y < gameMap.MapSize.Y; y++)
                    fogArray[x, y] = fogValue;
            }

            if (triggerChange)
                FogOfWar.TriggerChange();
        }

        public void GenerateFogForPlayer(long player, int rangeIncrease, bool canSeeIntoHiddenTiles, bool resetFog = true) => generateFog(gameMap.GetDrawableBuildingsForPlayer(player), gameMap.GetDrawableUnitsFromPlayer(player), rangeIncrease, canSeeIntoHiddenTiles, resetFog);

        private void generateFog(IEnumerable<DrawableBuilding> buildings, IEnumerable<DrawableUnit> units, int rangeIncrease, bool canSeeIntoHiddenTiles, bool resetFog = true)
        {
            var fogArray = FogOfWar.Value;

            if (resetFog)
                Array.Clear(fogArray, 0, fogArray.Length);

            //All the buildings the player owns shows its own tile.
            foreach (var drawableBuilding in buildings)
                fogArray[drawableBuilding.MapPosition.X, drawableBuilding.MapPosition.Y] = FogOfWarState.AllVisible;

            foreach (var drawableUnit in units)
            {
                if (drawableUnit.BeingCarried.Value)
                    continue;

                var visionRange = Math.Max(1, drawableUnit.UnitData.Vision + rangeIncrease);

                //Air Units don't get to get their sight increased
                if (drawableUnit.UnitData.MovementType != MovementType.Air)
                {
                    if (gameMap.TryGetDrawableBuilding(drawableUnit.MapPosition, out DrawableBuilding unitBuilding))
                        visionRange = Math.Max(1, visionRange + unitBuilding.BuildingTile.SightDistanceIncrease);
                    else
                    {
                        var tile = gameMap.GetDrawableTile(drawableUnit.MapPosition);
                        visionRange = Math.Max(1, visionRange + tile.TerrainTile.SightDistanceIncrease);
                    }
                }

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

                        if (!canSeeIntoHiddenTiles)
                        {
                            if (gameMap.TryGetDrawableBuilding(tilePosition, out DrawableBuilding building))
                            {
                                if (building.BuildingTile.LimitFogOfWarSightDistance > 0 && distance > building.BuildingTile.LimitFogOfWarSightDistance)
                                {
                                    if (fogArray[tilePosition.X, tilePosition.Y] == FogOfWarState.Hidden)
                                        fogArray[tilePosition.X, tilePosition.Y] = FogOfWarState.AirUnitsVisible;
                                    continue;
                                }
                            }
                            else
                            {
                                var tile = gameMap.GetDrawableTile(tilePosition);

                                if (tile.TerrainTile.LimitFogOfWarSightDistance > 0 && distance > tile.TerrainTile.LimitFogOfWarSightDistance)
                                {
                                    if (fogArray[tilePosition.X, tilePosition.Y] == FogOfWarState.Hidden)
                                        fogArray[tilePosition.X, tilePosition.Y] = FogOfWarState.AirUnitsVisible;
                                    continue;
                                }
                            }
                        }

                        fogArray[tilePosition.X, tilePosition.Y] = FogOfWarState.AllVisible;
                    }
                }
            }

            FogOfWar.TriggerChange();
        }
    }
}
