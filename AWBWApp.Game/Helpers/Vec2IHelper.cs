using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Primitives;
using osuTK;

namespace AWBWApp.Game.Helpers
{
    public static class Vec2IHelper
    {
        public static int ManhattonDistance(this Vector2I vector)
        {
            return Math.Abs(vector.X) + Math.Abs(vector.Y);
        }

        private static readonly Dictionary<int, List<Vector2I>> distance_cache = new Dictionary<int, List<Vector2I>>();

        /// <summary>
        /// Get the tiles that are x tiles away from a center point. 
        /// </summary>
        /// <param name="center">The center point.</param>
        /// <param name="distance">The number of tiles away from the center.</param>
        /// <returns>An IEnumerable which can be iterated to get all tiles from y-lowest to y-highest.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <see cref="distance"/> is negative.</exception>
        public static IEnumerable<Vector2I> GetAllTilesWithDistance(Vector2I center, int distance)
        {
            if (distance < 0)
                throw new ArgumentOutOfRangeException(nameof(distance), "Distance must be greater than 0.");

            if (distance == 0)
            {
                yield return center;
                yield break;
            }

            if (!distance_cache.TryGetValue(distance, out var offsets))
            {
                offsets = new List<Vector2I>();

                //Return values from bottom to top
                for (int i = -distance; i <= distance; i++)
                {
                    if (Math.Abs(i) == distance)
                    {
                        offsets.Add(new Vector2I(0, i));
                        continue;
                    }

                    var x = distance - Math.Abs(i);
                    offsets.Add(new Vector2I(-x, i));
                    offsets.Add(new Vector2I(x, i));
                }

                distance_cache.Add(distance, offsets);
            }

            foreach (var offset in offsets)
                yield return center + offset;
        }

        public static Vector2I ScalarMultiply(Vector2I left, Vector2I right) => new Vector2I(left.X * right.X, left.Y * right.Y);
        public static Vector2I ScalarMultiply(int left, Vector2I right) => new Vector2I(left * right.X, left * right.Y);
        public static Vector2I ScalarMultiply(Vector2I left, int right) => new Vector2I(left.X * right, left.Y * right);

        public static Vector2 ScalarMultiply(Vector2 left, Vector2I right) => new Vector2(left.X * right.X, left.Y * right.Y);
        public static Vector2 ScalarMultiply(float left, Vector2I right) => new Vector2(left * right.X, left * right.Y);
        public static Vector2 ScalarMultiply(Vector2I left, float right) => new Vector2(left.X * right, left.Y * right);
    }
}
