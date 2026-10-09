using System;
using TMPro;
using UnityEngine;

namespace FlappyTemplate
{
    // What the inside of a bet info window looks like. The window around it - panel, caption, close button,
    // backdrop - is set on those objects themselves; this is the card, the profit banner, the pairs of
    // columns under it, and the two buttons along the bottom.
    //
    // The defaults are the charcoal the bet info sheet is drawn in: a card that is the faintest wash of white
    // over the dark panel, quiet captions over white values, buttons of the same wash, and a banner that is
    // green above a payout of one and red below it - the one coloured thing on the dialog.
    [Serializable]
    public class BetInfoWindowStyle
    {
        [Header("Card")]
        [Tooltip("Drawn over the panel fill, so an alpha below one is a wash rather than a colour.")]
        public Color CardFill = new Color(1f, 1f, 1f, 0.04f);

        [Min(0f)]
        public float CardCornerRadius = 16f;

        [Tooltip("Inset of everything in the card from its edges.")]
        [Min(0f)]
        public float CardPadding = 18f;

        [Tooltip("Between one block of the card and the next - the banner, a rule, a pair of columns.")]
        [Min(0f)]
        public float SectionGap = 14f;

        [Tooltip("Between a caption and the value under it.")]
        [Min(0f)]
        public float CaptionGap = 6f;

        [Tooltip("Between the two columns of a pair.")]
        [Min(0f)]
        public float ColumnGap = 12f;

        [Header("Rules")]
        public Color LineColor = new Color(1f, 1f, 1f, 0.1f);

        [Min(0.5f)]
        public float LineThickness = 1f;

        [Header("Profit banner")]
        [Min(0f)]
        public float ProfitHeight = 104f;

        [Min(0f)]
        public float ProfitCornerRadius = 12f;

        [Tooltip("A payout of one or more - the bet came back whole or better.")]
        public Color ProfitPositiveFill = new Color(0.24f, 0.55f, 0.32f);

        public Color ProfitNegativeFill = new Color(0.72f, 0.15f, 0.1f);

        public Color ProfitTextColor = Color.white;

        [Min(1f)]
        public float ProfitCaptionSize = 20f;

        [Min(1f)]
        public float ProfitAmountSize = 26f;

        [Min(0f)]
        public float ProfitCoinSize = 44f;

        [Header("Text")]
        public TMP_FontAsset CaptionFont;

        [Min(1f)]
        public float CaptionSize = 17f;

        public Color CaptionColor = new Color(1f, 1f, 1f, 0.6f);

        public FontStyles CaptionStyle = FontStyles.Normal;

        public TMP_FontAsset ValueFont;

        [Min(1f)]
        public float ValueSize = 20f;

        public Color ValueColor = Color.white;

        public FontStyles ValueStyle = FontStyles.Normal;

        [Tooltip("The bet amount and the payout, which the design draws larger than everything around them.")]
        [Min(1f)]
        public float AmountSize = 22f;

        [Tooltip("The bet id and the seeds: long strings that wrap rather than fit.")]
        [Min(1f)]
        public float HashSize = 17f;

        [Tooltip("Size of the currency code beside an amount, and of the x after a payout, as a fraction of the text they sit against.")]
        [Range(0.3f, 1f)]
        public float SmallTextScale = 0.72f;

        [Header("Coin")]
        [Min(0f)]
        public float CoinSize = 26f;

        [Tooltip("Between a picture and the text beside it - the coin and its amount, the avatar and the name.")]
        [Min(0f)]
        public float IconGap = 8f;

        [Tooltip("The disc drawn where the currency image goes. Left showing when the server sends no image, with the currency's first letter over it.")]
        public Color CoinFill = new Color(0.98f, 0.72f, 0.2f);

        public Color CoinLetterColor = new Color(0.106f, 0.106f, 0.106f);

        [Header("Player")]
        [Min(0f)]
        public float AvatarSize = 36f;

        public Color AvatarFill = new Color(1f, 1f, 1f, 0.14f);

        public Color AvatarLetterColor = Color.white;

        [Header("Bet id")]
        [Min(0f)]
        public float TickSize = 30f;

        public Color TickFill = new Color(0.45f, 0.86f, 0.5f);

        public Color TickMarkColor = new Color(0.106f, 0.106f, 0.106f);

        [Min(0.5f)]
        public float TickThickness = 3.5f;

        [Tooltip("Leave empty and the tick is drawn from a disc and two bars, which needs no atlas entry.")]
        public Sprite TickIcon;

        [Header("Game")]
        public Vector2 GameImageSize = new Vector2(120f, 90f);

        [Min(0f)]
        public float GameImageCornerRadius = 10f;

        [Tooltip("Drawn where the game image goes, and left showing when the server sends none.")]
        public Color GameImageFill = new Color(1f, 1f, 1f, 0.08f);

        [Header("Time")]
        [Min(0f)]
        public float ClockSize = 30f;

        public Color ClockColor = Color.white;

        [Min(0.5f)]
        public float ClockThickness = 2.5f;

        [Tooltip("Leave empty and the clock is drawn from a ring and two hands.")]
        public Sprite ClockIcon;

        [Header("Buttons")]
        public Vector2 DetailsSize = new Vector2(160f, 48f);

        public Vector2 VerifySize = new Vector2(170f, 48f);

        [Min(0f)]
        public float ButtonCornerRadius = 12f;

        public Color ButtonFill = new Color(1f, 1f, 1f, 0.1f);

        public Color ButtonTextColor = Color.white;

        [Min(1f)]
        public float ButtonTextSize = 20f;

        [Header("Seeds")]
        [Min(0f)]
        public float SeedRowGap = 12f;

        [Header("Loader")]
        [Tooltip("Height the card stands at while there is nothing to show yet, so the window does not jump as the answer arrives.")]
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
        [Tooltip("Decimal places for money. Below zero uses the transaction's own Decimal Points, which is what the server says the currency takes.")]
        public int Decimals = -1;

        [Min(0)]
        public int PayoutDecimals = 4;

        [Tooltip("Drop trailing zeroes, keeping one after the point. What the web front does, so 0 prints as 0.0 rather than 0.0000.")]
        public bool TrimZeros = true;

        [Header("Time format")]
        [Tooltip("The part drawn bold - the day of the month, in the design this was taken from.")]
        public string DayFormat = "dd";

        public string TimeFormat = "HH:mm:ss";

        [Tooltip("Off prints the server's timestamp in UTC.")]
        public bool LocalTime = true;

        /// <summary>A copy, for a window that wants its own colours without editing the shared style.</summary>
        // Every field here is a value or a reference to something the style does not own - a font, a sprite -
        // so a shallow copy is a whole copy.
        public BetInfoWindowStyle Clone() => (BetInfoWindowStyle)MemberwiseClone();
    }
}
