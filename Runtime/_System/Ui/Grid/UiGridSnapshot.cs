using UnityEngine;

namespace FlappyTemplate
{
    /// <summary>Where every track and every item of a <see cref="UiGrid"/> landed, measured on demand.</summary>
    // The inspector draws a picture of the grid, and a picture drawn from the fields rather than from the
    // layout would be a second implementation of the same rules - one that disagrees with the real one
    // exactly where it matters, at a track that hit its maximum or an item the flow moved somewhere else.
    // So the grid measures itself into one of these and the inspector draws that.
    public struct UiGridSnapshot
    {
        public int ColumnCount;
        public int RowCount;

        /// <summary>Left edge of each column, from the left of the grid's rect, padding included.</summary>
        public float[] ColumnPositions;

        public float[] ColumnSizes;

        /// <summary>Top edge of each row, measured downward from the top of the grid's rect.</summary>
        public float[] RowPositions;

        public float[] RowSizes;

        public UiGridCell[] Cells;

        /// <summary>The rect the measurement was made against.</summary>
        public Vector2 Size;

        public bool IsValid => ColumnSizes != null && RowSizes != null;

        /// <summary>The item covering this cell, or -1. Spanned cells answer with the item that spans them.</summary>
        public int CellAt(int column, int row)
        {
            if (Cells == null)
                return -1;

            for (int i = 0; i < Cells.Length; i++)
            {
                if (Cells[i].Contains(column, row))
                    return i;
            }

            return -1;
        }

        /// <summary>How many items are covering this cell. More than one is a stack, with one of them hidden.</summary>
        public int CountAt(int column, int row)
        {
            if (Cells == null)
                return 0;

            int count = 0;
            for (int i = 0; i < Cells.Length; i++)
            {
                if (Cells[i].Contains(column, row))
                    count++;
            }

            return count;
        }

        /// <summary>True if any two items are sharing a cell.</summary>
        // Worth asking outright: a stack looks exactly like a single panel, and the one underneath is only
        // ever found by wondering where it went.
        public bool HasOverlap
        {
            get
            {
                if (Cells == null)
                    return false;

                for (int i = 0; i < Cells.Length; i++)
                {
                    for (int j = i + 1; j < Cells.Length; j++)
                    {
                        if (Cells[i].Overlaps(Cells[j]))
                            return true;
                    }
                }

                return false;
            }
        }
    }
}
