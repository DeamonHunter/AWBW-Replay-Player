using System.Collections.Generic;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Helpers;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Game.Logic
{
    public class BuildingDiscoveryController
    {
        private List<BuildingDiscovery> discoveries;

        public void Reset()
        {
            discoveries = new List<BuildingDiscovery>();
        }

        public void RegisterNewTurn(ReplaySetupContext context)
        {
            var newDiscovery = new BuildingDiscovery();

            foreach (var building in context.BuildingKnowledge)
                newDiscovery.Buildings.Add(building.Key, new Dictionary<string, BuildingTile>(building.Value));

            discoveries.Add(newDiscovery);
        }

        public void SetDiscoveries(int turnID, GameMap map)
        {
            foreach (var building in discoveries[turnID].Buildings)
            {
                if (!map.TryGetDrawableBuilding(building.Key, out var drawableBuilding))
                    continue;

                drawableBuilding.TeamToTile.SetTo(building.Value);
            }
        }

        private class BuildingDiscovery
        {
            public Dictionary<Vector2I, Dictionary<string, BuildingTile>> Buildings = new Dictionary<Vector2I, Dictionary<string, BuildingTile>>();
        }
    }
}
