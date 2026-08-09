namespace FlappyTemplate
{
    /// <summary>Which way a history strip runs.</summary>
    // The two are the same code with the axes swapped, so nothing below this cares which one is picked - it
    // only ever asks "along" and "across".
    public enum EHistoryFlow
    {
        /// <summary>A row. The usual case: a strip of the last few bets over or under the game.</summary>
        Horizontal,

        /// <summary>A column, for a side panel.</summary>
        Vertical,
    }
}
