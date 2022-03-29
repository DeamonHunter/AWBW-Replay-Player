using System.Collections.Generic;
using System.Numerics;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Country;
using AWBWApp.Game.Game.Tile;
using Newtonsoft.Json;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using osuTK.Graphics.ES30;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace AWBWApp.Game.API.Replay
{
    public class ReplayMap
    {
        [JsonProperty]
        public string TerrainName;
        [JsonProperty]
        public Vector2I Size;
        [JsonProperty]
        public short[] Ids;

        private const int pixels_per_tile = 5;

        public Texture GenerateTexture(TerrainTileStorage terrainStorage, BuildingStorage buildingStorage, CountryStorage countryStorage)
        {
            var image = new Image<Rgba32>(Configuration.Default, Size.X * pixels_per_tile, Size.Y * pixels_per_tile, new Rgba32(0, 0, 0, 255));
            Dictionary<short, Rgba32> mapColors = new Dictionary<short, Rgba32>();
            Dictionary<short, Rgba32> secondaryMapColors = new Dictionary<short, Rgba32>();

            for (int y = 0; y < Size.Y; y++)
            {
                for (int x = 0; x < Size.X; x++)
                {
                    var tileId = Ids[y * Size.X + x];

                    if (mapColors.TryGetValue(tileId, out var pixel))
                    {
                        setBlock(image, pixel, secondaryMapColors[tileId], x, y);
                        continue;
                    }

                    pixel = new Rgba32(0, 0, 0, 255);
                    var secondaryPixel = new Rgba32(0, 0, 0, 255);

                    if (terrainStorage.TryGetTileByAWBWId(tileId, out var tile))
                    {
                        var colour = Color4Extensions.FromHex(tile.Colour ?? "000000FF");
                        pixel = new Rgba32(new Vector4(colour.R, colour.G, colour.B, colour.A));
                        secondaryPixel = new Rgba32(new Vector4(colour.R, colour.G, colour.B, colour.A));
                    }
                    else if (buildingStorage.TryGetBuildingByAWBWId(tileId, out var building))
                    {
                        if (building.CountryID != 0)
                        {
                            var colour = Color4Extensions.FromHex(countryStorage.GetCountryByAWBWID(building.CountryID).Colours["playerList"]).Lighten(0.2f);
                            var darkenedColour = colour.Darken(0.4f);
                            pixel = new Rgba32(new Vector4(colour.R, colour.G, colour.B, colour.A));
                            secondaryPixel = new Rgba32(new Vector4(darkenedColour.R, darkenedColour.G, darkenedColour.B, darkenedColour.A));
                        }
                        else
                        {
                            var colour = Color4Extensions.FromHex(building.Colour ?? "000000FF");
                            var darkenedColor = colour.Darken(0.4f);
                            pixel = new Rgba32(new Vector4(colour.R, colour.G, colour.B, colour.A));
                            secondaryPixel = new Rgba32(new Vector4(darkenedColor.R, darkenedColor.G, darkenedColor.B, darkenedColor.A));
                        }
                    }

                    mapColors[tileId] = pixel;
                    secondaryMapColors[tileId] = secondaryPixel;
                    setBlock(image, pixel, secondaryPixel, x, y);
                }
            }

            var texture = new Texture(Size.X * pixels_per_tile, Size.Y * pixels_per_tile, true, All.Nearest);
            texture.SetData(new TextureUpload(image));
            return texture;
        }

        private void setBlock(Image<Rgba32> image, Rgba32 pixel, Rgba32 secondaryPixel, int x, int y)
        {
            var xStart = x * pixels_per_tile;
            var yStart = y * pixels_per_tile;

            image[xStart, yStart] = secondaryPixel;
            image[xStart + 1, yStart] = secondaryPixel;
            image[xStart + 2, yStart] = secondaryPixel;
            image[xStart + 3, yStart] = secondaryPixel;
            image[xStart + 4, yStart] = secondaryPixel;

            image[xStart, yStart + 1] = secondaryPixel;
            image[xStart + 1, yStart + 1] = pixel;
            image[xStart + 2, yStart + 1] = pixel;
            image[xStart + 3, yStart + 1] = pixel;
            image[xStart + 4, yStart + 1] = secondaryPixel;

            image[xStart, yStart + 2] = secondaryPixel;
            image[xStart + 1, yStart + 2] = pixel;
            image[xStart + 2, yStart + 2] = pixel;
            image[xStart + 3, yStart + 2] = pixel;
            image[xStart + 4, yStart + 2] = secondaryPixel;

            image[xStart, yStart + 3] = secondaryPixel;
            image[xStart + 1, yStart + 3] = pixel;
            image[xStart + 2, yStart + 3] = pixel;
            image[xStart + 3, yStart + 3] = pixel;
            image[xStart + 4, yStart + 3] = secondaryPixel;

            image[xStart, yStart + 4] = secondaryPixel;
            image[xStart + 1, yStart + 4] = secondaryPixel;
            image[xStart + 2, yStart + 4] = secondaryPixel;
            image[xStart + 3, yStart + 4] = secondaryPixel;
            image[xStart + 4, yStart + 4] = secondaryPixel;
        }
    }
}
