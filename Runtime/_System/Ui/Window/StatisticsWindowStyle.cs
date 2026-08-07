using System;
using TMPro;
using UnityEngine;

namespace FlappyTemplate
{
    // What the inside of a statistics window looks like. The window around it - panel, caption, close
    // button, backdrop - is UiWindowStyle's business; this is only the tabs, the rows and the reset button.
    //
    // The defaults are the violet-and-mint reading in the readme: mint for the tab that is showing and for
    // anything above zero, red for anything below it, amber for a count.
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

        public Color TabActiveFill = new Color(0.565f, 0.894f, 0.604f);

        public Color TabActiveText = new Color(0.11f, 0.1f, 0.29f);

        public Color TabIdleFill = new Color(0.18f, 0.16f, 0.36f);

        public Color TabIdleText = Color.white;

        public TMP_FontAsset TabFont;

        [Min(1f)]
        public float TabSize = 21f;

        public FontStyles TabStyle = FontStyles.Bold;

        [Header("Rows")]
        [Tooltip("Between one row and the next. The caption and its value are held together by Label Gap instead.")]
        [Min(0f)]
        public float RowSpacing = 16f;

        [Min(0f)]
        public float LabelGap = 2f;

        public TMP_FontAsset LabelFont;

        [Min(1f)]
        public float LabelSize = 22f;

        public Color LabelColor = Color.white;

        public FontStyles LabelStyle = FontStyles.Bold;

        public TMP_FontAsset ValueFont;

        [Min(1f)]
        public float ValueSize = 28f;

        public FontStyles ValueStyle = FontStyles.Bold;

        [Header("Value colours")]
        [Tooltip("Rows tinted Plain, and the separators on the counts line.")]
        public Color ValueColor = Color.white;

        public Color PositiveColor = new Color(0.29f, 0.87f, 0.5f);

        public Color NegativeColor = new Color(0.94f, 0.27f, 0.27f);

        [Tooltip("The bets part of the counts line. Wins and losses take the positive and negative colours.")]
        public Color CountColor = new Color(0.98f, 0.8f, 0.08f);

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
        public float ResetCornerRadius = 16f;

        public Color ResetFill = new Color(0.565f, 0.894f, 0.604f);

        public Color ResetIconColor = new Color(0.11f, 0.1f, 0.29f);

        [Range(0f, 1f)]
        public float ResetIconScale = 0.5f;

        [Min(0.5f)]
        public float ResetIconThickness = 4f;

        [Tooltip("Leave empty and the circular arrow is drawn from boxes, which needs no atlas entry.")]
        public Sprite ResetIcon;

        /// <summary>A deep copy. Nothing here is a reference type that needs untangling, but it keeps the
        /// two styles independent the same way UiWindowStyle.Clone does.</summary>
        public StatisticsWindowStyle Clone() => (StatisticsWindowStyle)MemberwiseClone();
    }
}
