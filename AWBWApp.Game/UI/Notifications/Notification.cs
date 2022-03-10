using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Notifications
{
    public abstract class Notification : Container
    {
        public event Action Closed;

        public abstract LocalisableString Text { get; set; }

        public virtual bool IsImportant => true;

        public virtual bool Read { get; set; }

        public Func<bool> Activated;

        public virtual bool DisplayOnTop => true;

        public bool WasClosed;

        protected override Container<Drawable> Content => content;
        private readonly Container content;
        private readonly NotificationCloseButton closeButton;

        protected Container NotificationContent;

        protected NotificationLight Light;

        protected Notification()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            AddRangeInternal(new Drawable[]
            {
                NotificationContent = new Container()
                {
                    Masking = true,
                    CornerRadius = 8,
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    AutoSizeDuration = 400,
                    AutoSizeEasing = Easing.OutQuint,
                    Children = new Drawable[]
                    {
                        new Box()
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.White
                        },
                        Light = new NotificationLight()
                        {
                            Margin = new MarginPadding { Left = 8, Vertical = 8 },
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft
                        },
                        content = new Container()
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Padding = new MarginPadding { Horizontal = 20, Vertical = 5 }
                        },
                        closeButton = new NotificationCloseButton()
                        {
                            Alpha = 0,
                            Action = Close,
                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.CentreRight,
                            Margin = new MarginPadding { Right = 5 }
                        }
                    }
                }
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            this.FadeInFromZero(200);
            NotificationContent.MoveToX(DrawSize.X).MoveToX(0, 500, Easing.OutQuint);
        }

        protected override bool OnHover(HoverEvent e)
        {
            closeButton.FadeIn(75);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            closeButton.FadeOut(75);
            base.OnHoverLost(e);
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (Activated?.Invoke() ?? true)
                Close();

            return base.OnClick(e);
        }

        public virtual void Close()
        {
            if (WasClosed) return;
            WasClosed = true;

            Closed?.Invoke();
            this.FadeOut(100);
            Expire();
        }

        private class NotificationCloseButton : ClickableContainer
        {
            public NotificationCloseButton()
            {
                Colour = new Color4(40, 40, 40, 255);
                AutoSizeAxes = Axes.Both;

                Children = new Drawable[]
                {
                    new SpriteIcon()
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Icon = FontAwesome.Solid.TimesCircle,
                        Size = new Vector2(20)
                    }
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                this.FadeColour(new Color4(239, 155, 20, 255), 200);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                this.FadeColour(new Color4(40, 40, 40, 255), 200);
                base.OnHoverLost(e);
            }
        }

        public class NotificationLight : Container
        {
            public NotificationLight()
            {
                Size = new Vector2(6, 25);
                Masking = true;
                CornerRadius = 3;

                Children = new[]
                {
                    new Box()
                    {
                        RelativeSizeAxes = Axes.Both
                    }
                };
            }
        }
    }
}
