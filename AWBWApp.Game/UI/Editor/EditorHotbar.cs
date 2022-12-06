using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Replay;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace AWBWApp.Game.UI.Editor
{
    public partial class EditorHotbar : Container
    {
        private FillFlowContainer<SpriteBox> hotbar;

        [Resolved]
        private Bindable<TerrainTile> selectedTile { get; set; }

        public EditorHotbar()
        {
            AutoSizeAxes = Axes.X;
            Height = 60;

            Masking = true;
            CornerRadius = 6;
            Children = new Drawable[]
            {
                new Box()
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(25, 25, 25, 180)
                },
                hotbar = new FillFlowContainer<SpriteBox>()
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    AutoSizeAxes = Axes.X,
                    RelativeSizeAxes = Axes.Y,
                    Direction = FillDirection.Horizontal,
                    Padding = new MarginPadding { Horizontal = 4 }
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load(TerrainTileStorage tileStorage)
        {
            var data = new List<(TerrainTile, Key)>()
            {
                (tileStorage.GetTileByAWBWId(1), Key.Number1),
                (tileStorage.GetTileByAWBWId(2), Key.Number2),
                (tileStorage.GetTileByAWBWId(3), Key.Number3),
                (tileStorage.GetTileByAWBWId(28), Key.Number4),
                (tileStorage.GetTileByAWBWId(29), Key.Number5),
                (tileStorage.GetTileByAWBWId(33), Key.Number6),
                (tileStorage.GetTileByAWBWId(4), Key.Number7),
                (tileStorage.GetTileByAWBWId(5), Key.Number8),
                (tileStorage.GetTileByAWBWId(15), Key.Number9),
                (tileStorage.GetTileByAWBWId(16), Key.Number0),
            };

            foreach (var pair in data)
            {
                hotbar.Add(new SpriteBox()
                {
                    Tile = pair.Item1,
                    KeyToReactTo = pair.Item2,
                    Action = selectTile
                });
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            selectedTile.BindValueChanged(_ => onSelectedTileChanged(), true);
        }

        private void selectTile(TerrainTile tile)
        {
            selectedTile.Value = tile;
        }

        private void onSelectedTileChanged()
        {
            foreach (var box in hotbar.Children)
                box.SetSelected(box.Tile == selectedTile.Value);
        }

        private partial class SpriteBox : CompositeDrawable
        {
            public Action<TerrainTile> Action;

            private Key keyToReactTo;

            public Key KeyToReactTo
            {
                get => keyToReactTo;
                set
                {
                    keyToReactTo = value;

                    var fullString = keyToReactTo.ToString();
                    numberText.Text = fullString.Substring(fullString.Length - 1); //Todo: Less hacky way of doing this
                }
            }

            private TerrainTile tile;

            public TerrainTile Tile
            {
                get => tile;
                set
                {
                    if (tile == value)
                        return;

                    tile = value;
                    updateVisual();
                }
            }

            private SpriteText numberText;
            private SpriteText numberTextShadow;
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

            public SpriteBox()
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
                    },
                    numberText = new TextureSpriteText("UI/Healthv2")
                    {
                        Anchor = Anchor.BottomRight,
                        Origin = Anchor.BottomRight,
                        Position = new Vector2(-2, -2),
                        Font = new FontUsage(size: 4f),
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
                if (textureStore == null || tile == null)
                    return;

                tileSprite.Texture = textureStore.Get($"Map/{currentSkin.Value}/{tile.Textures[WeatherType.Clear]}");
                tileSprite.Size = tileSprite.Texture.Size * 2;
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
                Action?.Invoke(Tile);
                return base.OnClick(e);
            }

            protected override bool OnKeyDown(KeyDownEvent e)
            {
                if (e.Key == keyToReactTo)
                {
                    Action?.Invoke(Tile);
                    return true;
                }

                return base.OnKeyDown(e);
            }
        }
    }
}
