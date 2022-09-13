using System;
using System.Collections.Generic;
using AWBWApp.Game.Helpers;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Lists;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Replay
{
    public class PowerDisplay : Container
    {
        private const float appear_duration = 500;
        private const float disappear_duration = 500;
        private const Easing easing_show = Easing.OutQuart;
        private const Easing easing_hide = Easing.InCubic;

        private readonly Container contentContainer;

        protected Sprite CharacterSprite;
        protected FillFlowContainer<Container> BouncingText;
        protected PowerStars BaseStars;
        protected PowerStars SuperStars;

        private string coName;

        public PowerDisplay(string coName, string powerName, bool super)
        {
            RelativeSizeAxes = Axes.X;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
            Size = new Vector2(0.66f, 80);
            Alpha = 0;

            this.coName = coName;

            AddInternal(contentContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
                RelativePositionAxes = Axes.Both,
                Anchor = Anchor.CentreRight,
                Origin = Anchor.CentreRight,
                Position = new Vector2(0, 2),
                Children = new Drawable[]
                {
                    SuperStars = new PowerStars(0.05f)
                    {
                        RelativeSizeAxes = Axes.X,
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre,
                        Position = new Vector2(0, -40),
                        Size = new Vector2(1.25f, 300),

                        Velocity = 20,
                        ScaleAdjustSpeed = 0.15f,
                        StarSizeMean = 0.5f,
                        StarSizeStandardDeviation = 0.12f,
                        MinimumStarSize = 0.25f,
                        ColourLight = Color4.LightYellow,
                        ColourDark = Color4.Yellow,
                        Alpha = super ? 1 : 0
                    },
                    new Container()
                    {
                        Masking = true,
                        RelativeSizeAxes = Axes.Both,
                        BorderThickness = 5,
                        BorderColour = Colour4.IndianRed,
                        Children = new Drawable[]
                        {
                            new Box()
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Colour4.DarkRed
                            },
                            BaseStars = new PowerStars(0.25f)
                            {
                                RelativeSizeAxes = Axes.Both,

                                Velocity = 25,
                                ScaleAdjustSpeed = 0.15f,
                                StarSizeMean = 0.3f,
                                StarSizeStandardDeviation = 0.12f,
                                MinimumStarSize = 0.175f,
                                ColourLight = Color4.LightYellow,
                                ColourDark = Color4.Yellow,
                                Alpha = 0.8f
                            }
                        }
                    },
                    CharacterSprite = new Sprite()
                    {
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomRight,
                        Position = new Vector2(0, 40),
                        Scale = new Vector2(-1.5f, 1.5f)
                    },
                    BouncingText = new FillFlowContainer<Container>()
                    {
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        AutoSizeAxes = Axes.Both,
                        RelativePositionAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Position = new Vector2(0.3f, -0.2f),
                    }
                }
            });

            foreach (var character in powerName)
            {
                BouncingText.Add(new Container()
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    AutoSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new TextureSpriteText("UI/Power")
                        {
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft,
                            Text = character.ToString(),
                            Font = FontUsage.Default.With(size: 1.5f),
                            Position = new Vector2(0, 0)
                        }
                    }
                });
            }
        }

        [BackgroundDependencyLoader]
        private void load(NearestNeighbourTextureStore textureStore)
        {
            CharacterSprite.Texture = textureStore.Get($"CO/{coName}-Full");
            CharacterSprite.Size = CharacterSprite.Texture.Size;

            ScheduleAfterChildren(() =>
            {
                if (BouncingText.Size.X > 480)
                    resizeBouncingText();
            });

            Animate();
        }

        private void resizeBouncingText()
        {
            var currentSize = BouncingText.Size.X;
            BouncingText.Scale = new Vector2(480 / currentSize);
        }

        public void Animate()
        {
            this.FadeIn();
            contentContainer.MoveToX(2).MoveToX(0, appear_duration, easing_show);
            CharacterSprite.MoveToX(0.4f).MoveToX(0f, appear_duration + 200, easing_show);
            BouncingText.MoveToX(0.85f).MoveToX(0.3f, appear_duration + 400, easing_show);

            using (BeginDelayedSequence(100))
            {
                var idx = -1;

                foreach (var characterContainer in BouncingText.Children)
                {
                    var delay = idx++ * 80;

                    foreach (var child in characterContainer.Children)
                    {
                        child.Position = Vector2.Zero;

                        using (BeginDelayedSequence(delay + 100))
                        {
                            child.MoveToY(10, 100, Easing.InOutSine);

                            using (BeginDelayedSequence(100))
                            {
                                child.MoveToY(-10, 200, Easing.InOutSine).Then().MoveToY(10, 200, Easing.InOutSine).Then().Loop(200, 6);
                                child.DelayUntilTransformsFinished().MoveToX(0, 100, Easing.InOutSine);
                            }
                        }
                    }
                }

                using (BeginDelayedSequence(2400))
                {
                    contentContainer.MoveToX(-2, disappear_duration, easing_hide);
                    CharacterSprite.MoveToX(0.4f, disappear_duration, easing_hide);
                    BouncingText.MoveToX(0.85f, disappear_duration, easing_hide);
                    this.Delay(disappear_duration).FadeOut();
                }
            }

            Expire();
        }
    }

    public class PowerStars : Drawable
    {
        private const float star_size = 100;
        private const float base_velocity = 50;

        /// <summary>
        /// How many screen-space pixels are smoothed over.
        /// Same behavior as Sprite's EdgeSmoothness.
        /// </summary>
        private const float edge_smoothness = 1;
        /// <summary>
        /// Whether we should drop-off alpha values of triangles more quickly to improve
        /// the visual appearance of fading. This defaults to on as it is generally more
        /// aesthetically pleasing, but should be turned off in buffered containers.
        /// </summary>
        public bool HideAlphaDiscrepancies = true;

        public int TargetStarCount { get; private set; }

        protected virtual bool CreateNewStars => true;

        private readonly SortedList<StarParticle> parts = new SortedList<StarParticle>(Comparer<StarParticle>.Default);

        /// <summary>
        /// The relative velocity of the triangles. Default is 1.
        /// </summary>
        public float Velocity = 1;

        public float ScaleAdjustSpeed = 1;

        private float spawnRatio = 1;

        public float SpawnRatio
        {
            get => spawnRatio;
            set
            {
                spawnRatio = value;
                Reset();
            }
        }

        public float StarSizeMean = 0.5f;
        public float StarSizeStandardDeviation = 0.16f;
        public float MinimumStarSize = 0.1f;

        private float starScale = 1;

        private IShader shader;
        private Texture texture;

        private Random stableRandom;

        private Color4 colourLight = Color4.White;

        public Color4 ColourLight
        {
            get => colourLight;
            set
            {
                if (colourLight == value) return;

                colourLight = value;
                updateColours();
            }
        }

        private Color4 colourDark = Color4.Black;

        public Color4 ColourDark
        {
            get => colourDark;
            set
            {
                if (colourDark == value) return;

                colourDark = value;
                updateColours();
            }
        }

        public PowerStars(float spawnRatio)
        {
            this.spawnRatio = spawnRatio;
        }

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders, TextureStore textureStore)
        {
            shader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE_ROUNDED);
            texture = textureStore.Get("UI/Star-White");
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            addStars(true);
        }

        protected override void Update()
        {
            base.Update();

            Invalidate(Invalidation.DrawNode);

            if (CreateNewStars)
                addStars(false);

            float adjustedAlpha = HideAlphaDiscrepancies ? MathF.Pow(DrawColourInfo.Colour.AverageColour.Linear.A, 3) : 1;

            float elapsedSeconds = (float)Time.Elapsed / 1000;

            float movedDistance = -elapsedSeconds * Velocity * base_velocity / (DrawWidth * starScale);

            for (int i = 0; i < parts.Count; i++)
            {
                var particle = parts[i];

                particle.Position.X += Math.Max(0.5f, parts[i].Scale) * movedDistance;
                particle.Colour.A = adjustedAlpha;
                parts[i] = particle;

                float leftPos = parts[i].Position.X + star_size * (ScaleAdjustSpeed * parts[i].Scale + (1 - ScaleAdjustSpeed)) * 0.5f / DrawWidth;
                if (leftPos < 0)
                    parts.RemoveAt(i);
            }
        }

        private void addStars(bool randomX)
        {
            const int max_particles = ushort.MaxValue / (IRenderer.VERTICES_PER_QUAD + 2);

            TargetStarCount = (int)Math.Min(max_particles, (DrawWidth * DrawHeight * 0.002f / (starScale * starScale) * SpawnRatio));

            for (int i = 0; i < TargetStarCount - parts.Count;)
                parts.Add(createStar(randomX));
        }

        private StarParticle createStar(bool randomX)
        {
            float u1 = 1 - nextRandom();
            float u2 = 1 - nextRandom();

            float randStdNormal = (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2));
            float scale = Math.Max(starScale * (StarSizeMean + StarSizeStandardDeviation * randStdNormal), MinimumStarSize);

            var shade = nextRandom();
            return new StarParticle
            {
                Scale = scale,
                Position = new Vector2(randomX ? nextRandom() : 1, nextRandom()),
                ColourShade = shade,
                Colour = CreateStarShade(shade)
            };
        }

        /// <summary>
        /// Clears and re-initialises triangles according to a given seed.
        /// </summary>
        /// <param name="seed">An optional seed to stabilise random positions / attributes. Note that this does not guarantee stable playback when seeking in time.</param>
        public void Reset(int? seed = null)
        {
            if (seed != null)
                stableRandom = new Random(seed.Value);

            parts.Clear();
            addStars(true);
        }

        protected Color4 CreateStarShade(float shade) => Interpolation.ValueAt(shade, colourDark, colourLight, 0, 1);

        private void updateColours()
        {
            for (int i = 0; i < parts.Count; i++)
            {
                StarParticle newParticle = parts[i];
                newParticle.Colour = CreateStarShade(newParticle.ColourShade);
                parts[i] = newParticle;
            }
        }

        private float nextRandom() => (float)(stableRandom?.NextDouble() ?? RNG.NextSingle());

        protected override DrawNode CreateDrawNode() => new StarDrawNode(this);

        private class StarDrawNode : DrawNode
        {
            protected new PowerStars Source => (PowerStars)base.Source;

            private IShader shader;
            private Texture texture;

            private readonly List<StarParticle> parts = new List<StarParticle>();
            private Vector2 size;

            private IVertexBatch<TexturedVertex2D> vertexBatch;

            public StarDrawNode(PowerStars source)
                : base(source)
            {
            }

            public override void ApplyState()
            {
                base.ApplyState();
                shader = Source.shader;
                texture = Source.texture;
                size = Source.DrawSize;

                parts.Clear();
                parts.AddRange(Source.parts);
            }

            public override void Draw(IRenderer renderer)
            {
                base.Draw(renderer);

                if (Source.TargetStarCount > 0 && (vertexBatch == null || vertexBatch.Size != Source.TargetStarCount))
                {
                    vertexBatch?.Dispose();
                    vertexBatch = renderer.CreateQuadBatch<TexturedVertex2D>(Source.TargetStarCount, 1);
                }
                shader.Bind();

                Vector2 localInflationAmount = edge_smoothness * DrawInfo.MatrixInverse.ExtractScale().Xy;

                foreach (var particle in parts)
                {
                    var offset = star_size * new Vector2(particle.Scale * 0.5f);

                    var quad = new osu.Framework.Graphics.Primitives.Quad(
                        Vector2Extensions.Transform(particle.Position * size + new Vector2(-offset.X, -offset.Y), DrawInfo.Matrix),
                        Vector2Extensions.Transform(particle.Position * size + new Vector2(-offset.X, offset.Y), DrawInfo.Matrix),
                        Vector2Extensions.Transform(particle.Position * size + new Vector2(offset.X, -offset.Y), DrawInfo.Matrix),
                        Vector2Extensions.Transform(particle.Position * size + new Vector2(offset.X, offset.Y), DrawInfo.Matrix)
                    );

                    ColourInfo colourInfo = DrawColourInfo.Colour;
                    colourInfo.ApplyChild(particle.Colour);

                    renderer.DrawQuad(texture, quad, colourInfo, null, vertexBatch.AddAction, Vector2.Divide(localInflationAmount, new Vector2(2 * offset.X, offset.Y)));
                }

                shader.Unbind();
            }

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);

                vertexBatch?.Dispose();
            }
        }

        protected struct StarParticle : IComparable<StarParticle>
        {
            /// <summary>
            /// The position of the top vertex of the triangle.
            /// </summary>
            public Vector2 Position;

            /// <summary>
            /// The colour shade of the triangle.
            /// This is needed for colour recalculation of visible triangles when <see cref="ColourDark"/> or <see cref="ColourLight"/> is changed.
            /// </summary>
            public float ColourShade;

            /// <summary>
            /// The colour of the triangle.
            /// </summary>
            public Color4 Colour;

            /// <summary>
            /// The scale of the triangle.
            /// </summary>
            public float Scale;

            /// <summary>
            /// Compares two <see cref="StarParticle"/>s. This is a reverse comparer because when the
            /// triangles are added to the particles list, they should be drawn from largest to smallest
            /// such that the smaller triangles appear on top.
            /// </summary>
            /// <param name="other"></param>
            public int CompareTo(StarParticle other) => other.Scale.CompareTo(Scale);
        }
    }
}
