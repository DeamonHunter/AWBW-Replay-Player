using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;

namespace AWBWApp.Game.Input
{
    public class GlobalActionContainer : KeyBindingContainer<AWBWGlobalAction>
    {
        private readonly Drawable handler;
        private InputManager parentInputManager;

        public GlobalActionContainer(AWBWAppGameBase game)
            : base(SimultaneousBindingMode.Unique)
        {
            if (game is IKeyBindingHandler<AWBWGlobalAction>)
                handler = game;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            parentInputManager = GetContainingInputManager();
        }

        public override IEnumerable<IKeyBinding> DefaultKeyBindings =>
            new IKeyBinding[]
            {
                new KeyBinding(InputKey.H, AWBWGlobalAction.PreviousTurn),
                new KeyBinding(InputKey.J, AWBWGlobalAction.PreviousAction),
                new KeyBinding(InputKey.K, AWBWGlobalAction.NextAction),
                new KeyBinding(InputKey.L, AWBWGlobalAction.NextTurn),
                new KeyBinding(InputKey.G, AWBWGlobalAction.ShowGridLines),
                new KeyBinding(InputKey.F, AWBWGlobalAction.ShowUnitsInFog),
            };

        protected override IEnumerable<Drawable> KeyBindingInputQueue
        {
            get
            {
                var inputQueue = parentInputManager?.NonPositionalInputQueue ?? base.KeyBindingInputQueue;

                return handler != null ? inputQueue.Prepend(handler) : inputQueue;
            }
        }
    }
}
