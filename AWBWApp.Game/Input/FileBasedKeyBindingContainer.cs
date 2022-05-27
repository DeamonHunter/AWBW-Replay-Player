using System;
using System.Collections.Generic;
using osu.Framework.Configuration;
using osu.Framework.Input.Bindings;
using osu.Framework.Platform;

namespace AWBWApp.Game.Input
{
    public abstract class FileBasedKeyBindingContainer<T> : KeyBindingContainer<T> where T : struct, Enum
    {
        private readonly KeyBindingConfigManager<T> config;

        protected FileBasedKeyBindingContainer(Storage storage, SimultaneousBindingMode simulMode = SimultaneousBindingMode.None, KeyCombinationMatchingMode comboMode = KeyCombinationMatchingMode.Any)
            : base(simulMode, comboMode)
        {
            config = new KeyBindingConfigManager<T>(storage);
        }

        protected override void ReloadMappings()
        {
            var keyBindings = new List<IKeyBinding>();

            foreach (var binding in DefaultKeyBindings)
            {
                var trackedBinding = new CombinationTrackedKeyBinding(config.Has((T)binding.Action) ? config.Get<string>((T)binding.Action) : binding.KeyCombination, binding.Action, SetConfig);
                trackedBinding.Event.Invoke(trackedBinding.Action, trackedBinding.KeyCombination);
                keyBindings.Add(trackedBinding);
            }

            KeyBindings = keyBindings;

            foreach (var binding in KeyBindings)
                config.SetValue((T)binding.Action, binding.KeyCombination.ToString());

            config.Save();
        }

        protected void SetConfig(object action, KeyCombination newCombination)
        {
            config.SetValue((T)action, newCombination.ToString());
        }

        protected override void Dispose(bool isDisposing)
        {
            if (IsDisposed)
                return;

            config?.Dispose();
            base.Dispose(isDisposing);
        }

        protected class CombinationTrackedKeyBinding : IKeyBinding
        {
            private KeyCombination keyCombination;

            public KeyCombination KeyCombination
            {
                get => keyCombination;
                set
                {
                    keyCombination = value;
                    Event?.Invoke(Action, keyCombination);
                }
            }

            public object Action { get; set; }

            public Action<object, KeyCombination> Event { get; private set; }

            /// <summary>
            /// Construct a new instance.
            /// </summary>
            /// <param name="keys">The combination of keys which will trigger this binding.</param>
            /// <param name="action">The resultant action which is triggered by this binding. Usually an enum type.</param>
            public CombinationTrackedKeyBinding(KeyCombination keys, object action, Action<object, KeyCombination> keyChangeEvent)
            {
                KeyCombination = keys;
                Action = action;
                Event = keyChangeEvent;
            }

            public override string ToString() => $"{KeyCombination}=>{Action}";
        }

        protected class KeyBindingConfigManager<T> : IniConfigManager<T> where T : struct, Enum
        {
            protected override string Filename => "keybindings.ini";

            public KeyBindingConfigManager(Storage storage)
                : base(storage)
            {
            }

            public bool Has(T key)
            {
                return ConfigStore.ContainsKey(key);
            }
        }
    }
}
