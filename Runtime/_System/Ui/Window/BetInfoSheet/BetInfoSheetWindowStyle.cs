using System;
using TMPro;
using UnityEngine;

namespace FlappyTemplate
{
    // What the inside of a bet info sheet looks like. The window around it - panel, close button, backdrop - is
    // set on those objects themselves; this is the header, the id line, the sections down the body with a
    // heading and an outlined box each, and the bet's parts in the last of them.
    //
    // The defaults are the flat charcoal reading the design is drawn in rather than the violet the windows next
    // door use: every value in a box with a thin rule round it, every box under a small icon and a caption, and
    // a single coloured thing on the whole dialog - the result the game draws at the top.
    [Serializable]
    public class BetInfoSheetWindowStyle
    {
        [Header("Sections")]
        [Tooltip("Between one block of the window and the next - the header, the id line, each section, the bar.")]
        [Min(0f)]
        public float SectionGap = 16f;

        [Tooltip("Between a section's heading and the box under it.")]
        [Min(0f)]
        public float HeadingGap = 8f;

        [Tooltip("Added after every heading and the id line's caption. Empty leaves them bare.")]
        public string HeadingSuffix = ":";

        [Min(1f)]
        public float HeadingSize = 22f;

        public Color HeadingColor = Color.white;

        public FontStyles HeadingStyle = FontStyles.Normal;

        [Header("Section icons")]
        [Tooltip("The small drawn mark in front of every heading - bars, a server, a screen, a hash, a list. Zero leaves them out.")]
        [Min(0f)]
        public float IconSize = 22f;

        public Color IconColor = Color.white;

        [Min(0.5f)]
        public float IconThickness = 2.5f;

        [Tooltip("Between an icon and its caption.")]
        [Min(0f)]
        public float IconGap = 12f;

        [Tooltip("Leave any of these empty and that mark is drawn from boxes, which needs no atlas entry.")]
        public Sprite StatisticsIcon;

        public Sprite ServerSeedIcon;

        public Sprite ClientSeedIcon;

        public Sprite HashIcon;

        public Sprite BetsIcon;

        [Header("Boxes")]
        [Tooltip("Inside the rule. Clear leaves the panel showing through, which is what the design does.")]
        public Color BoxFill = new Color(1f, 1f, 1f, 0f);

        public Color BoxBorder = new Color(1f, 1f, 1f, 0.16f);

        [Min(0f)]
        public float BoxBorderSize = 2f;

        [Min(0f)]
        public float BoxCornerRadius = 16f;

        [Tooltip("Inset of a box's contents from its rule, left and right.")]
        [Min(0f)]
        public float BoxPaddingX = 14f;

        [Tooltip("Inset of a box's contents from its rule, top and bottom.")]
        [Min(0f)]
        public float BoxPaddingY = 12f;

        [Header("Header")]
        [Tooltip("Width of the cell either side of the result: the currency button on the left, and room for the window's own close button on the right, so the result sits in the middle of the window rather than in the middle of what was left.")]
        [Min(0f)]
        public float HeaderSide = 44f;

        [Tooltip("Height of the strip the game's own view of the bet is given. Zero measures whatever is parented into Outcome, which needs that thing to report a height of its own - a layout group, a label, a Layout Element. A plain panel reports nothing, so give it a height here.")]
        [Min(0f)]
        public float OutcomeHeight = 0f;

        [Header("Result")]
        [Tooltip("The pill SetResult writes into - the one coloured thing on the dialog.")]
        public Color ResultFill = new Color(0.72f, 0.15f, 0.1f);

        public Color ResultBorder = new Color(0.98f, 0.8f, 0.2f);

        [Min(0f)]
        public float ResultBorderSize = 3f;

        public Color ResultTextColor = Color.white;

        [Min(1f)]
        public float ResultTextSize = 24f;

        public FontStyles ResultTextStyle = FontStyles.Normal;

        [Tooltip("Room left round the text inside the pill, across and down.")]
        public Vector2 ResultPadding = new Vector2(22f, 8f);

        [Min(0f)]
        public float ResultHeight = 46f;

        [Header("Currency toggle")]
        [Tooltip("The coin in the top left, which swaps every amount on the sheet between the currency it was bet in and its value in dollars.")]
        [Min(0f)]
        public float ToggleSize = 36f;

        public Color ToggleFill = new Color(1f, 1f, 1f, 0.08f);

        [Tooltip("The coin drawn on it while the amounts are in the bet's own currency.")]
        public Color ToggleCoinFill = new Color(0.98f, 0.72f, 0.2f);

        public Color ToggleCoinRim = new Color(0.8f, 0.5f, 0.1f);

        [Tooltip("The glyph on it while the amounts are in dollars.")]
        public Color ToggleTextColor = Color.white;

        [Min(1f)]
        public float ToggleTextSize = 20f;

        [Header("Id line")]
        [Min(1f)]
        public float RoundIdSize = 17f;

        public Color RoundIdCaptionColor = new Color(1f, 1f, 1f, 0.55f);

        public Color RoundIdValueColor = new Color(1f, 1f, 1f, 0.7f);

        [Header("Values")]
        public TMP_FontAsset Font;

        [Tooltip("A seed, a hash: long strings that wrap rather than fit.")]
        [Min(1f)]
        public float ValueSize = 20f;

