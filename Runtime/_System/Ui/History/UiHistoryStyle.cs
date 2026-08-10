using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace FlappyTemplate
{
    // What a history strip looks like: how big one element is, how far apart they sit, what the built-in chip is
    // made of, and how a new one arrives.
    //
    // Deliberately thin on colour. What a bet *means* - a win, a loss, a round that ended some third way - is
    // the game's business, and a game that cares says so by drawing its own element: the strip hands it the whole
    // HistoryDto and stays out of it. So there is one colour per part here rather than one per outcome, and the
    // chip these fields describe is the fallback for a strip that has not been given a prefab.
    [Serializable]
    public class UiHistoryStyle
    {
        [Header("Strip")]
        [Tooltip("One element, in canvas units. Zero on an axis leaves that side to the element itself - to a prefab's own Layout Element, or to the width its text asks for. Clamp needs this set along the flow: it is what the room is divided by.")]
        public Vector2 ElementSize = new Vector2(96f, 56f);

        [Tooltip("Between one element and the next.")]
        [Min(0f)]
        public float Gap = 8f;

        // Four floats rather than a RectOffset, for the reason UiWindowStyle gives at the same place: a
        // RectOffset is a handle onto a native object, so building one in a field initialiser throws before the
        // component exists, and it is a class, so a copied style would go on sharing it.
        [Tooltip("Inset of the elements from the left of the strip.")]
        public float PaddingLeft = 0f;

        public float PaddingTop = 0f;

        public float PaddingRight = 0f;

        public float PaddingBottom = 0f;

        [Header("Chip")]
        [Tooltip("The built-in element. Every field under this heading is ignored once an Element Prefab is given - a prefab is its own look.")]
        public Color Fill = new Color(0.149f, 0.137f, 0.263f);

        public Color BorderColor = new Color(1f, 1f, 1f, 0.08f);

        [Min(0f)]
        public float BorderSize = 1.5f;

        [Min(0f)]
        public float CornerRadius = 8f;

        [Tooltip("Width of the fade that smooths the corner arcs. About a pixel is right.")]
        [Min(0f)]
        public float EdgeSoftness = 1.25f;

        [Header("Text")]
        public Color TextColor = Color.white;

        public TMP_FontAsset Font;

        [Min(1f)]
        public float TextSize = 26f;

        public FontStyles TextStyle = FontStyles.Bold;

        public TextAlignmentOptions TextAlignment = TextAlignmentOptions.Center;

        [Tooltip("Inset of the text from the edges of the chip. Enough that a long value does not touch the border.")]
        [Min(0f)]
        public float TextInset = 6f;

        [Tooltip("Shrink the text on an element it does not fit rather than letting it spill. Off keeps every element's text the same size, which reads better on a strip of short values.")]
        public bool ShrinkText = true;

        [Header("Arrival")]
        [Tooltip("How long a new element takes to arrive. Zero puts it there with no animation at all.")]
        [Min(0f)]
        public float AppearDuration = 0.26f;

        [Tooltip("Scale it starts at. One skips the growing part.")]
        [Min(0f)]
        public float AppearScale = 0.7f;

        public bool AppearFade = true;

        public Ease AppearEase = Ease.OutBack;

        [Tooltip("Animate on unscaled time, so the strip still moves on a game that has paused itself by setting Time.timeScale to nothing.")]
        public bool UnscaledTime = true;

        [Tooltip("How long the strip takes to slide the newest element back into view while scrolling. Zero jumps.")]
        [Min(0f)]
        public float FollowDuration = 0.25f;

        [Header("Scrolling")]
        [Tooltip("Canvas units per notch of the wheel.")]
        [Min(1f)]
        public float ScrollSensitivity = 28f;

        [Tooltip("Let a flick carry on after the finger has left. What a touch screen expects, and WebGL on a phone is a touch screen.")]
        public bool ScrollInertia = true;

        [Tooltip("How quickly a flick runs out. Lower stops sooner; 0.135 is UGUI's own.")]
        [Range(0.01f, 0.99f)]
        public float ScrollDeceleration = 0.135f;

        /// <summary>A copy, for a strip that wants its own look without editing the shared style.</summary>
        // Every field here is a value or a reference to something the style does not own - a font - so a shallow
        // copy is a whole copy. Anything added later that is neither has to be copied by hand.
        public UiHistoryStyle Clone() => (UiHistoryStyle)MemberwiseClone();
    }
}
