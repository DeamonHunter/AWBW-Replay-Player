using AWBWApp.Game.UI.Interrupts;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Screens;

namespace AWBWApp.Game.UI.Editor.Interrupt
{
    public partial class ExitWithoutSavingInterrupt : SideInterupt
    {
        private readonly LoadingLayer blockingLayer;
        private readonly EditorScreen editorScreen;

        public ExitWithoutSavingInterrupt(EditorScreen editorScreen)
        {
            this.editorScreen = editorScreen;

            HeaderText = "Exit without Saving?";
            BodyText = "You haven't saved your latest changes!";

            SetInteractables(new Drawable[]
                {
                    new FillFlowContainer()
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Children = new Drawable[]
                        {
                            new InterruptButton
                            {
                                Text = "Save",
                                BackgroundColour = Color4Extensions.FromHex(@"1d681e"),
                                HoverColour = Color4Extensions.FromHex(@"1d681e").Lighten(0.2f),
                                Action = popUpSaveDialogue
                            },
                            new InterruptButton
                            {
                                Text = "Exit Without Saving",
                                BackgroundColour = Color4Extensions.FromHex(@"681d1f"),
                                HoverColour = Color4Extensions.FromHex(@"681d1f").Lighten(0.2f),
                                Action = closeEditor
                            },
                            new InterruptButton
                            {
                                Text = "Back To Editor",
                                BackgroundColour = Color4Extensions.FromHex(@"8e4012"),
                                HoverColour = Color4Extensions.FromHex(@"8e4012").Lighten(0.1f),
                                Action = Cancel
                            }
                        }
                    }
                }
            );

            Add(blockingLayer = new LoadingLayer(true)
            {
                RelativeSizeAxes = Axes.Both
            });
        }

        private void popUpSaveDialogue()
        {
            editorScreen.SaveMap(closeEditor);
        }

        private void closeEditor()
        {
            editorScreen.SetHasSaved();
            Schedule(() =>
            {
                editorScreen.Exit();
                ActionInvoked();
                Schedule(Hide);
            });
        }

        private partial class InterruptButton : BasicButton
        {
            public InterruptButton()
            {
                Height = 50;
                RelativeSizeAxes = Axes.X;
                Width = 0.9f;
                Anchor = Anchor.TopCentre;
                Origin = Anchor.TopCentre;

                Margin = new MarginPadding { Top = 5 };
                BackgroundColour = Color4Extensions.FromHex(@"150e14");
                SpriteText.Font.With(size: 18);
            }
        }
    }
}
