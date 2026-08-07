using System.Collections.Generic;
using UnityEngine;

namespace FlappyTemplate
{
    /// <summary>A picture of a grid written as names, one per cell. CSS calls this grid-template-areas.</summary>
    // The point of it is that the arrangement is one value you can hold, print, compare and pass from code:
    //
    //     header header header
    //     nav    body   aside
    //     footer footer footer
    //
    // A name repeated over neighbouring cells is one panel spanning them, and a dot is a cell with nothing
    // in it. Rows are separated by newlines or by a slash, so the same layout also fits on one line where
    // that reads better - "header header / nav body / footer footer" - which is the form a switch between
    // layouts in code usually wants.
    //
    // What it is not is a second placement system. It is parsed into cells and spans and handed to the grid
    // as the placement for the panels it names; everything the grid does with a placement it does with this.
    public class UiGridLayout
    {
        /// <summary>A cell with nothing in it, as CSS writes it.</summary>
        public const string Empty = ".";

        private static readonly char[] RowSeparators = { '\n', '\r', '/' };
        private static readonly char[] TokenSeparators = { ' ', '\t', ',', '|' };
        private static readonly char[] Quotes = { '"', '\'' };

        // Row-major, null where a cell is empty. Kept alongside the areas because the inspector draws the
        // picture cell by cell, and a bounding box cannot say which cells of it are really claimed.
        private readonly string[] cells;
        private readonly Dictionary<string, RectInt> areas;

        // The track sizes this layout carries, if it carries any. Null is not "no tracks" but "nothing to
        // say about them" - the grid's own lists are used, exactly as they are when there is no layout.
        private readonly GridTrack[] columnTracks;
        private readonly GridTrack[] rowTracks;

        private UiGridLayout(string[] cells, int columns, int rows, Dictionary<string, RectInt> areas, GridTrack[] columnTracks, GridTrack[] rowTracks)
        {
            this.cells = cells;
            this.areas = areas;
            this.columnTracks = columnTracks;
            this.rowTracks = rowTracks;
            Columns = columns;
            Rows = rows;
        }

        public int Columns { get; }

        public int Rows { get; }

        /// <summary>Every name the picture mentions.</summary>
        public Dictionary<string, RectInt>.KeyCollection Names => areas.Keys;

        public int Count => areas.Count;

        /// <summary>Starts building a layout in code, where a picture typed as a string reads as noise.</summary>
        // A string is the right shape for a layout kept in the inspector, switched at runtime or pasted
        // between the two. It is the wrong shape for one assembled from parts - a size that came from a
        // setting, a row that is only there in one mode - where it turns into string concatenation and the
        // compiler stops being able to help. Both build the same UiGridLayout, so the choice is only ever
        // about which reads better at the call site.
        public static Builder Build() => new Builder();

        /// <summary>Assembles a layout from tracks, rows and blocks. Finish with <see cref="Done"/>.</summary>
        public class Builder
        {
            // Anything that would come back out of ToString as two cells instead of one, or as a new row.
            private static readonly char[] Illegal = { ' ', '\t', ',', '|', '\n', '\r', '/', '"', '\'', ':' };

            private struct Block
            {
                public string Name;
                public int Column;
                public int Row;
                public int ColumnSpan;
                public int RowSpan;
            }

            private readonly List<string[]> picture = new List<string[]>();
            private readonly List<Block> blocks = new List<Block>();

            private GridTrack[] columnTracks;
            private GridTrack[] rowTracks;
            private int leastColumns;
            private int leastRows;

            /// <summary>The column sizes, left to right. Same as a cols: line.</summary>
            public Builder Columns(params GridTrack[] tracks)
            {
                columnTracks = tracks != null && tracks.Length > 0 ? (GridTrack[])tracks.Clone() : null;
                return this;
            }

            /// <summary>The row sizes, top to bottom. Same as a rows: line.</summary>
            public Builder Rows(params GridTrack[] tracks)
            {
                rowTracks = tracks != null && tracks.Length > 0 ? (GridTrack[])tracks.Clone() : null;
                return this;
            }

            /// <summary>Adds a row of the picture, a name per cell. Pass <see cref="Empty"/> for a gap.</summary>
            public Builder Row(params string[] names)
            {
                picture.Add(names ?? new string[0]);
                return this;
            }

