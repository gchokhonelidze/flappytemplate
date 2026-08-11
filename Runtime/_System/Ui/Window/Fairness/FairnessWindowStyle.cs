using System;
using TMPro;
using UnityEngine;

namespace FlappyTemplate
{
    // What the inside of a fairness window looks like. The window around it - panel, caption, close button,
    // backdrop - is set on those objects themselves; this is the client seed box, the two green buttons, the
    // section headings and the label-over-value blocks under them.
    //
    // The defaults are the violet-and-mint reading the rest of this folder is drawn in: a wash of white for
    // the input, mint buttons, and captions in bold over plain values.
    [Serializable]
    public class FairnessWindowStyle
    {
        [Header("Spacing")]
        [Tooltip("Between one label-over-value block and the next.")]
        [Min(0f)]
        public float RowGap = 10f;

        [Tooltip("Above a section heading - Current seed pair, Previous seed pair. Drawn as padding over the heading itself, so the first section is spaced from the controls the same way.")]
        [Min(0f)]
        public float SectionGap = 18f;

        [Tooltip("Between a caption and the value under it.")]
        [Min(0f)]
        public float CaptionGap = 4f;

        [Header("Headings")]
        public TMP_FontAsset HeadingFont;

        [Min(1f)]
        public float HeadingSize = 28f;

        public Color HeadingColor = Color.white;

        public FontStyles HeadingStyle = FontStyles.Bold;

        [Header("Text")]
        public TMP_FontAsset CaptionFont;

        [Min(1f)]
        public float CaptionSize = 22f;

        public Color CaptionColor = Color.white;

        public FontStyles CaptionStyle = FontStyles.Bold;

        public TMP_FontAsset ValueFont;

        [Min(1f)]
        public float ValueSize = 22f;

        public Color ValueColor = Color.white;

        public FontStyles ValueStyle = FontStyles.Bold;

        [Tooltip("The seeds and the hash: long strings that wrap rather than fit.")]
        [Min(1f)]
        public float HashSize = 18f;

        [Header("Client seed box")]
        [Min(0f)]
        public float InputHeight = 56f;

        [Min(0f)]
        public float InputCornerRadius = 8f;

        [Tooltip("Drawn over the panel fill, so an alpha below one is a wash rather than a colour.")]
        public Color InputFill = new Color(1f, 1f, 1f, 0.18f);

        [Tooltip("The box while the controls are locked - a round in play, or a request already on its way.")]
        public Color InputLockedFill = new Color(1f, 1f, 1f, 0.08f);

        [Min(1f)]
        public float InputTextSize = 24f;

        public Color InputTextColor = Color.white;

        public Color InputPlaceholderColor = new Color(1f, 1f, 1f, 0.45f);

        [Tooltip("Inset of the text from the left and right of the box.")]
        [Min(0f)]
        public float InputPadding = 14f;

        public Color CaretColor = Color.white;

        [Min(1f)]
        public float CaretWidth = 2f;

        public Color SelectionColor = new Color(1f, 1f, 1f, 0.35f);

        [Header("Padlock")]
        [Min(0f)]
        public float LockSize = 30f;

        public Color LockColor = Color.white;

        [Min(0.5f)]
        public float LockThickness = 3f;

        [Tooltip("Leave empty and the padlock is drawn from a ring and a rounded box, which costs no atlas entry and stays sharp at any size.")]
        public Sprite LockIcon;

        [Tooltip("Between the padlock, the box and the renew button.")]
        [Min(0f)]
        public float InputGap = 10f;

        [Header("Buttons")]
        [Tooltip("The small button beside the box, which sends the typed client seed and nothing else.")]
        public Vector2 RenewSize = new Vector2(56f, 56f);

        [Min(0f)]
        public float RandomizeHeight = 60f;

        [Min(0f)]
        public float ButtonCornerRadius = 10f;

        public Color ButtonFill = new Color(0.565f, 0.894f, 0.604f);

        [Tooltip("Both buttons while the controls are locked - a round in play, no seeds yet, or a request already on its way.")]
        public Color ButtonLockedFill = new Color(0.565f, 0.894f, 0.604f, 0.35f);

        public Color ButtonTextColor = new Color(0.11f, 0.1f, 0.29f);

        [Min(1f)]
        public float ButtonTextSize = 24f;

        [Header("Arrows")]
        [Tooltip("How far the circular arrow spans on either button.")]
        [Min(1f)]
        public float ArrowSize = 28f;

        [Min(0.5f)]
        public float ArrowThickness = 3f;

        [Tooltip("Between the arrow and the word beside it on the randomize button.")]
        [Min(0f)]
        public float ArrowGap = 10f;

        [Tooltip("Leave empty and the arrow is drawn from a ring, a notch and a diamond. Note the notch is painted in the button's own colour rather than cut, so it only disappears against a flat button.")]
        public Sprite ArrowIcon;

        [Header("Loader")]
        [Tooltip("Height of the row of dots shown while there are no seeds yet, or while a request is on its way.")]
        [Min(0f)]
        public float LoaderHeight = 60f;

        [Min(1f)]
        public float LoaderDotSize = 14f;

        [Min(0f)]
        public float LoaderDotGap = 12f;

        public Color LoaderColor = new Color(1f, 1f, 1f, 0.85f);

        [Tooltip("Seconds for one dot to swell and settle again. The three are staggered across it.")]
        [Min(0.05f)]
        public float LoaderPulse = 0.5f;

        /// <summary>A copy, for a window that wants its own colours without editing the shared style.</summary>
        // Every field here is a value or a reference to something the style does not own - a font, a sprite -
        // so a shallow copy is a whole copy.
        public FairnessWindowStyle Clone() => (FairnessWindowStyle)MemberwiseClone();
    }
}
