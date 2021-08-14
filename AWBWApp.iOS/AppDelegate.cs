// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Foundation;
using osu.Framework.iOS;
using AWBWApp.Game;

namespace AWBWApp.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : GameAppDelegate
    {
        protected override osu.Framework.Game CreateGame() => new AWBWAppGame();
    }
}
