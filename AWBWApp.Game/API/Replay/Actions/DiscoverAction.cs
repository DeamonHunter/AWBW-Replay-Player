using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Building;
using Newtonsoft.Json.Linq;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.API.Replay.Actions
{
    public class DiscoveryCollection
    {
        public Dictionary<string, Discovery> DiscoveryByID = new Dictionary<string, Discovery>();

        public Dictionary<Vector2I, Dictionary<string, BuildingTile>> OriginalDiscovery = new Dictionary<Vector2I, Dictionary<string, BuildingTile>>();

        public DiscoveryCollection(JToken discoveryCollection)
        {
            if (discoveryCollection.Type == JTokenType.Null)
                return;

            if (discoveryCollection.Type == JTokenType.Array)
            {
                if (((JArray)discoveryCollection).Count <= 0)
                    return;

                throw new Exception("Non-Empty Array for Discovery.");
            }

            foreach (var discoveryEntry in (JObject)discoveryCollection)
            {
                if (discoveryEntry.Value == null || discoveryEntry.Value.Type == JTokenType.Null)
                    continue;

                var discovery = new Discovery();

                var discoveryObj = (JObject)discoveryEntry.Value;

                if (discoveryObj.TryGetValue("buildings", out var buildingToken))
                {
                    foreach (var buildingEntry in (JArray)buildingToken)
                    {
                        var building = ReplayActionHelper.ParseJObjectIntoReplayBuilding((JObject)buildingEntry);
                        discovery.DiscoveredBuildings.Add(building.Position, building);
                    }

                    if (!discovery.IsEmpty())
                        DiscoveryByID.Add(discoveryEntry.Key, discovery);
                }
            }
        }

        public bool IsEmpty() => DiscoveryByID.Count <= 0;
    }

    public class Discovery
    {
        public Dictionary<Vector2I, ReplayBuilding> DiscoveredBuildings = new Dictionary<Vector2I, ReplayBuilding>();

        public bool IsEmpty() => DiscoveredBuildings.Count <= 0;
    }
}
