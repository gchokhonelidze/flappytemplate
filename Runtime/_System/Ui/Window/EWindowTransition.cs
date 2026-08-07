namespace FlappyTemplate
{
    /// <summary>How a window arrives, played backwards when it leaves.</summary>
    // Slide directions are named for the way the window travels as it opens: SlideUp comes up from below
    // the parent's bottom edge, SlideLeft comes in from the right and travels left.
    public enum EWindowTransition
    {
        /// <summary>Straight on and straight off, no tween at all.</summary>
        None,

        /// <summary>Alpha only, through a CanvasGroup.</summary>
        Fade,

        /// <summary>Grows from OpenScale to full size.</summary>
        Scale,

        /// <summary>Both at once - the one that reads as a dialog opening.</summary>
        ScaleFade,

        SlideUp,
        SlideDown,
        SlideLeft,
        SlideRight,
    }
}
