﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using osu.Framework.Caching;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Animations;
using osu.Framework.Graphics.Transforms;

namespace AWBWApp.Game.Helpers
{
    public static class AnimationHelper
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
            if (otherTransformable.Clock == null)
                return transform.Delay(0);

            if (baseTransformable.Clock != otherTransformable.Clock)
                throw new InvalidOperationException("Cannot support 2 different clocks.");

            return transform.Delay(Math.Max(0, otherTransformable.LatestTransformEndTime - baseTransformable.LatestTransformEndTime));
        }

        public static IDisposable BeginSequenceAfterTransformablesFinish<TA, TB>(this TA transformable, List<TB> delayUntilCompleted, bool recusive = true)
            where TA : Transformable
            where TB : Transformable
        {
            if (delayUntilCompleted == null || delayUntilCompleted.Count == 0)
                return transformable.BeginAbsoluteSequence(0, recusive);

            double delay = 0;

            foreach (var otherTransformable in delayUntilCompleted)
                delay = Math.Max(delay, otherTransformable.LatestTransformEndTime - otherTransformable.Time.Current);

            if (delay == 0)
                return transformable.BeginAbsoluteSequence(0, recusive);

            return transformable.BeginDelayedSequence(delay, recusive);
        }

        public static void ClearAnimationCache<T>(this Animation<T> animation)
        {
            var type = animation.GetType().BaseType;

            while (type != null)
            {
                var field = type.GetField("currentFrameCache", BindingFlags.Instance | BindingFlags.NonPublic);

                if (field != null)
                {
                    var value = (Cached)field.GetValue(animation);

                    Debug.Assert(value != null, "currentFrameCache had a null value.");
                    value.Invalidate();
                    return;
                }
                type = type.BaseType;
            }

            throw new Exception("Unable to find currentFrameCache");
        }
    }
}