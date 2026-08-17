using System;
using TMPro;
using UnityEngine;

namespace FlappyTemplate
{
    // What the inside of a game history window looks like. The window around it - panel, caption, close
    // button, backdrop - is set on those objects themselves; this is the round's outcome strip, the list of
    // transactions under it, the totals, and the two buttons along the bottom.
    //
    // The defaults are the violet-and-mint reading the rest of this folder is drawn in, with one addition the
    // other windows have no use for: a row is tinted by what the bet did - green for a round that paid, red
    // for one that did not - and the player's own row is outlined so it can be found in a list of forty.
    [Serializable]
    public class GameHistoryWindowStyle
    {
        [Header("Card")]
        [Tooltip("Drawn over the panel fill, so an alpha below one is a wash rather than a colour.")]
        public Color CardFill = new Color(1f, 1f, 1f, 0.1f);

        [Min(0f)]
        public float CardCornerRadius = 10f;

        [Tooltip("Inset of everything in a card from its edges.")]
        [Min(0f)]
        public float CardPadding = 18f;

        [Tooltip("Between one block of the window and the next - the outcome, the list, the totals, the bar.")]
        [Min(0f)]
        public float SectionGap = 14f;

        [Tooltip("Between a caption and the value beside it.")]
        [Min(0f)]
        public float ColumnGap = 12f;

        [Header("Outcome")]
        [Tooltip("Height of the strip the game's own view of the round is given. Zero measures whatever is parented into Outcome, which needs that thing to report a height of its own - a layout group, a label, a Layout Element. A plain panel reports nothing, so give it a height here.")]
        [Min(0f)]
        public float OutcomeHeight = 0f;

        [Header("Currency toggle")]
        [Tooltip("The small button that swaps every amount in the list between the currency it was bet in and its value in dollars.")]
        public Vector2 ToggleSize = new Vector2(52f, 52f);

        [Min(0f)]
        public float ToggleCornerRadius = 10f;

        public Color ToggleFill = new Color(0.69f, 0.35f, 0.95f);

        public Color ToggleTextColor = Color.white;

        [Min(1f)]
        public float ToggleTextSize = 24f;

        [Header("Transactions")]
        [Tooltip("The tallest the list may be. Past this it scrolls rather than growing, which is the whole reason it has a scroller of its own instead of leaving it to the window.")]
        [Min(0f)]
        public float ListMaxHeight = 260f;

        [Tooltip("Held open at this even with nothing in it, so a window waiting on a round does not jump as the rows arrive.")]
        [Min(0f)]
        public float ListMinHeight = 72f;

        [Min(1f)]
        public float RowHeight = 64f;

        [Min(0f)]
        public float RowGap = 6f;

        [Min(0f)]
        public float RowCornerRadius = 8f;

        [Tooltip("Inset of a row's contents from its edges.")]
        [Min(0f)]
        public float RowPadding = 8f;

        [Tooltip("A round that paid the bet back whole or better.")]
        public Color RowWinFill = new Color(0.36f, 0.72f, 0.35f);

        public Color RowLoseFill = new Color(0.72f, 0.33f, 0.36f);

        [Tooltip("Drawn around the player's own row, which is what makes it findable in a list of forty.")]
        public Color RowMineBorder = new Color(0.98f, 0.9f, 0.35f);

        [Min(0f)]
        public float RowMineBorderSize = 2f;

        public Color RowTextColor = Color.white;

        [Min(1f)]
        public float RowNameSize = 22f;

        [Min(1f)]
        public float RowAmountSize = 18f;

        [Tooltip("Between a picture and the text beside it - the coin and its amount, the avatar and the name.")]
        [Min(0f)]
        public float IconGap = 8f;

        [Header("Row mark")]
        [Tooltip("The plate at the head of a row, which says the row can be pressed for the bet behind it. Zero leaves it out.")]
        [Min(0f)]
        public float MarkSize = 34f;

        [Min(0f)]
        public float MarkCornerRadius = 8f;

        public Color MarkFill = new Color(1f, 1f, 1f, 0.22f);

        public Color MarkColor = Color.white;

        [Min(0.5f)]
        public float MarkThickness = 2.5f;

