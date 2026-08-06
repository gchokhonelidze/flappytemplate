namespace FlappyTemplate
{
    /// <summary>How a single row or column of a <see cref="UiGrid"/> works out its size.</summary>
    public enum EGridTrack
    {
        /// <summary>A size in canvas units, the same whatever the grid is given. CSS: <c>120px</c>.</summary>
        Fixed,

        /// <summary>A share of whatever is left after the other tracks and the gaps. CSS: <c>1fr</c>.</summary>
        Flexible,

        /// <summary>A percentage of the grid's content box, gaps excluded, so 50 and 50 fill it exactly.</summary>
        Percent,

        /// <summary>As big as the largest thing in it reports itself to be. CSS: <c>auto</c>.</summary>
        Auto,
    }
}