            /// <summary>Puts one name in one block of cells, wherever the rows so far have got to.</summary>
            // The other way round from Row, and better where only a few things are placed: saying that the
            // header is the top two cells beats drawing every cell of a grid to say where one of them goes.
            // Blocks are stamped after the rows, so one may be used to override the other.
            public Builder Area(string name, int column, int row, int columnSpan = 1, int rowSpan = 1)
            {
                blocks.Add(new Block
                {
                    Name = name,
                    Column = Mathf.Max(0, column),
                    Row = Mathf.Max(0, row),
                    ColumnSpan = Mathf.Max(1, columnSpan),
                    RowSpan = Mathf.Max(1, rowSpan),
                });

                return this;
            }

            /// <summary>Holds the grid open to at least this many tracks, for rows that hold nothing.</summary>
            public Builder Size(int columns, int rows)
            {
                leastColumns = Mathf.Max(0, columns);
                leastRows = Mathf.Max(0, rows);
                return this;
            }

            /// <summary>The finished layout, ready for <c>grid.SetLayout</c>.</summary>
            public UiGridLayout Done()
            {
                int columns = leastColumns;
                int rows = Mathf.Max(leastRows, picture.Count);

                for (int i = 0; i < picture.Count; i++)
                    columns = Mathf.Max(columns, picture[i].Length);

                for (int i = 0; i < blocks.Count; i++)
                {
                    columns = Mathf.Max(columns, blocks[i].Column + blocks[i].ColumnSpan);
                    rows = Mathf.Max(rows, blocks[i].Row + blocks[i].RowSpan);
                }

                // A line of sizes makes those tracks exist, the same as it does when one is read out of a
                // string - Columns(a, b, c) on its own is a three column grid with nothing in it yet.
                if (columnTracks != null)
                    columns = Mathf.Max(columns, columnTracks.Length);

                if (rowTracks != null)
                    rows = Mathf.Max(rows, rowTracks.Length);

                columns = Mathf.Max(1, columns);
                rows = Mathf.Max(1, rows);

                var cells = new string[columns * rows];

                for (int row = 0; row < picture.Count; row++)
                {
                    var names = picture[row];
                    for (int column = 0; column < names.Length && column < columns; column++)
                        cells[row * columns + column] = Clean(names[column]);
                }

                foreach (var block in blocks)
                {
                    var name = Clean(block.Name);
                    if (name == null)
                        continue;

                    for (int y = block.Row; y < block.Row + block.RowSpan && y < rows; y++)
                    {
                        for (int x = block.Column; x < block.Column + block.ColumnSpan && x < columns; x++)
                            cells[y * columns + x] = name;
                    }
                }

                return Create(cells, columns, rows, columnTracks, rowTracks);
            }

            public override string ToString() => Done().ToString();

            // A name is one word, because a layout has to survive being written out as a string and read
            // back - that is how it is stored in a scene. A name with a space in it would come back as two
            // cells, so it is said here, where the name was given, rather than found later as a panel that
            // will not show up.
            private static string Clean(string name)
            {
                if (string.IsNullOrEmpty(name))
                    return null;

                var trimmed = name.Trim();
                if (trimmed.Length == 0 || trimmed == Empty || trimmed == "-" || trimmed == "_")
                    return null;

                if (trimmed.IndexOfAny(Illegal) < 0)
                    return trimmed;

                Debug.LogWarning(
                    $"Ui Grid layout: \"{trimmed}\" cannot be a layout name - it has a space or a separator in it, "
                    + "and a layout is stored as text. Give that panel a Ui Grid Item and set its Area to a single word.");

                return trimmed;
            }
        }

