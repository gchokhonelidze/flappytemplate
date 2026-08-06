namespace FlappyTemplate.Editor
{
    /// <summary>What the grid inspector builds when it is asked for a new cell.</summary>
    public enum EGridCellKind
    {
        /// <summary>A Rounded Box - the package's own panel, with corners already rounded.</summary>
        RoundedBox,

        /// <summary>A plain UGUI Image.</summary>
        Image,

        /// <summary>A RectTransform and nothing else, for a cell that only holds other things.</summary>
        Empty,
    }
}
