using System;
using System.Collections.Generic;
using AWBWApp.Game.Helpers;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Components
{
    public class HintBox : Container
    {
        public List<string> Tips = new List<string>()
        {
            "Changing a player's country or unit look direction can be done by right clicking the player in the list.",
            "The player list and replay control bar can have their sized changed by their right click menu.",
            "Holding down a replay control button will activate auto advance.",
            "You can view a players current lifetime stats by right clicking them.",
            "Hover near the top of the screen for more Replay Options, including rebinding keys",
            "You can jump to any turn by clicking on the current turn dropdown in the replay control bar.",
            "You can set the FoW to a specific player and turn off units showing in fog, for a more authentic experience."
        };

        private TextFlowContainer textFlowContainer;
        private int shownTip;

        public HintBox()
        {
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            AutoSizeAxes = Axes.Y;

            Masking = true;
            CornerRadius = 5;

            AutoSizeDuration = 75;
            AutoSizeEasing = Easing.OutQuint;

            var random = new Random();
            random.Shuffle(Tips);

            Children = new Drawable[]
            {
                new Box()
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(20, 20, 20, 160)
                },
                new IconButton()
                {
                    Margin = new MarginPadding { Horizontal = 5 },
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Icon = FontAwesome.Solid.AngleLeft,
                    Action = () => { setToNextTip(-1); }
                },
                textFlowContainer = new TextFlowContainer()
                {
                    Padding = new MarginPadding { Vertical = 5 },
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.X,
                    Width = 0.85f,
                    Height = 100, //Todo: May not be large enough.
                    TextAnchor = Anchor.TopCentre
                },
                new IconButton()
                {
                    Margin = new MarginPadding { Horizontal = 5 },
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Icon = FontAwesome.Solid.AngleRight,
                    Action = () => { setToNextTip(1); }
                }
            };

            ChangeTip(random.Next(Tips.Count));
        }

        private void setToNextTip(int direction)
        {
            var index = shownTip + direction;

            if (index < 0)
                index = Tips.Count - 1;
            if (index > Tips.Count - 1)
                index = 0;

            ChangeTip(index);
        }

        public void ChangeTip(int index)
        {
            shownTip = index;
            textFlowContainer.Text = "TIP:\n\n" + Tips[index];
        }
    }
}
