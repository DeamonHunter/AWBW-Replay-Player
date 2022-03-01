using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK.Graphics;

namespace AWBWApp.Game.Game.Tile
{
    public class DrawableTile : CompositeDrawable
    {
        public static readonly Vector2I BASE_SIZE = new Vector2I(16);
        public static readonly Vector2I HALF_BASE_SIZE = new Vector2I(8);
        public static readonly Colour4 FogColor = new Colour4(150, 150, 150, 255);

        public BindableBool FogOfWarActive = new BindableBool();

        public readonly TerrainTile TerrainTile;

        private Sprite texture;

        private Dictionary<Weather, Texture> texturesByWeather;

        public DrawableTile(TerrainTile terrainTile)
        {
            TerrainTile = terrainTile;
            Size = BASE_SIZE;

            InternalChild = texture = new Sprite()
            {
                Anchor = Anchor.BottomLeft,
                Origin = Anchor.BottomLeft
            };

            FogOfWarActive.BindValueChanged(x => updateFog(x.NewValue));
        }

        [BackgroundDependencyLoader]
        private void load(NearestNeighbourTextureStore store)
        {
            texturesByWeather = new Dictionary<Weather, Texture>();

            foreach (var texturePair in TerrainTile.Textures)
            {
                var texture = store.Get(texturePair.Value);
                if (texture == null)
                    throw new Exception("Unable to find texture: " + texturePair.Value);

                texturesByWeather.Add(texturePair.Key, texture);
            }

            ChangeWeather(Weather.Clear);
        }

        public void ChangeWeather(Weather weather)
        {
            if (!texturesByWeather.TryGetValue(weather, out var weatherTexture))
                weatherTexture = texturesByWeather[Weather.Clear];

            texture.Texture = weatherTexture;
            texture.Size = weatherTexture.Size;
        }

        private void updateFog(bool foggy)
        {
            if (foggy)
                texture.FadeColour(FogColor, 150, Easing.OutQuint);
            else
                texture.FadeColour(Color4.White, 150, Easing.InQuint);
        }
    }
}
