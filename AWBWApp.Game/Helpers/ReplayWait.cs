using osu.Framework.Graphics.Transforms;

namespace AWBWApp.Game.Helpers
{
    public class ReplayWait
    {
        internal Transformable Transformable { get; set; }
        internal double Milliseconds { get; set; }

        public static ReplayWait WaitForMilliseconds(double milliseconds) => new ReplayWait { Milliseconds = milliseconds };

        public static ReplayWait WaitForTransformable(Transformable transformable) => new ReplayWait { Transformable = transformable };

        public bool IsComplete(double delta)
        {
            if (Transformable != null)
                return Transformable.LatestTransformEndTime <= Transformable.Time.Current;

            Milliseconds -= delta;
            return Milliseconds <= 0;
        }
    }
}
