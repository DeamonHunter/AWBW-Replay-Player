using System;
using System.Collections.Generic;
using AWBWApp.Game.Helpers;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Animations;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Battle
{
    public class BattleWindow : Container
    {
        public Container BattleContainer;
        public Sprite BattleBackgound;

        private List<BattleUnit> units = new List<BattleUnit>();

        private readonly Queue<IEnumerator<ReplayWait>> currentOngoingActions = new Queue<IEnumerator<ReplayWait>>();
        private Random random = new Random();
        private Box arrowBox;

        public BattleWindow()
        {
            Size = new Vector2(136, 168);

            Children = new Drawable[]
            {
                new Box()
                {
                    Colour = new Color4(36, 34, 31, 255),
                    RelativeSizeAxes = Axes.Both
                },
                arrowBox = new Box()
                {
                    Colour = new Color4(36, 34, 31, 255),
                    Size = new Vector2(16),
                    Origin = Anchor.Centre,
                    Rotation = 45
                },
                BattleContainer = new Container()
                {
                    Size = new Vector2(128, 160),
                    Position = new Vector2(4, 4),
                    Masking = true,
                    Children = new Drawable[]
                    {
                        BattleBackgound = new Sprite
                        {
                            RelativeSizeAxes = Axes.Both
                        }
                    }
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load(NearestNeighbourTextureStore textureStore)
        {
            BattleBackgound.Texture = textureStore.Get("Battle/Plains");
        }

        public void AnimateMove(bool flip)
        {
            this.ScaleTo(new Vector2(0.5f, 0)).ScaleTo(0.5f, 250, Easing.OutQuint).Delay(2000).ScaleTo(new Vector2(0.5f, 0), 250, Easing.InQuint).Then().FadeTo(0);

            currentOngoingActions.Clear();
            arrowBox.Anchor = flip ? Anchor.CentreLeft : Anchor.CentreRight;

            foreach (var unit in units)
            {
                unit.FinishTransforms();
                unit.Expire();
            }
            units.Clear();

            for (int i = 0; i < 5; i++)
            {
                var unit = new BattleUnit();

                if (flip)
                {
                    unit.X = 128 + Math.Abs(2 - i) * 4 + (i % 2 == 0 ? 32 : 0) - 32.5f - 16f;
                }
                else
                {
                    unit.X = 58 - Math.Abs(2 - i) * 4 + (i % 2 == 0 ? 32 : 0) - 32;
                    unit.Scale = new Vector2(-1, 1);
                }

                unit.Y = 90 + i * 14;
                var performAction = unit.AnimateMove(75 + i * 30, 200 - i * 20, random, flip).GetEnumerator();
                currentOngoingActions.Enqueue(performAction);
                BattleContainer.Add(unit);
                units.Add(unit);
            }
        }

        public void AnimateCounter(bool flip)
        {
            this.ScaleTo(new Vector2(0.5f, 0)).Delay(150).ScaleTo(0.5f, 250, Easing.OutQuint).Delay(2000).ScaleTo(new Vector2(0.5f, 0), 250, Easing.InQuint).Then().FadeTo(0);

            arrowBox.Anchor = flip ? Anchor.CentreRight : Anchor.CentreLeft;

            currentOngoingActions.Clear();

            foreach (var unit in units)
            {
                unit.FinishTransforms();
                unit.Expire();
            }
            units.Clear();

            for (int i = 0; i < 5; i++)
            {
                var unit = new BattleUnit();

                if (flip)
                {
                    unit.X = 128 + Math.Abs(2 - i) * 4 + (i % 2 == 0 ? 32 : 0) - 70 - 16;
                    unit.Scale = new Vector2(-1, 1);
                }
                else
                    unit.X = 58 - Math.Abs(2 - i) * 4 + (i % 2 == 0 ? 32 : 0);

                unit.Y = 90 + i * 14;
                var performAction = unit.AnimateCounter(75 + i * 10 + 850, random).GetEnumerator();
                currentOngoingActions.Enqueue(performAction);
                BattleContainer.Add(unit);
                units.Add(unit);
            }
        }

        protected override void Update()
        {
            for (int i = 0; i < currentOngoingActions.Count; i++)
            {
                var ongoingAction = currentOngoingActions.Dequeue();

                if (ongoingAction.Current != null)
                {
                    if (!ongoingAction.Current.IsComplete(Time.Elapsed))
                    {
                        currentOngoingActions.Enqueue(ongoingAction);
                        continue;
                    }
                }

                while (true)
                {
                    try
                    {
                        if (!ongoingAction.MoveNext())
                        {
                            ongoingAction.Dispose();
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        ongoingAction.Dispose();
                        break;
                    }

                    if (ongoingAction.Current == null || ongoingAction.Current.IsComplete(Time.Elapsed))
                        continue;

                    currentOngoingActions.Enqueue(ongoingAction);
                    break;
                }
            }
        }
    }

    public class BattleUnit : Container
    {
        public Sprite Idle;
        public Sprite MuzzleFlash;

        private List<Texture> muzzleFlashes = new List<Texture>();

        public TextureAnimation Running;
        public TextureAnimation Fireing;

        public BattleUnit()
        {
            Size = new Vector2(32, 32);
            Origin = Anchor.BottomCentre;

            Children = new Drawable[]
            {
                Idle = new Sprite
                {
                    Size = new Vector2(32),
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Alpha = 0
                },
                MuzzleFlash = new Sprite
                {
                    Size = new Vector2(16),
                    X = -4,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Alpha = 0
                },
                Running = new TextureAnimation()
                {
                    Size = new Vector2(32),
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Alpha = 0
                },
                Fireing = new TextureAnimation()
                {
                    Size = new Vector2(32),
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Alpha = 0,
                    Loop = false
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load(NearestNeighbourTextureStore textureStore)
        {
            Idle.Texture = textureStore.Get("Battle/OrangeStar/Infantry_Stop");
            textureStore.LoadIntoAnimation("Battle/OrangeStar/Infantry_Move", Running, new double[] { 100, 100, 100, 100, 100, 100 });
            textureStore.LoadIntoAnimation("Battle/OrangeStar/Infantry_Fire", Fireing, new double[] { 100, 100 });

            var i = 0;

            while (true)
            {
                var texture = textureStore.Get($"Battle/OrangeStar/Infantry_Flash-{i}");
                if (texture == null)
                    break;

                muzzleFlashes.Add(texture);
                i++;
            }
        }

        public IEnumerable<ReplayWait> AnimateMove(double initialDelay, double fireDelay, Random random, bool flip)
        {
            Reset();

            yield return ReplayWait.WaitForMilliseconds(initialDelay);
            Idle.Hide();

            Running.Show();
            Running.Restart();

            this.MoveToOffset(new Vector2(flip ? -32.5f : 32.5f, 0), 400);
            yield return ReplayWait.WaitForTransformable(this);

            Running.Hide();
            Idle.Show();

            yield return ReplayWait.WaitForMilliseconds(fireDelay);

            Idle.Hide();
            Fireing.Show();

            for (int i = 0; i < 4; i++)
            {
                Fireing.Restart();
                MuzzleFlash.Show();
                MuzzleFlash.Texture = random.Pick(muzzleFlashes);
                yield return ReplayWait.WaitForMilliseconds(75);
                MuzzleFlash.Hide();

                yield return ReplayWait.WaitForMilliseconds(75);
            }

            Idle.Show();
            Fireing.Hide();
        }

        public IEnumerable<ReplayWait> AnimateCounter(double fireDelay, Random random)
        {
            Reset();

            yield return ReplayWait.WaitForMilliseconds(fireDelay);

            Idle.Hide();
            Fireing.Show();

            for (int i = 0; i < 4; i++)
            {
                Fireing.Restart();
                MuzzleFlash.Show();
                MuzzleFlash.Texture = random.Pick(muzzleFlashes);
                yield return ReplayWait.WaitForMilliseconds(75);
                MuzzleFlash.Hide();

                yield return ReplayWait.WaitForMilliseconds(75);
            }

            Idle.Show();
            Fireing.Hide();
        }

        public void Reset()
        {
            Idle.Show();
            Running.Hide();
            Fireing.Hide();
            MuzzleFlash.Hide();
        }
    }
}
