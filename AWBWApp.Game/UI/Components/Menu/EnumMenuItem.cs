using System;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Localisation;

namespace AWBWApp.Game.UI.Components.Menu
{
    public class EnumMenuItem<T> : MenuItem where T : Enum
    {
        public EnumMenuItem(LocalisableString text, Bindable<T> bindable)
            : base(text)
        {
            var genericBindable = new Bindable<object>(default(T));

            bindable.BindValueChanged(x =>
            {
                genericBindable.Value = x.NewValue;
            }, true);

            genericBindable.BindValueChanged(x =>
            {
                bindable.Value = (T)x.NewValue;
            });

            Items = ((T[])Enum.GetValues(typeof(T))).Select(x => new StatefulMenuItem(x.ToString(), genericBindable, x)).ToArray();
        }
    }

    public class StatefulMenuItem : MenuItem
    {
        public readonly Bindable<object> State;
        private readonly object valueToSet;

        public StatefulMenuItem(LocalisableString text, Bindable<object> bindable, object valueToSet)
            : base(text)
        {
            State = bindable;
            this.valueToSet = valueToSet;
            Action.Value = () =>
            {
                State.Value = valueToSet;
            };
        }

        public IconUsage? GetIconForState(object state) => state.Equals(valueToSet) ? (IconUsage?)FontAwesome.Solid.Check : null;
    }
}
