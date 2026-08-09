using System;
using UnityEngine;

namespace FlappyTemplate
{
    /// <summary>One named look for a history element: what a win looks like, what a loss looks like, what
    /// anything else a game can tell apart looks like.</summary>
    // A scenario is a name and four colours, and the name is the whole point of it. Nothing here decides what
    // a bet *is* - UiHistory does that, from the amounts, from a key in the outcome, or from whatever the game
    // sets Classify to - and then asks for the scenario of that name. So a game with three kinds of round has
    // three of these, and nothing anywhere needs an enum widening to know about the third.
    //
    // Colours only. Shape - the corner radius, the font, the size of the chip - is one look for the whole
    // strip and lives on UiHistoryStyle: a win that is a different shape from a loss reads as two components
    // rather than as one strip.
    [Serializable]
    public class UiHistoryScenario
    {
        [Tooltip("What the game calls this case. Matched without regard for case, so \"Win\" and \"win\" are the same scenario.")]
        public string Name = "win";

        public Color Fill = new Color(0.149f, 0.137f, 0.263f);

        public Color BorderColor = new Color(1f, 1f, 1f, 0.08f);

        [Tooltip("Negative uses the strip's own border size, which is what all but a scenario that wants to stand out should do.")]
        public float BorderSize = -1f;

        public Color TextColor = Color.white;

        [Header("Accent")]
        [Tooltip("The bar under the element, drawn only on the ones Mark picks out - the player's own bets, by default.")]
        public Color AccentColor = new Color(1f, 0.851f, 0.243f);

        [Tooltip("Its thickness. Zero leaves it off for this scenario however the element was marked.")]
        [Min(0f)]
        public float AccentSize = 4f;

        public UiHistoryScenario Clone() => (UiHistoryScenario)MemberwiseClone();
    }
}
