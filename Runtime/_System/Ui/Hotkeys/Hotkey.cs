using System;
using UnityEngine;

namespace FlappyTemplate
{
    // One key, and the one thing it does. Made by Hotkeys.Bind and handed back so it can be dropped again:
    //
    //     var min = Hotkeys.Bind(KeyCode.D, "Min", () => bet.Min());
    //     ...
    //     min.Dispose();
    //
    // A binding is live from the moment it is made until it is disposed, unbound, or replaced by another
    // binding on the same key - there is only ever one per key, which is what lets the window print the list
    // without having to say which of three things D does. Disposing a binding that has already gone is not an
    // error, which is what makes OnDisable a safe place to do it.
    //
    // The label is what the window prints beside the key cap, and it goes through Translator.Label - so
    // "Cash out" is the en_US wording of hotkeys.cash_out and comes back in the player's language, while a
    // wording nothing knows is printed as it was typed. See Translations/README.md.
    public class Hotkey : IDisposable
    {
        internal Action Down;
        internal Action Up;
        internal Action Held;

        private string label;

        internal Hotkey(KeyCode key, string label, Action down, Action up, Action held)
        {
            Key = key;
            this.label = label ?? string.Empty;
            Down = down;
            Up = up;
            Held = held;
        }

        /// <summary>The key that fires it.</summary>
        public KeyCode Key { get; }

        /// <summary>What the window prints beside the key cap. Setting it repaints whatever is showing the
        /// list, so a control that renames itself mid-round does not need to rebind to be read.</summary>
        public string Label
        {
            get => label;
            set
            {
                var wanted = value ?? string.Empty;
                if (label == wanted)
                    return;

                label = wanted;

                if (IsBound)
                    Hotkeys.Changed();
            }
        }

        /// <summary>Off stops the binding firing and greys its row, without taking it out of the list. For a
        /// control that is only there part of the time - a cash-out key during a round - so the player can see
        /// the key exists rather than wondering whether they have remembered it wrong.</summary>
        public bool Enabled
        {
            get => enabled;
            set
            {
                if (enabled == value)
                    return;

                enabled = value;

                if (IsBound)
                    Hotkeys.Changed();
            }
        }

        private bool enabled = true;

        /// <summary>Whether the key is held down as things stand. What the window paints the cap from.</summary>
        public bool IsDown { get; internal set; }

        /// <summary>False once the binding has been unbound, disposed, or replaced by another on the same
        /// key. A binding that is not bound never fires again, whatever is still holding it.</summary>
        public bool IsBound { get; internal set; } = true;

        /// <summary>Drops the binding. Safe to call twice, and safe on one that was replaced - which is what
        /// lets a MonoBehaviour dispose in OnDisable without asking whether anything happened since.</summary>
        public void Dispose() => Hotkeys.Unbind(this);

        public override string ToString() => Key + " -> " + label;
    }
}
