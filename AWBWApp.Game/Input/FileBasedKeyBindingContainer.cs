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
                if (config.Has((T)binding.Action))
                    keyBindings.Add(new KeyBinding(config.Get<string>((T)binding.Action), binding.Action));
                else
                    keyBindings.Add(new KeyBinding(binding.KeyCombination, binding.Action));
            }

            KeyBindings = keyBindings;

            foreach (var binding in KeyBindings)
                config.SetValue((T)binding.Action, binding.KeyCombination.ToString());

            config.Save();
        }

        private class KeyBindingConfigManager<T> : IniConfigManager<T> where T : struct, Enum
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
