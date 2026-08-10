using System;
using UnityEngine;

namespace FlappyTemplate
{
    /// <summary>One line of a statistics window: a caption, the field under it, and how it is coloured.</summary>
    // The list of these is the whole of what a statistics window shows, in the order it shows it. Rows can
    // be added, dropped or renamed from the inspector without touching the component - which is the point,
    // since which of the ten fields a game cares about is a decision about that game and not about the
    // window.
    [Serializable]
    public class StatisticsRow
    {
        [Tooltip("The caption above the value. Whatever the game's language is - nothing here is translated.")]
        public string Label = "";

        public EStatField Field = EStatField.Wager;

        public EStatTint Tint = EStatTint.Plain;

        [Tooltip("Printed after the value. A percent sign, a currency code - anything the format itself should not carry.")]
        public string Suffix = "";

        [Tooltip("Off leaves the row out entirely rather than blank, so the ones below close up.")]
        public bool Enabled = true;

        public StatisticsRow()
        {
        }

        public StatisticsRow(string label, EStatField field, EStatTint tint = EStatTint.Plain, string suffix = "")
        {
            Label = label;
            Field = field;
            Tint = tint;
            Suffix = suffix;
        }
    }
}
