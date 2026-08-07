using System;
using TMPro;
using UnityEngine;

namespace FlappyTemplate
{
    // Everything a window looks like, in one object, so a game can hold a single style and hand it to every
    // window it opens rather than setting twenty fields per window. Nothing here is structural: change any
    // of it at any time and call ApplyStyle, and the window in front of the player follows.
    //
    // The defaults are a dark violet dialog with a white close button, which is what the statistics window
    // in the readme is built from. A game with its own palette is expected to replace them wholesale.
    [Serializable]
    public class UiWindowStyle
    {
        [Header("Panel")]
        public Color Fill = new Color(0.357f, 0.298f, 0.62f);

        public Color BorderColor = new Color(0.227f, 0.18f, 0.42f);

        [Min(0f)]
        public float BorderSize = 3f;

        [Min(0f)]
        public float CornerRadius = 26f;

        [Tooltip("Width of the fade that smooths the corner arcs. About a pixel is right.")]
        [Min(0f)]
        public float EdgeSoftness = 1.25f;

        [Header("Caption")]
        [Tooltip("The whole header block: the drag strip, the close button and the title. Content starts under it.")]
        [Min(0f)]
        public float CaptionHeight = 104f;

        [Tooltip("Drawn over the panel fill, so the default is a wash rather than a colour - alpha at nothing leaves the header the same violet as the body.")]
        public Color CaptionFill = new Color(0f, 0f, 0f, 0.12f);

        [Tooltip("Space between the top of the caption and the title, which is what leaves room for the close button above it.")]
        [Min(0f)]
        public float TitleTopInset = 44f;

        public TMP_FontAsset TitleFont;

        [Min(1f)]
        public float TitleSize = 34f;

        public Color TitleColor = Color.white;

        public FontStyles TitleStyle = FontStyles.Bold;

        public TextAlignmentOptions TitleAlignment = TextAlignmentOptions.Center;

        [Header("Close button")]
        public Vector2 CloseSize = new Vector2(44f, 44f);

        [Tooltip("From the top right corner of the panel, inwards. Positive x or negative y pushes it back out over the edge.")]
        public Vector2 CloseOffset = new Vector2(-18f, -18f);

        public Color CloseFill = Color.white;

        public Color CloseBorderColor = new Color(0f, 0f, 0f, 0f);

        [Min(0f)]
        public float CloseBorderSize = 0f;

        [Tooltip("Negative rounds it fully into a circle whatever the size.")]
        public float CloseCornerRadius = -1f;

        public Color CloseIconColor = new Color(0.18f, 0.16f, 0.35f);

        [Tooltip("Bar thickness of the drawn cross. Ignored when a sprite is given.")]
        [Min(0.5f)]
        public float CloseIconThickness = 4.5f;

        [Range(0f, 1f)]
        [Tooltip("How much of the button the cross spans.")]
        public float CloseIconScale = 0.42f;

        [Tooltip("Leave empty and the cross is drawn from two rotated boxes, which costs no atlas entry and stays sharp at any size.")]
        public Sprite CloseIcon;

        [Header("Content")]
        // Four floats rather than a RectOffset, which looks like the obvious type for this and is the wrong
        // one twice over. A RectOffset is a handle onto a native object, so building one in a field
        // initialiser throws before the component exists - "set_left is not allowed to be called from a
        // MonoBehaviour constructor" - and it is a class, so a style copied field by field would go on
        // sharing its padding with the style it came from.
        [Tooltip("Inset of the content area from the left of the panel.")]
        public float ContentPaddingLeft = 20f;

        [Tooltip("Inset of the content area below the caption. Measured from the bottom of the caption, not the top of the panel.")]
        public float ContentPaddingTop = 0f;

        public float ContentPaddingRight = 20f;

        public float ContentPaddingBottom = 20f;

        [Header("Backdrop")]
        public Color BackdropColor = new Color(0f, 0f, 0f, 0.55f);

        [Header("Transition")]
        [Tooltip("Scale a window starts at when it grows in, and shrinks back to when it leaves.")]
        [Min(0f)]
        public float OpenScale = 0.86f;

        /// <summary>A copy, for a window that wants its own colours without editing the shared style.</summary>
        // Every field here is a value or a reference to something the style does not own - a font, a sprite -
        // so a shallow copy is a whole copy. Anything added later that is neither has to be copied by hand.
        public UiWindowStyle Clone() => (UiWindowStyle)MemberwiseClone();
    }
}
