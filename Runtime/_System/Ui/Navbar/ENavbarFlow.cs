namespace FlappyTemplate
{
    /// <summary>Which way the bar runs.</summary>
    // The two are the same code with the axes swapped, so nothing below this cares which one is picked - it
    // only ever asks "along" and "across", the same way the history strip does.
    public enum ENavbarFlow
    {
        /// <summary>A row. The usual case: a strip of buttons over or under the game.</summary>
        Horizontal,

        /// <summary>A column, down one side.</summary>
        Vertical,
    }
}
