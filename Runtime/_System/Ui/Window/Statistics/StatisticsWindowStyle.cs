using System;
using TMPro;
using UnityEngine;

namespace FlappyTemplate
{
    // What the inside of a statistics window looks like. The window around it - panel, caption, close
    // button, backdrop - is set on those objects themselves; this is only the tabs, the rows and the reset
    // button.
    //
    // The defaults are the charcoal the bet info sheet is drawn in: a wash of white for the tab that is
    // showing, quiet captions over white figures, green for anything above zero, red for anything below it,
    // gold for a count.
    [Serializable]
    public class StatisticsWindowStyle
    {
        [Header("Tabs")]
        [Min(0f)]
        public float TabHeight = 46f;

        [Min(0f)]
        public float TabSpacing = 10f;

        [Min(0f)]
        public float TabCornerRadius = 12f;

        [Tooltip("Space between the tab row and the first value.")]
        [Min(0f)]
        public float TabGap = 22f;

        public Color TabActiveFill = new Color(1f, 1f, 1f, 0.14f);

        public Color TabActiveText = Color.white;

        public Color TabIdleFill = new Color(1f, 1f, 1f, 0.04f);

        public Color TabIdleText = new Color(1f, 1f, 1f, 0.6f);

        public TMP_FontAsset TabFont;

        [Min(1f)]
        public float TabSize = 18f;

        public FontStyles TabStyle = FontStyles.Normal;

        [Header("Rows")]
        [Tooltip("Between one row and the next. The caption and its value are held together by Label Gap instead.")]
        [Min(0f)]
        public float RowSpacing = 16f;

        [Min(0f)]
        public float LabelGap = 2f;

        public TMP_FontAsset LabelFont;

        [Min(1f)]
        public float LabelSize = 17f;

        public Color LabelColor = new Color(1f, 1f, 1f, 0.6f);

        public FontStyles LabelStyle = FontStyles.Normal;

        public TMP_FontAsset ValueFont;

        [Min(1f)]
        public float ValueSize = 22f;

        public FontStyles ValueStyle = FontStyles.Bold;

        [Header("Value colours")]
        [Tooltip("Rows tinted Plain, and the separators on the counts line.")]
        public Color ValueColor = Color.white;

        public Color PositiveColor = new Color(0.45f, 0.86f, 0.5f);

        public Color NegativeColor = new Color(0.94f, 0.36f, 0.36f);

        [Tooltip("The bets part of the counts line. Wins and losses take the positive and negative colours.")]
        public Color CountColor = new Color(0.98f, 0.8f, 0.2f);

        [Header("Value format")]
        [Tooltip("Decimal places. Below zero prints the number exactly as the server sent it, which is the only way to be sure nothing was rounded off.")]
        public int Decimals = -1;

        [Tooltip("Thousands separators, when a decimal count is set.")]
        public bool GroupDigits = true;

        [Tooltip("Printed before every money value - a currency symbol, usually. Counts are left alone.")]
        public string Prefix = "";

        [Header("Reset button")]
        public Vector2 ResetSize = new Vector2(56f, 56f);

        [Tooltip("From the bottom left corner of the content area, inwards.")]
        public Vector2 ResetOffset = new Vector2(4f, 4f);

        [Min(0f)]
        public float ResetCornerRadius = 12f;

        public Color ResetFill = new Color(1f, 1f, 1f, 0.1f);

        public Color ResetIconColor = Color.white;

        [Range(0f, 1f)]
        public float ResetIconScale = 0.5f;

        [Min(0.5f)]
        public float ResetIconThickness = 3f;

        [Tooltip("Leave empty and the circular arrow is drawn from boxes, which needs no atlas entry.")]
        public Sprite ResetIcon;

        /// <summary>A deep copy. Nothing here is a reference type that needs untangling, but it keeps the
        /// two styles independent.</summary>
        public StatisticsWindowStyle Clone() => (StatisticsWindowStyle)MemberwiseClone();
    }
}
