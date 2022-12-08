using System;
using System.IO;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osuTK;

namespace AWBWApp.Game.UI.Editor.Components
{
    public partial class ReplayMapFileSelector : FileSelector
    {
        public ReplayMapFileSelector(string initialFile)
            : base(initialFile ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), new[] { ".json" })
        {
        }

        protected override DirectorySelectorBreadcrumbDisplay CreateBreadcrumb() => new BasicDirectorySelectorBreadcrumbDisplay();

        protected override Drawable CreateHiddenToggleButton() =>
            new BasicButton
            {
                Size = new Vector2(200, 25),
                Text = "Toggle hidden items",
                Action = ShowHiddenItems.Toggle
            };

        protected override DirectorySelectorDirectory CreateDirectoryItem(DirectoryInfo directory, string displayName = null) => new FixedDirectorySelectorDirectory(directory, displayName);

        protected override DirectorySelectorDirectory CreateParentDirectoryItem(DirectoryInfo directory) => new FixedDirectorySelectorDirectory(directory, "..");

        protected override ScrollContainer<Drawable> CreateScrollContainer() => new BasicScrollContainer();

        protected override DirectoryListingFile CreateFileItem(FileInfo file) => new StateFulDirectoryListingFile(file);

        protected override void NotifySelectionError()
        {
            this.FlashColour(Colour4.Red, 300);
        }

        private partial class StateFulDirectoryListingFile : DirectoryListingFile
        {
            private Colour4 selectedColour = new Colour4(100, 100, 100, 255);
            private Colour4 hoverColor = new Colour4(150, 150, 150, 255);

            private Box hover;
            private bool selected;

            public StateFulDirectoryListingFile(FileInfo file)
                : base(file)
            {
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                AutoSizeAxes = Axes.Y;
                RelativeSizeAxes = Axes.X;
                Width = 1;
                AddInternal(hover = new Box()
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = selectedColour
                });
            }

            protected override IconUsage? Icon => FontAwesome.Regular.File;

            protected override SpriteText CreateSpriteText() =>
                new SpriteText
                {
                    Font = FrameworkFont.Regular.With(size: FONT_SIZE)
                };

            protected override bool OnHover(HoverEvent e)
            {
                hover.FadeColour(hoverColor, 150, Easing.OutQuint).FadeTo(0.4f, 150, Easing.OutQuint);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                base.OnHoverLost(e);
                hover.FadeColour(selectedColour, 150, Easing.OutQuint).FadeTo(selected ? 0.4f : 0, 150, Easing.OutQuint);
            }
        }

        private partial class FixedDirectorySelectorDirectory : BasicDirectorySelectorDirectory
        {
            private Box hover;

            public FixedDirectorySelectorDirectory(DirectoryInfo directory, string displayName = null)
                : base(directory, displayName)
            {
            }

            protected override IconUsage? Icon =>
                Directory == null || Directory.Name.Contains(Path.DirectorySeparatorChar)
                    ? FontAwesome.Solid.Database
                    : FontAwesome.Regular.Folder;

            protected override void LoadComplete()
            {
                base.LoadComplete();
                AutoSizeAxes = Axes.Y;
                RelativeSizeAxes = Axes.X;
                Width = 1;
                AddInternal(hover = new Box()
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Colour4(150, 150, 150, 255),
                    Alpha = 0
                });
            }

            protected override bool OnHover(HoverEvent e)
            {
                hover.FadeTo(0.4f, 150, Easing.OutQuint);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                base.OnHoverLost(e);
                hover.FadeTo(0, 150, Easing.OutQuint);
            }
        }
    }
}
