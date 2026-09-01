using BepInEx.Configuration;
using UnityEngine;

namespace PlantPeek
{
    /// <summary>
    /// Key checks for a hold-to-inspect binding.
    ///
    /// Deliberately not KeyboardShortcut.IsPressed()/IsDown(). Those end in:
    ///
    ///     _modifierBlockKeyCodes.All(c =&gt; !Input.GetKey(c) || allKeys.Contains(c))
    ///
    /// where _modifierBlockKeyCodes is every supported key except the mouse buttons - so they
    /// report false whenever *any* other key is held. That is sensible for a config-menu
    /// shortcut that must not fire mid-combo, and wrong for this: the player is holding W to
    /// walk down the row of crops they are inspecting. The binding simply never fired while
    /// moving.
    ///
    /// These check the bound key and its declared modifiers, and ignore everything else.
    /// </summary>
    internal static class Hotkey
    {
        internal static bool IsHeld(KeyboardShortcut shortcut)
        {
            var main = shortcut.MainKey;
            if (main == KeyCode.None || !UnityEngine.Input.GetKey(main))
            {
                return false;
            }

            return ModifiersHeld(shortcut);
        }

        internal static bool WasPressed(KeyboardShortcut shortcut)
        {
            var main = shortcut.MainKey;
            if (main == KeyCode.None || !UnityEngine.Input.GetKeyDown(main))
            {
                return false;
            }

            return ModifiersHeld(shortcut);
        }

        private static bool ModifiersHeld(KeyboardShortcut shortcut)
        {
            foreach (var modifier in shortcut.Modifiers)
            {
                if (!UnityEngine.Input.GetKey(modifier))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
