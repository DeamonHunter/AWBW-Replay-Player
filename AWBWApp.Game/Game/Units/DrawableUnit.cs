using System;
using System.Collections.Generic;
using AWBWApp.Game.API;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.Units;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Replay;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
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
    public class DrawableUnit : CompositeDrawable
    {
        public static readonly Vector2I BASE_SIZE = new Vector2I(16);

        public readonly UnitData UnitData;
        public long UnitID { get; private set; }
        public long? OwnerID { get; private set; }

        public BindableInt HealthPoints = new BindableInt();
        public BindableInt Fuel = new BindableInt();
        public BindableInt Ammo = new BindableInt();

        public BindableBool CanMove = new BindableBool();
        public BindableBool HasCaptured = new BindableBool();
        public BindableBool BeingCarried = new BindableBool();

        public BindableBool CanBeSeen = new BindableBool();
        public BindableBool Dived = new BindableBool();
        public Vector2I MapPosition { get; private set; }

        private TextureAnimation textureAnimation;
        private TextureAnimation divedAnimation;
        private TextureSpriteText healthSpriteText;
        private TargetReticule targetReticule;

        private string country;

        public HashSet<long> Cargo = new HashSet<long>();

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
                healthSpriteText = new TextureSpriteText("UI/Health")
                {
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                    Font = new FontUsage(size: 1.5f)
                }
            };

            HealthPoints.BindValueChanged(UpdateHp);
            CanMove.BindValueChanged(updateCanMove, true);
            Dived.BindValueChanged(x => updateAnimation());
            BeingCarried.BindValueChanged(x => updateAnimation());
            UpdateUnit(unit);
        }

        public DrawableUnit(UnitData unitData, ReplayUnit unit, string countryCode)
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
                healthSpriteText = new TextureSpriteText("UI/Health")
                {
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                    Font = new FontUsage(size: 1.5f)
                }
            };

            country = countryCode;

            HealthPoints.BindValueChanged(UpdateHp);
            CanMove.BindValueChanged(updateCanMove, true);
            Dived.BindValueChanged(x => updateAnimation());
            BeingCarried.BindValueChanged(x => updateAnimation());
            UpdateUnit(unit);
        }

        public void UpdateUnit(AWBWUnit unit)
        {
            UnitID = unit.ID;
            OwnerID = unit.OwnedBy;
            country = unit.CountryCode;
            HealthPoints.Value = unit.HitPoints;
            Fuel.Value = unit.Fuel;
            Ammo.Value = unit.Ammo;
            CanMove.Value = !unit.HasMoved;
            HasCaptured.Value = unit.HasCaptured;
            Dived.Value = unit.UnitDived == "Y" || unit.UnitDived == "D";

            Cargo.Clear();
            if (unit.Cargo1 != null && unit.Cargo1 != "?")
                Cargo.Add(int.Parse(unit.Cargo1));
            if (unit.Cargo2 != null && unit.Cargo2 != "?")
                Cargo.Add(int.Parse(unit.Cargo2));

            MoveToPosition(new Vector2I(unit.X, unit.Y));
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
            if (unit.TimesCaptured.HasValue)
                HasCaptured.Value = unit.TimesCaptured.Value == 0;
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

        private void updateCanMove(ValueChangedEvent<bool> canMove)
        {
            Color4 animationColour;
            if (canMove.NewValue)
                animationColour = Color4.White;
            else
                animationColour = new Color4(200, 200, 200, 255);
            textureAnimation.Colour = animationColour;
        }
    }
}
