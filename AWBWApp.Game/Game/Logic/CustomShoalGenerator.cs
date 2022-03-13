using System.Collections.Generic;
using System.Text;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Tile;

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

                switch (originalId)
                {
                    case 28:
                    {
                        var nearby = getNearbyTiles(map, i, x, y);

                        var id = -1;

                        foreach (var outCome in seaOutComes)
                        {
                            if (outCome.Item1.NorthWest != TerrainType.None && nearby.NorthWest != TerrainType.None && (outCome.Item1.NorthWest & nearby.NorthWest) == 0)
                                continue;
                            if (outCome.Item1.North != TerrainType.None && nearby.North != TerrainType.None && (outCome.Item1.North & nearby.North) == 0)
                                continue;
                            if (outCome.Item1.NorthEast != TerrainType.None && nearby.NorthEast != TerrainType.None && (outCome.Item1.NorthEast & nearby.NorthEast) == 0)
                                continue;
                            if (outCome.Item1.East != TerrainType.None && nearby.East != TerrainType.None && (outCome.Item1.East & nearby.East) == 0)
                                continue;
                            if (outCome.Item1.SouthEast != TerrainType.None && nearby.SouthEast != TerrainType.None && (outCome.Item1.SouthEast & nearby.SouthEast) == 0)
                                continue;
                            if (outCome.Item1.South != TerrainType.None && nearby.South != TerrainType.None && (outCome.Item1.South & nearby.South) == 0)
                                continue;
                            if (outCome.Item1.SouthWest != TerrainType.None && nearby.SouthWest != TerrainType.None && (outCome.Item1.SouthWest & nearby.SouthWest) == 0)
                                continue;
                            if (outCome.Item1.West != TerrainType.None && nearby.West != TerrainType.None && (outCome.Item1.West & nearby.West) == 0)
                                continue;

                            id = tileStorage.GetTileByCode(outCome.Item2).AWBWId;
                            break;
                        }

                        if (id != -1)
                            customShoal.Ids[i] = (short)id;
                        else
                            customShoal.Ids[i] = originalId;
                        break;
                    }

                    case 29:
                    case 30:
                    case 31:
                    case 32:
                    {
                        var nearby = getNearbyTiles(map, i, x, y);

                        var id = tileStorage.GetTileByCode(constructCustomShoal(nearby)).AWBWId;

                        if (id != -1)
                            customShoal.Ids[i] = (short)id;
                        else
                            customShoal.Ids[i] = originalId;
                        break;
                    }

                    default:
                        customShoal.Ids[i] = originalId;
                        break;
                }
            }

            return customShoal;
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
