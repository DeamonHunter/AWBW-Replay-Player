using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Country;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Helpers;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Animations;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK.Graphics;

namespace AWBWApp.Game.Game.Building
{
    public partial class DrawableBuilding : CompositeDrawable, IHasMapPosition
    {
        public static readonly Vector2I BASE_SIZE = new Vector2I(16);
        public static readonly Colour4 FOG_COLOUR = new Colour4(150, 150, 150, 255);

        public Bindable<bool> HasDoneAction = new Bindable<bool>();
        public BindableBool FogOfWarActive = new BindableBool();

        public BindableInt CaptureHealth = new BindableInt();

        public long? OwnerID { get; private set; }
        public Vector2I MapPosition { get; private set; }

        public Dictionary<string, BuildingTile> TeamToTile = new Dictionary<string, BuildingTile>();

        public readonly BuildingTile BuildingTile;
        private BuildingTile shownBuildingTile;

        private TextureAnimation textureAnimation;
        private Sprite baseTile;
        private Dictionary<WeatherType, List<Texture>> buildingTexturesByWeather;
        private Dictionary<WeatherType, Texture> baseTextureByWeather;

        [Resolved]
        private NearestNeighbourTextureStore textureStore { get; set; }

        [Resolved]
        private BuildingStorage buildingStorage { get; set; }

        [Resolved]
        private TerrainTileStorage terrainStorage { get; set; }

        [Resolved]
        private IBindable<WeatherType> currentWeather { get; set; }

        [Resolved]
        private IBindable<BuildingSkin> buildingSkin { get; set; }

        [Resolved]
        private IBindable<MapSkin> mapSkin { get; set; }

        private readonly IBindable<CountryData> countryBindindable;
        private IBindable<bool> revealBuildingInFog;

        public DrawableBuilding(BuildingTile buildingTile, Vector2I tilePosition, long? ownerID, IBindable<CountryData> country)
        {
            BuildingTile = buildingTile;
            OwnerID = ownerID;
            MapPosition = tilePosition;

            InternalChildren = new Drawable[]
            {
                baseTile = new Sprite()
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                },
                textureAnimation = new TextureAnimation()
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    Loop = true
                }
            };

            countryBindindable = country?.GetBoundCopy();

            Size = BASE_SIZE;
            HasDoneAction.BindValueChanged(x => updateBuildingColour(x.NewValue));
            FogOfWarActive.BindValueChanged(x =>
            {
                if (shownBuildingTile != BuildingTile && LoadState == LoadState.Loaded)
                    updateAnimation();
                updateBuildingColour(x.NewValue);
            });

            shownBuildingTile = BuildingTile;
        }

        [BackgroundDependencyLoader]
        private void load(AWBWConfigManager configManager)
        {
            countryBindindable?.BindValueChanged(_ => updateAnimation());
            mapSkin?.BindValueChanged(_ => updateAnimation());
            buildingSkin?.BindValueChanged(_ => updateAnimation());
            currentWeather.BindValueChanged(x => changeWeather(x.NewValue));

            revealBuildingInFog = configManager.GetBindable<bool>(AWBWSetting.ReplayOnlyShownKnownInfo);
            revealBuildingInFog.BindValueChanged(x => updateBuildingColour(x.NewValue), true);

            updateAnimation();
        }

        public CountryData GetCurrentCountry() => countryBindindable?.Value;

        public void UpdateFogOfWarBuilding(bool unitsShown, string currentTeam)
        {
            var original = shownBuildingTile;

            if (unitsShown || !TeamToTile.TryGetValue(currentTeam, out shownBuildingTile))
                shownBuildingTile = BuildingTile;

            if (shownBuildingTile != original && LoadState == LoadState.Loaded)
                updateAnimation();
        }

        private void updateAnimation()
        {
            buildingTexturesByWeather = new Dictionary<WeatherType, List<Texture>>();
            baseTextureByWeather = new Dictionary<WeatherType, Texture>();

            var buildingTile = FogOfWarActive.Value ? shownBuildingTile : BuildingTile;

            if (countryBindindable != null && buildingTile.CountryID != -1)
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
                    var texture = textureStore.Get($"Map/{buildingSkin.Value}/{texturePair.Value}-{i}");

                    if (texture == null)
                    {
                        //AW1 skin doesn't have animations
                        if (buildingSkin.Value == BuildingSkin.AW1 && i != 0)
                            break;

                        throw new Exception($"Improperly configured BuildingTile. Animation count wrong or image missing: Map/{buildingSkin.Value}/{texturePair.Value}-{i}");
                    }

                    textureList.Add(texture);
                }

                buildingTexturesByWeather.Add(texturePair.Key, textureList);
            }

            var grassTile = terrainStorage.GetTileByAWBWId(1);

            foreach (var texturePair in grassTile.Textures)
            {
                var texture = textureStore.Get($"Map/{mapSkin.Value.ToFolder(buildingSkin.Value)}/{texturePair.Value}");
                baseTextureByWeather.Add(texturePair.Key, texture);
            }

            changeWeather(currentWeather.Value);
        }

        private void changeWeather(WeatherType weatherType)
        {
            if (!buildingTexturesByWeather.TryGetValue(weatherType, out var weatherTextures))
                weatherTextures = buildingTexturesByWeather[WeatherType.Clear];

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

            if (!baseTextureByWeather.TryGetValue(weatherType, out var baseTexture))
                baseTexture = baseTextureByWeather[WeatherType.Clear];

            baseTile.Texture = baseTexture;
            updateBuildingColour(false);
        }

        private void updateBuildingColour(bool fadeOut)
        {
            Color4 colour;
            if (FogOfWarActive.Value)
                colour = FOG_COLOUR;
            else
                colour = Color4.White;

            if (HasDoneAction.Value && (!FogOfWarActive.Value || (revealBuildingInFog?.Value ?? true)))
                colour = colour.Darken(0.2f);

            textureAnimation.FadeColour(colour, 250, fadeOut ? Easing.OutQuint : Easing.InQuint);
        }
    }
}
