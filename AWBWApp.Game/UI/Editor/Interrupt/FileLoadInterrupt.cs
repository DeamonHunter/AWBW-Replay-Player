using System;
using System.IO;
using System.Threading.Tasks;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.UI.Editor.Components;
using AWBWApp.Game.UI.Interrupts;
using Newtonsoft.Json;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace AWBWApp.Game.UI.Editor.Interrupt
{
    public partial class FileLoadInterrupt : BaseInterrupt
    {
        public const float ENTER_DURATION = 500;
        public const float EXIT_DURATION = 200;

        private readonly float animationBaseXOffset = -50f;

        private TaskCompletionSource<ReplayMap> onFileOpened;
        private readonly TextFlowContainer errorText;

        private ReplayMapFileSelector fileSelector;

        public FileLoadInterrupt(TaskCompletionSource<ReplayMap> onFileOpened)
        {
            this.onFileOpened = onFileOpened;

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
                                Text = "Loading File"
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
                                    fileSelector = new ReplayMapFileSelector(null)
                                    {
                                        RelativeSizeAxes = Axes.Both
                                    }
                                }
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
                                        Text = "Open",
                                        BackgroundColour = Color4Extensions.FromHex(@"1d681e"),
                                        HoverColour = Color4Extensions.FromHex(@"1d681e").Lighten(0.2f),
                                        Action = () => Schedule(openSelectedFile),
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
                                Margin = new MarginPadding { Top = 5 },
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

            fileSelector.CurrentFile.BindValueChanged(x => onDirectoryFileClicked(x.NewValue.Name));
            Show();
        }

        private void onDirectoryFileClicked(string newValue)
        {
            fileSelector.SetSelectionToFile(newValue);
        }

        private void openSelectedFile()
        {
            if (fileSelector.CurrentFile.Value == null)
            {
                errorText.Text = "Please select a file.";
                return;
            }

            ReplayMap map;

            try
            {
                using (var file = File.Open(fileSelector.CurrentFile.Value.FullName, FileMode.Open))
                {
                    using (var sr = new StreamReader(file))
                        map = JsonConvert.DeserializeObject<ReplayMap>(sr.ReadToEnd());

                    if (map.Ids == null)
                        throw new Exception("Failed to deserialise file.");
                }
            }
            catch (Exception e)
            {
                errorText.Text = "Could not open file. The selected file may not be a map file.";
                Logger.Log(e.ToString());
                return;
            }

            onFileOpened.SetResult(map);
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

        protected override void Cancel()
        {
            base.Cancel();
            onFileOpened.SetCanceled();
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
