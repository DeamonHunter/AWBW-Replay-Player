using System;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Components;
using AWBWApp.Game.UI.Toolbar;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Layout;
using osu.Framework.Localisation;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Replay
{
    /// <summary>
    /// A recreation of <see cref="Dropdown{T}"/> but this allows the header to not be a part of this drawable.
    /// </summary>
    public class ReplayBarWidgetDropdown : HeaderDetachedDropdown<Turn>
    {
        public float OffsetHeight { set => ((ReplayBarDropdownMenu)Menu).OffsetHeight = value; }

        protected override DropdownHeader CreateDetachedHeader() => Header = new ReplayBarDownHeader();

        public override DropdownHeader GetDetachedHeader()
        {
            if (Header != null)
                return Header;

            base.GetDetachedHeader();
            Header.Action += () => ((ReplayBarDropdownMenu)Menu).ScrollIntoView(SelectedItem);

            return Header;
        }

        protected override DropdownMenu CreateMenu() =>
            new ReplayBarDropdownMenu()
            {
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
                MaxHeight = 312,
            };

        protected override LocalisableString GenerateItemText(Turn item)
        {
            return $"{item.Day} - {item.Player ?? $"[Unknown Username:{item.PlayerID}]"}";
        }

        public class ReplayBarDownHeader : DropdownHeader
        {
            protected readonly TextFlowContainer Text;

            private string text;

            protected override LocalisableString Label
            {
                get => text;
                set => setText(value);
            }

            public ReplayBarDownHeader()
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
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Size = new Vector2(125, 35)
                    },
                    Text = new TextFlowContainer()
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        TextAnchor = Anchor.Centre,
                        AutoSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Horizontal = 5 }
                    }
                };
            }

            private void setText(LocalisableString localisable)
            {
                var localisedText = localisable.ToString();

                if (localisedText.IsNullOrEmpty())
                {
                    text = "";
                    Text.Text = "";
                    return;
                }

                var splits = localisedText.Split(" - ");

                if (splits.Length != 2)
                    throw new Exception("Failed to split text.");

                text = $"Day {splits[0]}\n{splits[1]}";
                Text.Text = text;
            }
        }

        private class ReplayBarDropdownMenu : DropdownMenu
        {
            public float OffsetHeight { get; set; }

            private readonly LayoutValue positionLayout = new LayoutValue(Invalidation.DrawInfo | Invalidation.RequiredParentSizeToFit);

            protected override Menu CreateSubMenu() => new AWBWSubMenu(this, null);

            protected override ScrollContainer<Drawable> CreateScrollContainer(Direction direction) => new BasicScrollContainer(direction);

            protected override DrawableDropdownMenuItem CreateDrawableDropdownMenuItem(MenuItem item) => new ReplayBarDropdownMenuItem(item);

            public ReplayBarDropdownMenu()
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

            public void ScrollIntoView(DropdownMenuItem<Turn> itemToScrollTo)
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

            private class ReplayBarDropdownMenuItem : DrawableDropdownMenuItem
            {
                private DrawableAWBWMenuItem.TextContainer text;

                public ReplayBarDropdownMenuItem(MenuItem item)
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
                        text.BoldText.FadeIn(80, Easing.OutQuint);
                        text.NormalText.FadeOut(80, Easing.OutQuint);
                    }
                    else
                    {
                        text.BoldText.FadeOut(80, Easing.OutQuint);
                        text.NormalText.FadeIn(80, Easing.OutQuint);
                    }
                }

                protected override Drawable CreateContent() => text = new DrawableAWBWMenuItem.TextContainer();
            }
        }
    }

    public struct Turn
    {
        public int TurnIndex;
        public int Day;
        public string Player;
        public long PlayerID;
    }
}
