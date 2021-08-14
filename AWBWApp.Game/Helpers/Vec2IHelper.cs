using System;
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

        public static Vector2I ScalarMultiply(Vector2I left, Vector2I right) => new Vector2I(left.X * right.X, left.Y * right.Y);
        public static Vector2I ScalarMultiply(int left, Vector2I right) => new Vector2I(left * right.X, left * right.Y);
        public static Vector2I ScalarMultiply(Vector2I left, int right) => new Vector2I(left.X * right, left.Y * right);

        public static Vector2 ScalarMultiply(Vector2 left, Vector2I right) => new Vector2(left.X * right.X, left.Y * right.Y);
        public static Vector2 ScalarMultiply(float left, Vector2I right) => new Vector2(left * right.X, left * right.Y);
        public static Vector2 ScalarMultiply(Vector2I left, float right) => new Vector2(left.X * right, left.Y * right);
    }
}
