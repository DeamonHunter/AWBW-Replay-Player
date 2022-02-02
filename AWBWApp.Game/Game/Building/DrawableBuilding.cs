using System;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Animations;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osuTK.Graphics;

namespace AWBWApp.Game.Game.Building
{
    public class DrawableBuilding : CompositeDrawable, IHasMapPosition
    {
        public static readonly Vector2I BASE_SIZE = new Vector2I(16);

        public Bindable<bool> HasDoneAction = new Bindable<bool>();

        public long? OwnerID { get; private set; }
        public Vector2I MapPosition { get; private set; }

        public readonly BuildingTile BuildingTile;

        private TextureAnimation textureAnimation;

        public DrawableBuilding(BuildingTile buildingTile, long? ownerID, Vector2I tilePosition)
        {
            BuildingTile = buildingTile;
            OwnerID = ownerID;
            MapPosition = tilePosition;

            Size = BASE_SIZE;
            Position = GameMap.GetDrawablePositionForBottomOfTile(tilePosition);
            InternalChild = textureAnimation = new TextureAnimation()
            {
                Anchor = Anchor.BottomLeft,
                Origin = Anchor.BottomLeft
            };

            HasDoneAction.BindValueChanged(updateHasActed);
        }

        [BackgroundDependencyLoader]
        private void load(NearestNeighbourTextureStore store)
        {
            var texture = store.Get($"{BuildingTile.BaseTexture}-0");
            if (texture == null)
                throw new Exception($"Improperly configured BuildingTile. Base image missing: {BuildingTile.BaseTexture}-0");
            textureAnimation.Size = texture.Size;

            if (BuildingTile.Frames == null)
            {
                textureAnimation.AddFrame(texture);
                return;
            }

            textureAnimation.AddFrame(texture, BuildingTile.Frames[0]);

            for (var i = 1; i < BuildingTile.Frames.Length; i++)
            {
                texture = store.Get($"{BuildingTile.BaseTexture}-{i}");
                if (texture == null)
                    throw new Exception($"Improperly configured BuildingTile. Animation count wrong or image missing: {BuildingTile.BaseTexture}-{i}");
                textureAnimation.AddFrame(texture, BuildingTile.Frames[i]);
            }
            textureAnimation.Seek(BuildingTile.FrameOffset);
        }

        private void updateHasActed(ValueChangedEvent<bool> hasActed)
        {
            Color4 animationColour;
            if (hasActed.NewValue)
                animationColour = new Color4(200, 200, 200, 255);
            else
                animationColour = Color4.White;
            textureAnimation.Colour = animationColour;
        }
    }
}
