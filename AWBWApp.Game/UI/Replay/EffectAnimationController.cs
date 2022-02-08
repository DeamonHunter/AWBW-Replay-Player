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

    public class AdjustableRateTextureAnimation : CompositeDrawable
    {
        public TextureAnimation Animation { get; private set; }

        private double rate;

        public double Rate
        {
            get => rate;
            set
            {
                rate = value;
                consumeClockTime();
            }
        }

        private ManualClock manualClock = new ManualClock();

        public AdjustableRateTextureAnimation(TextureAnimation animation)
        {
            Animation = animation;
            Animation.Clock = new FramedClock(manualClock);
            base.AddInternal(Animation);
        }

        public override IFrameBasedClock Clock
        {
            get => base.Clock;
            set
            {
                base.Clock = value;
                consumeClockTime();
            }
        }

        public void RestartClockWithRate(double rate)
        {
            Rate = rate;
            Animation.Restart();
            consumeClockTime();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // always consume to zero out elapsed for update loop.
            double elapsed = consumeClockTime();
            manualClock.CurrentTime += elapsed;
        }

        private double lastConsumedTime;

        protected override void Update()
        {
            base.Update();

            double consumedTime = consumeClockTime();
            if (Animation.IsPlaying)
                manualClock.CurrentTime += consumedTime * rate;
        }

        private double consumeClockTime()
        {
            double elapsed = Time.Current - lastConsumedTime;
            lastConsumedTime = Time.Current;
            return elapsed;
        }
    }

    public class EffectAnimation : PoolableDrawable
    {
        private AdjustableRateTextureAnimation clockController;
        private string animationPath;

        [Resolved]
        private NearestNeighbourTextureStore textureStore { get; set; }

        public EffectAnimation()
        {
            Anchor = Anchor.TopLeft;
            Origin = Anchor.Centre;

            var animation = new TextureAnimation(false)
            {
                Loop = false,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre
            };

            InternalChild = clockController = new AdjustableRateTextureAnimation(animation)
            {
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
                clockController.Animation.Size = texture.Size;
                clockController.Animation.DefaultFrameLength = 100;
                clockController.Animation.AddFrame(texture);
                play(duration, startDelay);
                return;
            }

            clockController.Animation.Size = texture.Size;
            clockController.Animation.DefaultFrameLength = 100;
            clockController.Animation.AddFrame(texture);

            int idx = 1;

            while (true)
            {
                texture = textureStore.Get($"{path}-{idx++}");

                if (texture == null)
                    break;

                if (texture.Size != clockController.Animation.Size)
                    throw new Exception($"Texture animation '{path}' doesn't remain the same size.");

                clockController.Animation.AddFrame(texture);
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
                    clockController.RestartClockWithRate(clockController.Animation.Duration / duration);
                    if (LifetimeEnd == double.MaxValue)
                        LifetimeEnd = Time.Current + duration;
                });
            }
            else
            {
                clockController.RestartClockWithRate(clockController.Animation.Duration / duration);
                if (LifetimeEnd == double.MaxValue)
                    LifetimeEnd = Time.Current + duration;
            }
        }
    }
}
