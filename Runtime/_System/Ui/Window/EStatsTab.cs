namespace FlappyTemplate
{
    /// <summary>Which half of <see cref="StatsDto"/> a statistics window is showing.</summary>
    public enum EStatsTab
    {
        /// <summary>This session. The only one the reset button can clear.</summary>
        Current,

        /// <summary>Everything the account has ever played.</summary>
        Overall,
    }
}
