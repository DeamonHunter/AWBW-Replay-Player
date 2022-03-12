using System.Collections.Generic;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Tile;

namespace AWBWApp.Game.Game.Logic
{
    public class CustomShoalGenerator
    {
        private readonly TerrainTileStorage tileStorage;
        private readonly BuildingStorage buildingStorage;

        private readonly List<(NearbyTiles, string)> shoalOutComes = new List<(NearbyTiles, string)>
        {
            //Full Circle
            (new NearbyTiles { North = TerrainType.Land, East = TerrainType.Land, South = TerrainType.Land, West = TerrainType.Land }, "Shoal-N-E-S-W"),

            //Empty Circle
            (new NearbyTiles { North = TerrainType.Sea, East = TerrainType.Sea, South = TerrainType.Sea, West = TerrainType.Sea }, "Shoal-C-AN-AE-AS-AW"),

            (new NearbyTiles { North = TerrainType.Shoal, East = TerrainType.Sea, South = TerrainType.Sea, West = TerrainType.Sea }, "Shoal-C-AE-AS-AW"),
            (new NearbyTiles { North = TerrainType.Sea, East = TerrainType.Shoal, South = TerrainType.Sea, West = TerrainType.Sea }, "Shoal-C-AN-AS-AW"),
            (new NearbyTiles { North = TerrainType.Sea, East = TerrainType.Sea, South = TerrainType.Shoal, West = TerrainType.Sea }, "Shoal-C-AN-AE-AW"),
            (new NearbyTiles { North = TerrainType.Sea, East = TerrainType.Sea, South = TerrainType.Sea, West = TerrainType.Shoal }, "Shoal-C-AN-AE-AS"),

            (new NearbyTiles { North = TerrainType.Shoal, East = TerrainType.Shoal, South = TerrainType.Sea, West = TerrainType.Sea }, "Shoal-C-AS-AW"),
            (new NearbyTiles { North = TerrainType.Shoal, East = TerrainType.Sea, South = TerrainType.Shoal, West = TerrainType.Sea }, "Shoal-C-AE-AW"),
            (new NearbyTiles { North = TerrainType.Shoal, East = TerrainType.Sea, South = TerrainType.Sea, West = TerrainType.Shoal }, "Shoal-C-AE-AS"),
            (new NearbyTiles { North = TerrainType.Sea, East = TerrainType.Shoal, South = TerrainType.Shoal, West = TerrainType.Sea }, "Shoal-C-AN-AW"),
            (new NearbyTiles { North = TerrainType.Sea, East = TerrainType.Shoal, South = TerrainType.Sea, West = TerrainType.Shoal }, "Shoal-C-AN-AS"),
            (new NearbyTiles { North = TerrainType.Sea, East = TerrainType.Sea, South = TerrainType.Shoal, West = TerrainType.Shoal }, "Shoal-C-AN-AE"),

            (new NearbyTiles { North = TerrainType.Shoal, East = TerrainType.Shoal, South = TerrainType.Shoal, West = TerrainType.Sea }, "Shoal-C-AW"),
            (new NearbyTiles { North = TerrainType.Shoal, East = TerrainType.Shoal, South = TerrainType.Sea, West = TerrainType.Shoal }, "Shoal-C-AS"),
            (new NearbyTiles { North = TerrainType.Shoal, East = TerrainType.Sea, South = TerrainType.Shoal, West = TerrainType.Shoal }, "Shoal-C-AE"),
            (new NearbyTiles { North = TerrainType.Sea, East = TerrainType.Shoal, South = TerrainType.Shoal, West = TerrainType.Shoal }, "Shoal-C-AN"),

            (new NearbyTiles { North = TerrainType.Shoal, East = TerrainType.Shoal, South = TerrainType.Shoal, West = TerrainType.Shoal }, "Shoal-C"),

            //Missing an edge
            (new NearbyTiles { East = TerrainType.Land, South = TerrainType.Land, West = TerrainType.Land }, "Shoal-E-S-W"),
            (new NearbyTiles { North = TerrainType.Land, South = TerrainType.Land, West = TerrainType.Land }, "Shoal-N-S-W"),
            (new NearbyTiles { North = TerrainType.Land, East = TerrainType.Land, West = TerrainType.Land }, "Shoal-N-E-W"),
            (new NearbyTiles { North = TerrainType.Land, East = TerrainType.Land, South = TerrainType.Land }, "Shoal-N-E-S"),

            //Tunnels
            (new NearbyTiles { North = TerrainType.Land, South = TerrainType.Land }, "Shoal-N-S"),
            (new NearbyTiles { East = TerrainType.Land, West = TerrainType.Land }, "Shoal-E-W"),

            //Corners
            (new NearbyTiles { North = TerrainType.Land, East = TerrainType.Land }, "Shoal-N-E"),
            (new NearbyTiles { East = TerrainType.Land, South = TerrainType.Land }, "Shoal-E-S"),
            (new NearbyTiles { South = TerrainType.Land, West = TerrainType.Land }, "Shoal-S-W"),
            (new NearbyTiles { North = TerrainType.Land, West = TerrainType.Land }, "Shoal-N-W"),

            //Single Edge Tiles
            (new NearbyTiles { North = TerrainType.Land, East = TerrainType.Sea, South = TerrainType.Sea, West = TerrainType.Sea }, "Shoal-N-AW-AS-AE"),
            (new NearbyTiles { North = TerrainType.Land, West = TerrainType.Sea, East = TerrainType.Sea }, "Shoal-N-AW-AE"),
            (new NearbyTiles { North = TerrainType.Land, West = TerrainType.Sea, South = TerrainType.Sea }, "Shoal-N-AW-AS"),
            (new NearbyTiles { North = TerrainType.Land, West = TerrainType.Sea }, "Shoal-N-AW"),
            (new NearbyTiles { North = TerrainType.Land, East = TerrainType.Sea, South = TerrainType.Sea }, "Shoal-N-AS-AE"),
            (new NearbyTiles { North = TerrainType.Land, East = TerrainType.Sea }, "Shoal-N-AE"),
            (new NearbyTiles { North = TerrainType.Land, South = TerrainType.Sea }, "Shoal-N-AS"),
            (new NearbyTiles { North = TerrainType.Land }, "Shoal-N"),

            (new NearbyTiles { East = TerrainType.Land, North = TerrainType.Sea, South = TerrainType.Sea, West = TerrainType.Sea }, "Shoal-E-AN-AW-AS"),
            (new NearbyTiles { East = TerrainType.Land, North = TerrainType.Sea, South = TerrainType.Sea }, "Shoal-E-AN-AS"),
            (new NearbyTiles { East = TerrainType.Land, North = TerrainType.Sea, West = TerrainType.Sea }, "Shoal-E-AN-AW"),
            (new NearbyTiles { East = TerrainType.Land, North = TerrainType.Sea }, "Shoal-E-AN"),
            (new NearbyTiles { East = TerrainType.Land, South = TerrainType.Sea, West = TerrainType.Sea }, "Shoal-E-AW-AS"),
            (new NearbyTiles { East = TerrainType.Land, South = TerrainType.Sea }, "Shoal-E-AS"),
            (new NearbyTiles { East = TerrainType.Land, West = TerrainType.Sea }, "Shoal-E-AW"),
            (new NearbyTiles { East = TerrainType.Land }, "Shoal-E"),

            (new NearbyTiles { East = TerrainType.Sea, West = TerrainType.Sea, South = TerrainType.Land, North = TerrainType.Sea }, "Shoal-S-AE-AN-AW"),
            (new NearbyTiles { East = TerrainType.Sea, West = TerrainType.Sea, South = TerrainType.Land }, "Shoal-S-AE-AW"),
            (new NearbyTiles { East = TerrainType.Sea, South = TerrainType.Land, North = TerrainType.Sea }, "Shoal-S-AE-AN"),
            (new NearbyTiles { East = TerrainType.Sea, South = TerrainType.Land }, "Shoal-S-AE"),
            (new NearbyTiles { West = TerrainType.Sea, South = TerrainType.Land, North = TerrainType.Sea }, "Shoal-S-AN-AW"),
            (new NearbyTiles { West = TerrainType.Sea, South = TerrainType.Land }, "Shoal-S-AW"),
            (new NearbyTiles { South = TerrainType.Land, North = TerrainType.Sea }, "Shoal-S-AN"),
            (new NearbyTiles { South = TerrainType.Land }, "Shoal-S"),

            (new NearbyTiles { South = TerrainType.Sea, North = TerrainType.Sea, West = TerrainType.Land, East = TerrainType.Sea }, "Shoal-W-AS-AE-AN"),
            (new NearbyTiles { South = TerrainType.Sea, North = TerrainType.Sea, West = TerrainType.Land }, "Shoal-W-AS-AN"),
            (new NearbyTiles { South = TerrainType.Sea, West = TerrainType.Land, East = TerrainType.Sea }, "Shoal-W-AS-AE"),
            (new NearbyTiles { South = TerrainType.Sea, West = TerrainType.Land }, "Shoal-W-AS"),
            (new NearbyTiles { North = TerrainType.Sea, West = TerrainType.Land, East = TerrainType.Sea }, "Shoal-W-AE-AN"),
            (new NearbyTiles { North = TerrainType.Sea, West = TerrainType.Land }, "Shoal-W-AN"),
            (new NearbyTiles { West = TerrainType.Land, East = TerrainType.Sea }, "Shoal-W-AE"),
            (new NearbyTiles { West = TerrainType.Land }, "Shoal-W"),
        };

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

