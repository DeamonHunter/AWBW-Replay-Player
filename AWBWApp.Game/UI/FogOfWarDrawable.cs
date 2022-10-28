using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Game.Tile;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osuTK.Graphics;

namespace AWBWApp.Game.UI
{
    public class FogOfWarDrawable : Container<Box>
    {
        private FogOfWarTile[,] tiles;

        public FogOfWarDrawable()
        {
            AutoSizeAxes = Axes.Both;
        }

        public void NewStart(GameMap map, FogOfWarGenerator generator)
        {
            Clear();

            tiles = new FogOfWarTile[map.MapSize.X, map.MapSize.Y];

            for (int x = 0; x < map.MapSize.X; x++)
            {
                for (int y = 0; y < map.MapSize.Y; y++)
                {
                    tiles[x, y] = new FogOfWarTile
                    {
                        Position = GameMap.GetDrawablePositionForBottomOfTile(new Vector2I(x, y))
                    };
                    Add(tiles[x, y]);
                }
            }

            generator.FogOfWar.BindValueChanged(updateFog);
        }

        private void updateFog(ValueChangedEvent<FogOfWarState[,]> fogValues)
        {
            var xLength = fogValues.NewValue.GetLength(0);
            var yLength = fogValues.NewValue.GetLength(1);

            for (int x = 0; x < xLength; x++)
            {
                for (int y = 0; y < yLength; y++)
                {
                    if (fogValues.NewValue[x, y] == FogOfWarState.AllVisible)
                        tiles[x, y].SetClear();
                    else
                        tiles[x, y].SetFoggy();
                }
            }
        }

        private class FogOfWarTile : Box
        {
            private const float fog_transparency = 0.4f;
            private bool foggy;

            public FogOfWarTile()
            {
                Size = DrawableTile.BASE_SIZE;
                Colour = ColourInfo.SingleColour(Color4.Black);
                Alpha = 0;
            }

            public void SetFoggy()
            {
                if (foggy)
                    return;

                this.FadeTo(fog_transparency, 150, Easing.OutQuint);
                foggy = true;
            }

            public void SetClear()
            {
                if (!foggy)
                    return;

                this.FadeTo(0, 150, Easing.InQuint);
                foggy = false;
            }
        }
    }
}
