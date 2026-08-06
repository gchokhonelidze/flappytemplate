using UnityEngine;

namespace FlappyTemplate
{
    /// <summary>One child of a <see cref="UiGrid"/> and the block of cells it ended up in.</summary>
    // What a UiGridItem asked for, after auto-placement has found somewhere for the ones that asked for
    // nothing and after every span has been trimmed to fit. This is what the grid lays out from and what
    // the inspector draws its map from, so both are looking at the same answer.
    public struct UiGridCell
    {
        public RectTransform Target;
        public int Column;
        public int Row;
        public int ColumnSpan;
        public int RowSpan;

        /// <summary>Resolved from the item's own override, or from the grid if it has none.</summary>
        public EGridAlign Horizontal;

        public EGridAlign Vertical;

        /// <summary>False when the item was carried here by the flow rather than placed by hand.</summary>
        public bool Explicit;

        /// <summary>True when the cell came from the grid's layout rather than from the item's own numbers.</summary>
        public bool FromLayout;

        /// <summary>The name this item answers to in a layout.</summary>
        public string Area;

        public bool Contains(int column, int row) =>
            column >= Column && column < Column + ColumnSpan && row >= Row && row < Row + RowSpan;

        /// <summary>Whether these two items are sharing any cell, and so drawing on top of each other.</summary>
        // Only ever true of items placed by hand: two of those are allowed to claim the same cell, the way
        // CSS allows it, while the flow will not put an item anywhere it finds something already.
        public bool Overlaps(UiGridCell other) =>
            Column < other.Column + other.ColumnSpan && other.Column < Column + ColumnSpan &&
            Row < other.Row + other.RowSpan && other.Row < Row + RowSpan;
    }
}
