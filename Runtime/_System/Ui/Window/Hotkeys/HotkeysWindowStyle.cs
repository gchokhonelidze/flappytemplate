using System;
using TMPro;
using UnityEngine;

namespace FlappyTemplate
{
    // What the inside of a hotkeys window looks like. The window around it - panel, caption, close button,
    // backdrop - is set on those objects themselves; this is the drawn keyboard across the top, the list of
    // bindings under it, and the button along the bottom that switches the whole feature on and off.
    //
    // The defaults are the violet-and-gold the web front's own hotkeys dialog is drawn in: a pale keyboard so the
    // caps read as caps, gold for a key that is bound to something, and a lighter violet plate per row. Three
    // colours carry the whole meaning of the window - plain, bound, and held down - so they are worth keeping
    // clearly apart if they are changed.
    [Serializable]
    public class HotkeysWindowStyle
    {
        [Header("Blocks")]
        [Tooltip("Between one block of the window and the next - the keyboard, the list, the button.")]
        [Min(0f)]
        public float SectionGap = 14f;

        [Header("Keyboard")]
        [Tooltip("The sheet the caps are drawn on. Drawn over the panel fill, so an alpha below one is a wash rather than a colour.")]
        public Color KeyboardFill = Color.white;

        [Min(0f)]
        public float KeyboardCornerRadius = 12f;

        [Tooltip("Inset of the caps from the edges of that sheet.")]
        [Min(0f)]
        public float KeyboardPadding = 10f;

        [Tooltip("How tall one row of caps is. The width of each is worked out from the row, so a cap is as wide as its share of the window.")]
        [Min(1f)]
        public float KeyHeight = 34f;

        [Tooltip("Between two caps in a row.")]
        [Min(0f)]
        public float KeyGap = 4f;

        [Tooltip("Between one row of caps and the next.")]
        [Min(0f)]
        public float KeyRowGap = 4f;

        [Min(0f)]
        public float KeyCornerRadius = 5f;

        [Tooltip("A key nothing is bound to.")]
        public Color KeyFill = new Color(1f, 1f, 1f, 1f);

        [Tooltip("Outlines the plain caps, so a white key on a white sheet still reads as a key. Bound caps have no border - their colour is what says so.")]
        [Min(0f)]
        public float KeyBorderSize = 1f;

        public Color KeyBorderColor = new Color(0.80f, 0.78f, 0.85f);

        [Tooltip("A key that is bound to something. The one colour a player has to learn from this window.")]
        public Color KeyBoundFill = new Color(0.91f, 0.77f, 0.36f);

        [Tooltip("A bound key while it is held down.")]
        public Color KeyDownFill = new Color(0.99f, 0.89f, 0.53f);

        public Color KeyTextColor = new Color(0.29f, 0.25f, 0.40f);

        public Color KeyBoundTextColor = new Color(0.20f, 0.15f, 0.32f);

        public Color KeyDownTextColor = new Color(0.14f, 0.10f, 0.26f);

        [Min(1f)]
        public float KeyTextSize = 15f;

        public TMP_FontAsset KeyFont;

        public FontStyles KeyTextStyle = FontStyles.Normal;

        [Header("List")]
        [Tooltip("The tallest the list may be. Past this it scrolls rather than growing, which is what keeps the button along the bottom on screen.")]
        [Min(0f)]
        public float ListMaxHeight = 320f;

        [Tooltip("Held open at this even with nothing in it, so the window does not collapse to a caption and a keyboard on a game that binds nothing.")]
        [Min(0f)]
        public float ListMinHeight = 80f;

        [Min(1f)]
        public float RowHeight = 62f;

        [Min(0f)]
        public float RowGap = 8f;

        [Min(0f)]
        public float RowCornerRadius = 8f;

        [Tooltip("Inset of a row's contents from its edges.")]
        [Min(0f)]
        public float RowPadding = 10f;

        public Color RowFill = new Color(1f, 1f, 1f, 0.12f);

        [Tooltip("A binding whose Enabled is off: the key is shown so the player knows it exists, greyed so they know it is not doing anything yet.")]
        public Color RowDisabledFill = new Color(1f, 1f, 1f, 0.05f);

        [Header("List captions")]
        public TMP_FontAsset LabelFont;

        [Min(1f)]
        public float LabelSize = 19f;

        public Color LabelColor = Color.white;

        public Color LabelDisabledColor = new Color(1f, 1f, 1f, 0.45f);

        public FontStyles LabelStyle = FontStyles.Bold;

        [Header("List key caps")]
        [Tooltip("The cap at the right of a row. Wider than a cap on the keyboard because it carries a whole key name - Enter, Shift, Page Up - rather than one letter.")]
        public Vector2 CapSize = new Vector2(84f, 42f);

        [Min(0f)]
        public float CapCornerRadius = 8f;

        public Color CapFill = new Color(0.91f, 0.77f, 0.36f);

        [Tooltip("The same cap while the key is held down - the other half of the window answering a press.")]
        public Color CapDownFill = new Color(0.99f, 0.89f, 0.53f);

        public Color CapDisabledFill = new Color(0.91f, 0.77f, 0.36f, 0.35f);

        public Color CapTextColor = new Color(0.20f, 0.15f, 0.32f);

        [Min(1f)]
        public float CapTextSize = 22f;

        public TMP_FontAsset CapFont;

        public FontStyles CapTextStyle = FontStyles.Bold;

        [Header("Empty")]
        [Tooltip("Shown in place of the list while the game has bound nothing.")]
        [Min(1f)]
        public float EmptySize = 17f;

        public Color EmptyColor = new Color(1f, 1f, 1f, 0.55f);

        [Header("Footer")]
        [Tooltip("The button that switches hotkeys on and off for the player. Full width, so its height is all there is to set.")]
        [Min(1f)]
        public float ButtonHeight = 52f;

        [Min(0f)]
        public float ButtonCornerRadius = 10f;

        [Tooltip("While hotkeys are on.")]
        public Color ButtonOnFill = new Color(0.45f, 0.76f, 0.50f);

        [Tooltip("While they are off - grey rather than red, because off is a setting the player chose and not a fault.")]
        public Color ButtonOffFill = new Color(0.79f, 0.78f, 0.84f);

        public Color ButtonTextColor = new Color(0.16f, 0.13f, 0.26f);

        [Min(1f)]
        public float ButtonTextSize = 20f;

        public TMP_FontAsset ButtonFont;

        public FontStyles ButtonTextStyle = FontStyles.Bold;

        [Header("Scrolling")]
        [Min(1f)]
        public float ScrollSensitivity = 28f;

        public bool ScrollInertia = true;

        [Range(0.01f, 0.99f)]
        public float ScrollDeceleration = 0.135f;
    }
}
