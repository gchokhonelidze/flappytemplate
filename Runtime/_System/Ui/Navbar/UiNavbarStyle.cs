using System;
using TMPro;
using UnityEngine;

namespace FlappyTemplate
{
    // What a navbar looks like: the strip behind the buttons, the buttons themselves, the glyphs drawn in
    // them and the captions under them.
    //
    // The defaults are the violet-and-mint reading the statistics and fairness windows are drawn in, so a
    // bar dropped into a scene beside them arrives matching rather than as a row of white squares.
    [Serializable]
    public class UiNavbarStyle
    {
        [Header("Bar")]
        [Tooltip("The strip behind the buttons. Off leaves them floating over whatever is behind the bar, which is what a bar over the game itself usually wants.")]
        public bool ShowBar = true;

        public Color BarFill = new Color(0.18f, 0.16f, 0.36f, 0.92f);

        [Min(0f)]
        public float BarCornerRadius = 20f;

        [Tooltip("Between the strip's edge and the buttons in it. Ignored while Show Bar is off, since there is no edge to be inset from.")]
        [Min(0f)]
        public float BarPadding = 8f;

        [Header("Buttons")]
        public Vector2 ButtonSize = new Vector2(56f, 56f);

        [Min(0f)]
        public float ButtonSpacing = 10f;

        [Min(0f)]
        public float ButtonCornerRadius = 16f;

        public Color ButtonFill = new Color(0.565f, 0.894f, 0.604f);

        [Tooltip("While the window a button opens is on screen. The bar repaints when a window opens or closes, so this is how a player sees which one they are looking at.")]
        public Color ButtonActiveFill = new Color(0.98f, 0.8f, 0.08f);

        [Header("Icons")]
        public Color IconColor = new Color(0.11f, 0.1f, 0.29f);

        [Tooltip("The glyph's square as a fraction of the smaller side of the button.")]
        [Range(0f, 1f)]
        public float IconScale = 0.5f;

        [Tooltip("Stroke width of the glyphs that are drawn from boxes rather than from a sprite.")]
        [Min(0.5f)]
        public float IconThickness = 4f;

        [Header("Labels")]
        [Tooltip("A caption under each glyph. Off is the usual case for a bar of small buttons over the game.")]
        public bool ShowLabels = false;

        public TMP_FontAsset LabelFont;

        [Min(1f)]
        public float LabelSize = 14f;

        public Color LabelColor = Color.white;

        public FontStyles LabelStyle = FontStyles.Bold;

        [Tooltip("Between the glyph and the caption under it.")]
        [Min(0f)]
        public float LabelGap = 3f;

        [Tooltip("The band the caption is drawn in, off the bottom of the button. The glyph gets what is left.")]
        [Min(0f)]
        public float LabelHeight = 18f;

        /// <summary>A deep copy. Nothing here is a reference type that needs untangling beyond the font,
        /// which is shared on purpose, but it keeps the two styles independent.</summary>
        public UiNavbarStyle Clone() => (UiNavbarStyle)MemberwiseClone();
    }
}
