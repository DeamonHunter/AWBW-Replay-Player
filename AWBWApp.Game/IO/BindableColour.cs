using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics;

namespace AWBWApp.Game.IO
{
    public class BindableColour : Bindable<Colour4>
    {
        public override void Parse(object input)
        {
            if (input is string colour)
                Value = Colour4.FromHex(colour);
            else
                base.Parse(input);
        }

        public override string ToString(string format, IFormatProvider formatProvider) => Value.ToHex();

        protected override Bindable<Colour4> CreateInstance() => new BindableColour();
    }
}
