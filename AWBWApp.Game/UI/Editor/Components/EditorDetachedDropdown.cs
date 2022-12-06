using System;
using AWBWApp.Game.UI.Components;
using AWBWApp.Game.UI.Toolbar;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Layout;
using osu.Framework.Localisation;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Editor.Components
{
    /// <summary>
    /// A recreation of <see cref="Dropdown{T}"/> but this allows the header to not be a part of this drawable.
    /// </summary>
    public partial class EditorDetachedDropdown<T> : HeaderDetachedDropdown<T>
    {
        public float OffsetHeight { set => ((EditorDropdownMenu)Menu).OffsetHeight = value; }

        protected override DropdownHeader CreateDetachedHeader() => Header = new EditorDropdownHeader();

        public override DropdownHeader GetDetachedHeader()
        {
            if (Header != null)
                return Header;

            base.GetDetachedHeader();
            Header.Action += () => ((EditorDropdownMenu)Menu).ScrollIntoView(SelectedItem);

            return Header;
        }

        protected override DropdownMenu CreateMenu() =>
            new EditorDropdownMenu()
            {
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
                MaxHeight = 312,
            };

        public partial class EditorDropdownHeader : DropdownHeader
        {
            protected override LocalisableString Label
            {
                get => Text.Text;
                set => Text.Text = value;
            }

            protected readonly SpriteText Text;

            private const float minimum_width = 160;

            public EditorDropdownHeader()
            {
                Masking = true;
                CornerRadius = 6;
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;
                RelativeSizeAxes = Axes.None;
                AutoSizeAxes = Axes.Both;
                AutoSizeEasing = Easing.OutQuint;
                AutoSizeDuration = 300;

                BackgroundColour = Color4.Black.Opacity(0.4f);

                Foreground.RelativeSizeAxes = Axes.None;
                Foreground.AutoSizeAxes = Axes.Both;
                Foreground.Children = new Drawable[]
                {
                    new Container() //Spacer to set minimum Size
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(minimum_width, 35)
                    },
                    Text = new SpriteText()
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre
                    }
                };
            }
        }

        private partial class EditorDropdownMenu : DropdownMenu
        {
            public float OffsetHeight { get; set; }

            private readonly LayoutValue positionLayout = new LayoutValue(Invalidation.DrawInfo | Invalidation.RequiredParentSizeToFit);

            protected override Menu CreateSubMenu() => new AWBWSubMenu(this, null);

            protected override ScrollContainer<Drawable> CreateScrollContainer(Direction direction) => new BasicScrollContainer(direction);

            protected override DrawableDropdownMenuItem CreateDrawableDropdownMenuItem(MenuItem item) => new EditorDropdownMenuItem(item);

            public EditorDropdownMenu()
            {
                AddLayout(positionLayout);
            }

            protected override void Update()
            {
                base.Update();

                if (positionLayout.IsValid || State != MenuState.Open)
                    return;

                correctAnchor();
            }

            private void correctAnchor()
            {
                var inputManager = GetContainingInputManager();

                Vector2 menuBottomRight;
                if ((Origin & Anchor.y2) != 0)
                    menuBottomRight = ToSpaceOfOtherDrawable(new Vector2(DrawWidth, MaxHeight * Scale.Y + DrawSize.Y + OffsetHeight), inputManager);
                else
                    menuBottomRight = ToSpaceOfOtherDrawable(new Vector2(DrawWidth, MaxHeight * Scale.Y), inputManager);

                if (menuBottomRight.Y > inputManager.DrawSize.Y)
                {
                    Origin = switchAxisAnchors(Origin, Anchor.y0, Anchor.y2);
                    Parent.Parent.Y = 0;
                }
                else
                {
                    Origin = switchAxisAnchors(Origin, Anchor.y2, Anchor.y0);
                    Parent.Parent.Y = OffsetHeight;
                }

                positionLayout.Validate();

                static Anchor switchAxisAnchors(Anchor originalValue, Anchor toDisable, Anchor toEnable) => (originalValue & ~toDisable) | toEnable;
            }

            protected override void UpdateSize(Vector2 newSize)
            {
                Width = newSize.X;
                this.ResizeHeightTo(newSize.Y, 300, Easing.OutQuint);
            }

            public void ScrollIntoView(DropdownMenuItem<T> itemToScrollTo)
            {
                if (State == MenuState.Closed)
                    return;

                foreach (var item in DrawableMenuItems)
                {
                    if (item.Item != itemToScrollTo)
                        continue;

                    var position = ContentContainer.Current + ContentContainer.GetChildPosInContent(item);
                    position = MathF.Max(0f, Math.Min(position - MaxHeight * 0.5f, ContentContainer.AvailableContent));

                    ContentContainer.ScrollTo(position, false);
                    break;
                }
            }

            protected override void AnimateOpen()
            {
                correctAnchor();
                this.FadeIn(300, Easing.OutQuint);
            }

            protected override void AnimateClose()
            {
                this.FadeOut(300, Easing.OutQuint);
            }

            private partial class EditorDropdownMenuItem : DrawableDropdownMenuItem
            {
                private DrawableAWBWMenuItem.InnerMenuContainer innerMenu;

                public EditorDropdownMenuItem(MenuItem item)
                    : base(item)
                {
                    Anchor = Anchor.CentreLeft;
                    Origin = Anchor.CentreLeft;

                    BackgroundColour = new Color4(40, 40, 40, 255);
                    BackgroundColourHover = new Color4(70, 70, 70, 255);
                    BackgroundColourSelected = new Color4(55, 55, 55, 255);
                }

                protected override bool OnHover(HoverEvent e)
                {
                    updateState();
                    return base.OnHover(e);
                }

                protected override void OnHoverLost(HoverLostEvent e)
                {
                    updateState();
                    base.OnHoverLost(e);
                }

                private void updateState()
                {
                    Alpha = Item.Action.Disabled ? 0.2f : 1f;

                    if (IsHovered && !Item.Action.Disabled)
                    {
                        innerMenu.BoldText.FadeIn(80, Easing.OutQuint);
                        innerMenu.NormalText.FadeOut(80, Easing.OutQuint);
                    }
                    else
                    {
                        innerMenu.BoldText.FadeOut(80, Easing.OutQuint);
                        innerMenu.NormalText.FadeIn(80, Easing.OutQuint);
                    }
                }

                protected override Drawable CreateContent() => innerMenu = new DrawableAWBWMenuItem.InnerMenuContainer();
            }
        }
    }
}
