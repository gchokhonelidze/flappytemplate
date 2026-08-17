using System.Collections.Generic;
using UnityEngine;

namespace FlappyTemplate
{
    // What a key is called on screen, and the shape of the keyboard the hotkeys window draws.
    //
    // Two different names, because they are read in two different places. The **face** is what is printed on
    // the drawn keyboard - lower case, `backspace`, `caps`, the way the key is actually engraved - and comes
    // from the layout below. The **name** is what a row in the list and a badge on a button say - `D`, `Enter`,
    // `Shift` - and is worked out from the KeyCode, so it is right for every key there is rather than only for
    // the hundred on the picture.
    //
    // The layout is one plain table. A game that wants another one - a numpad, a game pad, the arrow cluster -
    // adds rows to it, and the window draws them: nothing below counts on there being five rows or on any
    // particular key being in them.
    internal static class HotkeyCaps
    {
        // One cap. Units is its width against a plain letter key, which is how a real keyboard is measured -
        // the window turns them into flexible tracks, so a row fills the width whatever it is made of.
        public readonly struct Cap
        {
            public readonly KeyCode Key;
            public readonly string Face;
            public readonly float Units;

            public Cap(KeyCode key, string face, float units = 1f)
            {
                Key = key;
                Face = face;
                Units = units > 0f ? units : 1f;
            }
        }

        /// <summary>The keyboard the window draws, a row at a time - the same five rows the web front shows, so
        /// a player who has used one recognises the other.</summary>
        public static readonly Cap[][] Rows =
        {
            new[]
            {
                new Cap(KeyCode.BackQuote, "`"),
                new Cap(KeyCode.Alpha1, "1"),
                new Cap(KeyCode.Alpha2, "2"),
                new Cap(KeyCode.Alpha3, "3"),
                new Cap(KeyCode.Alpha4, "4"),
                new Cap(KeyCode.Alpha5, "5"),
                new Cap(KeyCode.Alpha6, "6"),
                new Cap(KeyCode.Alpha7, "7"),
                new Cap(KeyCode.Alpha8, "8"),
                new Cap(KeyCode.Alpha9, "9"),
                new Cap(KeyCode.Alpha0, "0"),
                new Cap(KeyCode.Minus, "-"),
                new Cap(KeyCode.Equals, "="),
                new Cap(KeyCode.Backspace, "backspace", 2f),
            },
            new[]
            {
                new Cap(KeyCode.Tab, "tab", 1.5f),
                new Cap(KeyCode.Q, "q"),
                new Cap(KeyCode.W, "w"),
                new Cap(KeyCode.E, "e"),
                new Cap(KeyCode.R, "r"),
                new Cap(KeyCode.T, "t"),
                new Cap(KeyCode.Y, "y"),
                new Cap(KeyCode.U, "u"),
                new Cap(KeyCode.I, "i"),
                new Cap(KeyCode.O, "o"),
                new Cap(KeyCode.P, "p"),
                new Cap(KeyCode.LeftBracket, "["),
                new Cap(KeyCode.RightBracket, "]"),
                new Cap(KeyCode.Backslash, "\\"),
            },
            new[]
            {
                new Cap(KeyCode.CapsLock, "caps", 1.75f),
                new Cap(KeyCode.A, "a"),
                new Cap(KeyCode.S, "s"),
                new Cap(KeyCode.D, "d"),
                new Cap(KeyCode.F, "f"),
                new Cap(KeyCode.G, "g"),
                new Cap(KeyCode.H, "h"),
                new Cap(KeyCode.J, "j"),
                new Cap(KeyCode.K, "k"),
                new Cap(KeyCode.L, "l"),
                new Cap(KeyCode.Semicolon, ";"),
                new Cap(KeyCode.Quote, "'"),
                new Cap(KeyCode.Return, "enter", 2.25f),
            },
            new[]
            {
                new Cap(KeyCode.LeftShift, "shift", 2.25f),
                new Cap(KeyCode.Z, "z"),
                new Cap(KeyCode.X, "x"),
                new Cap(KeyCode.C, "c"),
                new Cap(KeyCode.V, "v"),
                new Cap(KeyCode.B, "b"),
                new Cap(KeyCode.N, "n"),
                new Cap(KeyCode.M, "m"),
                new Cap(KeyCode.Comma, ","),
                new Cap(KeyCode.Period, "."),
                new Cap(KeyCode.Slash, "/"),
                new Cap(KeyCode.RightShift, "shift", 2.25f),
            },
            new[]
            {
                new Cap(KeyCode.Space, "space", 12f),
            },
        };

        /// <summary>What a row's caps come to, in cap widths. What the flexible tracks are weighted by.</summary>
        public static float Units(Cap[] row)
        {
            float total = 0f;

            if (row == null)
                return 0f;

            for (int i = 0; i < row.Length; i++)
                total += row[i].Units;

            return total;
        }

        /// <summary>What a list row or a badge on a button calls the key: <c>D</c>, <c>1</c>, <c>Enter</c>,
        /// <c>Shift</c>. Worked out from the KeyCode rather than looked up, so it is right for the keys the
        /// drawn keyboard does not have on it.</summary>
        public static string Name(KeyCode key)
        {
            if (key == KeyCode.None)
                return string.Empty;

            if (named.TryGetValue(key, out var found))
                return found;

            var text = key.ToString();

            // Alpha1 is the 1 along the top and Keypad1 is the one on the numpad. Both read as "1" to a player,
            // so the numpad one says where it is - a hint that would be missed on the row that needs it least.
            if (text.StartsWith("Alpha") && text.Length == 6)
                return text.Substring(5);

            if (text.StartsWith("Keypad") && text.Length == 7)
                return "Num " + text.Substring(6);

            return text;
        }

        // The keys whose enum name is not what anybody calls them. Everything else - the letters, the digits,
        // the function keys - reads correctly as it is.
        private static readonly Dictionary<KeyCode, string> named = new Dictionary<KeyCode, string>
        {
            { KeyCode.Return, "Enter" },
            { KeyCode.KeypadEnter, "Num Enter" },
            { KeyCode.Escape, "Esc" },
            { KeyCode.LeftShift, "Shift" },
            { KeyCode.RightShift, "Shift" },
            { KeyCode.LeftControl, "Ctrl" },
            { KeyCode.RightControl, "Ctrl" },
            { KeyCode.LeftAlt, "Alt" },
            { KeyCode.RightAlt, "Alt" },
            { KeyCode.LeftCommand, "Cmd" },
            { KeyCode.RightCommand, "Cmd" },
            { KeyCode.BackQuote, "`" },
            { KeyCode.Minus, "-" },
            { KeyCode.Equals, "=" },
            { KeyCode.LeftBracket, "[" },
            { KeyCode.RightBracket, "]" },
            { KeyCode.Backslash, "\\" },
            { KeyCode.Semicolon, ";" },
            { KeyCode.Quote, "'" },
            { KeyCode.Comma, "," },
            { KeyCode.Period, "." },
            { KeyCode.Slash, "/" },
            { KeyCode.UpArrow, "Up" },
            { KeyCode.DownArrow, "Down" },
            { KeyCode.LeftArrow, "Left" },
            { KeyCode.RightArrow, "Right" },
            { KeyCode.PageUp, "Page Up" },
            { KeyCode.PageDown, "Page Down" },
            { KeyCode.KeypadPlus, "Num +" },
            { KeyCode.KeypadMinus, "Num -" },
            { KeyCode.KeypadMultiply, "Num *" },
            { KeyCode.KeypadDivide, "Num /" },
            { KeyCode.KeypadPeriod, "Num ." },
        };
    }
}
