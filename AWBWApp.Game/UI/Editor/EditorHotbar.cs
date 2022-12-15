using System.Collections.Generic;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.UI.Editor.Components;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK.Graphics;
using osuTK.Input;

namespace AWBWApp.Game.UI.Editor
{
    public partial class EditorHotbar : Container
    {
        private FillFlowContainer<EditorSpriteButton> hotbar;

        [Resolved]
        private Bindable<TerrainTile> selectedTile { get; set; }

        [Resolved]
        private Bindable<BuildingTile> selectedBuilding { get; set; }

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
                hotbar = new FillFlowContainer<EditorSpriteButton>()
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Padding = new MarginPadding { Horizontal = 4 }
                },
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
                hotbar.Add(new EditorHotkeyButton()
                {
                    Tile = pair.Item1,
                    KeyToReactTo = pair.Item2,
                    Action = selectTile
                });
            }
        }

        public void SetHotbarSlot(int slot, TerrainTile tile, BuildingTile building)
        {
            var hotbarSlot = hotbar.Children[slot];
            hotbarSlot.Tile = tile;
            hotbarSlot.Building = building;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            selectedTile.BindValueChanged(_ => onSelectedTileChanged(), true);
            selectedBuilding.BindValueChanged(_ => onSelectedTileChanged(), true);
        }

        private void selectTile(TerrainTile tile, BuildingTile building)
        {
            selectedTile.Value = tile;
            selectedBuilding.Value = building;
        }

        private void onSelectedTileChanged()
        {
            foreach (var box in hotbar.Children)
                box.SetSelected(box.Tile == selectedTile.Value && box.Building == selectedBuilding.Value);
        }
    }
}
