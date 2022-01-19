using AWBWApp.Game.UI.Select;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Screens;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game
{
    public class MainScreen : Screen
    {
        private Screen replayScreen;

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                new Box
                {
                    Colour = Color4.Violet,
                    RelativeSizeAxes = Axes.Both,
                },
                new SpriteText
                {
                    Y = 20,
                    Text = "Main Screen",
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Font = FontUsage.Default.With(size: 40)
                },
                new SpinningBox
                {
                    Anchor = Anchor.Centre,
                },
                new BasicButton()
                {
                    BackgroundColour = Color4.AliceBlue,
                    Colour = Color4.Green,
                    Size = new Vector2(80, 40),
                    Y = 40,
                    X = 80,
                    Text = "Song Select",
                    Action = () => this.Push(consumeReplaySelect())
                }
            };

            preLoadReplaySelect();
        }

        public override void OnResuming(IScreen last)
        {
            base.OnResuming(last);

            preLoadReplaySelect();
        }

        private void preLoadReplaySelect()
        {
            if (replayScreen == null)
                LoadComponentAsync(replayScreen = new ReplaySelectScreen());
        }

        private Screen consumeReplaySelect()
        {
            var rs = replayScreen;
            replayScreen = null;
            return rs;
        }
    }
}
