using System;
using System.Collections.Generic;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.Country;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Replay;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Animations;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Transforms;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.Game.Units
{
    public class DrawableUnit : CompositeDrawable, IHasMapPosition
    {
        public static readonly Vector2I BASE_SIZE = new Vector2I(16);
        public static readonly Colour4 FogColor = new Colour4(150, 150, 150, 255);

        public readonly UnitData UnitData;
        public long UnitID { get; private set; }
        public long? OwnerID { get; private set; }

        public bool UnitAnimatingIn
        {
            get => unitAnimatingIn;
            set
            {
                if (unitAnimatingIn == value) return;

                unitAnimatingIn = value;
                updateUnitColour(true);
            }
        }

        private bool unitAnimatingIn;

        public BindableInt HealthPoints = new BindableInt();
        public BindableInt Fuel = new BindableInt();
        public BindableInt Ammo = new BindableInt();

        public BindableBool FogOfWarActive = new BindableBool();
        public BindableBool CanMove = new BindableBool();
        public BindableBool IsCapturing = new BindableBool();
        public BindableBool BeingCarried = new BindableBool();

        public BindableInt MovementRange = new BindableInt();
        public Bindable<Vector2I> AttackRange = new Bindable<Vector2I>();

        public BindableBool Dived = new BindableBool();
        public Vector2I MapPosition { get; private set; }

        private UnitTextureAnimation textureAnimation;
        private TextureSpriteText healthSpriteText;

        private Sprite capturing;

        public CountryData Country { get; private set; }

        public HashSet<long> Cargo = new HashSet<long>();

        private IBindable<bool> showUnitInFog;

        public DrawableUnit(UnitData unitData, ReplayUnit unit, CountryData country, IBindable<FaceDirection> unitFaceDirection)
        {
            Country = country;
            UnitData = unitData;
            Size = BASE_SIZE;

            InternalChildren = new Drawable[]
            {
                textureAnimation = new UnitTextureAnimation()
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft
                },
                capturing = new Sprite
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    Alpha = 0
                },
                healthSpriteText = new TextureSpriteText("UI/Health")
                {
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                    Font = new FontUsage(size: 1.5f)
                }
            };

            MovementRange.Value = unitData.MovementRange;
            AttackRange.Value = unitData.AttackRange;

            HealthPoints.BindValueChanged(updateHp);
            IsCapturing.BindValueChanged(updateCapturing);
            BeingCarried.BindValueChanged(_ => updateCarried());
            unitFaceDirection?.BindValueChanged(x => updateFaceDirection(x.NewValue));
            UpdateUnit(unit);
        }

        public void UpdateUnit(ReplayUnit unit)
        {
            UnitID = unit.ID;
            if (unit.PlayerID.HasValue)
                OwnerID = unit.PlayerID;

            if (unit.HitPoints.HasValue)
                HealthPoints.Value = (int)MathF.Ceiling(unit.HitPoints.Value);
            if (unit.Fuel.HasValue)
                Fuel.Value = unit.Fuel.Value;
            if (unit.Ammo.HasValue)
                Ammo.Value = unit.Ammo.Value;

            if (unit.TimesMoved.HasValue)
                CanMove.Value = unit.TimesMoved.Value == 0;
            if (unit.SubHasDived.HasValue)
                Dived.Value = unit.SubHasDived.Value;

            if (unit.BeingCarried.HasValue)
                BeingCarried.Value = unit.BeingCarried.Value;

            if (unit.Position.HasValue)
                MoveToPosition(unit.Position.Value);

            if (unit.MovementPoints.HasValue)
                MovementRange.Value = unit.MovementPoints.Value;

            if (unit.Range.HasValue && unit.Range != Vector2I.Zero)
                AttackRange.Value = unit.Range.Value;

            Cargo.Clear();

            if (unit.CargoUnits != null)
            {
                foreach (var cargoUnit in unit.CargoUnits)
                    Cargo.Add(cargoUnit);
            }
        }

        public void CheckForDesyncs(ReplayUnit replayUnit)
        {
            if (UnitID != replayUnit.ID)
                throw new Exception($"Checking for desync on the wrong unit. Tried to check for {replayUnit.ID} but tried to check {UnitID}.");
            //Todo: More checks
        }

        [BackgroundDependencyLoader]
        private void load(NearestNeighbourTextureStore store, AWBWConfigManager configManager)
        {
            capturing.Texture = store.Get("UI/Capturing");
            capturing.Size = capturing.Texture.Size;

            if (UnitData.Frames == null)
            {
                var texture = store.Get($"{UnitData.BaseTextureByTeam[Country.Code]}-0");
                textureAnimation.Size = texture.Size;
                textureAnimation.AddFrame(texture);
                return;
            }

            for (var i = 0; i < UnitData.Frames.Length; i++)
            {
                var texture = store.Get($"{UnitData.BaseTextureByTeam[Country.Code]}-{i}");
                if (texture == null)
                    throw new Exception("Improperly configured UnitData. Animation count wrong.");
                if (i == 0)
                    textureAnimation.Size = texture.Size;
                textureAnimation.AddFrame(texture, UnitData.Frames[i]);
            }
            textureAnimation.Seek(UnitData.FrameOffset);

            showUnitInFog = configManager.GetBindable<bool>(AWBWSetting.ReplayShowHiddenUnits);
            showUnitInFog.BindValueChanged(x => updateUnitColour(x.NewValue));
            CanMove.BindValueChanged(x => updateUnitColour(x.NewValue));
            FogOfWarActive.BindValueChanged(x => updateUnitColour(x.NewValue));
            Dived.BindValueChanged(x => updateUnitColour(x.NewValue));

            updateUnitColour(true);
            updateCarried();
        }

        public void MoveToPosition(Vector2I position, bool updateVisual = true)
        {
            MapPosition = position;

            if (updateVisual)
            {
                ClearTransforms();
                this.MoveTo(GetRealPositionFromMapTiles(MapPosition));
            }
        }

        Vector2 GetRealPositionFromMapTiles(Vector2I position)
        {
            return Vec2IHelper.ScalarMultiply(position, BASE_SIZE) + new Vector2I(0, BASE_SIZE.Y);
        }

        public TransformSequence<DrawableUnit> FollowPath(IList<UnitPosition> path, bool reverse = false)
        {
            if (path.Count < 1)
                throw new Exception("Path must contain at least 1 position.");

            var transformSequence = this.MoveTo(GetRealPositionFromMapTiles(new Vector2I(path[0].X, path[0].Y)));

            if (path.Count == 2)
            {
                //Only moving 1 tile
                transformSequence.Then().MoveTo(GetRealPositionFromMapTiles(new Vector2I(path[1].X, path[1].Y)), 400, Easing.InOutQuad);
                return transformSequence;
            }

            for (int i = 1; i < path.Count; i++)
            {
                var pathNode = path[i];
                if (i == 1)
                    transformSequence.Then().MoveTo(GetRealPositionFromMapTiles(new Vector2I(pathNode.X, pathNode.Y)), 350, Easing.InQuad);
                else if (i == path.Count - 1)
                    transformSequence.Then().MoveTo(GetRealPositionFromMapTiles(new Vector2I(pathNode.X, pathNode.Y)), 350, Easing.OutQuad);
                else
                    transformSequence.Then().MoveTo(GetRealPositionFromMapTiles(new Vector2I(pathNode.X, pathNode.Y)), 140);
            }

            return transformSequence;
        }

        private void updateFaceDirection(FaceDirection faceDirection)
        {
            textureAnimation.Scale = new Vector2(faceDirection == Country.FaceDirection ? 1 : -1, 1);
            textureAnimation.Anchor = faceDirection == Country.FaceDirection ? Anchor.BottomLeft : Anchor.BottomRight;
        }

        private void updateHp(ValueChangedEvent<int> healthPoints)
        {
            if (healthPoints.NewValue >= 10)
            {
                healthSpriteText.Hide();
                return;
            }

            if (healthPoints.OldValue >= 10)
                healthSpriteText.Show();

            healthSpriteText.Text = healthPoints.NewValue.ToString();
        }

        private void updateCarried()
        {
            if (BeingCarried.Value)
                textureAnimation.Hide();
            else
                textureAnimation.Show();
        }

        private void updateCapturing(ValueChangedEvent<bool> isCapturing)
        {
            if (isCapturing.NewValue)
                capturing.ScaleTo(0.5f).ScaleTo(1, 200, Easing.OutBounce).FadeIn(100, Easing.InQuint);
            else
                capturing.FadeOut();
        }

        private void updateUnitColour(bool newValue)
        {
            Color4 colour;

            if (FogOfWarActive.Value)
                colour = FogColor;
            else
                colour = Color4.White;

            if (!CanMove.Value)
                colour = colour.Darken(0.25f);

            textureAnimation.FadeColour(colour, 250, newValue ? Easing.OutQuint : Easing.InQuint);
            textureAnimation.TransformTo("GreyscaleAmount", CanMove.Value ? 0f : 0.5f, 250, newValue ? Easing.OutQuint : Easing.InQuint);

            float alpha = 1;
            if (BeingCarried.Value || (FogOfWarActive.Value && !(showUnitInFog?.Value ?? true)))
                alpha = 0f;
            else if (Dived.Value)
                alpha = 0.7f;

            textureAnimation.FadeTo(alpha, 250, Easing.OutQuint);
        }

        private class UnitTextureAnimation : Animation<Texture>
        {
            public float GreyscaleAmount
            {
                get => textureHolder.GreyscaleAmount;
                set => textureHolder.GreyscaleAmount = value;
            }

            private GreyscaleSprite textureHolder;

            public UnitTextureAnimation(bool startAtCurrentTime = true)
                : base(startAtCurrentTime)
            {
            }

            public override Drawable CreateContent() =>
                textureHolder = new GreyscaleSprite
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre
                };

            protected override void DisplayFrame(Texture content) => textureHolder.Texture = content;

            protected override float GetFillAspectRatio() => textureHolder.FillAspectRatio;

            protected override Vector2 GetCurrentDisplaySize() => new Vector2(textureHolder.Texture?.DisplayWidth ?? 0, textureHolder.Texture?.DisplayHeight ?? 0);
        }

        private class GreyscaleSprite : Sprite
        {
            public float GreyscaleAmount
            {
                get => greyscaleAmount;
                set
                {
                    greyscaleAmount = value;
                    Invalidate(Invalidation.DrawNode);
                }
            }

            private float greyscaleAmount;

            [BackgroundDependencyLoader]
            private void load(ShaderManager shaders)
            {
                TextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, "GreyscaleSprite");
                RoundedTextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, "GreyscaleSpriteRounded");
            }

            protected override DrawNode CreateDrawNode() => new GreyscaleDrawNode(this);

            private class GreyscaleDrawNode : SpriteDrawNode
            {
                public new GreyscaleSprite Source => (GreyscaleSprite)base.Source;

                private float greyScaleAmount;

                public GreyscaleDrawNode(GreyscaleSprite source)
                    : base(source)
                {
                }

                public override void ApplyState()
                {
                    base.ApplyState();
                    greyScaleAmount = Source.greyscaleAmount;
                }

                protected override void Blit(Action<TexturedVertex2D> vertexAction)
                {
                    Shader.GetUniform<float>("greyscaleAmount").UpdateValue(ref greyScaleAmount);

                    base.Blit(vertexAction);
                }
            }
        }
    }
}
