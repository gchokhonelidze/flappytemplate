namespace FlappyTemplate
{
    /// <summary>Where the buttons sit along a bar wider than they need.</summary>
    // Only read when the bar is not being fitted to its buttons: a fitted bar is exactly as long as they
    // are, and there is nothing left over to align them in.
    public enum ENavbarAlign
    {
        /// <summary>Left, or top.</summary>
        Start,

        /// <summary>The middle of the bar.</summary>
        Center,

        /// <summary>Right, or bottom.</summary>
        End,

        /// <summary>Spread out, with the first and last against the ends. Spacing becomes a minimum rather
        /// than the gap - for a bar pinned across the whole width of the screen.</summary>
        Spread,
    }
}
