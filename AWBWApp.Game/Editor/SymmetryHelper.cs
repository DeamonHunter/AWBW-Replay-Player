using System;
using System.ComponentModel;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Tile;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Editor
{
    public enum SymmetryDirection
    {
        [Description("Vertical")]
        Vertical,
        [Description("Upwards Diagonal")]
        UpwardsDiagonal,
        [Description("Horizontal")]
        Horizontal,
        [Description("Downwards Diagonal")]
        DownwardsDiagonal
    }

    public enum SymmetryMode
    {
        [Description("No Symmetry")]
        None,
        [Description("Mirrored")]
        Mirror,
        [Description("Rotated")]
        Rotated
    }

    public static class SymmetryHelper
    {
        public static Vector2I GetSymmetricalTile(Vector2I position, Vector2I symmetryCenter, SymmetryDirection direction, SymmetryMode mode)
        {
            if (mode == SymmetryMode.None)
                throw new ArgumentException("SymmetryMode cannot be None.", nameof(mode));

            var diff = position - symmetryCenter;

            switch (direction)
            {
                case SymmetryDirection.Vertical:
                {
                    switch (mode)
                    {
                        case SymmetryMode.Mirror:
                            return new Vector2I(symmetryCenter.X - diff.X, position.Y);

                        case SymmetryMode.Rotated:
                            return new Vector2I(symmetryCenter.X - diff.X, symmetryCenter.Y - diff.Y);
                    }

                    throw new ArgumentException($"Unknown mode {mode}", nameof(mode));
                }

                case SymmetryDirection.UpwardsDiagonal:
                {
                    switch (mode)
                    {
                        case SymmetryMode.Mirror:
                            return new Vector2I(symmetryCenter.X - diff.Y, symmetryCenter.Y - diff.X);

                        case SymmetryMode.Rotated:
                            return new Vector2I(symmetryCenter.X - diff.X, symmetryCenter.Y - diff.Y);
                    }

                    throw new ArgumentException($"Unknown mode {mode}", nameof(mode));
                }

                case SymmetryDirection.Horizontal:
                {
                    switch (mode)
                    {
                        case SymmetryMode.Mirror:
                            return new Vector2I(position.X, symmetryCenter.Y - diff.Y);

                        case SymmetryMode.Rotated:
                            return new Vector2I(symmetryCenter.X - diff.X, symmetryCenter.Y - diff.Y);
                    }

                    throw new ArgumentException($"Unknown mode {mode}", nameof(mode));
                }

                case SymmetryDirection.DownwardsDiagonal:
                {
                    switch (mode)
                    {
                        case SymmetryMode.Mirror:
                            return new Vector2I(symmetryCenter.Y + diff.Y, symmetryCenter.X + diff.X);

                        case SymmetryMode.Rotated:
                            return new Vector2I(symmetryCenter.Y - diff.X, symmetryCenter.X - diff.Y);
                    }

                    throw new ArgumentException($"Unknown mode {mode}", nameof(mode));
                }
            }

            throw new ArgumentException($"Unknown direction {direction}", nameof(direction));
        }

        public static int GetTerrainTileForSymmetry(TerrainTile tile, SymmetryMode mode, SymmetryDirection direction)
        {
            switch (mode)
            {
                case SymmetryMode.Mirror:
                    return tile.SymmetryMirror != null ? tile.SymmetryMirror[(int)direction] : tile.AWBWID;

                case SymmetryMode.Rotated:
                    return tile.SymmetryRotated != null ? tile.SymmetryRotated[(int)direction] : tile.AWBWID;

                default:
                    return tile.AWBWID;
            }
        }

        public static int GetBuildingTileForSymmetry(BuildingTile building, SymmetryMode mode, SymmetryDirection direction)
        {
            switch (mode)
            {
                case SymmetryMode.Mirror:
                    return building.SymmetryMirror != null ? building.SymmetryMirror[(int)direction] : building.AWBWID;

                case SymmetryMode.Rotated:
                    return building.SymmetryRotated != null ? building.SymmetryRotated[(int)direction] : building.AWBWID;

                default:
                    return building.AWBWID;
            }
        }
    }
}
