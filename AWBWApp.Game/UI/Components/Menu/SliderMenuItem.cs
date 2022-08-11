using osu.Framework.Bindables;
using osu.Framework.Graphics.UserInterface;

namespace AWBWApp.Game.UI.Components.Menu
{
    public class SliderMenuItem : MenuItem
    {
        public readonly BindableNumber<float> State;

        private readonly Bindable<float> original; //Todo: is there a way to keep this original binding to configs around?

        public SliderMenuItem(Bindable<float> bindable, float min, float max, float defaultValue, float precision)
            : base(string.Empty)
        {
            original = bindable;
            State = new BindableNumber<float>();
            State.BindTo(original);
            State.MinValue = min;
            State.MaxValue = max;
            State.Precision = precision;
            State.Default = defaultValue;
        }

        public SliderMenuItem(BindableNumber<float> bindable)
            : base(string.Empty)
        {
            State = bindable;
        }
    }
}
