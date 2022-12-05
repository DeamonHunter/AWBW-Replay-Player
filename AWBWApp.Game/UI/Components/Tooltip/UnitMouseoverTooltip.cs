using System.Collections.Generic;
using AWBWApp.Game.Game.Country;
using AWBWApp.Game.Game.Units;
using AWBWApp.Game.Helpers;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Animations;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Components.Tooltip
{
    /// <summary>
    /// Recreation of <see cref="TooltipContainer.Tooltip"/> which sets the tooltip to our colours
    /// </summary>
    public partial class UnitMouseoverTooltip : VisibilityContainer, ITooltip<UnitMouseoverTooltip.UnitMouseOverInfo>
    {
        private FillFlowContainer<UnitCollection> units;
        private Dictionary<UnitData, (int, long)> prevUnitCounts = new Dictionary<UnitData, (int, long)>();
        private bool prevShowValue;
        private float prevValuePercentage;

        [Resolved]
        private UnitStorage unitStorage { get; set; }

        public UnitMouseoverTooltip()
        {
            Alpha = 0;
            AutoSizeAxes = Axes.Both;

            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(40, 40, 40, 255),
                },
                units = new FillFlowContainer<UnitCollection>()
                {
                    AutoSizeAxes = Axes.Both,
                    Padding = new MarginPadding(5),
                    MaximumSize = new Vector2(200, float.MaxValue)
                }
            };
        }

        private bool updateContent(UnitMouseOverInfo content, out Dictionary<UnitData, (int, long)> unitCounts)
        {
            unitCounts = new Dictionary<UnitData, (int, long)>();

            foreach (var unit in content.Units)
            {
                if (!unitCounts.TryGetValue(unit.UnitData, out var unitCount))
                    unitCount = (0, 0);

                unitCounts[unit.UnitData] = (unitCount.Item1 + 1, unitCount.Item2 + (long)(content.ValueMultiplier * unit.UnitData.Cost * unit.HealthPoints.Value / 10));
            }

            if (content.ShowValue != prevShowValue)
                return true;

            if (content.ValueMultiplier != prevValuePercentage)
                return true;

            foreach (var pair in unitCounts)
            {
                if (prevUnitCounts.TryGetValue(pair.Key, out var other) && other.Item1 == pair.Value.Item1 && other.Item2 == pair.Value.Item2)
                    continue;

                return true;
            }

            foreach (var pair in prevUnitCounts)
            {
                if (unitCounts.TryGetValue(pair.Key, out var other) && other == pair.Value && other.Item2 == pair.Value.Item2)
                    continue;

                return true;
            }

            return false;
        }

        public virtual void SetContent(UnitMouseOverInfo content)
        {
            if (!updateContent(content, out var unitCounts))
                return;

            prevUnitCounts = unitCounts;
            prevShowValue = content.ShowValue;
            prevValuePercentage = content.ValueMultiplier;

            units.Clear();

            if (content.Units.Count <= 0)
                return;

            var country = content.Units[0].Country;

            prevUnitCounts = unitCounts;
            units.Clear();

            foreach (var id in unitStorage.GetAllUnitIds())
            {
                var data = unitStorage.GetUnitByAWBWId(id);
                if (!unitCounts.TryGetValue(data, out var unitCount))
                    continue;

                units.Add(new UnitCollection(data, country, content.ShowValue ? unitCount.Item2 : unitCount.Item1));
            }
        }

        public virtual void Refresh() { }

        /// <summary>
        /// Called whenever the tooltip appears. When overriding do not forget to fade in.
        /// </summary>
        protected override void PopIn() => this.FadeIn();

        /// <summary>
        /// Called whenever the tooltip disappears. When overriding do not forget to fade out.
        /// </summary>
        protected override void PopOut() => this.FadeOut();

        /// <summary>
        /// Called whenever the position of the tooltip changes. Can be overridden to customize
        /// easing.
        /// </summary>
        /// <param name="pos">The new position of the tooltip.</param>
        public virtual void Move(Vector2 pos) => Position = pos;

        public struct UnitMouseOverInfo
        {
            public List<DrawableUnit> Units;
            public float ValueMultiplier;
            public bool ShowValue;
        }

        private partial class UnitCollection : FillFlowContainer
        {
            private TextureAnimation animation;
            private UnitData unitData;
            private CountryData country;

            public UnitCollection(UnitData unit, CountryData country, long shownCount)
            {
                unitData = unit;
                this.country = country;

                Padding = new MarginPadding { Vertical = 2, Horizontal = 4 };
                AutoSizeAxes = Axes.X;
                Height = 20;
                Direction = FillDirection.Horizontal;

                Children = new Drawable[]
                {
                    animation = new TextureAnimation()
                    {
                        Size = new Vector2(16)
                    },
                    new SpriteText()
                    {
                        Text = $"x{shownCount}"
                    }
                };
            }

            [BackgroundDependencyLoader]
            private void load(TextureStore textureStore)
            {
                textureStore.LoadIntoAnimation($"{country.UnitPath}/{unitData.IdleAnimation.Texture}", animation, unitData.IdleAnimation.Frames, unitData.IdleAnimation.FrameOffset);
            }
        }
    }
}
