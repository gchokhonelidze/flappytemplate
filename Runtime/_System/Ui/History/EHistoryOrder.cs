namespace FlappyTemplate
{
    /// <summary>Which end of the strip the newest bet lands on.</summary>
    // Not a matter of taste as much as of where the player is looking. A strip along the bottom of a game
    // usually grows away from the action, so the newest goes nearest to it.
    public enum EHistoryOrder
    {
        /// <summary>Newest at the left, or at the top: the strip grows away from its start.</summary>
        NewestFirst,

        /// <summary>Newest at the right, or at the bottom.</summary>
        NewestLast,
    }
}
