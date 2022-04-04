using System;
using System.Collections.Generic;
using AWBWApp.Game.UI.Notifications;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Framework.Threading;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Toolbar
{
    public class AWBWMenuBar : OverlayContainer
    {
        protected override bool BlockPositionalInput => false;

        public AWBWMenu Menu;

        public float HideDelay = 1000;
        public float HoverShowDelay = 250;

        private ScheduledDelegate hoverHideDelegate;
        private ScheduledDelegate hoverShowDelegate;

        public override bool PropagateNonPositionalInputSubTree => true;
        public override bool PropagatePositionalInputSubTree => true;

        private NotificationOverlay notificationOverlay;

        private NotificationButton notificationButton;

        private HoverDrawable hoverDrawable;

        public AWBWMenuBar(IReadOnlyList<MenuItem> menuItems, NotificationOverlay overlay)
        {
            notificationOverlay = overlay;

            RelativeSizeAxes = Axes.Both;

            Children = new Drawable[]
            {
                hoverDrawable = new HoverDrawable(updateHoverShow, onHoverLost, updateHoverShow)
                {
                    RelativeSizeAxes = Axes.X,
                    Size = new Vector2(1, 35),
                    AlwaysPresent = true
                },
                new FillFlowContainer()
                {
                    Direction = FillDirection.Vertical,
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        Menu = new AWBWMenu
                        {
                            Items = menuItems,
                            RelativeSizeAxes = Axes.X
                        },
                        overlay
                    }
                },
                notificationButton = new NotificationButton()
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Width = 140,
                    Action = notificationOverlay.ToggleVisibility
                }
            };

            notificationOverlay.State.BindValueChanged(x =>
            {
                if (x.NewValue == Visibility.Visible)
                    Show();
            });

            notificationOverlay.UnreadCount.BindValueChanged(x =>
            {
                notificationButton.UnreadNotifications = x.NewValue;

                if (State.Value == Visibility.Hidden)
                {
                    notificationButton.FadeIn(300, Easing.OutQuint);
                    notificationButton.ScaleTo(1, 300, Easing.OutQuint);
                }
            });
        }

        protected override void Update()
        {
            base.Update();

            if (State.Value != Visibility.Visible)
            {
                if (notificationButton.IsHovered && hoverShowDelegate == null)
                    hoverShowDelegate = Scheduler.AddDelayed(Show, HoverShowDelay);

                return;
            }

            if (hoverDrawable.IsHovered || Menu.IsActive || notificationOverlay.State.Value == Visibility.Visible)
            {
                if (hoverHideDelegate != null)
                {
                    hoverHideDelegate.Cancel();
                    hoverHideDelegate = null;
                }
                return;
            }

            if (hoverHideDelegate == null || hoverHideDelegate.Completed)
                hoverHideDelegate = Scheduler.AddDelayed(Hide, HideDelay);
        }

        protected override void PopIn()
        {
            Menu.FadeIn(300, Easing.OutQuint);
            Menu.ScaleTo(Vector2.One, 300, Easing.OutQuint);

            notificationButton.FadeIn(300, Easing.OutQuint);
            notificationButton.ScaleTo(1, 300, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            Menu.FadeOut(300, Easing.OutQuint);
            Menu.ScaleTo(new Vector2(1, 0), 300, Easing.OutQuint);

            notificationButton.FadeOut(300, Easing.OutQuint);
            notificationButton.ScaleTo(new Vector2(1, 0), 300, Easing.OutQuint);
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (notificationOverlay.State.Value == Visibility.Visible)
                notificationOverlay.Hide();

            return base.OnClick(e);
        }

        private void onHoverLost()
        {
            if (hoverShowDelegate == null)
                return;

            hoverShowDelegate?.Cancel();
            hoverShowDelegate = null;
        }

        private void updateHoverShow()
        {
            hoverShowDelegate?.Cancel();
            hoverShowDelegate = null;

            hoverHideDelegate?.Cancel();
            hoverHideDelegate = null;

            if (State.Value != Visibility.Visible)
                hoverShowDelegate = Scheduler.AddDelayed(Show, HoverShowDelay);
        }

        private class HoverDrawable : Drawable
        {
            private readonly Action onHover;
            private readonly Action onHoverLost;
            private readonly Action onMouseMove;

            public HoverDrawable(Action onHover, Action onHoverLost, Action onMouseMove)
            {
                this.onHover = onHover;
                this.onHoverLost = onHoverLost;
                this.onMouseMove = onMouseMove;
            }

            protected override bool OnHover(HoverEvent e)
            {
                onHover();
                return base.OnHover(e);
            }

            protected override bool OnMouseMove(MouseMoveEvent e)
            {
                onMouseMove();
                return base.OnMouseMove(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                onHoverLost();
                base.OnHoverLost(e);
            }
        }

        private class NotificationButton : Button
        {
            public LocalisableString Text
            {
                get => text.Text;
                set => text.Text = value;
            }

            public int UnreadNotifications
            {
                get => unreadNotifcations;
                set
                {
                    if (unreadNotifcations == value) return;

                    unreadNotifcations = value;
                    updateNotifications();
                }
            }

            private int unreadNotifcations;

            private BasicButton button;

            private Box backgroundBox;
            private Box hoverBox;
            private SpriteText text;

            private CircularContainer countContainer;
            private SpriteText countText;

            public NotificationButton()
            {
                Height = 26;
                Children = new Drawable[]
                {
                    backgroundBox = new Box()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(40, 40, 40, 255)
                    },
                    hoverBox = new Box()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(70, 70, 70, 255)
                    },
                    new FillFlowContainer()
                    {
                        Origin = Anchor.TopRight,
                        Anchor = Anchor.TopRight,
                        RelativeSizeAxes = Axes.Y,
                        AutoSizeAxes = Axes.X,
                        Direction = FillDirection.Horizontal,
                        Padding = new MarginPadding { Left = 3, Right = 10 },
                        Spacing = new Vector2(10, 0),
                        Children = new Drawable[]
                        {
                            new SpriteText()
                            {
                                Origin = Anchor.CentreRight,
                                Anchor = Anchor.CentreRight,
                                Text = "Notifications"
                            },
                            countContainer = new CircularContainer()
                            {
                                Masking = true,
                                Origin = Anchor.CentreRight,
                                Anchor = Anchor.CentreRight,
                                Size = new Vector2(24),
                                Children = new Drawable[]
                                {
                                    new Box()
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = new Color4(150, 20, 20, 255),
                                    },
                                    countText = new SpriteText()
                                    {
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Colour = Color4.White,
                                        Text = "0"
                                    }
                                }
                            },
                        }
                    }
                };

                Enabled.BindValueChanged(enabledChanged);
                updateNotifications();
            }

            private void updateNotifications()
            {
                if (unreadNotifcations == 0)
                {
                    countContainer.ScaleTo(new Vector2(0, 1), 200, Easing.InCubic);
                    return;
                }

                countText.Text = unreadNotifcations >= 99 ? "99+" : unreadNotifcations.ToString();
                countText.ScaleTo(new Vector2(1, 1.5f)).ScaleTo(1, 200, Easing.OutCubic);
                countContainer.ScaleTo(1, 500, Easing.OutQuint);
            }

            protected override bool OnClick(ClickEvent e)
            {
                if (Enabled.Value)
                    backgroundBox.FlashColour(Color4.White, 200);
                return base.OnClick(e);
            }

            protected override bool OnHover(HoverEvent e)
            {
                if (Enabled.Value)
                    hoverBox.FadeIn(200);

                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                base.OnHoverLost(e);
                hoverBox.FadeOut(200);
            }

            private void enabledChanged(ValueChangedEvent<bool> e) => this.FadeColour((e.NewValue ? Color4.White : Color4.Gray), 200, Easing.OutQuint);
        }
    }
}
