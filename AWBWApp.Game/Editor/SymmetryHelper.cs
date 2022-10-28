﻿using System;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Editor
{
    public enum SymmetryDirection
    {
        Vertical,
        UpwardsDiagonal,
        Horizontal,
        DownwardsDiagonal
    }

    public enum SymmetryMode
    {
        None,
        Mirror,
        MirrorInverted
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

                        case SymmetryMode.MirrorInverted:
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

                        case SymmetryMode.MirrorInverted:
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

                        case SymmetryMode.MirrorInverted:
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

                        case SymmetryMode.MirrorInverted:
                            return new Vector2I(symmetryCenter.Y - diff.X, symmetryCenter.X - diff.Y);
                    }

                    throw new ArgumentException($"Unknown mode {mode}", nameof(mode));
                }
            }

            throw new ArgumentException($"Unknown direction {direction}", nameof(direction));
        }
    }
}
