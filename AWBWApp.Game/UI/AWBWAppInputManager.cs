// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input;
using osuTK.Input;

namespace AWBWApp.Game.UI
{
    public class AWBWAppUserInputManager : UserInputManager
    {
        internal AWBWAppUserInputManager()
        {
        }

        protected override MouseButtonEventManager CreateButtonEventManagerFor(MouseButton button)
        {
            switch (button)
            {
                case MouseButton.Middle:
                case MouseButton.Right:
                    return new DragableMouseManager(button);
            }

            return base.CreateButtonEventManagerFor(button);
        }

        private class DragableMouseManager : MouseButtonEventManager
        {
            public DragableMouseManager(MouseButton button)
                : base(button)
            {
            }

            public override bool EnableDrag => true; // allow right-mouse dragging for absolute scroll in scroll containers.
            public override bool EnableClick => false;
            public override bool ChangeFocusOnClick => false;
        }
    }
}
