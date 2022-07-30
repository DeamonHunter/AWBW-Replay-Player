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
        public static readonly Colour4 FOG_COLOUR = new Colour4(150, 150, 150, 255);

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

        private DrawableUnitSpriteContainer spriteContainer;
        private TextureAnimation statsAnimation;
        private TextureSpriteText healthSpriteText;

        public CountryData Country => country.Value;

        public HashSet<long> Cargo = new HashSet<long>();

        private Bindable<MovementState> movementState;

        private IBindable<bool> revealUnitInFog;
        private IBindable<CountryData> country;
        private IBindable<bool> movementAnimations;

        [Resolved]
        private NearestNeighbourTextureStore textureStore { get; set; }

        public DrawableUnit(UnitData unitData, ReplayUnit unit, IBindable<CountryData> country, IBindable<FaceDirection> unitFaceDirection)
        {
            this.country = country.GetBoundCopy();
            UnitData = unitData;
            Size = BASE_SIZE;

            InternalChildren = new Drawable[]
            {
                spriteContainer = new DrawableUnitSpriteContainer
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    Size = BASE_SIZE
                },
                healthSpriteText = new TextureSpriteText("UI/Healthv2")
                {
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                    Font = new FontUsage(size: 2.25f),
                    Position = new Vector2(1, 1)
                },
                statsAnimation = new TextureAnimation()
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    Size = new Vector2(7, 8)
                }
            };

            MovementRange.Value = unitData.MovementRange;
            AttackRange.Value = unitData.AttackRange;

            HealthPoints.BindValueChanged(updateHp, true);
            BeingCarried.BindValueChanged(x => updateUnitColour(x.NewValue));
            unitFaceDirection?.BindValueChanged(x => spriteContainer.UpdateFaceDirection(x.NewValue, this.country.Value), true);
            movementState = new Bindable<MovementState>();
            movementState.BindValueChanged(x => spriteContainer.SetMovementState(movementAnimations.Value ? x.NewValue : MovementState.Idle));

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

        [BackgroundDependencyLoader]
        private void load(AWBWConfigManager configManager)
        {
            country.BindValueChanged(x => spriteContainer.LoadAnimations(UnitData, x.NewValue, textureStore), true);

            revealUnitInFog = configManager.GetBindable<bool>(AWBWSetting.ReplayOnlyShownKnownInfo);
            revealUnitInFog.BindValueChanged(x => updateUnitColour(x.NewValue));

            movementAnimations = configManager.GetBindable<bool>(AWBWSetting.ReplayMovementAnimations);

            CanMove.BindValueChanged(x => updateUnitColour(x.NewValue));
            FogOfWarActive.BindValueChanged(x => updateUnitColour(x.NewValue));
            Dived.BindValueChanged(x => updateUnitColour(x.NewValue));

            IsCapturing.BindValueChanged(_ => updateStatIndicators(false));
            Fuel.BindValueChanged(_ => updateStatIndicators(false));
            Ammo.BindValueChanged(_ => updateStatIndicators(false), true);

            updateUnitColour(true);

            spriteContainer.FinishTransforms(true);
            statsAnimation.FinishTransforms();
        }

        private int lastCargoAmount;

        protected override void Update()
        {
            base.Update();
            if (lastCargoAmount == Cargo.Count)
                return;

            lastCargoAmount = Cargo.Count;
            updateStatIndicators(false);
        }

        public void MoveToPosition(Vector2I position, bool updateVisual = true)
        {
            MapPosition = position;

            if (updateVisual)
            {
                FinishTransforms();
                Position = getRealPositionFromMapTiles(MapPosition);
                movementState.Value = MovementState.Idle;
            }
        }

        private Vector2 getRealPositionFromMapTiles(Vector2I position)
        {
            return Vec2IHelper.ScalarMultiply(position, BASE_SIZE) + new Vector2I(0, BASE_SIZE.Y);
        }

        public TransformSequence<DrawableUnit> FollowPath(ReplayController controller, IList<UnitPosition> path, bool reverse = false)
        {
            if (path.Count < 1)
                throw new Exception("Path must contain at least 1 position.");

            var transformSequence = this.MoveTo(getRealPositionFromMapTiles(new Vector2I(path[0].X, path[0].Y)));

            bool fogActive(UnitPosition position) => controller.ShouldPlayerActionBeHidden(new Vector2I(position.X, position.Y));

            if (path.Count == 2)
            {
                //Only moving 1 tile

                var movementDirection = getMovementDirection(path[0], path[1]);
                transformSequence.Then().TransformBindableTo(FogOfWarActive, fogActive(path[1])).TransformBindableTo(movementState, movementDirection)
                                 .MoveTo(getRealPositionFromMapTiles(new Vector2I(path[1].X, path[1].Y)), 400, Easing.InOutQuad);
            }
            else
            {
                for (int i = 1; i < path.Count; i++)
                {
                    var pathNode = path[i];
                    var movementDirection = getMovementDirection(path[i - 1], pathNode);

                    transformSequence.Then().TransformBindableTo(FogOfWarActive, fogActive(pathNode)).TransformBindableTo(movementState, movementDirection);

                    if (i == 1)
                        transformSequence.MoveTo(getRealPositionFromMapTiles(new Vector2I(pathNode.X, pathNode.Y)), 350, Easing.InQuad);
                    else if (i == path.Count - 1)
                        transformSequence.MoveTo(getRealPositionFromMapTiles(new Vector2I(pathNode.X, pathNode.Y)), 350, Easing.OutQuad);
                    else
                        transformSequence.MoveTo(getRealPositionFromMapTiles(new Vector2I(pathNode.X, pathNode.Y)), 140);
                }
            }

            transformSequence.Then().TransformBindableTo(movementState, MovementState.Idle);
            return transformSequence;
        }

        private MovementState getMovementDirection(UnitPosition a, UnitPosition b)
        {
            if (a.X < b.X)
                return MovementState.MoveRight;
            if (a.X > b.X)
                return MovementState.MoveLeft;
            if (a.Y < b.Y)
                return MovementState.MoveDown;
            if (a.Y > b.Y)
                return MovementState.MoveUp;

            return MovementState.Idle;
        }

        private void updateHp(ValueChangedEvent<int> healthPoints)
        {
            healthSpriteText.Text = healthPoints.NewValue >= 10 ? "" : healthPoints.NewValue.ToString();
        }

        private void updateStatIndicators(bool unitRevealed)
        {
            if (unitHidden())
                return;

            var lowFuel = (float)Fuel.Value / UnitData.MaxFuel <= 0.25f;
            var lowAmmo = UnitData.MaxAmmo > 0 && (float)Ammo.Value / UnitData.MaxAmmo <= 0.25f;
            var hasCargo = Cargo.Count > 0;
            var capturing = IsCapturing.Value;

            if (!unitRevealed && !lowAmmo && !lowFuel && !hasCargo && !capturing)
            {
                statsAnimation.Hide();
                return;
            }

            statsAnimation.ClearFrames();

            if (lowAmmo)
                statsAnimation.AddFrame(textureStore.Get("UI/LowAmmo"), 1000);

            if (lowFuel)
                statsAnimation.AddFrame(textureStore.Get("UI/LowFuel"), 1000);

            if (hasCargo)
                statsAnimation.AddFrame(textureStore.Get("UI/HasCargo"), 1000);

            if (capturing)
                statsAnimation.AddFrame(textureStore.Get("UI/Capturing"), 1000);

            if (statsAnimation.FrameCount < 2 && !capturing && !hasCargo)
                statsAnimation.AddFrame(null, 1000);

            if (statsAnimation.Alpha == 0)
            {
                statsAnimation.ScaleTo(0.5f).ScaleTo(1, 200, Easing.OutBounce);
                statsAnimation.FadeIn(100, Easing.InQuint);
            }

            statsAnimation.Play();
        }

        private void updateUnitColour(bool newValue)
        {
            Color4 colour;

            if (FogOfWarActive.Value)
                colour = FOG_COLOUR;
            else
                colour = Color4.White;

            if (!CanMove.Value)
                colour = colour.Darken(0.25f);

            spriteContainer.FadeColour(colour, 250, newValue ? Easing.OutQuint : Easing.InQuint);
            spriteContainer.TransformTo("GreyscaleAmount", CanMove.Value ? 0f : 0.5f, 250, newValue ? Easing.OutQuint : Easing.InQuint);

            float alpha = 1;
            if (unitHidden())
                alpha = 0f;
            else if (Dived.Value)
                alpha = 0.7f;

            spriteContainer.FadeTo(alpha, 250, Easing.OutQuint);

            updateStatIndicators(alpha > 0);
            statsAnimation.FadeTo(alpha > 0 ? 1 : 0, 250, Easing.OutQuint);
            healthSpriteText.FadeTo(alpha > 0 ? 1 : 0, 250, Easing.OutQuint);
        }

        private bool unitHidden() => BeingCarried.Value || (FogOfWarActive.Value && !(revealUnitInFog?.Value ?? true));

        private class DrawableUnitSpriteContainer : Container
        {
            private readonly UnitTextureAnimation idleAnim;
            private readonly UnitTextureAnimation moveUpAnim;
            private readonly UnitTextureAnimation moveDownAnim;
            private readonly UnitTextureAnimation moveSideAnim;

            private MovementState currentMovementState;

            private float greyscaleAmount;

            public float GreyscaleAmount
            {
                get => greyscaleAmount;
                set
                {
                    greyscaleAmount = value;
                    idleAnim.GreyscaleAmount = value;
                    moveUpAnim.GreyscaleAmount = value;
                    moveDownAnim.GreyscaleAmount = value;
                    moveSideAnim.GreyscaleAmount = value;
                }
            }

            public DrawableUnitSpriteContainer()
            {
                Children = new Drawable[]
                {
                    idleAnim = new UnitTextureAnimation(),
                    moveUpAnim = new UnitTextureAnimation(),
                    moveDownAnim = new UnitTextureAnimation(),
                    moveSideAnim = new UnitTextureAnimation()
                };
            }

            public void LoadAnimations(UnitData unitData, CountryData countryData, NearestNeighbourTextureStore textureStore)
            {
                textureStore.LoadIntoAnimation($"{countryData.UnitPath}/{unitData.IdleAnimation.Texture}", idleAnim, unitData.IdleAnimation.Frames, unitData.IdleAnimation.FrameOffset);
                textureStore.LoadIntoAnimation($"{countryData.UnitPath}/{unitData.MoveSideAnimation.Texture}", moveSideAnim, unitData.MoveSideAnimation.Frames, unitData.MoveSideAnimation.FrameOffset);
                textureStore.LoadIntoAnimation($"{countryData.UnitPath}/{unitData.MoveUpAnimation.Texture}", moveUpAnim, unitData.MoveUpAnimation.Frames, unitData.MoveUpAnimation.FrameOffset);
                textureStore.LoadIntoAnimation($"{countryData.UnitPath}/{unitData.MoveDownAnimation.Texture}", moveDownAnim, unitData.MoveDownAnimation.Frames, unitData.MoveDownAnimation.FrameOffset);

                SetMovementState(currentMovementState, true);
            }

            public void UpdateFaceDirection(FaceDirection faceDirection, CountryData countryData)
            {
                idleAnim.Scale = new Vector2(faceDirection == countryData.FaceDirection ? 1 : -1, 1);
                idleAnim.Origin = faceDirection == countryData.FaceDirection ? Anchor.BottomLeft : Anchor.BottomRight;
            }

            public void SetMovementState(MovementState state, bool forceUpdate = false)
            {
                if (!forceUpdate && currentMovementState == state)
                    return;

                currentMovementState = state;
                idleAnim.Hide();
                idleAnim.Stop();
                moveUpAnim.Hide();
                moveUpAnim.Stop();
                moveDownAnim.Hide();
                moveDownAnim.Stop();
                moveSideAnim.Hide();
                moveSideAnim.Stop();

                switch (state)
                {
                    case MovementState.Idle:
                        idleAnim.Show();
                        idleAnim.Play();
                        break;

                    case MovementState.MoveUp:
                        moveUpAnim.Show();
                        moveUpAnim.Play();
                        break;

                    case MovementState.MoveDown:
                        moveDownAnim.Show();
                        moveDownAnim.Play();
                        break;

                    case MovementState.MoveLeft:
                    case MovementState.MoveRight:
                        moveSideAnim.Show();
                        moveSideAnim.Play();

                        moveSideAnim.Scale = new Vector2(state == MovementState.MoveRight ? 1 : -1, 1);
                        moveSideAnim.Origin = state == MovementState.MoveRight ? Anchor.BottomLeft : Anchor.BottomRight;
                        break;
                }
            }
        }

        private enum MovementState
        {
            Idle,
            MoveUp,
            MoveDown,
            MoveLeft,
            MoveRight
        }

        private class UnitTextureAnimation : Animation<Texture>
        {
            private float greyscaleAmount;

            public float GreyscaleAmount
            {
                get => greyscaleAmount;
                set
                {
                    greyscaleAmount = value;

                    if (textureHolder != null)
                        textureHolder.GreyscaleAmount = value;
                }
            }

            private GreyscaleSprite textureHolder;

            public UnitTextureAnimation(bool startAtCurrentTime = true)
                : base(startAtCurrentTime)
            {
                Anchor = Anchor.BottomLeft;
                Origin = Anchor.BottomLeft;
            }

            public override Drawable CreateContent() =>
                textureHolder = new GreyscaleSprite
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    GreyscaleAmount = greyscaleAmount
                };

            protected override void ClearDisplay() => textureHolder.Texture = null;

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
