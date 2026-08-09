namespace FlappyTemplate
{
    /// <summary>When the body of a window scrolls rather than growing.</summary>
    // A window is bounded by the screen it is drawn on, and content is not: a dialog with its details open, a
    // history with forty rows, a rules page in a language that takes more words. Something has to give, and
    // scrolling is the one answer that does not either clip the bottom or push it off the top.
    public enum EWindowScroll
    {
        /// <summary>Never. A window too tall for its parent stays too tall - the way it was before.</summary>
        Never,

        /// <summary>Only once the content will not fit the height the window is allowed. The usual case.</summary>
        WhenTooTall,

        /// <summary>Always, so the scrollbar and the room it takes do not appear and disappear as content
        /// changes.</summary>
        Always,
    }
}
