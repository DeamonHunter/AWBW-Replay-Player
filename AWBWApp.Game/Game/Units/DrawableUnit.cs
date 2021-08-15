using System;
using System.Collections.Generic;
using AWBWApp.Game.API;
using AWBWApp.Game.Game.Units;
using AWBWApp.Game.Helpers;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Animations;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transforms;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.Game.Unit
{
    public class DrawableUnit : CompositeDrawable
    {
        public static readonly Vector2I BASE_SIZE = new Vector2I(16);

        public readonly UnitData UnitData;
        public long UnitID { get; private set; }

        public BindableInt HealthPoints = new BindableInt();
        public BindableInt Fuel = new BindableInt();
        public BindableInt Ammo = new BindableInt();

        public BindableBool HasMoved = new BindableBool();
        public BindableBool HasCaptured = new BindableBool();

        public BindableBool CanBeSeen = new BindableBool();
        public BindableBool Dived = new BindableBool();
        public Vector2I MapPosition { get; private set; }

        private TextureAnimation textureAnimation;
        private TextureAnimation divedAnimation;
        private SpriteText healthSpriteText;
        private string country;

        public long? Cargo1;
        public long? Cargo2;

        public DrawableUnit(UnitData unitData, AWBWUnit unit)
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
                healthSpriteText = new SpriteText()
                {
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                    Font = new FontUsage(null, 8f)
                }
            };

            HealthPoints.BindValueChanged(UpdateHp);
            HasMoved.BindValueChanged(updateHasMoved);
            Dived.BindValueChanged(updateDived);
            UpdateUnit(unit);
        }

        public void UpdateUnit(AWBWUnit unit)
        {
            UnitID = unit.ID;
            country = unit.CountryCode;
            HealthPoints.Value = unit.HitPoints;
            Fuel.Value = unit.Fuel;
            Ammo.Value = unit.Ammo;
            HasMoved.Value = unit.HasMoved;
            HasCaptured.Value = unit.HasCaptured;
            Dived.Value = unit.UnitDived == "Y" || unit.UnitDived == "D";
            if (unit.Cargo1 == null || unit.Cargo1 == "?")
                Cargo1 = null;
            if (unit.Cargo2 == null || unit.Cargo2 == "?")
                Cargo2 = null;
            MoveToPosition(new Vector2I(unit.X, unit.Y));
        }

        [BackgroundDependencyLoader]
        private void load(NearestNeighbourTextureStore store)
        {
            if (UnitData.Frames == null)
            {
                var texture = store.Get($"{UnitData.BaseTextureByTeam[country]}-0");
                textureAnimation.Size = texture.Size;
                textureAnimation.AddFrame(texture);

                if (UnitData.DivedTextureByTeam != null)
                {
                    texture = store.Get($"{UnitData.DivedTextureByTeam[country]}-0");
                    divedAnimation.Size = texture.Size;
                    divedAnimation.AddFrame(texture);
                }
                return;
            }

            for (var i = 0; i < UnitData.Frames.Length; i++)
            {
                var texture = store.Get($"{UnitData.BaseTextureByTeam[country]}-{i}");
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
                    var texture = store.Get($"{UnitData.DivedTextureByTeam[country]}-{i}");
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

        public TransformSequence<DrawableUnit> FollowPath(IEnumerable<UnitPosition> path)
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

            MapPosition = new Vector2I(pathNode.X, pathNode.Y);

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

        private void updateDived(ValueChangedEvent<bool> dived)
        {
            if (dived.NewValue)
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

        private void updateHasMoved(ValueChangedEvent<bool> hasMoved)
        {
            ColourInfo animationColour;
            if (hasMoved.NewValue)
                animationColour = ColourInfo.SingleColour(new SRGBColour() { Linear = new Color4(200, 200, 200, 255) });
            else
                animationColour = ColourInfo.SingleColour(new SRGBColour() { Linear = Color4.White });
            textureAnimation.Colour = ColourInfo.SingleColour(animationColour);
        }

        public void LoadUnit(DrawableUnit unit)
        {
            if (unit.Cargo1.HasValue)
            {
                if (!unit.Cargo2.HasValue)
                    unit.Cargo2 = unit.UnitID;
                else
                    throw new Exception("Attempted to load more than 2 units. Possible replay error.");
            }
            else
                unit.Cargo1 = unit.UnitID;
        }

        public void UnloadUnit(DrawableUnit unit)
        {
            if (unit.Cargo1.HasValue && unit.Cargo1.Value == unit.UnitID)
                unit.Cargo1 = null;
            if (unit.Cargo2.HasValue && unit.Cargo2.Value == unit.UnitID)
                unit.Cargo2 = null;
        }
    }
}
