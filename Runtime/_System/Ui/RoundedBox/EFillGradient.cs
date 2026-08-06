namespace FlappyTemplate
{
    /// <summary>How a RoundedBox runs its fill colour across the shape.</summary>
    public enum EFillGradient
    {
        /// <summary>One flat colour.</summary>
        None = 0,

        /// <summary>Runs across the box at a set angle, spanning the shape whatever that angle is.</summary>
        Linear = 1,

        /// <summary>Runs from the middle out to the edge, following the shape rather than a circle.</summary>
        Radial = 2,
    }
}
