﻿using osu.Framework.Graphics;
using osu.Framework.Graphics.Pooling;

namespace AWBWApp.Game.Helpers
{
    public class ReplayWait
    {
        internal Drawable Transformable { get; set; }
        internal double Milliseconds { get; set; }

        public static ReplayWait WaitForMilliseconds(double milliseconds) => new ReplayWait { Milliseconds = milliseconds };

        public static ReplayWait WaitForTransformable(Drawable transformable) => new ReplayWait { Transformable = transformable };

        public bool IsComplete(double delta)
        {
            if (Transformable != null)
            {
                if (Transformable.Parent == null)
                    return true;

                if (Transformable is PoolableDrawable)
                    return Transformable.IsLoaded && Transformable.LifetimeEnd <= Transformable.Time.Current;

                return Transformable.IsLoaded && Transformable.LatestTransformEndTime <= Transformable.Time.Current;
            }

            Milliseconds -= delta;
            return Milliseconds <= 0;
        }
    }
}
