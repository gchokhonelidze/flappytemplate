namespace FlappyTemplate
{
    /// <summary>What happens when there are more bets than the strip has room for.</summary>
    public enum EHistoryOverflow
    {
        /// <summary>Show as many as fit and drop the oldest. Nothing to drag, nothing hanging out of the
        /// strip - what a row across the top of a game wants.</summary>
        // Counting how many fit needs a size to divide by, so this mode wants Element Size set along the
        // flow. With it at zero there is nothing to count with and everything is shown.
        Clamp,

        /// <summary>Keep them all and let the strip be dragged. The bar is clipped to its own rect, a flick
        /// carries, and the wheel works over it.</summary>
        Scroll,
    }
}
