namespace FlappyTemplate
{
    /// <summary>The direction an unplaced item is carried when it looks for a free cell.</summary>
    // Whichever axis is named here is the one that fills up first, and the other is the one that grows:
    // in Row the columns are the ones you defined and rows are added underneath as they are needed.
    public enum EGridFlow
    {
        /// <summary>Fill a row left to right, then start another row. CSS: <c>grid-auto-flow: row</c>.</summary>
        Row,

        /// <summary>Fill a column top to bottom, then start another column.</summary>
        Column,
    }
}
