using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Lists;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Weather
{
    public class RainAnimation : Drawable
    {
        protected virtual Vector2 ParticleBaseSize => new Vector2(3, 40);
        protected virtual float ParticleBaseVelocity => 750;
        protected virtual float ParticlesPerPixelArea => 0.000075f;
        protected virtual float ParticleCountChangeSmoothing => 0.2f;
        protected virtual Vector2 ParticleRandomAngle => new Vector2(0.1f, 0.15f);
        protected virtual bool CreateNewParticles => true;

        public float ParticleSizeMean = 0.65f;
        public float ParticleSizeStandardDeviation = 0.22f;
        public float ParitcleSizeMinimum = 0.5f;

        public float Velocity = 1;
        public float ParticleSpawnMultiplier = 1;

        public double CurrentParticleCount { get; private set; }
        public double TargetParticleCount { get; private set; }

        public Color4 ColourLight = new Color4(201, 210, 239, 255).Lighten(0.25f);
        public Color4 ColourDark = new Color4(129, 148, 211, 255).Lighten(0.15f);

        protected Texture Texture;

        private const float edge_smoothness = 1;
        private IShader shader;
        private readonly SortedList<RainParticle> parts = new SortedList<RainParticle>(Comparer<RainParticle>.Default);
        private Random stableRandom;

        public RainAnimation()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load(IRenderer renderer, ShaderManager shaders)
        {
            Texture = renderer.WhitePixel;
            shader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
        }

        protected override void Update()
        {
            base.Update();

            Invalidate(Invalidation.DrawNode);

            if (CreateNewParticles)
                addStars(false);

            float elapsedSeconds = (float)Time.Elapsed / 1000;

            float movedDistance = -elapsedSeconds * ParticleBaseVelocity / DrawHeight;

            for (int i = 0; i < parts.Count; i++)
            {
                var particle = parts[i];

                particle.Position += particle.Direction * particle.Velocity * movedDistance;
                parts[i] = particle;

                float topPos = parts[i].Position.Y - (parts[i].Scale * ParticleBaseSize.Y * 0.5f) / DrawHeight;
                if (topPos > 1)
                    parts.RemoveAt(i);
            }
        }

        private void addStars(bool randomY)
        {
            if (ParticleSpawnMultiplier <= 0)
            {
                CurrentParticleCount = parts.Count;
                return;
            }

            const int max_particles = ushort.MaxValue / (IRenderer.VERTICES_PER_QUAD + 2);

            TargetParticleCount = Math.Min(max_particles, (DrawWidth * DrawHeight * ParticlesPerPixelArea * ParticleSpawnMultiplier));

            var particleDiff = TargetParticleCount - CurrentParticleCount;
            var changePerSecond = Math.Max(50, Math.Max(TargetParticleCount, CurrentParticleCount) * 1.5f);
            var particleChange = Math.Sign(particleDiff) * changePerSecond * ParticleCountChangeSmoothing * (Time.Elapsed / 1000) / (DrawHeight / (ParticleBaseVelocity * 0.5));

            //Smooth out changes in particle count
            if (particleChange > 0)
                CurrentParticleCount = Math.Min(TargetParticleCount, CurrentParticleCount + particleChange);
            else
                CurrentParticleCount = Math.Max(TargetParticleCount, CurrentParticleCount + particleChange * 0.25f);

            for (int i = 0; i < CurrentParticleCount - parts.Count;)
                parts.Add(createStar(randomY));
        }

        private RainParticle createStar(bool randomY)
        {
            float u1 = 1 - nextRandom();
            float u2 = 1 - nextRandom();

            float randStdNormal = (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2));
            float scale = Math.Max((ParticleSizeMean + ParticleSizeStandardDeviation * randStdNormal), ParitcleSizeMinimum);

            var shade = nextRandom();
            return new RainParticle
            {
                Scale = scale,
                Position = new Vector2(nextRandom() * 1.15f, (randomY ? nextRandom() : 0) - 0.05f - 0.15f * nextRandom()),
                Direction = new Vector2(nextRandom() * (ParticleRandomAngle.Y - ParticleRandomAngle.X) + ParticleRandomAngle.X, -1).Normalized(),
                Velocity = Velocity * Math.Max(0.75f, scale * 1.25f),
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

        protected Color4 CreateStarShade(float shade) => Interpolation.ValueAt(shade, ColourDark, ColourLight, 0, 1);

        private float nextRandom() => (float)(stableRandom?.NextDouble() ?? RNG.NextSingle());

        protected override DrawNode CreateDrawNode() => new RainDrawNode(this);

        private class RainDrawNode : DrawNode
        {
            protected new RainAnimation Source => (RainAnimation)base.Source;

            private IShader shader;
            private Texture texture;

            private readonly List<RainParticle> parts = new List<RainParticle>();
            private Vector2 size;

            private IVertexBatch<TexturedVertex2D> vertexBatch;

            public RainDrawNode(RainAnimation source)
                : base(source)
            {
            }

            public override void ApplyState()
            {
                base.ApplyState();
                shader = Source.shader;
                texture = Source.Texture;
                size = Source.DrawSize;

                parts.Clear();
                parts.AddRange(Source.parts);
            }

            public override void Draw(IRenderer renderer)
            {
                base.Draw(renderer);

                var targetCount = (int)Math.Max(Source.TargetParticleCount, Source.CurrentParticleCount);

                if (targetCount > 0 && (vertexBatch == null || vertexBatch.Size != targetCount))
                {
                    vertexBatch?.Dispose();
                    vertexBatch = renderer.CreateQuadBatch<TexturedVertex2D>(targetCount, 1);
                }
                shader.Bind();

                Vector2 localInflationAmount = edge_smoothness * DrawInfo.MatrixInverse.ExtractScale().Xy;

                foreach (var particle in parts)
                {
                    var offset = new Vector2(Source.ParticleBaseSize.X * (0.5f + 0.5f * particle.Scale) * 0.5f, Source.ParticleBaseSize.Y * particle.Scale * 0.5f);
                    var skew = particle.Direction.X * Source.ParticleBaseVelocity * 0.5f * (Source.ParticleBaseSize.Y * particle.Scale / size.X);

                    var quad = new osu.Framework.Graphics.Primitives.Quad(
                        Vector2Extensions.Transform(particle.Position * size + new Vector2(-offset.X + skew, -offset.Y), DrawInfo.Matrix),
                        Vector2Extensions.Transform(particle.Position * size + new Vector2(-offset.X - skew, offset.Y), DrawInfo.Matrix),
                        Vector2Extensions.Transform(particle.Position * size + new Vector2(offset.X + skew, -offset.Y), DrawInfo.Matrix),
                        Vector2Extensions.Transform(particle.Position * size + new Vector2(offset.X - skew, offset.Y), DrawInfo.Matrix)
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

        protected struct RainParticle : IComparable<RainParticle>
        {
            public Vector2 Position;
            public Vector2 Direction;
            public float Velocity;

            public Color4 Colour;
            public float Scale;
            public int CompareTo(RainParticle other) => other.Scale.CompareTo(Scale);
        }
    }
}