                        var id = -1;

                        foreach (var outCome in shoalOutComes)
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

                    default:
                        customShoal.Ids[i] = originalId;
                        break;
                }
            }

            return customShoal;
        }

        private NearbyTiles getNearbyTiles(ReplayMap map, int tileIndex, int x, int y)
        {
            var centerType = getTerrainTypeFromId(map.Ids[tileIndex]);

            var nearby = new NearbyTiles
            {
                NorthWest = x > 0 && y > 0 ? getTerrainTypeFromId(map.Ids[tileIndex - map.Size.X - 1]) : centerType,
                North = y > 0 ? getTerrainTypeFromId(map.Ids[tileIndex - map.Size.X]) : centerType,
                NorthEast = x < map.Size.X - 1 && y > 0 ? getTerrainTypeFromId(map.Ids[tileIndex - map.Size.X + 1]) : centerType,

                West = x > 0 ? getTerrainTypeFromId(map.Ids[tileIndex - 1]) : centerType,
                East = x < map.Size.X - 1 ? getTerrainTypeFromId(map.Ids[tileIndex + 1]) : centerType,

                SouthWest = x > 0 && y < map.Size.Y - 1 ? getTerrainTypeFromId(map.Ids[tileIndex + map.Size.X - 1]) : centerType,
                South = y < map.Size.Y - 1 ? getTerrainTypeFromId(map.Ids[tileIndex + map.Size.X]) : centerType,
                SouthEast = x < map.Size.X - 1 && y < map.Size.Y - 1 ? getTerrainTypeFromId(map.Ids[tileIndex + map.Size.X + 1]) : centerType
            };

            return nearby;
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
