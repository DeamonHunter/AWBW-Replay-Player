using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Helpers;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Animations;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Pooling;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Timing;

namespace AWBWApp.Game.UI.Replay
{
    /// <summary>
    /// An over engineered setup so that we can play generic animations that may require a bunch of different animations all at once.
    /// </summary>
    public class EffectAnimationController : Container
    {
        private Dictionary<string, DrawablePool<EffectAnimation>> pools = new Dictionary<string, DrawablePool<EffectAnimation>>();

        public EffectAnimation PlayAnimation(string animation, double length, Vector2I position, double startDelay, float rotation)
        {
            if (!pools.TryGetValue(animation, out var pool))
            {
                pool = new DrawablePool<EffectAnimation>(0);
                pools.Add(animation, pool);
            }

            var drawable = pool.Get(x =>
            {
                x.Setup(animation, length, startDelay);
                x.Rotation = rotation;
                x.Position = GameMap.GetDrawablePositionForBottomOfTile(position) + DrawableTile.HALF_BASE_SIZE;
            });

            AddInternal(drawable);

            return drawable;
        }
    }

    public class AdjustablePlaybackTextureAnimation : TextureAnimation
    {
        private StopwatchClock clock;

        public AdjustablePlaybackTextureAnimation(bool startAtCurrentTime)
            : base(startAtCurrentTime)
        {
            clock = new StopwatchClock(true);
            Clock = new FramedClock(clock);
        }

        public void RestartAnimation(double rate)
        {
            clock.Rate = rate;
            Clock.ProcessFrame();
            this.Restart();
        }
    }

    public class EffectAnimation : PoolableDrawable
    {
        private AdjustablePlaybackTextureAnimation textureAnimation;
        private string animationPath;

        [Resolved]
        private NearestNeighbourTextureStore textureStore { get; set; }

        public EffectAnimation()
        {
            Anchor = Anchor.TopLeft;
            Origin = Anchor.Centre;

            InternalChild = textureAnimation = new AdjustablePlaybackTextureAnimation(false)
            {
                Loop = false,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre
            };
        }

        public void Setup(string path, double duration, double startDelay)
        {
            if (animationPath == null)
                Scheduler.AddOnce(() => load(path, duration, startDelay));
            else
                play(duration, startDelay);
        }

        private void load(string path, double duration, double startDelay)
        {
            var texture = textureStore.Get($"{path}-0");

            if (texture == null)
            {
                texture = textureStore.Get($"{path}");
                textureAnimation.Size = texture.Size;
                textureAnimation.DefaultFrameLength = 100;
                textureAnimation.AddFrame(texture);
                play(duration, startDelay);
                return;
            }

            textureAnimation.Size = texture.Size;
            textureAnimation.DefaultFrameLength = 100;
            textureAnimation.AddFrame(texture);

            int idx = 1;

            while (true)
            {
                texture = textureStore.Get($"{path}-{idx++}");

                if (texture == null)
                    break;

                if (texture.Size != textureAnimation.Size)
                    throw new Exception($"Texture animation '{path}' doesn't remain the same size.");

                textureAnimation.AddFrame(texture);
            }

            animationPath = path;
            play(duration, startDelay);
        }

        private void play(double duration, double startDelay)
        {
            if (startDelay > 0)
            {
                var alpha = Alpha;
                this.FadeOut().Delay(startDelay).FadeTo(alpha).OnComplete(x =>
                {
                    textureAnimation.RestartAnimation(textureAnimation.Duration / duration);
                    if (LifetimeEnd == double.MaxValue)
                        LifetimeEnd = Time.Current + duration;
                });
            }
            else
            {
                textureAnimation.RestartAnimation(textureAnimation.Duration / duration);
                if (LifetimeEnd == double.MaxValue)
                    LifetimeEnd = Time.Current + duration;
            }
        }
    }
}
