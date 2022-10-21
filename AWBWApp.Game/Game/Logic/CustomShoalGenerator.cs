using System.Collections.Generic;
using System.Text;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.UI.Editor;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Game.Logic
{
    public class CustomShoalGenerator
    {
        private readonly TerrainTileStorage tileStorage;
        private readonly BuildingStorage buildingStorage;

        private readonly List<(NearbyTiles, string)> seaOutComes = new List<(NearbyTiles, string)>
        {
            //Full Circle
            (new NearbyTiles { North = TerrainType.Land, East = TerrainType.Land, South = TerrainType.Land, West = TerrainType.Land }, "Sea-N-E-S-W"),

            //Missing an edge
            (new NearbyTiles { East = TerrainType.Land, South = TerrainType.Land, West = TerrainType.Land }, "Sea-E-S-W"),
            (new NearbyTiles { North = TerrainType.Land, South = TerrainType.Land, West = TerrainType.Land }, "Sea-N-S-W"),
            (new NearbyTiles { North = TerrainType.Land, East = TerrainType.Land, West = TerrainType.Land }, "Sea-N-E-W"),
            (new NearbyTiles { North = TerrainType.Land, East = TerrainType.Land, South = TerrainType.Land }, "Sea-N-E-S"),

            //Tunnels
            (new NearbyTiles { North = TerrainType.Land, South = TerrainType.Land }, "Sea-N-S"),
            (new NearbyTiles { East = TerrainType.Land, West = TerrainType.Land }, "Sea-E-W"),

            //Corners
            (new NearbyTiles { North = TerrainType.Land, East = TerrainType.Land, SouthWest = TerrainType.Land }, "Sea-N-E-SW"),
            (new NearbyTiles { North = TerrainType.Land, East = TerrainType.Land }, "Sea-N-E"),

            (new NearbyTiles { East = TerrainType.Land, South = TerrainType.Land, NorthWest = TerrainType.Land }, "Sea-E-S-NW"),
            (new NearbyTiles { East = TerrainType.Land, South = TerrainType.Land }, "Sea-E-S"),

            (new NearbyTiles { South = TerrainType.Land, West = TerrainType.Land, NorthEast = TerrainType.Land }, "Sea-S-W-NE"),
            (new NearbyTiles { South = TerrainType.Land, West = TerrainType.Land }, "Sea-S-W"),

            (new NearbyTiles { North = TerrainType.Land, West = TerrainType.Land, SouthEast = TerrainType.Land }, "Sea-N-W-SE"),
            (new NearbyTiles { North = TerrainType.Land, West = TerrainType.Land }, "Sea-N-W"),

            //Edge
            (new NearbyTiles { North = TerrainType.Land, SouthEast = TerrainType.Land, SouthWest = TerrainType.Land }, "Sea-N-SE-SW"),
            (new NearbyTiles { North = TerrainType.Land, SouthEast = TerrainType.Land }, "Sea-N-SE"),
            (new NearbyTiles { North = TerrainType.Land, SouthWest = TerrainType.Land }, "Sea-N-SW"),
            (new NearbyTiles { North = TerrainType.Land }, "Sea-N"),

            (new NearbyTiles { East = TerrainType.Land, NorthWest = TerrainType.Land, SouthWest = TerrainType.Land }, "Sea-E-NW-SW"),
            (new NearbyTiles { East = TerrainType.Land, NorthWest = TerrainType.Land }, "Sea-E-NW"),
            (new NearbyTiles { East = TerrainType.Land, SouthWest = TerrainType.Land }, "Sea-E-SW"),
            (new NearbyTiles { East = TerrainType.Land }, "Sea-E"),

            (new NearbyTiles { South = TerrainType.Land, NorthWest = TerrainType.Land, NorthEast = TerrainType.Land }, "Sea-S-NW-NE"),
            (new NearbyTiles { South = TerrainType.Land, NorthWest = TerrainType.Land }, "Sea-S-NW"),
            (new NearbyTiles { South = TerrainType.Land, NorthEast = TerrainType.Land }, "Sea-S-NE"),
            (new NearbyTiles { South = TerrainType.Land }, "Sea-S"),

            (new NearbyTiles { West = TerrainType.Land, NorthEast = TerrainType.Land, SouthEast = TerrainType.Land }, "Sea-W-NE-SE"),
            (new NearbyTiles { West = TerrainType.Land, NorthEast = TerrainType.Land }, "Sea-W-NE"),
            (new NearbyTiles { West = TerrainType.Land, SouthEast = TerrainType.Land }, "Sea-W-SE"),
            (new NearbyTiles { West = TerrainType.Land }, "Sea-W"),

            //Full Corners
            (new NearbyTiles { NorthWest = TerrainType.Land, NorthEast = TerrainType.Land, SouthEast = TerrainType.Land, SouthWest = TerrainType.Land }, "Sea-NW-NE-SE-SW"),

            //Missing 1 corner
            (new NearbyTiles { NorthEast = TerrainType.Land, SouthEast = TerrainType.Land, SouthWest = TerrainType.Land }, "Sea-NE-SE-SW"),
            (new NearbyTiles { NorthWest = TerrainType.Land, SouthEast = TerrainType.Land, SouthWest = TerrainType.Land }, "Sea-NW-SE-SW"),
            (new NearbyTiles { NorthWest = TerrainType.Land, NorthEast = TerrainType.Land, SouthWest = TerrainType.Land }, "Sea-NW-NE-SW"),
            (new NearbyTiles { NorthWest = TerrainType.Land, NorthEast = TerrainType.Land, SouthEast = TerrainType.Land }, "Sea-NW-NE-SE"),

            //Missing 2 corners

            (new NearbyTiles { SouthEast = TerrainType.Land, SouthWest = TerrainType.Land }, "Sea-SE-SW"),
            (new NearbyTiles { NorthWest = TerrainType.Land, SouthWest = TerrainType.Land }, "Sea-NW-SW"),
            (new NearbyTiles { NorthWest = TerrainType.Land, NorthEast = TerrainType.Land, }, "Sea-NW-NE"),
            (new NearbyTiles { NorthEast = TerrainType.Land, SouthEast = TerrainType.Land }, "Sea-NE-SE"),
            (new NearbyTiles { NorthWest = TerrainType.Land, SouthEast = TerrainType.Land, SouthWest = TerrainType.Land }, "Sea-NW-SE"),
            (new NearbyTiles { NorthEast = TerrainType.Land, SouthWest = TerrainType.Land }, "Sea-NE-SW"),

            //Missing 3 corners
            (new NearbyTiles { NorthWest = TerrainType.Land }, "Sea-NW"),
            (new NearbyTiles { NorthEast = TerrainType.Land }, "Sea-NE"),
            (new NearbyTiles { SouthEast = TerrainType.Land }, "Sea-SE"),
            (new NearbyTiles { SouthWest = TerrainType.Land }, "Sea-SW"),
        };

        public CustomShoalGenerator(TerrainTileStorage tileStorage, BuildingStorage buildingStorage)
        {
            this.tileStorage = tileStorage;
            this.buildingStorage = buildingStorage;
        }

        public ReplayMap CreateCustomShoalVersion(ReplayMap map)
        {
            var customShoal = new ReplayMap
            {
                TerrainName = map.TerrainName,
                Size = map.Size,
                Ids = new short[map.Ids.Length]
            };

            for (int i = 0; i < map.Ids.Length; i++)
            {
                var originalId = map.Ids[i];
                var x = i % map.Size.X;
                var y = i / map.Size.X;

                var nearby = getNearbyTiles(map, i, x, y);
                customShoal.Ids[i] = getNewIDForTile(nearby, originalId);
            }

            return customShoal;
        }

        private short getNewIDForTile(NearbyTiles nearbyTiles, short originalId)
        {
            switch (originalId)
            {
                case 28:
                {
                    short id = -1;

                    foreach (var outCome in seaOutComes)
                    {
                        if (outCome.Item1.NorthWest != TerrainType.None && nearbyTiles.NorthWest != TerrainType.None && (outCome.Item1.NorthWest & nearbyTiles.NorthWest) == 0)
                            continue;
                        if (outCome.Item1.North != TerrainType.None && nearbyTiles.North != TerrainType.None && (outCome.Item1.North & nearbyTiles.North) == 0)
                            continue;
                        if (outCome.Item1.NorthEast != TerrainType.None && nearbyTiles.NorthEast != TerrainType.None && (outCome.Item1.NorthEast & nearbyTiles.NorthEast) == 0)
                            continue;
                        if (outCome.Item1.East != TerrainType.None && nearbyTiles.East != TerrainType.None && (outCome.Item1.East & nearbyTiles.East) == 0)
                            continue;
                        if (outCome.Item1.SouthEast != TerrainType.None && nearbyTiles.SouthEast != TerrainType.None && (outCome.Item1.SouthEast & nearbyTiles.SouthEast) == 0)
                            continue;
                        if (outCome.Item1.South != TerrainType.None && nearbyTiles.South != TerrainType.None && (outCome.Item1.South & nearbyTiles.South) == 0)
                            continue;
                        if (outCome.Item1.SouthWest != TerrainType.None && nearbyTiles.SouthWest != TerrainType.None && (outCome.Item1.SouthWest & nearbyTiles.SouthWest) == 0)
                            continue;
                        if (outCome.Item1.West != TerrainType.None && nearbyTiles.West != TerrainType.None && (outCome.Item1.West & nearbyTiles.West) == 0)
                            continue;

                        id = (short)tileStorage.GetTileByCode(outCome.Item2).AWBWID;
                        break;
                    }

                    return id != -1 ? id : originalId;
                }

                case 29:
                case 30:
                case 31:
                case 32:
                {
                    var id = tileStorage.GetTileByCode(constructCustomShoal(nearbyTiles)).AWBWID;

                    return id != -1 ? (short)id : originalId;
                }

                default:
                    return originalId;
            }
        }

        public void UpdateEditorMapAtPosition(EditorGameMap map, Vector2I position)
        {
            for (int x = position.X - 1; x <= position.X + 1; x++)
            {
                if (x < 0 || x >= map.MapSize.X)
                    continue;

                for (int y = position.Y - 1; y <= position.Y + 1; y++)
                {
                    if (y < 0 || y >= map.MapSize.Y)
                        continue;

                    var tileId = map.GetTileIDAtPosition(new Vector2I(x, y));
                    var nearby = getNearbyTiles(map, x, y);
                    var newId = getNewIDForTile(nearby, tileId);
                    map.ChangeTile(new Vector2I(x, y), tileId, newId);
                }
            }
        }

        private NearbyTiles getNearbyTiles(ReplayMap map, int tileIndex, int x, int y)
        {
            var centerTile = map.Ids[tileIndex];

            var northWestTile = x > 0 && y > 0 ? map.Ids[tileIndex - map.Size.X - 1] : centerTile;
            var northTile = y > 0 ? map.Ids[tileIndex - map.Size.X] : centerTile;
            var northEastTile = x < map.Size.X - 1 && y > 0 ? map.Ids[tileIndex - map.Size.X + 1] : centerTile;

            var westTile = x > 0 ? map.Ids[tileIndex - 1] : centerTile;
            var eastTile = x < map.Size.X - 1 ? map.Ids[tileIndex + 1] : centerTile;

            var southWestTile = x > 0 && y < map.Size.Y - 1 ? map.Ids[tileIndex + map.Size.X - 1] : centerTile;
            var southTile = y < map.Size.Y - 1 ? map.Ids[tileIndex + map.Size.X] : centerTile;
            var southEastTile = x < map.Size.X - 1 && y < map.Size.Y - 1 ? map.Ids[tileIndex + map.Size.X + 1] : centerTile;

            //Todo: May need more special handlings
            //26 is Hbridge
            //27 is VBridge
            //4 is HRiver
            //5 is VRiver

            if (northWestTile == 26)
                northWestTile = 5;
            if (northTile == 26)
                northTile = 5;
            if (northEastTile == 26)
                northEastTile = 5;

            if (southWestTile == 26)
                southWestTile = 5;
            if (southTile == 26)
                southTile = 5;
            if (southEastTile == 26)
                southEastTile = 5;

            if (northEastTile == 27)
                northEastTile = 4;
            if (eastTile == 27)
                eastTile = 4;
            if (southEastTile == 27)
                southEastTile = 4;

            if (northWestTile == 27)
                northWestTile = 4;
            if (westTile == 27)
                westTile = 4;
            if (southWestTile == 27)
                southWestTile = 4;

            return new NearbyTiles
            {
                NorthWest = getTerrainTypeFromId(northWestTile),
                North = getTerrainTypeFromId(northTile),
                NorthEast = getTerrainTypeFromId(northEastTile),

                West = getTerrainTypeFromId(westTile),
                East = getTerrainTypeFromId(eastTile),

                SouthWest = getTerrainTypeFromId(southWestTile),
                South = getTerrainTypeFromId(southTile),
                SouthEast = getTerrainTypeFromId(southEastTile)
            };
        }

        private NearbyTiles getNearbyTiles(EditorGameMap map, int x, int y)
        {
            var centerTile = map.GetTileIDAtPosition(new Vector2I(x, y));

            var northWestTile = x > 0 && y > 0 ? map.GetTileIDAtPosition(new Vector2I(x - 1, y - 1)) : centerTile;
            var northTile = y > 0 ? map.GetTileIDAtPosition(new Vector2I(x, y - 1)) : centerTile;
            var northEastTile = x < map.MapSize.X - 1 && y > 0 ? map.GetTileIDAtPosition(new Vector2I(x + 1, y - 1)) : centerTile;

            var westTile = x > 0 ? map.GetTileIDAtPosition(new Vector2I(x - 1, y)) : centerTile;
            var eastTile = x < map.MapSize.X - 1 ? map.GetTileIDAtPosition(new Vector2I(x + 1, y)) : centerTile;

            var southWestTile = x > 0 && y < map.MapSize.Y - 1 ? map.GetTileIDAtPosition(new Vector2I(x - 1, y + 1)) : centerTile;
            var southTile = y < map.MapSize.Y - 1 ? map.GetTileIDAtPosition(new Vector2I(x, y + 1)) : centerTile;
            var southEastTile = x < map.MapSize.X - 1 && y < map.MapSize.Y - 1 ? map.GetTileIDAtPosition(new Vector2I(x + 1, y + 1)) : centerTile;

            //Todo: May need more special handlings
            //26 is Hbridge
            //27 is VBridge
            //4 is HRiver
            //5 is VRiver

            if (northWestTile == 26)
                northWestTile = 5;
            if (northTile == 26)
                northTile = 5;
            if (northEastTile == 26)
                northEastTile = 5;

            if (southWestTile == 26)
                southWestTile = 5;
            if (southTile == 26)
                southTile = 5;
            if (southEastTile == 26)
                southEastTile = 5;

            if (northEastTile == 27)
                northEastTile = 4;
            if (eastTile == 27)
                eastTile = 4;
            if (southEastTile == 27)
                southEastTile = 4;

            if (northWestTile == 27)
                northWestTile = 4;
            if (westTile == 27)
                westTile = 4;
            if (southWestTile == 27)
                southWestTile = 4;

            return new NearbyTiles
            {
                NorthWest = getTerrainTypeFromId(northWestTile),
                North = getTerrainTypeFromId(northTile),
                NorthEast = getTerrainTypeFromId(northEastTile),

                West = getTerrainTypeFromId(westTile),
                East = getTerrainTypeFromId(eastTile),

                SouthWest = getTerrainTypeFromId(southWestTile),
                South = getTerrainTypeFromId(southTile),
                SouthEast = getTerrainTypeFromId(southEastTile)
            };
        }

        private string constructCustomShoal(NearbyTiles tiles)
        {
            var sb = new StringBuilder(12);

            if ((tiles.North & TerrainType.Land) != 0)
                sb.Append("N-");
            else if ((tiles.North & TerrainType.CustomShoals) == 0)
                sb.Append("AN-");

            if ((tiles.East & TerrainType.Land) != 0)
                sb.Append("E-");
            else if ((tiles.East & TerrainType.CustomShoals) == 0)
                sb.Append("AE-");

            if ((tiles.South & TerrainType.Land) != 0)
                sb.Append("S-");
            else if ((tiles.South & TerrainType.CustomShoals) == 0)
                sb.Append("AS-");

            if ((tiles.West & TerrainType.Land) != 0)
                sb.Append("W-");
            else if ((tiles.West & TerrainType.CustomShoals) == 0)
                sb.Append("AW-");

            if (sb.Length > 0)
                sb.Remove(sb.Length - 1, 1);

            return sb.Length > 0 ? $"Shoal-{sb}" : "Shoal-C";
        }

        private TerrainType getTerrainTypeFromId(int id)
        {
            if (buildingStorage.ContainsBuildingWithAWBWId(id))
                return TerrainType.Land;

            return tileStorage.GetTileByAWBWId(id).TerrainType;
        }

        private struct NearbyTiles
        {
            public TerrainType NorthWest;
            public TerrainType North;
            public TerrainType NorthEast;
            public TerrainType East;
            public TerrainType SouthEast;
            public TerrainType South;
            public TerrainType SouthWest;
            public TerrainType West;
        }
    }
}
