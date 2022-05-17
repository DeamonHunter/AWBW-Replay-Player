using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Platform;

namespace AWBWApp.Game.Input
{
    public class GlobalActionContainer : FileBasedKeyBindingContainer<AWBWGlobalAction>
    {
        private readonly Drawable handler;
        private InputManager parentInputManager;

        public GlobalActionContainer(AWBWAppGameBase game, Storage storage)
            : base(storage, SimultaneousBindingMode.Unique)
        {
            if (game is IKeyBindingHandler<AWBWGlobalAction>)
                handler = game;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            parentInputManager = GetContainingInputManager();
        }

        public void ResetToDefault()
        {
            var bindings = new List<IKeyBinding>();
            foreach (var binding in DefaultKeyBindings)
                bindings.Add(binding);

            KeyBindings = bindings;
        }

        public IKeyBinding GetKeyBindingForAction(AWBWGlobalAction action) => KeyBindings.First(x => (AWBWGlobalAction)x.Action == action);

        public void ClearKeyBindingsWithKeyCombination(KeyCombination combination)
        {
            foreach (var keyBinding in KeyBindings)
            {
                if (keyBinding.KeyCombination.Equals(combination))
                    keyBinding.KeyCombination = new KeyCombination("");
            }
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
