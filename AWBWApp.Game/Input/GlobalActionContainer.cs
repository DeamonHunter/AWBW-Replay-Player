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
            {
                var trackedBinding = new CombinationTrackedKeyBinding(binding.KeyCombination, binding.Action, SetConfig);
                trackedBinding.Event.Invoke(trackedBinding.Action, trackedBinding.KeyCombination);
                bindings.Add(trackedBinding);
            }

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
                new CombinationTrackedKeyBinding(InputKey.H, AWBWGlobalAction.PreviousTurn, SetConfig),
                new CombinationTrackedKeyBinding(InputKey.J, AWBWGlobalAction.PreviousAction, SetConfig),
                new CombinationTrackedKeyBinding(InputKey.K, AWBWGlobalAction.NextAction, SetConfig),
                new CombinationTrackedKeyBinding(InputKey.L, AWBWGlobalAction.NextTurn, SetConfig),
                new CombinationTrackedKeyBinding(InputKey.G, AWBWGlobalAction.ShowGridLines, SetConfig),
                new CombinationTrackedKeyBinding(InputKey.F, AWBWGlobalAction.ShowUnitsAndBuildingsInFog, SetConfig),
                new CombinationTrackedKeyBinding(InputKey.C, AWBWGlobalAction.ShowTileCursor, SetConfig),
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
