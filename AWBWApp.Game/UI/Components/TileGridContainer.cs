using System;
using System.Collections;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osuTK;

namespace AWBWApp.Game.UI.Components
{
    /// <summary>
    /// A container which sets each tile to a specific depth so that ordering is always the same.
    /// </summary>
    public partial class TileGridContainer<T> : CompositeDrawable where T : Drawable
    {
        private T[,] tiles;

        public T this[int x, int y] => tiles[x, y];

        public Vector2I GridSize => new Vector2I(tiles.GetLength(0), tiles.GetLength(1));

        private Vector2 tileSize;

        public TileGridContainer(Vector2 tileSize)
        {
            this.tileSize = tileSize;
            tiles = new T[0, 0];
        }

        public void ClearToSize(Vector2I gridSize)
        {
            ClearInternal();
            tiles = new T[gridSize.X, gridSize.Y];
        }

        public void SetToSize(Vector2I gridSize)
        {
            var oldTiles = tiles;

            tiles = new T[gridSize.X, gridSize.Y];

            if (oldTiles == null)
                return;

            for (int x = 0; x < oldTiles.GetLength(0); X++)
            {
                for (int y = 0; y < oldTiles.GetLength(1); X++)
                    AddTile(oldTiles[x, y], new Vector2I(x, y));
            }
        }

        public void AddTile(T drawable, Vector2I gridPosition)
        {
            if (gridPosition.X < 0 || gridPosition.Y < 0 || gridPosition.X >= GridSize.X || gridPosition.Y >= GridSize.Y)
                throw new ArgumentException("Grid position is outside of the Grid", nameof(gridPosition));

            var original = tiles[gridPosition.X, gridPosition.Y];
            if (original == drawable)
                return; //Should this still set position?

            if (original != null)
                RemoveTile(gridPosition);

            drawable.Position = new Vector2(gridPosition.X * tileSize.X, gridPosition.Y * tileSize.Y);

            tiles[gridPosition.X, gridPosition.Y] = drawable;
            AddInternal(drawable);
            ChangeInternalChildDepth(drawable, -1 * (gridPosition.Y * GridSize.X + gridPosition.X));
        }

        public bool RemoveTile(Vector2I gridPosition)
        {
            var tile = tiles[gridPosition.X, gridPosition.Y];
            if (tile == null)
                return false;

            RemoveInternal(tile, false);
            tile.Expire();
            tiles[gridPosition.X, gridPosition.Y] = null;

            return true;
        }

        public bool TryGet(Vector2I gridPosition, out T tile)
        {
            if (gridPosition.X < 0 || gridPosition.Y < 0 || gridPosition.X >= GridSize.X || gridPosition.Y >= GridSize.Y)
            {
                tile = null;
                return false;
            }

            tile = tiles[gridPosition.X, gridPosition.Y];
            return tile != null;
        }

        public Enumerator GetEnumerator() => new Enumerator(tiles);

        public struct Enumerator : IEnumerator<T>
        {
            private T[,] container;
            private Vector2I currentIndex;

            internal Enumerator(T[,] container)
            {
                this.container = container;
                currentIndex = new Vector2I(-1, 0);
            }

            public bool MoveNext()
            {
                while (true)
                {
                    currentIndex.X++;

                    if (currentIndex.X >= container.GetLength(0))
                    {
                        currentIndex.X = 0;
                        currentIndex.Y++;
                        if (currentIndex.Y >= container.GetLength(1))
                            return false;
                    }

                    if (container[currentIndex.X, currentIndex.Y] != null)
                        return true;
                }
            }

            public void Reset() => currentIndex = new Vector2I(-1, 0);

            public readonly T Current => container[currentIndex.X, currentIndex.Y];

            readonly object IEnumerator.Current => Current;

            public void Dispose()
            {
                container = null;
            }
        }
    }
}
