using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.UI.Toolbar;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Components
{
    public class TeamOrPlayerDropdown : BasicDropdown<object>
    {
        public string Prefix
        {
            get => header.Prefix;
            set => header.Prefix = value;
        }

        private TeamOrPlayerHeader header;

        public void SetDropdownItems(Dictionary<long, PlayerInfo> players, bool teamGame)
        {
            ClearItems();

            AddDropdownItem(teamGame ? "Active Team" : "Active Player", "");

            var knownTeams = new HashSet<string>();

            foreach (var player in players)
            {
                if (teamGame)
                {
                    if (knownTeams.Contains(player.Value.Team))
                        continue;

                    AddDropdownItem($"Team {player.Value.Team}", player.Value.Team);
                    knownTeams.Add(player.Value.Team);
                }
                else
                    AddDropdownItem($"{player.Value.Username ?? $"[Unknown Username:{player.Value.UserID}]"}", player.Value.ID);
            }

            Current.Value = "";
        }

        protected override DropdownHeader CreateHeader() =>
            header = new TeamOrPlayerHeader()
            {
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
            };

        protected override DropdownMenu CreateMenu() =>
            new TeamOrPlayerDropdownMenu()
            {
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
                MaxHeight = 312,
            };

        private class TeamOrPlayerHeader : DropdownHeader
        {
            public string Prefix { get; set; }

            protected readonly TextFlowContainer Text;

            private LocalisableString text;

            protected override LocalisableString Label
            {
                get => text;
                set => setText(value);
            }

            public TeamOrPlayerHeader()
            {
                Masking = true;
                CornerRadius = 6;
                RelativeSizeAxes = Axes.None;
                AutoSizeAxes = Axes.Y;
                RelativeSizeAxes = Axes.X;

                BackgroundColour = new Color4(20, 20, 20, 255);

                Foreground.AutoSizeAxes = Axes.Y;
                Foreground.RelativeSizeAxes = Axes.X;
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
                        AutoSizeAxes = Axes.Y,
                        RelativeSizeAxes = Axes.X,
                        Padding = new MarginPadding { Horizontal = 5 }
                    }
                };
            }

            private void setText(LocalisableString localisable)
            {
                if (text == localisable)
                    return;

                text = localisable;
                Text.Text = $"{Prefix}{localisable}";
            }
        }

        private class TeamOrPlayerDropdownMenu : DropdownMenu
        {
            protected override osu.Framework.Graphics.UserInterface.Menu CreateSubMenu() => new AWBWSubMenu(this, null);

            protected override ScrollContainer<Drawable> CreateScrollContainer(Direction direction) => new BasicScrollContainer(direction);

            protected override DrawableDropdownMenuItem CreateDrawableDropdownMenuItem(MenuItem item) => new ReplayBarDropdownMenuItem(item);

            public TeamOrPlayerDropdownMenu()
            {
                Anchor = Anchor.BottomCentre;
                Origin = Anchor.BottomCentre;
            }

            protected override void UpdateSize(Vector2 newSize)
            {
                Width = newSize.X;
                this.ResizeHeightTo(newSize.Y, 300, Easing.OutQuint);
            }

            protected override void AnimateOpen()
            {
                this.FadeIn(300, Easing.OutQuint);
            }

            protected override void AnimateClose()
            {
                this.FadeOut(300, Easing.OutQuint);
            }

            private class ReplayBarDropdownMenuItem : DrawableDropdownMenuItem
            {
                private DrawableAWBWMenuItem.InnerMenuContainer innerMenu;

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
