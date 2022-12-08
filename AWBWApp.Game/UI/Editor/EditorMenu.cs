using AWBWApp.Game.Editor;
using AWBWApp.Game.UI.Editor.Components;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Editor
{
    public partial class EditorMenu : Container
    {
        private EditorDetachedDropdown<SymmetryMode> symmetryModeDropdown;
        private EditorDetachedDropdown<SymmetryDirection> symmetryDirectionDropdown;
        private DropdownHeader symmetryDirectionHeader;

        public EditorMenu()
        {
            RelativeSizeAxes = Axes.Both; //Todo: Do we need to set size?

            symmetryModeDropdown = new EditorDetachedDropdown<SymmetryMode>()
            {
                Anchor = Anchor.BottomRight,
                Origin = Anchor.BottomLeft,
                Width = 150,
                OffsetHeight = -Height - Padding.Bottom
            };
            symmetryDirectionDropdown = new EditorDetachedDropdown<SymmetryDirection>()
            {
                Anchor = Anchor.BottomRight,
                Origin = Anchor.BottomLeft,
                Width = 150,
                OffsetHeight = Height - Padding.Bottom
            };

            Children = new Drawable[]
            {
                new EditorHotbar()
                {
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Position = new Vector2(0, -10),
                },
                new Container()
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    Position = new Vector2(0, -10),
                    Width = 170,
                    Height = 250,
                    Children = new Drawable[]
                    {
                        new Container()
                        {
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft,
                            Masking = true,
                            CornerRadius = 6,
                            AutoSizeAxes = Axes.Y,
                            RelativeSizeAxes = Axes.X,
                            Children = new Drawable[]
                            {
                                new Box()
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = Color4.Black.Opacity(0.4f)
                                },
                                new FillFlowContainer()
                                {
                                    AutoSizeAxes = Axes.Y,
                                    RelativeSizeAxes = Axes.X,
                                    Direction = FillDirection.Vertical,
                                    AutoSizeEasing = Easing.OutQuint,
                                    AutoSizeDuration = 100,
                                    Margin = new MarginPadding { Vertical = 5 },
                                    Children = new Drawable[]
                                    {
                                        symmetryDirectionHeader = symmetryDirectionDropdown.GetDetachedHeader(),
                                        symmetryModeDropdown.GetDetachedHeader(),
                                    }
                                },
                            }
                        },
                        symmetryModeDropdown,
                        symmetryDirectionDropdown,
                    }
                }
            };

            symmetryDirectionHeader.Hide();
        }

        [BackgroundDependencyLoader]
        private void load(Bindable<SymmetryMode> symmetryMode, Bindable<SymmetryDirection> symmetryDirection)
        {
            symmetryModeDropdown.Current.BindTo(symmetryMode);
            symmetryModeDropdown.Current.BindValueChanged(x => symmetryModeChanged(x.NewValue));
            symmetryModeDropdown.Items = new[]
            {
                SymmetryMode.None,
                SymmetryMode.Mirror,
                SymmetryMode.MirrorInverted
            };

            symmetryDirectionDropdown.Current.BindTo(symmetryDirection);
            symmetryDirectionDropdown.Items = new[]
            {
                SymmetryDirection.Vertical,
                SymmetryDirection.UpwardsDiagonal,
                SymmetryDirection.Horizontal,
                SymmetryDirection.DownwardsDiagonal
            };
        }

        private void symmetryModeChanged(SymmetryMode newMode)
        {
            if (newMode != SymmetryMode.None)
                symmetryDirectionHeader.Show();
            else
                symmetryDirectionHeader.Hide();
        }
    }
}