        /// <summary>Reads a layout, or null if there is nothing in it to read.</summary>
        // Forgiving on purpose: this is typed into an inspector and pasted out of code, so quotes around a
        // row, commas, pipes and any amount of spacing are all allowed to mean the same thing. What it will
        // not do is guess at a name it cannot see - an unmatched name is reported by the inspector rather
        // than corrected here.
        public static UiGridLayout Parse(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            var lines = text.Split(RowSeparators, System.StringSplitOptions.RemoveEmptyEntries);
            var rows = new List<string[]>();
            GridTrack[] columnTracks = null;
            GridTrack[] rowTracks = null;

            foreach (var line in lines)
            {
                var trimmed = line.Trim().Trim(Quotes).Trim();
                if (trimmed.Length == 0)
                    continue;

                var tokens = trimmed.Split(TokenSeparators, System.StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0)
                    continue;

                // A line naming an axis is about the size of its tracks rather than about what is in them.
                // It is what makes a layout able to change the shape of the grid and not only the seating -
                // one column left standing is no use if it is the fixed-width one.
                var axis = Axis(tokens[0]);
                if (axis == 0)
                {
                    columnTracks = Tracks(tokens);
                    continue;
                }

                if (axis == 1)
                {
                    rowTracks = Tracks(tokens);
                    continue;
                }

                rows.Add(tokens);
            }

            if (rows.Count == 0 && columnTracks == null && rowTracks == null)
                return null;

            int columns = 0;
            for (int i = 0; i < rows.Count; i++)
                columns = Mathf.Max(columns, rows[i].Length);

            // The picture is padded out to whichever is bigger, so a cols: line longer than the picture
            // still makes those tracks exist and every index below walks one rectangle.
            if (columnTracks != null)
                columns = Mathf.Max(columns, columnTracks.Length);

            int rowCount = rows.Count;
            if (rowTracks != null)
                rowCount = Mathf.Max(rowCount, rowTracks.Length);

            columns = Mathf.Max(1, columns);
            rowCount = Mathf.Max(1, rowCount);

            var cells = new string[columns * rowCount];

            for (int row = 0; row < rows.Count; row++)
            {
                var tokens = rows[row];
                for (int column = 0; column < tokens.Length; column++)
                {
                    var name = tokens[column];
                    if (name == Empty || name == "-" || name == "_")
                        continue;

                    cells[row * columns + column] = name;
                }
            }

            return Create(cells, columns, rowCount, columnTracks, rowTracks);
        }

        // Where the blocks come from, whether the cells were read out of a string or put there by a builder.
        // One implementation, so the two ways of writing a layout cannot come to different conclusions about
        // what it says.
        private static UiGridLayout Create(string[] cells, int columns, int rows, GridTrack[] columnTracks, GridTrack[] rowTracks)
        {
            var areas = new Dictionary<string, RectInt>();

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    var name = cells[row * columns + column];
                    if (string.IsNullOrEmpty(name))
                        continue;

                    // A name met again anywhere else widens its box to reach there. Two separate blocks
                    // with one name become one block covering both and whatever is between them, which is
                    // what CSS calls an invalid template - IsSolid is how the inspector says so.
                    if (areas.TryGetValue(name, out var box))
                    {
                        int x = Mathf.Min(box.x, column);
                        int y = Mathf.Min(box.y, row);
                        areas[name] = new RectInt(x, y, Mathf.Max(box.xMax, column + 1) - x, Mathf.Max(box.yMax, row + 1) - y);
                    }
                    else
                    {
                        areas[name] = new RectInt(column, row, 1, 1);
                    }
                }
            }