        [Tooltip("Leave empty and the mark is drawn from three bars stacked into a drum, which needs no atlas entry.")]
        public Sprite MarkIcon;

        [Header("Player")]
        [Min(0f)]
        public float AvatarSize = 32f;

        public Color AvatarFill = new Color(0.18f, 0.16f, 0.36f);

        public Color AvatarLetterColor = Color.white;

        [Header("Coin")]
        [Min(0f)]
        public float CoinSize = 20f;

        [Tooltip("The disc drawn where the currency image goes. Left showing when the server sends no image, with the currency's first letter over it.")]
        public Color CoinFill = new Color(0.98f, 0.8f, 0.08f);

        public Color CoinLetterColor = new Color(0.18f, 0.16f, 0.35f);

        [Header("Totals")]
        [Min(0f)]
        public float StatRowGap = 8f;

        [Min(1f)]
        public float StatSize = 22f;

        public Color StatCaptionColor = Color.white;

        public Color StatValueColor = Color.white;

        [Header("Text")]
        public TMP_FontAsset CaptionFont;

        [Min(1f)]
        public float CaptionSize = 22f;

        public Color CaptionColor = Color.white;

        public FontStyles CaptionStyle = FontStyles.Bold;

        public TMP_FontAsset ValueFont;

        public Color ValueColor = Color.white;

        public FontStyles ValueStyle = FontStyles.Bold;

        [Tooltip("The seeds: long strings that wrap rather than fit.")]
        [Min(1f)]
        public float HashSize = 18f;

        [Tooltip("Size of the currency code beside an amount, as a fraction of the text it sits against.")]
        [Range(0.3f, 1f)]
        public float SmallTextScale = 0.72f;

        [Header("Buttons")]
        public Vector2 DetailsSize = new Vector2(160f, 52f);

        public Vector2 VerifySize = new Vector2(170f, 52f);

        [Min(0f)]
        public float ButtonCornerRadius = 10f;

        public Color ButtonFill = new Color(0.565f, 0.894f, 0.604f);

        public Color ButtonTextColor = new Color(0.11f, 0.1f, 0.29f);

        [Min(1f)]
        public float ButtonTextSize = 22f;

        [Header("Seeds")]
        [Min(0f)]
        public float SeedRowGap = 12f;

        [Tooltip("Between a seed's caption and the string under it.")]
        [Min(0f)]
        public float CaptionGap = 6f;

        [Header("Scrolling")]
        [Min(1f)]
        public float ScrollSensitivity = 28f;

        [Tooltip("Let a flick carry on after the finger has left. What a touch screen expects, and WebGL on a phone is a touch screen.")]
        public bool ScrollInertia = true;

        [Range(0.01f, 0.99f)]
        public float ScrollDeceleration = 0.135f;

        [Header("Loader")]
        [Tooltip("Height the list stands at while the round is still on its way, so the window does not jump as the answer arrives.")]
        [Min(0f)]
        public float LoaderHeight = 140f;

        [Min(1f)]
        public float LoaderDotSize = 14f;

        [Min(0f)]
        public float LoaderDotGap = 12f;

        public Color LoaderColor = new Color(1f, 1f, 1f, 0.85f);

        [Tooltip("Seconds for one dot to swell and settle again. The three are staggered across it.")]
        [Min(0.05f)]
        public float LoaderPulse = 0.5f;

        [Header("Amounts")]
        [Tooltip("Decimal places for an amount in the currency it was bet in. Below zero uses the transaction's own Decimal Points, which is what the server says that currency takes.")]
        public int Decimals = -1;

        [Tooltip("Decimal places for an amount converted to dollars - the totals, and every row while the currency toggle is on dollars.")]
        [Min(0)]
        public int UsdDecimals = 2;

        [Tooltip("Drop trailing zeroes, keeping one after the point. What the web front does, so 0 prints as 0.0 rather than 0.00000000.")]
        public bool TrimZeros = true;

        /// <summary>A copy, for a window that wants its own colours without editing the shared style.</summary>
        // Every field here is a value or a reference to something the style does not own - a font, a sprite -
        // so a shallow copy is a whole copy.
        public GameHistoryWindowStyle Clone() => (GameHistoryWindowStyle)MemberwiseClone();
    }
}
