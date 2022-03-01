using System;
using System.Collections.Generic;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.Country;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Game.Units;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Replay;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Animations;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transforms;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.Game.Unit
{
    public class DrawableUnit : CompositeDrawable, IHasMapPosition
    {
        public static readonly Vector2I BASE_SIZE = new Vector2I(16);
        public static readonly Colour4 FogColor = new Colour4(150, 150, 150, 255);

        public readonly UnitData UnitData;
        public long UnitID { get; private set; }
        public int? OwnerID { get; private set; }

        public BindableInt HealthPoints = new BindableInt();
        public BindableInt Fuel = new BindableInt();
        public BindableInt Ammo = new BindableInt();

        public BindableBool FogOfWarActive = new BindableBool();
        public BindableBool CanMove = new BindableBool();
        public BindableBool IsCapturing = new BindableBool();
        public BindableBool BeingCarried = new BindableBool();

        public BindableBool CanBeSeen = new BindableBool();
        public BindableBool Dived = new BindableBool();
        public Vector2I MapPosition { get; private set; }

        private TextureAnimation textureAnimation;
        private TextureAnimation divedAnimation;
        private TextureSpriteText healthSpriteText;

        private Sprite capturing;

        private CountryData country;

        public HashSet<long> Cargo = new HashSet<long>();

        public DrawableUnit(UnitData unitData, ReplayUnit unit, CountryData country)
        {
            UnitData = unitData;
            Size = BASE_SIZE;
            InternalChildren = new Drawable[]
            {
                textureAnimation = new TextureAnimation()
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft
                },
                divedAnimation = new TextureAnimation()
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    Alpha = 0
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

            this.country = country;

            HealthPoints.BindValueChanged(UpdateHp);
            CanMove.BindValueChanged(x => updateUnitColour(x.NewValue, FogOfWarActive.Value, x.NewValue), true);
            FogOfWarActive.BindValueChanged(x => updateUnitColour(CanMove.Value, x.NewValue, x.NewValue), true);
            IsCapturing.BindValueChanged(updateCapturing);
            Dived.BindValueChanged(x => updateAnimation());
            BeingCarried.BindValueChanged(x => updateAnimation());
            UpdateUnit(unit);
        }

        public void UpdateUnit(ReplayUnit unit)
        {
            UnitID = unit.ID;
            if (unit.PlayerID.HasValue)
                OwnerID = unit.PlayerID;

            if (unit.HitPoints.HasValue)
                HealthPoints.Value = (int)unit.HitPoints.Value;
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
        private void load(NearestNeighbourTextureStore store)
        {
            capturing.Texture = store.Get("UI/Capturing");
            capturing.Size = capturing.Texture.Size;

            if (UnitData.Frames == null)
            {
                var texture = store.Get($"{UnitData.BaseTextureByTeam[country.Code]}-0");
                textureAnimation.Size = texture.Size;
                textureAnimation.AddFrame(texture);

                if (UnitData.DivedTextureByTeam != null)
                {
                    texture = store.Get($"{UnitData.DivedTextureByTeam[country.Code]}-0");
                    divedAnimation.Size = texture.Size;
                    divedAnimation.AddFrame(texture);
                }
                return;
            }

            for (var i = 0; i < UnitData.Frames.Length; i++)
            {
                var texture = store.Get($"{UnitData.BaseTextureByTeam[country.Code]}-{i}");
                if (texture == null)
                    throw new Exception("Improperly configured UnitData. Animation count wrong.");
                if (i == 0)
                    textureAnimation.Size = texture.Size;
                textureAnimation.AddFrame(texture, UnitData.Frames[i]);
            }
            textureAnimation.Seek(UnitData.FrameOffset);

            if (UnitData.DivedTextureByTeam != null)
            {
                for (var i = 0; i < UnitData.Frames.Length; i++)
                {
                    var texture = store.Get($"{UnitData.DivedTextureByTeam[country.Code]}-{i}");
                    if (texture == null)
                        throw new Exception("Improperly configured UnitData. Animation count wrong.");
                    if (i == 0)
                        divedAnimation.Size = texture.Size;
                    divedAnimation.AddFrame(texture, UnitData.Frames[i]);
                }
                divedAnimation.Seek(UnitData.FrameOffset);
            }
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

        public TransformSequence<DrawableUnit> FollowPath(IEnumerable<UnitPosition> path, bool reverse = false)
        {
            var enumerator = path.GetEnumerator();

            enumerator.MoveNext();

            UnitPosition pathNode = enumerator.Current;
            if (pathNode == null)
                throw new Exception("Path must contain at least 1 position.");

            var transformSequence = this.MoveTo(GetRealPositionFromMapTiles(new Vector2I(pathNode.X, pathNode.Y)));

            while (enumerator.MoveNext())
            {
                pathNode = enumerator.Current;
                if (pathNode == null)
                    throw new Exception("Path contained null position.");

                transformSequence.Then().MoveTo(GetRealPositionFromMapTiles(new Vector2I(pathNode.X, pathNode.Y)), 250);
            }

            return transformSequence;
        }

        private void UpdateHp(ValueChangedEvent<int> healthPoints)
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

        private void updateAnimation()
        {
            if (BeingCarried.Value)
            {
                textureAnimation.Hide();
                divedAnimation.Hide();
            }
            else if (Dived.Value)
            {
                textureAnimation.Hide();
                divedAnimation.Show();
            }
            else
            {
                textureAnimation.Show();
                divedAnimation.Hide();
            }
        }

        private void updateCapturing(ValueChangedEvent<bool> isCapturing)
        {
            if (isCapturing.NewValue)
                capturing.ScaleTo(0.5f).ScaleTo(1, 200, Easing.OutBounce).FadeIn(100, Easing.InQuint);
            else
                capturing.FadeOut();
        }

        private void updateUnitColour(bool canMove, bool foggy, bool fadeOut)
        {
            Color4 colour;
            if (foggy)
                colour = FogColor;
            else
                colour = Color4.White;

            if (canMove)
                colour = colour.Darken(0.2f);

            textureAnimation.FadeColour(colour, 250, fadeOut ? Easing.OutQuint : Easing.InQuint);
        }
    }
}
