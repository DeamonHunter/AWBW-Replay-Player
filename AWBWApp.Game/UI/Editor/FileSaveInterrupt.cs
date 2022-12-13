using System;
using System.IO;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Editor.Components;
using AWBWApp.Game.UI.Interrupts;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace AWBWApp.Game.UI.Editor
{
    public partial class FileSaveInterrupt : BaseInterrupt
    {
        public const float ENTER_DURATION = 500;
        public const float EXIT_DURATION = 200;

        private readonly float animationBaseXOffset = -50f;

        private Action<string> onFileSelected;
        private readonly TextFlowContainer errorText;

        private ReplayMapFileSelector fileSelector;
        private BasicTextBox fileNameTextBox;

        public FileSaveInterrupt(string lastFile, Action<string> onFileSelected)
        {
            this.onFileSelected = onFileSelected;

            RelativeSizeAxes = Axes.Both;
            Children = new Drawable[]
            {
                new Box()
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4Extensions.FromHex(@"221a21"),
                },
                new GridContainer()
                {
                    RelativeSizeAxes = Axes.Both,
                    RowDimensions = new Dimension[]
                    {
                        new Dimension(mode: GridSizeMode.Absolute, size: 40f),
                        new Dimension(),
                        new Dimension(mode: GridSizeMode.Absolute, size: 50f),
                        new Dimension(mode: GridSizeMode.Absolute, size: 60f),
                        new Dimension(mode: GridSizeMode.Absolute, size: 60f),
                    },
                    Content = new Drawable[][]
                    {
                        new Drawable[]
                        {
                            new SpriteText()
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                RelativeSizeAxes = Axes.X,
                                Width = 0.95f,
                                Font = FontUsage.Default.With(size: 25),
                                Text = "Saving File"
                            }
                        },
                        new Drawable[]
                        {
                            new Container()
                            {
                                RelativeSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    new Box()
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = new Color4(20, 20, 20, 180)
                                    },
                                    fileSelector = new ReplayMapFileSelector(lastFile)
                                    {
                                        RelativeSizeAxes = Axes.Both
                                    }
                                }
                            }
                        },
                        new Drawable[]
                        {
                            fileNameTextBox = new BasicTextBox()
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                RelativeSizeAxes = Axes.X,
                                Height = 30,
                                Width = 0.95f,
                                PlaceholderText = "File Name",
                                Text = lastFile.IsNullOrEmpty() ? "Map.json" : Path.GetFileName(lastFile)
                            }
                        },
                        new Drawable[]
                        {
                            new Container()
                            {
                                RelativeSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    new InterruptButton
                                    {
                                        Text = "Cancel",
                                        BackgroundColour = Color4Extensions.FromHex(@"681d1f"),
                                        HoverColour = Color4Extensions.FromHex(@"681d1f").Lighten(0.2f),
                                        Action = Cancel,
                                        RelativePositionAxes = Axes.X,
                                        Position = new Vector2(-0.25f, 0f)
                                    },
                                    new InterruptButton
                                    {
                                        Text = "Save",
                                        BackgroundColour = Color4Extensions.FromHex(@"1d681e"),
                                        HoverColour = Color4Extensions.FromHex(@"1d681e").Lighten(0.2f),
                                        Action = () => Schedule(checkFileThenSend),
                                        RelativePositionAxes = Axes.X,
                                        Position = new Vector2(0.25f, 0f)
                                    }
                                }
                            }
                        },
                        new Drawable[]
                        {
                            errorText = new TextFlowContainer()
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                TextAnchor = Anchor.TopCentre,
                                RelativeSizeAxes = Axes.X,
                                Width = 0.95f,
                                Colour = Color4.Red
                            }
                        }
                    }
                },
            };

            fileSelector.CurrentFile.BindValueChanged(x => onDirectoryFileClicked(x.NewValue));
            fileNameTextBox.Current.BindValueChanged(x => onTextFieldChange(x.NewValue), true);
            Show();
        }

        private void onDirectoryFileClicked(FileInfo info)
        {
            if (info == null)
                return;

            fileNameTextBox.Text = info.Name;
        }

        private void onTextFieldChange(string newValue)
        {
            fileSelector.SetSelectionToFile(newValue);
        }

        private void checkFileThenSend()
        {
            if (fileNameTextBox.Text.IsNullOrEmpty())
            {
                errorText.Text = "Please input a file name.";
                return;
            }

            var directoryPath = fileSelector.CurrentPath.Value.FullName;
            var filePath = Path.Combine(directoryPath, fileNameTextBox.Text);

            //Todo: Throw warning if file exists

            onFileSelected.Invoke(filePath);
            ActionInvoked();
            Schedule(Hide);
        }

        protected override void PopIn()
        {
            base.PopIn();
            this.MoveToX(animationBaseXOffset).MoveToX(0, ENTER_DURATION, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            base.PopOut();
            this.MoveToX(0).MoveToX(animationBaseXOffset, ENTER_DURATION, Easing.OutQuint);
        }

        protected override bool OnClick(ClickEvent e)
        {
            return true;
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key == Key.Escape)
            {
                Cancel();
                return true;
            }

            return base.OnKeyDown(e);
        }

        private partial class InterruptButton : BasicButton
        {
            public InterruptButton()
            {
                Height = 50;
                RelativeSizeAxes = Axes.X;
                Width = 0.45f;
                Anchor = Anchor.TopCentre;
                Origin = Anchor.TopCentre;

                Margin = new MarginPadding { Top = 5 };
                BackgroundColour = Color4Extensions.FromHex(@"150e14");
                SpriteText.Font.With(size: 18);
            }
        }
    }
}
