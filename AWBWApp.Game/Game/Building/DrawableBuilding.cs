using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Country;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
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
        public static readonly Colour4 FOG_COLOUR = new Colour4(150, 150, 150, 255);

        public Bindable<bool> HasDoneAction = new Bindable<bool>();
        public BindableBool FogOfWarActive = new BindableBool();

        public BindableInt CaptureHealth = new BindableInt();

        public long? OwnerID { get; private set; }
        public Vector2I MapPosition { get; private set; }

        public Dictionary<string, int> TeamKnowledge = new Dictionary<string, int>();

        public readonly BuildingTile BuildingTile;

        private TextureAnimation textureAnimation;
        private Dictionary<WeatherType, List<Texture>> texturesByWeather;

        [Resolved]
        private NearestNeighbourTextureStore textureStore { get; set; }

        [Resolved]
        private BuildingStorage buildingStorage { get; set; }

        [Resolved]
        private IBindable<WeatherType> currentWeather { get; set; }

        [Resolved]
        private IBindable<MapSkin> currentSkin { get; set; }

        private readonly IBindable<CountryData> countryBindindable;

        public DrawableBuilding(BuildingTile buildingTile, Vector2I tilePosition, long? ownerID, IBindable<CountryData> country)
        {
            BuildingTile = buildingTile;
            OwnerID = ownerID;
            MapPosition = tilePosition;

            InternalChild = textureAnimation = new TextureAnimation()
            {
                Anchor = Anchor.BottomLeft,
                Origin = Anchor.BottomLeft,
                Loop = true
            };

            countryBindindable = country?.GetBoundCopy();

            Size = BASE_SIZE;
            HasDoneAction.BindValueChanged(x => updateBuildingColour(x.NewValue));
            FogOfWarActive.BindValueChanged(x => updateBuildingColour(x.NewValue));
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            countryBindindable?.BindValueChanged(_ => updateAnimation());
            currentSkin?.BindValueChanged(_ => updateAnimation());
            currentWeather.BindValueChanged(x => changeWeather(x.NewValue));

            updateAnimation();
        }

        private void updateAnimation()
        {
            texturesByWeather = new Dictionary<WeatherType, List<Texture>>();

            var buildingTile = BuildingTile;

            if (countryBindindable != null)
            {
                var country = countryBindindable.Value;
                buildingTile = buildingStorage.GetBuildingByTypeAndCountry(buildingTile.BuildingType, country.AWBWID);
            }

            foreach (var texturePair in buildingTile.Textures)
            {
                var textureList = new List<Texture>();

                var frameLength = buildingTile.Frames?.Length ?? 1;

                for (int i = 0; i < frameLength; i++)
                {
                    var texture = textureStore.Get($"Map/{currentSkin.Value}/{texturePair.Value}-{i}");

                    if (texture == null)
                    {
                        //AW1 skin doesn't have animations
                        if (currentSkin.Value == MapSkin.AW1 && i != 0)
                            break;

                        throw new Exception($"Improperly configured BuildingTile. Animation count wrong or image missing: Map/{currentSkin.Value}/{texturePair.Value}-{i}");
                    }

                    textureList.Add(texture);
                }

                texturesByWeather.Add(texturePair.Key, textureList);
            }

            changeWeather(currentWeather.Value);
        }

        private void changeWeather(WeatherType weatherType)
        {
            if (!texturesByWeather.TryGetValue(weatherType, out var weatherTextures))
                weatherTextures = texturesByWeather[WeatherType.Clear];

            var playbackPosition = textureAnimation.PlaybackPosition;
            if (double.IsNaN(playbackPosition))
                playbackPosition = 0;

            textureAnimation.ClearFrames();

            textureAnimation.Size = weatherTextures[0].Size;

            var buildingTile = BuildingTile;

            if (countryBindindable != null)
            {
                var country = countryBindindable.Value;
                buildingTile = buildingStorage.GetBuildingByTypeAndCountry(buildingTile.BuildingType, country.AWBWID);
            }

            if (buildingTile.Frames != null)
            {
                for (int i = 0; i < weatherTextures.Count; i++)
                    textureAnimation.AddFrame(weatherTextures[i], buildingTile.Frames[i]);
            }
            else
                textureAnimation.AddFrame(weatherTextures[0]);

            textureAnimation.Seek(playbackPosition);
            textureAnimation.Play();

            updateBuildingColour(false);
        }

        private void updateBuildingColour(bool fadeOut)
        {
            Color4 colour;
            if (FogOfWarActive.Value)
                colour = FOG_COLOUR;
            else
                colour = Color4.White;

            if (HasDoneAction.Value)
                colour = colour.Darken(0.2f);

            textureAnimation.FadeColour(colour, 250, fadeOut ? Easing.OutQuint : Easing.InQuint);
        }
    }
}