            return new UiGridLayout(cells, columns, rows, areas, columnTracks, rowTracks);
        }

        // Which axis a line is about, or -1 for a line that is part of the picture. The colon is what makes
        // it a heading rather than a cell, so a panel may still be called "rows" if it must be.
        private static int Axis(string first)
        {
            if (!first.EndsWith(":", System.StringComparison.Ordinal))
                return -1;

            var word = first.Substring(0, first.Length - 1).Trim().ToLowerInvariant();
            switch (word)
            {
                case "col":
                case "cols":
                case "column":
                case "columns":
                    return 0;

                case "row":
                case "rows":
                    return 1;

                default:
                    return -1;
            }
        }

        private static GridTrack[] Tracks(string[] tokens)
        {
            var tracks = new List<GridTrack>();

            // From one past the heading. A token that is not a size is dropped rather than taken for one:
            // guessing at it would silently make a column the wrong shape.
            for (int i = 1; i < tokens.Length; i++)
            {
                if (GridTrack.TryParse(tokens[i], out var track))
                    tracks.Add(track);
            }

            return tracks.Count > 0 ? tracks.ToArray() : null;
        }

        public bool Contains(string name) => !string.IsNullOrEmpty(name) && areas.ContainsKey(name);

        /// <summary>The block of cells this name covers.</summary>
        public bool TryGetArea(string name, out RectInt area)
        {
            if (!string.IsNullOrEmpty(name))
                return areas.TryGetValue(name, out area);

            area = default;
            return false;
        }

        /// <summary>The name written in this cell, or null.</summary>
        public string CellAt(int column, int row)
        {
            if (column < 0 || row < 0 || column >= Columns || row >= Rows)
                return null;

            return cells[row * Columns + column];
        }

        /// <summary>Whether every cell of this name's block is really this name.</summary>
        // A name in two corners of the picture makes a block covering everything between them, including
        // cells belonging to other names - so the panel would be laid over them. Worth being told about.
        public bool IsSolid(string name)
        {
            if (!TryGetArea(name, out var area))
                return true;

            for (int row = area.y; row < area.yMax; row++)
            {
                for (int column = area.x; column < area.xMax; column++)
                {
                    if (CellAt(column, row) != name)
                        return false;
                }
            }

            return true;
        }

        /// <summary>Whether this layout says how big the track at this index should be.</summary>
        public bool HasTrack(int axis, int index)
        {
            var tracks = axis == 0 ? columnTracks : rowTracks;
            return tracks != null && index >= 0 && index < tracks.Length;
        }

        /// <summary>The size this layout gives that track, or null to leave it to the grid's own list.</summary>
        public GridTrack Track(int axis, int index)
        {
            var tracks = axis == 0 ? columnTracks : rowTracks;
            return tracks != null && index >= 0 && index < tracks.Length ? tracks[index] : null;
        }

        /// <summary>This layout with one track's size changed, extending the line to reach it if need be.</summary>
        // Extending pads with what the grid would have used anyway - a share each - rather than with
        // nothing, because a track written as a gap in the line would come out as a track of no width.
        public string WithTrack(int axis, int index, GridTrack track)
        {
            var tracks = axis == 0 ? columnTracks : rowTracks;
            int count = Mathf.Max(index + 1, tracks != null ? tracks.Length : 0);
            var next = new GridTrack[count];

            for (int i = 0; i < count; i++)
                next[i] = tracks != null && i < tracks.Length ? tracks[i] : GridTrack.Flexible();

            next[index] = track;
            return Compose(Format(cells, Columns, Rows), axis == 0 ? next : columnTracks, axis == 0 ? rowTracks : next);
        }

        /// <summary>This layout with a track put in at an index: a line of empty cells, and a size for it.</summary>
        public string Inserted(int axis, int at)
        {
            int columns = Columns + (axis == 0 ? 1 : 0);
            int rows = Rows + (axis == 0 ? 0 : 1);
            var grid = new string[columns * rows];

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    int fromColumn = axis == 0 && column > at ? column - 1 : column;
                    int fromRow = axis == 1 && row > at ? row - 1 : row;

                    if (axis == 0 && column == at)
                        continue;

                    if (axis == 1 && row == at)
                        continue;

                    grid[row * columns + column] = CellAt(fromColumn, fromRow);
                }
            }

            return Compose(Format(grid, columns, rows), Shifted(0, axis == 0 ? at : -1, 1), Shifted(1, axis == 1 ? at : -1, 1));
        }

        /// <summary>This layout with a track taken out: its cells and its size both go.</summary>
        public string Removed(int axis, int at)
        {
            int columns = Mathf.Max(1, Columns - (axis == 0 ? 1 : 0));
            int rows = Mathf.Max(1, Rows - (axis == 0 ? 0 : 1));
            var grid = new string[columns * rows];

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    int fromColumn = axis == 0 && column >= at ? column + 1 : column;
                    int fromRow = axis == 1 && row >= at ? row + 1 : row;
                    grid[row * columns + column] = CellAt(fromColumn, fromRow);
                }
            }

            return Compose(Format(grid, columns, rows), Shifted(0, axis == 0 ? at : -1, -1), Shifted(1, axis == 1 ? at : -1, -1));
        }

        private GridTrack[] Shifted(int axis, int at, int delta)
        {
            var tracks = axis == 0 ? columnTracks : rowTracks;
            if (tracks == null || at < 0)
                return tracks;

            var next = new List<GridTrack>(tracks);

            if (delta > 0)
                next.Insert(Mathf.Clamp(at, 0, next.Count), GridTrack.Flexible());
            else if (at < next.Count)
                next.RemoveAt(at);

            return next.Count > 0 ? next.ToArray() : null;
        }

        /// <summary>This layout with one more cell claimed by a name, growing the picture to reach it.</summary>
        public string With(string name, int column, int row)
        {
            int columns = Mathf.Max(Columns, column + 1);
            int rows = Mathf.Max(Rows, row + 1);
            var grid = Copy(columns, rows, null);

            grid[row * columns + column] = name;
            return Compose(Format(grid, columns, rows), columnTracks, rowTracks);
        }

        /// <summary>This layout with one name's block picked up and put down at another cell, span and all.</summary>
        public string Moved(string name, int column, int row)
        {
            if (!TryGetArea(name, out var block))
                return With(name, column, row);

            int columns = Mathf.Max(Columns, column + block.width);
            int rows = Mathf.Max(Rows, row + block.height);

            // Lifted out first, so the cells it used to hold are free even if the new block overlaps them.
            var grid = Copy(columns, rows, name);

            for (int y = 0; y < block.height; y++)
            {
                for (int x = 0; x < block.width; x++)
                    grid[(row + y) * columns + column + x] = name;
            }

            return Compose(Format(grid, columns, rows), columnTracks, rowTracks);
        }

        /// <summary>This layout with two names exchanged wherever either appears.</summary>
        // What dropping one panel onto another means here. Moving the block on top of the other one would
        // eat the cells underneath it and change the shape of a panel nobody dragged - exchanging the names
        // leaves both blocks exactly as they were drawn and only says which panel is in which.
        public string Swapped(string first, string second)
        {
            var grid = Copy(Columns, Rows, null);

            for (int i = 0; i < grid.Length; i++)
            {
                if (grid[i] == first)
                    grid[i] = second;
                else if (grid[i] == second)
                    grid[i] = first;
            }

            return Compose(Format(grid, Columns, Rows), columnTracks, rowTracks);
        }

        // Every rewrite goes out through here, so the track lines survive a panel being dragged from one
        // cell to another - they are part of the layout, not decoration on the string it was typed as.
        private static string Compose(string picture, GridTrack[] columnTracks, GridTrack[] rowTracks)
        {
            var text = new System.Text.StringBuilder();

            Line(text, "cols", columnTracks);
            Line(text, "rows", rowTracks);
            text.Append(picture);

            return text.ToString();
        }

        private static void Line(System.Text.StringBuilder text, string heading, GridTrack[] tracks)
        {
            if (tracks == null || tracks.Length == 0)
                return;

            text.Append(heading).Append(':');

            for (int i = 0; i < tracks.Length; i++)
                text.Append(' ').Append(tracks[i] != null ? tracks[i].ToToken() : "1fr");

            text.Append('\n');
        }

        private string[] Copy(int columns, int rows, string without)
        {
            var grid = new string[columns * rows];

            for (int row = 0; row < Rows && row < rows; row++)
            {
                for (int column = 0; column < Columns && column < columns; column++)
                {
                    var name = CellAt(column, row);
                    if (name != without)
                        grid[row * columns + column] = name;
                }
            }

            return grid;
        }

        public override string ToString() => Compose(Format(cells, Columns, Rows), columnTracks, rowTracks);

        /// <summary>Writes a grid of names out as the picture, with the columns lined up.</summary>
        public static string Format(string[] cells, int columns, int rows)
        {
            if (cells == null || columns <= 0 || rows <= 0)
                return string.Empty;

            // Padded per column rather than to one width for the whole picture: a single long name would
            // otherwise push every column out to its length and the shape would be lost in the spacing.
            var widths = new int[columns];
            for (int column = 0; column < columns; column++)
            {
                for (int row = 0; row < rows; row++)
                    widths[column] = Mathf.Max(widths[column], Name(cells, columns, column, row).Length);
            }

            var text = new System.Text.StringBuilder();
            for (int row = 0; row < rows; row++)
            {
                if (row > 0)
                    text.Append('\n');

                for (int column = 0; column < columns; column++)
                {
                    if (column > 0)
                        text.Append(' ');

                    var name = Name(cells, columns, column, row);
                    text.Append(name);

                    // Nothing after the last column: trailing spaces are invisible and survive a copy out
                    // into code, where they turn into a diff nobody meant to make.
                    if (column < columns - 1)
                        text.Append(' ', widths[column] - name.Length);
                }
            }

            return text.ToString();
        }

        private static string Name(string[] cells, int columns, int column, int row)
        {
            int index = row * columns + column;
            var name = index >= 0 && index < cells.Length ? cells[index] : null;
            return string.IsNullOrEmpty(name) ? Empty : name;
        }
    }
}