        public Color ValueColor = Color.white;

        public FontStyles ValueStyle = FontStyles.Normal;

        [Tooltip("The three figures in the Statistics box.")]
        [Min(1f)]
        public float StatSize = 20f;

        public FontStyles StatValueStyle = FontStyles.Bold;

        [Tooltip("The caption over each figure in the Statistics box.")]
        [Min(1f)]
        public float StatCaptionSize = 17f;

        public Color StatCaptionColor = new Color(1f, 1f, 1f, 0.6f);

        public FontStyles StatCaptionStyle = FontStyles.Normal;

        [Tooltip("Between a total's caption and the figure under it.")]
        [Min(0f)]
        public float StatGap = 4f;

        [Tooltip("Size of the currency code or dollar sign beside an amount, as a fraction of the text it sits against.")]
        [Range(0.3f, 1f)]
        public float SmallTextScale = 1f;

        [Header("Round bets")]
        [Tooltip("The player's line: the avatar, the name, the two amounts and the chevron.")]
        [Min(1f)]
        public float RowHeight = 56f;

        [Min(0f)]
        public float RowCornerRadius = 10f;

        [Tooltip("Inset of a row's contents from its edges, left and right.")]
        [Min(0f)]
        public float RowPadding = 10f;

        public Color RowFill = new Color(1f, 1f, 1f, 0.05f);

        public Color RowTextColor = Color.white;

        [Tooltip("What came back, on a bet - or a place - that paid back whole or better.")]
        public Color RowWinColor = new Color(0.45f, 0.86f, 0.5f);

        [Tooltip("What came back, on a bet that did not.")]
        public Color RowLoseColor = Color.white;

        [Min(1f)]
        public float RowNameSize = 20f;

        [Min(1f)]
        public float RowAmountSize = 20f;

        [Tooltip("Between the avatar and the name, and between one column and the next.")]
        [Min(0f)]
        public float RowColumnGap = 10f;

        [Header("Avatar")]
        [Min(0f)]
        public float AvatarSize = 36f;

        [Min(0f)]
        public float AvatarCornerRadius = 8f;

        [Tooltip("Behind the player's picture, and all there is with the first letter of their name on it where the server sends none.")]
        public Color AvatarFill = new Color(1f, 1f, 1f, 0.14f);

        public Color AvatarLetterColor = Color.white;

        [Header("Chevron")]
        [Tooltip("The mark at the end of the player's line that opens the places the bet was spread over. Shown only on a bet the server sent places for.")]
        [Min(0f)]
        public float ChevronSize = 14f;

        [Min(0.5f)]
        public float ChevronThickness = 2f;

        public Color ChevronColor = Color.white;

        [Header("Places")]
        [Tooltip("One line per place under the player's line, while it is open.")]
        [Min(1f)]
        public float PlaceHeight = 34f;

        [Min(1f)]
        public float PlaceTextSize = 17f;

        public Color PlaceTextColor = new Color(1f, 1f, 1f, 0.75f);

        [Tooltip("The multiplier beside a place's name.")]
        public Color RowPayoutColor = new Color(1f, 1f, 1f, 0.5f);

        [Header("Verify")]
        public Vector2 VerifySize = new Vector2(170f, 48f);

        [Min(0f)]
        public float VerifyCornerRadius = 12f;

        public Color VerifyFill = new Color(1f, 1f, 1f, 0.1f);

        public Color VerifyBorder = new Color(1f, 1f, 1f, 0.16f);

        public Color VerifyTextColor = Color.white;

        [Min(1f)]
        public float VerifyTextSize = 20f;

        [Header("Loader")]
        [Tooltip("Height the body stands at while the bet is still on its way, so the window does not jump as the answer arrives.")]
        [Min(0f)]
        public float LoaderHeight = 140f;

        [Min(1f)]
        public float LoaderDotSize = 14f;

        [Min(0f)]
        public float LoaderDotGap = 12f;

        public Color LoaderColor = new Color(1f, 1f, 1f, 0.7f);

        [Tooltip("Seconds for one dot to swell and settle again. The three are staggered across it.")]
        [Min(0.05f)]
        public float LoaderPulse = 0.5f;

        [Header("Amounts")]
        [Tooltip("Decimal places for an amount in the currency it was bet in. Below zero uses the transaction's own Decimal Points, which is what the server says that currency takes.")]
        public int Decimals = -1;

        [Tooltip("Decimal places for an amount in dollars, while the currency toggle is on dollars. Below zero uses what the system says a figure takes.")]
        public int UsdDecimals = -1;

        [Tooltip("Drop trailing zeroes, keeping one after the point. What the web front does, so 0 prints as 0.0 rather than 0.00.")]
        public bool TrimZeros = true;

        [Tooltip("Written in front of a dollar figure. The design reads $ 0.0.")]
        public string UsdPrefix = "$ ";

        [Tooltip("Decimal places for a payout multiplier.")]
        [Min(0)]
        public int PayoutDecimals = 2;

        /// <summary>A copy, for a window that wants its own colours without editing the shared style.</summary>
        // Every field here is a value or a reference to something the style does not own - a font, a sprite -
        // so a shallow copy is a whole copy.
        public BetInfoSheetWindowStyle Clone() => (BetInfoSheetWindowStyle)MemberwiseClone();
    }
}
