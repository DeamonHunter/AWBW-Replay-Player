using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Animations;
using osu.Framework.Graphics.Textures;
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

        public static void LoadIntoAnimation(this TextureStore textureStore, string baseTextureAnimation, Animation<Texture> animation, double[] frameTimes = null, double frameOffset = 0)
        {
            //We cannot be sure that the animation coming in is clear. So make sure it is before starting.
            animation.ClearFrames();

            if (frameTimes == null)
            {
                var texture = textureStore.Get($"{baseTextureAnimation}-0") ?? textureStore.Get(baseTextureAnimation);

                if (texture == null)
                    throw new Exception($"Tried to load the image '{baseTextureAnimation}' but it is missing.");

                animation.Size = texture.Size;
                animation.AddFrame(texture);
                return;
            }

            for (var i = 0; i < frameTimes.Length; i++)
            {
                var texture = textureStore.Get($"{baseTextureAnimation}-{i}");
                if (texture == null)
                    throw new Exception($"Improperly configured animation. Missing image '{baseTextureAnimation}-{i}'.");

                if (i == 0)
                    animation.Size = texture.Size;
                animation.AddFrame(texture, frameTimes[i]);
            }

            //Reset the animation to start where we expect
            animation.Seek(frameOffset);
        }
    }
}
