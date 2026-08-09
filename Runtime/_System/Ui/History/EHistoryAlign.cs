namespace FlappyTemplate
{
    /// <summary>Where a strip that is not full sits in the room it has.</summary>
    // Only ever visible before the strip fills up - which is the first thirty seconds of every session, so
    // it is worth getting right. End with the newest last is the crash-game look: bets arrive at the right
    // edge and push the rest leftwards.
    public enum EHistoryAlign
    {
        /// <summary>Packed to the left, or to the top.</summary>
        Start,

        /// <summary>Centred in whatever room there is.</summary>
        Center,

        /// <summary>Packed to the right, or to the bottom.</summary>
        End,
    }
}
