using UnityEngine;
using UnityEngine.InputSystem;

namespace FlappyTemplate
{
    // Whether a key is held down this frame. The only question the registry ever asks of the keyboard: the
    // presses and the releases are edges Hotkeys works out itself by comparing one frame to the next.
    //
    // One question rather than three is the whole reason Hotkeys.Reader is worth having. A game on another
    // input path - a gamepad, a row of on-screen buttons on a phone - replaces that one delegate and gets
    // presses, releases, held keys, the down-state the window paints from and the suppression while an input
    // field has focus, without writing any of it again.
    internal static class HotkeyInput
    {
        /// <summary>Whether the key is held, read from the Input System's keyboard. False with no keyboard
        /// attached - a phone - rather than an error.</summary>
        public static bool Read(KeyCode key)
        {
            var keyboard = Keyboard.current;

            if (keyboard == null)
                return false;

            var physical = ToKey(key);

            return physical != Key.None && keyboard[physical].isPressed;
        }

        /// <summary>The Input System key at the place on the keyboard a KeyCode names, or Key.None for a
        /// KeyCode with no keyboard key behind it (the mouse and joystick buttons).</summary>
        // Bindings stay KeyCode because that is what UiHotkey and UiHotkeyMark have serialized in every scene
        // made so far: Key numbers its members differently, so changing the field's type would quietly move
        // each saved binding onto some other key. The two enums only meet here.
        //
        // The letters, the digits, the keypad and the function keys run in order in both enums, so they are
        // worked out by offset; the rest are named one by one.
        public static Key ToKey(KeyCode key)
        {
            if (key >= KeyCode.A && key <= KeyCode.Z)
                return Key.A + (key - KeyCode.A);

            // KeyCode counts the top row 0 to 9; Key counts it as it is laid out, 1 to 9 and then 0.
            if (key >= KeyCode.Alpha1 && key <= KeyCode.Alpha9)
                return Key.Digit1 + (key - KeyCode.Alpha1);

            if (key >= KeyCode.Keypad0 && key <= KeyCode.Keypad9)
                return Key.Numpad0 + (key - KeyCode.Keypad0);

            if (key >= KeyCode.F1 && key <= KeyCode.F12)
                return Key.F1 + (key - KeyCode.F1);

            switch (key)
            {
                case KeyCode.Alpha0: return Key.Digit0;
                case KeyCode.Space: return Key.Space;
                case KeyCode.Return: return Key.Enter;
                case KeyCode.Tab: return Key.Tab;
                case KeyCode.BackQuote: return Key.Backquote;
                case KeyCode.Quote: return Key.Quote;
                case KeyCode.Semicolon: return Key.Semicolon;
                case KeyCode.Comma: return Key.Comma;
                case KeyCode.Period: return Key.Period;
                case KeyCode.Slash: return Key.Slash;
                case KeyCode.Backslash: return Key.Backslash;
                case KeyCode.LeftBracket: return Key.LeftBracket;
                case KeyCode.RightBracket: return Key.RightBracket;
                case KeyCode.Minus: return Key.Minus;
                case KeyCode.Equals: return Key.Equals;
                case KeyCode.LeftShift: return Key.LeftShift;
                case KeyCode.RightShift: return Key.RightShift;
                case KeyCode.LeftAlt: return Key.LeftAlt;
                case KeyCode.RightAlt: return Key.RightAlt;
                case KeyCode.AltGr: return Key.AltGr;
                case KeyCode.LeftControl: return Key.LeftCtrl;
                case KeyCode.RightControl: return Key.RightCtrl;
                case KeyCode.LeftMeta: return Key.LeftMeta;
                case KeyCode.RightMeta: return Key.RightMeta;
                case KeyCode.Menu: return Key.ContextMenu;
                case KeyCode.Escape: return Key.Escape;
                case KeyCode.LeftArrow: return Key.LeftArrow;
                case KeyCode.RightArrow: return Key.RightArrow;
                case KeyCode.UpArrow: return Key.UpArrow;
                case KeyCode.DownArrow: return Key.DownArrow;
                case KeyCode.Backspace: return Key.Backspace;
                case KeyCode.PageDown: return Key.PageDown;
                case KeyCode.PageUp: return Key.PageUp;
                case KeyCode.Home: return Key.Home;
                case KeyCode.End: return Key.End;
                case KeyCode.Insert: return Key.Insert;
                case KeyCode.Delete: return Key.Delete;
                case KeyCode.CapsLock: return Key.CapsLock;
                case KeyCode.Numlock: return Key.NumLock;
                case KeyCode.Print: return Key.PrintScreen;
                case KeyCode.ScrollLock: return Key.ScrollLock;
                case KeyCode.Pause: return Key.Pause;
                case KeyCode.KeypadEnter: return Key.NumpadEnter;
                case KeyCode.KeypadDivide: return Key.NumpadDivide;
                case KeyCode.KeypadMultiply: return Key.NumpadMultiply;
                case KeyCode.KeypadPlus: return Key.NumpadPlus;
                case KeyCode.KeypadMinus: return Key.NumpadMinus;
                case KeyCode.KeypadPeriod: return Key.NumpadPeriod;
                case KeyCode.KeypadEquals: return Key.NumpadEquals;
                default: return Key.None;
            }
        }
    }
}
