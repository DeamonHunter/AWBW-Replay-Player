// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osuTK.Graphics;
using osuTK.Input;

namespace AWBWApp.Game.Tests.Visual
{
    public partial class StepTextBox : BasicTextBox
    {
        public Action<string> ValueChanged;

        private readonly string defaultText;

        public StepTextBox(string description, string start)
        {
            // Styling
            Height = 25;
            RelativeSizeAxes = Axes.X;

            BackgroundUnfocused = Color4.RoyalBlue.Darken(0.75f);
            BackgroundFocused = Color4.RoyalBlue;

            Masking = true;

            PlaceholderText = description;
            defaultText = start;

            // Bind to the underlying sliderbar
            var currentNumber = Current;
            currentNumber.ValueChanged += x => ValueChanged?.Invoke(x.NewValue);
            Text = defaultText;
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            // Reset value via right click. This shouldn't happen if a drag (via left button) is in progress.
            if (!IsDragged && e.Button == MouseButton.Right)
            {
                Text = defaultText;
                Flash();
            }

            return base.OnMouseDown(e);
        }

        protected void Flash()
        {
            var flash = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = Color4.RoyalBlue,
                Blending = BlendingParameters.Additive,
                Alpha = 0.6f,
            };

            Add(flash);
            flash.FadeOut(200).Expire();
        }

        public override string ToString() => Text;
    }
}
