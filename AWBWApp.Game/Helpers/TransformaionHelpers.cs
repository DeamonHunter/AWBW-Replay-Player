using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Transforms;

namespace AWBWApp.Game.Helpers
{
    public static class TransformaionHelpers
    {
        public static TransformSequence<TA> WaitForTransformationToComplete<TA, TB>(this TA drawable, TB other)
            where TA : Transformable
            where TB : Transformable
        {
            if (drawable.Clock != other.Clock)
                throw new InvalidOperationException("Cannot support 2 different clocks.");

            return drawable.Delay(other.LatestTransformEndTime - drawable.Time.Current);
        }

        public static TransformSequence<TA> AddDelayDependingOnDifferenceBetweenEndTimes<TA, TB>(this TransformSequence<TA> transform, TA baseTransformable, TB otherTransformable)
            where TA : Transformable
            where TB : Transformable
        {
            if (baseTransformable.Clock != otherTransformable.Clock)
                throw new InvalidOperationException("Cannot support 2 different clocks.");

            return transform.Delay(Math.Max(0, otherTransformable.LatestTransformEndTime - baseTransformable.LatestTransformEndTime));
        }
    }
}
