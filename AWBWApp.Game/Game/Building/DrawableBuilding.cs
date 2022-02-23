using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Animations;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
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
        private Dictionary<Weather, List<Texture>> texturesByWeather;

        public DrawableBuilding(BuildingTile buildingTile, long? ownerID, Vector2I tilePosition)
        {
            BuildingTile = buildingTile;
            OwnerID = ownerID;
            MapPosition = tilePosition;

            Size = BASE_SIZE;
            Position = GameMap.GetDrawablePositionForBottomOfTile(tilePosition);
            HasDoneAction.BindValueChanged(updateHasActed);
        }

        [BackgroundDependencyLoader]
        private void load(NearestNeighbourTextureStore store)
        {
            texturesByWeather = new Dictionary<Weather, List<Texture>>();

            foreach (var texturePair in BuildingTile.Textures)
            {
                var textureList = new List<Texture>();

                var frameLength = BuildingTile.Frames?.Length ?? 1;

                for (int i = 0; i < frameLength; i++)
                {
                    var texture = store.Get($"{texturePair.Value}-{i}");
                    if (texture == null)
                        throw new Exception($"Improperly configured BuildingTile. Animation count wrong or image missing: {texturePair.Value}-{i}");

                    textureList.Add(texture);
                }

                texturesByWeather.Add(texturePair.Key, textureList);
            }

            ChangeWeather(Weather.Clear);
        }

        public void ChangeWeather(Weather weather)
        {
            if (!texturesByWeather.TryGetValue(weather, out var weatherTextures))
                weatherTextures = texturesByWeather[Weather.Clear];

            //Todo: I don't particular like creating a new texture animation like this. This is caused by a need to invalidate the cache so that it doesn't take 1 second to change weather.

            var playbackPosition = textureAnimation?.PlaybackPosition ?? BuildingTile.FrameOffset;

            InternalChild = textureAnimation = new TextureAnimation()
            {
                Anchor = Anchor.BottomLeft,
                Origin = Anchor.BottomLeft,
                Size = weatherTextures[0].Size
            };

            if (BuildingTile.Frames != null)
            {
                for (int i = 0; i < BuildingTile.Frames.Length; i++)
                    textureAnimation.AddFrame(weatherTextures[i], BuildingTile.Frames[i]);
            }
            else
                textureAnimation.AddFrame(weatherTextures[0]);

            textureAnimation.Seek(playbackPosition);
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
