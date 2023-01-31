using System;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Helpers;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Editor.Components
{
    public partial class EditorSpriteButton : CompositeDrawable, IHasTooltip
    {
        public Action<TerrainTile, BuildingTile> Action;

        private TerrainTile tile;

        public TerrainTile Tile
        {
            get => tile;
            set
            {
                if (tile == value)
                    return;

                texturePath = null;
                tile = value;
                building = null;
                updateVisual();
            }
        }

        private BuildingTile building;

        public BuildingTile Building
        {
            get => building;
            set
            {
                if (building == value)
                    return;

                texturePath = null;
                building = value;
                tile = null;
                updateVisual();
            }
        }

        private string texturePath;

        public string TexturePath
        {
            get => texturePath;
            set
            {
                if (texturePath == value)
                    return;

                texturePath = value;
                building = null;
                tile = null;
                updateVisual();
            }
        }

        public LocalisableString TooltipText { get; set; }

        private Sprite tileSprite;
        private Box background;
        private Box hoverBox;

        [Resolved]
        private NearestNeighbourTextureStore textureStore { get; set; }

        [Resolved]
        private IBindable<MapSkin> currentSkin { get; set; }

        private bool isSelected;

        private static Color4 background_colour = new Color4(20, 20, 20, 150);
        private static Color4 selected_colour = new Color4(0, 0, 0, 175);
        private static Color4 hover_color = new Color4(100, 100, 100, 150);

        public EditorSpriteButton()
        {
            Size = new Vector2(50, 50);
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            Masking = true;
            CornerRadius = 6;

            InternalChildren = new Drawable[]
            {
                background = new Box()
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(20, 20, 20, 150)
                },
                hoverBox = new Box()
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = hover_color.Opacity(0f)
                },
                tileSprite = new Sprite()
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(32, 32)
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            updateVisual();
        }

        private void updateVisual()
        {
            if (textureStore == null || (tile == null && building == null && texturePath == null))
                return;

            string tileTexturePath;

            if (texturePath == null)
            {
                if (building != null)
                    tileTexturePath = $"Map/{currentSkin.Value}/{building?.Textures[WeatherType.Clear]}-0";
                else
                    tileTexturePath = $"Map/{currentSkin.Value}/{tile?.Textures[WeatherType.Clear]}";
            }
            else
                tileTexturePath = texturePath;

            tileSprite.Texture = textureStore.Get(tileTexturePath);

            var max = Math.Max(tileSprite.Texture.Size.X, tileSprite.Texture.Size.Y);
            tileSprite.Size = tileSprite.Texture.Size * 2 * Math.Min(1, 20 / max);
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;

            background.FadeColour(isSelected ? selected_colour : background_colour, 150, Easing.OutQuint);
        }

        protected override bool OnHover(HoverEvent e)
        {
            hoverBox.FadeColour(hover_color, 150, Easing.OutQuint);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            base.OnHoverLost(e);
            hoverBox.FadeColour(hover_color.Opacity(0f), 150, Easing.OutQuint);
        }

        protected override bool OnClick(ClickEvent e)
        {
            Action?.Invoke(Tile, Building);
            return base.OnClick(e);
        }
    }
}
