using System;
using TMPro;
using UnityEngine;

namespace FlappyTemplate
{
    // What the inside of a sound window looks like. The window around it - panel, caption, close button,
    // backdrop - is set on those objects themselves; this is the two cards, the switch on each and the slider
    // under it.
    //
    // The defaults are the violet-and-gold the hotkeys dialog next door is drawn in, so the two look like they
    // came from the same game. Three colours carry the whole meaning: the switch is green when the channel is
    // on and grey when it is off, and everything below a switched-off channel fades - a slider that still
    // looked live under a muted channel would read as a control that does nothing.
    [Serializable]
    public class SoundWindowStyle
    {
        [Header("Blocks")]
        [Tooltip("Between one channel's card and the next.")]
        [Min(0f)]
        public float SectionGap = 14f;

        [Header("Card")]
        public Color CardFill = new Color(1f, 1f, 1f, 0.12f);

        [Tooltip("A card whose channel is switched off.")]
        public Color CardOffFill = new Color(1f, 1f, 1f, 0.05f);

        [Min(0f)]
        public float CardCornerRadius = 10f;

        [Tooltip("Inset of a card's contents from its edges.")]
        [Min(0f)]
        public float CardPadding = 14f;

        [Tooltip("The caption row, which is as tall as the switch on it.")]
        [Min(1f)]
        public float TitleHeight = 44f;

        [Tooltip("The slider row under it.")]
        [Min(1f)]
        public float SliderHeight = 34f;

        [Tooltip("Between the two rows of a card.")]
        [Min(0f)]
        public float RowGap = 10f;

        [Header("Caption")]
        public TMP_FontAsset LabelFont;

        [Min(1f)]
        public float LabelSize = 20f;

        public Color LabelColor = Color.white;

        [Tooltip("While the channel is switched off. The caption stays readable - a muted channel is a setting the player chose, not a control that has gone away.")]
        public Color LabelOffColor = new Color(1f, 1f, 1f, 0.45f);

        public FontStyles LabelStyle = FontStyles.Bold;

        [Header("Switch")]
        [Tooltip("The pill on the right of a card's caption row. Its corner radius is half its height, so it stays a pill at any size.")]
        public Vector2 SwitchSize = new Vector2(78f, 40f);

        [Tooltip("While the channel is on.")]
        public Color SwitchOnFill = new Color(0.45f, 0.76f, 0.50f);

        [Tooltip("While it is off - grey rather than red, because off is a setting the player chose and not a fault.")]
        public Color SwitchOffFill = new Color(0.79f, 0.78f, 0.84f);

        [Tooltip("The knob that slides from one end of the pill to the other.")]
        public Color KnobFill = Color.white;

        [Tooltip("How far the knob sits inside the pill, all the way round.")]
        [Min(0f)]
        public float KnobInset = 4f;

        [Header("Slider")]
        [Tooltip("The bar the handle runs along.")]
        [Min(1f)]
        public float TrackHeight = 8f;

        public Color TrackFill = new Color(1f, 1f, 1f, 0.18f);

        [Tooltip("The part of the track to the left of the handle - how loud the channel is set to.")]
        public Color FillColor = new Color(0.91f, 0.77f, 0.36f);

        [Tooltip("The same while the channel is switched off.")]
        public Color FillOffColor = new Color(0.91f, 0.77f, 0.36f, 0.30f);

        [Tooltip("The grip. Round, so its corner radius is half of this.")]
        [Min(1f)]
        public float HandleSize = 24f;

        public Color HandleFill = Color.white;

        public Color HandleOffColor = new Color(1f, 1f, 1f, 0.45f);

        [Header("Percentage")]
        [Tooltip("The number at the right of the slider row. Off leaves the row to the slider alone.")]
        public bool ShowPercent = true;

        [Tooltip("How wide the number's column is. Wide enough for \"100%\" at the size below.")]
        [Min(0f)]
        public float PercentWidth = 62f;

        public TMP_FontAsset PercentFont;

        [Min(1f)]
        public float PercentSize = 17f;

        public Color PercentColor = new Color(1f, 1f, 1f, 0.75f);

        public Color PercentOffColor = new Color(1f, 1f, 1f, 0.35f);

        public FontStyles PercentStyle = FontStyles.Normal;
    }
}
