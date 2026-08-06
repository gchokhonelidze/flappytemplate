using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // A grid in the shape of CSS's, cut to what a canvas can do without a second thought: tracks that are
    // a fixed size, a share of the leftover, a percentage of the box or as big as their contents; items
    // that span more than one track; and a flow that drops anything unplaced into the first free cell.
    //
    // What it deliberately does not do is own its cells. A cell here is a child RectTransform of its own -
    // a panel you can select, colour, animate and prefab - rather than a rectangle the grid draws into.
    // That is the difference from GridLayoutGroup, whose cells are all one size, and it is why placement
    // lives on the child (UiGridItem) rather than in a list on this component: dragging a panel out of the
    // hierarchy takes its placement with it.
    //
    // Sizing runs per axis and in the order UGUI expects - widths are settled before heights are measured -
    // so a label that wraps reports the height it needs at the width the grid just gave it.
    [AddComponentMenu("Layout/Ui Grid")]
    public class UiGrid : LayoutGroup
    {
        // Stands in for a track that was asked for beyond the end of the list and for one that serialised
        // as null; Auto because a track nothing knows the size of is best left to its contents.
        private static readonly GridTrack FallbackTrack = GridTrack.Auto();

        // Scratch for the ILayoutIgnorer check, which is asked once per child per snapshot and would
        // otherwise allocate a list each time.
        private static readonly List<Component> Ignorers = new List<Component>();

        // Cannot grow past this in one placement pass. An item spanning more than the grid is wide would
        // otherwise never find a free cell and would walk down the rows for as long as it is allowed to.
        private const int MaxLines = 512;

        [Tooltip("A picture of the arrangement, one name per cell, rows separated by a newline or a slash. A name repeated over neighbouring cells is one panel spanning them; a dot is an empty cell. Panels it names are shown and placed, panels it does not name are hidden. Empty, every panel is shown and placed by its own Column and Row.")]
        [TextArea(2, 10)]
        [SerializeField]
        private string layout;

        [SerializeField]
        private List<GridTrack> columns = new List<GridTrack> { GridTrack.Flexible(), GridTrack.Flexible() };

        [SerializeField]
        private List<GridTrack> rows = new List<GridTrack> { GridTrack.Flexible(), GridTrack.Flexible() };

        [Tooltip("Space between columns, in canvas units. Gaps are taken out of the box before percentages and shares are worked out, so tracks still add up to the full width.")]
        [SerializeField]
        private float columnGap = 8f;

        [SerializeField]
        private float rowGap = 8f;

        [Tooltip("The direction items with no placement of their own are carried. Row fills the columns you defined and adds rows underneath as it needs them; Column does the opposite.")]
        [SerializeField]
        private EGridFlow flow = EGridFlow.Row;

        [Tooltip("Lets a later small item fall back into a hole an earlier large one skipped over. Off, the flow only ever moves forward, and the order on screen matches the order in the hierarchy.")]
        [SerializeField]
        private bool dense;

        [Tooltip("The shape of the rows the flow adds past the ones defined above.")]
        [SerializeField]
        private GridTrack implicitRow = GridTrack.Auto();

        [Tooltip("The shape of the columns the flow adds past the ones defined above.")]
        [SerializeField]
        private GridTrack implicitColumn = GridTrack.Auto();

        [Tooltip("How items sit in their cell across. Stretch writes the item's width; the others leave the width alone and only move it.")]
        [SerializeField]
        private EGridAlign horizontalAlign = EGridAlign.Stretch;

        [SerializeField]
        private EGridAlign verticalAlign = EGridAlign.Stretch;

        private readonly List<UiGridCell> cells = new List<UiGridCell>();

        // One bool per cell of the placement grid, row-major along whichever axis the flow fills first.
        private readonly List<bool> occupancy = new List<bool>();

        private readonly List<RectTransform> queried = new List<RectTransform>();

        private float[] columnSizes = new float[0];
        private float[] rowSizes = new float[0];
        private float[] columnPositions = new float[0];
        private float[] rowPositions = new float[0];
        private bool[] locked = new bool[0];

        private int columnCount = 1;
        private int rowCount = 1;

        // Parsed once per change of the string rather than once per layout pass, which is every frame for a
        // grid something is animating. Not serialized: the string is the value, this is only its shape.
        [System.NonSerialized]
        private UiGridLayout parsed;

        [System.NonSerialized]
        private string parsedFrom;

        [System.NonSerialized]
        private bool parseValid;

        // Showing and hiding children walks the same children this would be called again for, so it is done
        // once and not re-entered while it runs.
        [System.NonSerialized]
        private bool applying;

        /// <summary>The defined columns, left to right. Call <see cref="Rebuild"/> after changing one.</summary>
        public List<GridTrack> Columns => columns;

        /// <summary>The defined rows, top to bottom. Call <see cref="Rebuild"/> after changing one.</summary>
        public List<GridTrack> Rows => rows;

        public float ColumnGap
        {
            get => columnGap;
            set => SetProperty(ref columnGap, value);
        }

        public float RowGap
        {
            get => rowGap;
            set => SetProperty(ref rowGap, value);
        }

        public EGridFlow Flow
        {
            get => flow;
            set => SetProperty(ref flow, value);
        }

        public bool Dense
        {
            get => dense;
            set => SetProperty(ref dense, value);
        }

        public EGridAlign HorizontalAlign
        {
            get => horizontalAlign;
            set => SetProperty(ref horizontalAlign, value);
        }

        public EGridAlign VerticalAlign
        {
            get => verticalAlign;
            set => SetProperty(ref verticalAlign, value);
        }

        /// <summary>Columns actually in use, which is more than the defined ones if the flow added some.</summary>
        public int ColumnCount => columnCount;

        public int RowCount => rowCount;

        /// <summary>Ask for the layout to be worked out again. Needed after changing a track by code.</summary>
        public void Rebuild() => SetDirty();

        /// <summary>The arrangement, as a picture of names. Setting it shows, hides and places the panels.</summary>
        /// <example>grid.Layout = "header header / nav body / footer footer";</example>
        public string Layout
        {
            get => layout;
            set => SetLayout(value);
        }

        /// <summary>The layout as parsed, or null if there is none. Null means every panel places itself.</summary>
        public UiGridLayout Template
        {
            get
            {
                // Asked once per track per sizing pass, so the usual answer - the string is the same object
                // it was last time - costs a reference check rather than a walk down two strings.
                if (parseValid && ReferenceEquals(parsedFrom, layout))
                    return parsed;

                // Compared rather than flagged, so a layout typed into the inspector, pasted in by an undo
                // or written straight to the field by a script all take effect the same way. An equal string
                // from somewhere else adopts the new reference and skips the parse.
                if (parseValid && string.Equals(parsedFrom, layout))
                {
                    parsedFrom = layout;
                    return parsed;
                }

                parsed = UiGridLayout.Parse(layout);
                parsedFrom = layout;
                parseValid = true;

                return parsed;
            }
        }

        /// <summary>Switches to another arrangement: named panels are shown and placed, the rest are hidden.</summary>
        // The whole point of the feature, so it is one call and it is complete - there is no second step to
        // forget. Panels are matched by name, so a layout naming something this grid has not got is not an
        // error: it is a layout for a grid that will have it, and the rest of it still applies.
        public void SetLayout(string text)
        {
            layout = text;
            parsed = UiGridLayout.Parse(text);
            parsedFrom = text;
            parseValid = true;

            ApplyVisibility();
            SetDirty();
        }

        /// <summary>Drops the layout: every panel is shown again and placed by its own Column and Row.</summary>
        public void ClearLayout() => SetLayout(null);

        /// <summary>Whether this panel would be shown by the current layout. True when there is no layout.</summary>
        public bool Shows(string area)
        {
            var template = Template;
            return template == null || template.Contains(area);
        }

        /// <summary>Writes the arrangement the grid is in right now out as a layout string.</summary>
        // For getting the first one: arrange the panels by hand, read it out, and paste it into the code
        // that will switch between it and the next one. It reads the placements the grid actually resolved,
        // so what comes out is what is on screen rather than what was typed anywhere.
        public string ReadLayout()
        {
            var snapshot = Snapshot();
            var names = new string[snapshot.ColumnCount * snapshot.RowCount];

            for (int i = 0; i < snapshot.Cells.Length; i++)
            {
                var cell = snapshot.Cells[i];
                for (int row = cell.Row; row < cell.Row + cell.RowSpan && row < snapshot.RowCount; row++)
                {
                    for (int column = cell.Column; column < cell.Column + cell.ColumnSpan && column < snapshot.ColumnCount; column++)
                        names[row * snapshot.ColumnCount + column] = cell.Area;
                }
            }

            return UiGridLayout.Format(names, snapshot.ColumnCount, snapshot.RowCount);
        }

        /// <summary>The name a child answers to: its own if it has one, otherwise the object's.</summary>
        public static string AreaOf(Transform child)
        {
            if (child == null)
                return null;

            var item = child.GetComponent<UiGridItem>();
            return item != null ? item.Area : child.name;
        }

        // Shows what the layout names and hides what it does not. Every child is walked, including the ones
        // already hidden - a layout that names one of those has to be able to bring it back.
        //
        // Anything wearing an ILayoutIgnorer is left alone throughout: a background or an overlay that is
        // not part of the arrangement says so the same way it says the grid should not place it.
        private void ApplyVisibility()
        {
            if (applying)
                return;

            applying = true;
            try
            {
                var template = Template;

                for (int i = 0; i < rectTransform.childCount; i++)
                {
                    var child = rectTransform.GetChild(i) as RectTransform;
                    if (child == null || IsIgnored(child))
                        continue;

                    bool wanted = template == null || template.Contains(AreaOf(child));
                    if (child.gameObject.activeSelf == wanted)
                        continue;

                    child.gameObject.SetActive(wanted);

#if UNITY_EDITOR
                    // Nothing else marks the scene as changed when this runs from the inspector, and a
                    // panel that hides itself again on reload is worse than one that never hid.
                    if (!Application.isPlaying)
                        UnityEditor.EditorUtility.SetDirty(child.gameObject);
#endif
                }
            }
            finally
            {
                applying = false;
            }
        }

        public override void CalculateLayoutInputHorizontal()
        {
            // Fills rectChildren, skipping the inactive and anything wearing an ILayoutIgnorer.
            base.CalculateLayoutInputHorizontal();

            Place(rectChildren);

            // No width to work against yet, so percentages and shares can only offer their floors - which
            // is exactly what a parent asking "how small may this be" wants to hear.
            float min = Total(0, Measure(0, -1f, true)) + padding.horizontal;
            float preferred = Total(0, Measure(0, -1f, false)) + padding.horizontal;

            SetLayoutInputForAxis(min, Mathf.Max(min, preferred), HasElastic(0) ? 1f : 0f, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            // Placement is settled and the children have their widths, so this only measures. Running the
            // flow again here would be a second answer to a question already answered.
            float min = Total(1, Measure(1, -1f, true)) + padding.vertical;
            float preferred = Total(1, Measure(1, -1f, false)) + padding.vertical;

            SetLayoutInputForAxis(min, Mathf.Max(min, preferred), HasElastic(1) ? 1f : 0f, 1);
        }

        public override void SetLayoutHorizontal()
        {
            Measure(0, rectTransform.rect.width - padding.horizontal, false);
            Positions(0);
            Apply(0);
        }

        public override void SetLayoutVertical()
        {
            Measure(1, rectTransform.rect.height - padding.vertical, false);
            Positions(1);
            Apply(1);
        }

        /// <summary>Works the whole layout out against the current rect and hands back the result.</summary>
        // For the inspector, which needs the answer outside a layout pass and against a rect that may not
        // exist yet. It writes the same working buffers the layout uses, which is harmless: every pass
        // recomputes them from scratch, and nothing reads them between passes.
        public UiGridSnapshot Snapshot() => Snapshot(rectTransform.rect.size);

        public UiGridSnapshot Snapshot(Vector2 size)
        {
            Collect(queried);
            Place(queried);

            Measure(0, size.x - padding.horizontal, false);
            Positions(0);
            Measure(1, size.y - padding.vertical, false);
            Positions(1);

            return new UiGridSnapshot
            {
                ColumnCount = columnCount,
                RowCount = rowCount,
                ColumnPositions = (float[])columnPositions.Clone(),
                ColumnSizes = (float[])columnSizes.Clone(),
                RowPositions = (float[])rowPositions.Clone(),
                RowSizes = (float[])rowSizes.Clone(),
                Cells = cells.ToArray(),
                Size = size,
            };
        }

        /// <summary>The track at this index, real or implicit. Never null.</summary>
        // The layout is asked first, because a layout that can rearrange the panels but not resize the
        // tracks they sit in is only half an arrangement: the one column a narrow layout leaves standing is
        // no use if it is still the fixed-width one the wide layout needed.
        public GridTrack TrackAt(int axis, int index)
        {
            var template = Template;
            if (template != null)
            {
                var given = template.Track(axis, index);
                if (given != null)
                    return given;
            }

            var list = axis == 0 ? columns : rows;
            if (index >= 0 && index < list.Count && list[index] != null)
                return list[index];

            return (axis == 0 ? implicitColumn : implicitRow) ?? FallbackTrack;
        }

        private void Collect(List<RectTransform> into)
        {
            into.Clear();

            for (int i = 0; i < rectTransform.childCount; i++)
            {
                var child = rectTransform.GetChild(i) as RectTransform;
                if (child == null || !child.gameObject.activeInHierarchy)
                    continue;

                if (IsIgnored(child))
                    continue;

                into.Add(child);
            }
        }

        private static bool IsIgnored(RectTransform child)
        {
            child.GetComponents(typeof(ILayoutIgnorer), Ignorers);

            for (int i = 0; i < Ignorers.Count; i++)
            {
                if (((ILayoutIgnorer)Ignorers[i]).ignoreLayout)
                    return true;
            }

            return false;
        }

        // Placement in the flow's own terms: "minor" runs along the axis that is filled - columns in Row
        // flow - and "major" is the one that grows. Written this way once rather than twice with the axes
        // swapped, since the two directions differ in nothing but which list is which.
        private void Place(List<RectTransform> items)
        {
            cells.Clear();
            occupancy.Clear();

            var template = Template;
            bool rowFlow = flow == EGridFlow.Row;

            // A layout is a picture of the whole grid, so it says how many columns and rows there are; the
            // track lists only say how big they are, and any it does not reach are implicit ones.
            int fixedCount = template != null
                ? Mathf.Max(1, rowFlow ? template.Columns : template.Rows)
                : Mathf.Max(1, rowFlow ? columns.Count : rows.Count);

            int lines = 0;
            int cursorMinor = 0;
            int cursorMajor = 0;

            // Hand-placed items claim their cells first, whatever their order in the hierarchy, so the flow
            // carries the rest around them instead of through them.
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var rect = items[i];
                    if (rect == null)
                        continue;

                    var item = rect.GetComponent<UiGridItem>();
                    var area = item != null ? item.Area : rect.name;

                    // The layout wins where it has something to say. A panel it does not name falls back to
                    // its own numbers, which is what lets one arrangement be given by name while the rest of
                    // the grid carries on being placed the way it was.
                    // Declared out here because the short circuit above leaves it untouched when there is no
                    // layout at all, which is most grids.
                    RectInt block = default;
                    bool fromLayout = template != null && template.TryGetArea(area, out block);
                    bool auto = !fromLayout && (item == null || item.AutoPlace);
                    if (auto != (pass == 1))
                        continue;

                    int minorSpan = Mathf.Max(1, item == null ? 1 : (rowFlow ? item.ColumnSpan : item.RowSpan));
                    int majorSpan = Mathf.Max(1, item == null ? 1 : (rowFlow ? item.RowSpan : item.ColumnSpan));

                    if (fromLayout)
                    {
                        minorSpan = rowFlow ? block.width : block.height;
                        majorSpan = rowFlow ? block.height : block.width;
                    }

                    minorSpan = Mathf.Min(minorSpan, fixedCount);

                    int minor;
                    int major;
                    if (auto)
                    {
                        Find(minorSpan, majorSpan, fixedCount, ref cursorMinor, ref cursorMajor, out minor, out major);
                    }
                    else
                    {
                        int wantMinor = fromLayout ? (rowFlow ? block.x : block.y) : (rowFlow ? item.Column : item.Row);
                        int wantMajor = fromLayout ? (rowFlow ? block.y : block.x) : (rowFlow ? item.Row : item.Column);

                        minor = Mathf.Clamp(wantMinor, 0, fixedCount - 1);
                        minorSpan = Mathf.Min(minorSpan, fixedCount - minor);
                        major = Mathf.Clamp(wantMajor, 0, MaxLines - 1);
                    }

                    Occupy(minor, minorSpan, major, majorSpan, fixedCount, ref lines);

                    cells.Add(new UiGridCell
                    {
                        Target = rect,
                        Column = rowFlow ? minor : major,
                        Row = rowFlow ? major : minor,
                        ColumnSpan = rowFlow ? minorSpan : majorSpan,
                        RowSpan = rowFlow ? majorSpan : minorSpan,
                        Horizontal = item != null && item.OverrideAlign ? item.HorizontalAlign : horizontalAlign,
                        Vertical = item != null && item.OverrideAlign ? item.VerticalAlign : verticalAlign,
                        Explicit = !auto,
                        FromLayout = fromLayout,
                        Area = area,
                    });
                }
            }

            if (rowFlow)
            {
                columnCount = fixedCount;
                rowCount = Mathf.Max(Mathf.Max(1, rows.Count), lines);
            }
            else
            {
                rowCount = fixedCount;
                columnCount = Mathf.Max(Mathf.Max(1, columns.Count), lines);
            }

            // A layout with a row of nothing but dots has no item to reach that far, and the row would go
            // missing - along with the gap it was there to make.
            if (template != null)
            {
                columnCount = Mathf.Max(columnCount, template.Columns);
                rowCount = Mathf.Max(rowCount, template.Rows);
            }
        }

        // Walks the flow looking for a block of free cells the right shape. The cursor is where the last
        // item finished, so the usual case - a run of single cells - is one look each rather than a scan
        // from the start; dense throws that away in exchange for filling the holes.
        private void Find(int minorSpan, int majorSpan, int fixedCount, ref int cursorMinor, ref int cursorMajor, out int minor, out int major)
        {
            int m = dense ? 0 : cursorMinor;
            int j = dense ? 0 : cursorMajor;

            for (int guard = 0; guard < MaxLines * fixedCount; guard++)
            {
                if (m + minorSpan > fixedCount)
                {
                    m = 0;
                    j++;
                    if (j >= MaxLines)
                        break;

                    continue;
                }

                if (IsFree(m, minorSpan, j, majorSpan, fixedCount))
                {
                    minor = m;
                    major = j;
                    cursorMinor = m + minorSpan;
                    cursorMajor = j;
                    return;
                }

                m++;
            }

            // Nothing fits anywhere it is allowed to look - an item wider than the grid, or a grid that has
            // run out of room. Placing it at the start of the next line keeps it visible and on the grid,
            // where a silently dropped item would just be missing.
            minor = 0;
            major = Mathf.Min(cursorMajor + 1, MaxLines - 1);
        }

        private bool IsFree(int minor, int minorSpan, int major, int majorSpan, int fixedCount)
        {
            for (int j = major; j < major + majorSpan; j++)
            {
                for (int m = minor; m < minor + minorSpan; m++)
                {
                    int index = j * fixedCount + m;

                    // Past the end of what has been allocated is past the end of what anything occupies.
                    if (index >= occupancy.Count)
                        return true;

                    if (occupancy[index])
                        return false;
                }
            }

            return true;
        }

        private void Occupy(int minor, int minorSpan, int major, int majorSpan, int fixedCount, ref int lines)
        {
            int needed = Mathf.Min(major + majorSpan, MaxLines);
            while (occupancy.Count < needed * fixedCount)
                occupancy.Add(false);

            lines = Mathf.Max(lines, needed);

            for (int j = major; j < needed; j++)
            {
                for (int m = minor; m < minor + minorSpan && m < fixedCount; m++)
                    occupancy[j * fixedCount + m] = true;
            }
        }

        // Sizes every track along one axis. A negative available size means the question is being asked
        // without a box to answer it in - what a parent does when it is working out how big to be - and
        // then anything that can only be a share of something reports its floor instead.
        private float[] Measure(int axis, float available, bool minimal)
        {
            int count = CountOf(axis);
            float gaps = GapOf(axis) * Mathf.Max(0, count - 1);
            var sizes = Buffer(axis, count);

            bool definite = available >= 0f;
            float free = definite ? Mathf.Max(0f, available - gaps) : 0f;

            for (int i = 0; i < count; i++)
            {
                var track = TrackAt(axis, i);
                switch (track.Mode)
                {
                    case EGridTrack.Fixed:
                        sizes[i] = track.Size;
                        break;

                    case EGridTrack.Percent:
                        sizes[i] = definite ? free * track.Size * 0.01f : track.Min;
                        break;

                    case EGridTrack.Auto:
                        sizes[i] = 0f;
                        break;

                    default:
                        // Flexible starts at nothing and is filled from the leftover below; with no box to
                        // take a share of, its floor is all it can claim.
                        sizes[i] = definite ? 0f : track.Min;
                        break;
                }
            }

            MeasureContent(axis, sizes, minimal);

            float used = 0f;
            for (int i = 0; i < count; i++)
            {
                sizes[i] = TrackAt(axis, i).Clamp(sizes[i]);
                used += sizes[i];
            }

            if (definite)
                Distribute(axis, sizes, free - used);

            return sizes;
        }

        // Grows the Auto tracks until everything in them fits. Items covering one track are settled first
        // so that the ones spanning several are asked only for what those have not already provided -
        // otherwise a wide item would inflate every track it crosses by its full width.
        private void MeasureContent(int axis, float[] sizes, bool minimal)
        {
            if (!HasAuto(axis))
                return;

            float gap = GapOf(axis);

            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < cells.Count; i++)
                {
                    var cell = cells[i];
                    int start = axis == 0 ? cell.Column : cell.Row;
                    if (start >= sizes.Length)
                        continue;

                    int span = Mathf.Clamp(axis == 0 ? cell.ColumnSpan : cell.RowSpan, 1, sizes.Length - start);
                    if ((span == 1) != (pass == 0))
                        continue;

                    int autos = 0;
                    float have = gap * (span - 1);
                    for (int t = start; t < start + span; t++)
                    {
                        have += sizes[t];
                        if (TrackAt(axis, t).Mode == EGridTrack.Auto)
                            autos++;
                    }

                    if (autos == 0)
                        continue;

                    float need = minimal
                        ? LayoutUtility.GetMinSize(cell.Target, axis)
                        : LayoutUtility.GetPreferredSize(cell.Target, axis);

                    float extra = need - have;
                    if (extra <= 0f)
                        continue;

                    float share = extra / autos;
                    for (int t = start; t < start + span; t++)
                    {
                        if (TrackAt(axis, t).Mode == EGridTrack.Auto)
                            sizes[t] += share;
                    }
                }
            }
        }

        // Hands the leftover to the flexible tracks in proportion to their weights. A track that hits a
        // limit takes itself out and the rest is shared again, which is what keeps a sidebar capped at 300
        // from swallowing the space it could not use.
        private void Distribute(int axis, float[] sizes, float remaining)
        {
            if (remaining <= 0.01f)
                return;

            int count = sizes.Length;
            if (locked.Length != count)
                locked = new bool[count];

            for (int i = 0; i < count; i++)
            {
                var track = TrackAt(axis, i);
                locked[i] = track.Mode != EGridTrack.Flexible || track.Size <= 0f;
            }

            // Each round can only lock tracks, so this settles; four is past the point where another round
            // would move anything a pixel on any grid worth building.
            for (int pass = 0; pass < 4 && remaining > 0.01f; pass++)
            {
                float weight = 0f;
                for (int i = 0; i < count; i++)
                {
                    if (!locked[i])
                        weight += TrackAt(axis, i).Size;
                }

                if (weight <= 0f)
                    return;

                float spare = remaining;
                bool clamped = false;

                for (int i = 0; i < count; i++)
                {
                    if (locked[i])
                        continue;

                    var track = TrackAt(axis, i);
                    float target = sizes[i] + spare * track.Size / weight;
                    float held = track.Clamp(target);

                    remaining -= held - sizes[i];
                    sizes[i] = held;

                    if (held < target - 0.01f)
                    {
                        locked[i] = true;
                        clamped = true;
                    }
                }

                if (!clamped)
                    return;
            }
        }

        private float[] Positions(int axis)
        {
            var sizes = axis == 0 ? columnSizes : rowSizes;
            var positions = axis == 0 ? Resize(ref columnPositions, sizes.Length) : Resize(ref rowPositions, sizes.Length);

            float gap = GapOf(axis);
            float cursor = axis == 0 ? padding.left : padding.top;

            for (int i = 0; i < sizes.Length; i++)
            {
                positions[i] = cursor;
                cursor += sizes[i] + gap;
            }

            return positions;
        }

        private void Apply(int axis)
        {
            var sizes = axis == 0 ? columnSizes : rowSizes;
            var positions = axis == 0 ? columnPositions : rowPositions;
            float gap = GapOf(axis);

            for (int i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                if (cell.Target == null)
                    continue;

                int start = axis == 0 ? cell.Column : cell.Row;
                if (start >= sizes.Length)
                    continue;

                int span = Mathf.Clamp(axis == 0 ? cell.ColumnSpan : cell.RowSpan, 1, sizes.Length - start);

                float position = positions[start];
                float size = gap * (span - 1);
                for (int t = start; t < start + span; t++)
                    size += sizes[t];

                var align = axis == 0 ? cell.Horizontal : cell.Vertical;
                if (align == EGridAlign.Stretch)
                {
                    SetChildAlongAxis(cell.Target, axis, position, size);
                    continue;
                }

                float desired = LayoutUtility.GetPreferredSize(cell.Target, axis);
                if (desired <= 0f)
                {
                    // Nothing in there has an opinion about its own size - a plain panel, which is most of
                    // them. Its size is left alone and editable rather than driven to zero, and only where
                    // it sits in the cell is written.
                    desired = cell.Target.rect.size[axis];
                    SetChildAlongAxis(cell.Target, axis, position + (size - desired) * Factor(align));
                    continue;
                }

                desired = Mathf.Min(desired, size);
                SetChildAlongAxis(cell.Target, axis, position + (size - desired) * Factor(align), desired);
            }
        }

        private static float Factor(EGridAlign align)
        {
            switch (align)
            {
                case EGridAlign.Center:
                    return 0.5f;

                case EGridAlign.End:
                    return 1f;

                default:
                    return 0f;
            }
        }

        private float Total(int axis, float[] sizes)
        {
            float total = GapOf(axis) * Mathf.Max(0, sizes.Length - 1);
            for (int i = 0; i < sizes.Length; i++)
                total += sizes[i];

            return total;
        }

        private bool HasAuto(int axis)
        {
            int count = CountOf(axis);
            for (int i = 0; i < count; i++)
            {
                if (TrackAt(axis, i).Mode == EGridTrack.Auto)
                    return true;
            }

            return false;
        }

        // Whether anything here would use a bigger box if it were given one, which is what a parent layout
        // reads to decide whether this is worth stretching.
        private bool HasElastic(int axis)
        {
            int count = CountOf(axis);
            for (int i = 0; i < count; i++)
            {
                var mode = TrackAt(axis, i).Mode;
                if (mode == EGridTrack.Flexible || mode == EGridTrack.Percent)
                    return true;
            }

            return false;
        }

        private float GapOf(int axis) => axis == 0 ? columnGap : rowGap;

        private int CountOf(int axis) => Mathf.Max(1, axis == 0 ? columnCount : rowCount);

        private float[] Buffer(int axis, int count)
        {
            return axis == 0 ? Resize(ref columnSizes, count) : Resize(ref rowSizes, count);
        }

        private static float[] Resize(ref float[] array, int count)
        {
            if (array.Length != count)
                array = new float[count];

            return array;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            // The string is what was saved; what it means for which panels are showing is worked out again
            // here rather than trusted from the scene, so a layout edited in a prefab reaches its instances.
            ApplyVisibility();
        }

        // A panel dropped into the grid has to be looked at: the layout may not name it, in which case it
        // should arrive hidden rather than land on top of whatever is in the cell the flow gives it.
        protected override void OnTransformChildrenChanged()
        {
            base.OnTransformChildrenChanged();
            ApplyVisibility();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            columnGap = Mathf.Max(0f, columnGap);
            rowGap = Mathf.Max(0f, rowGap);

            // Showing and hiding sends OnEnable and OnDisable around the children, which Unity will not have
            // done from inside OnValidate. One turn of the editor loop later it is an ordinary change.
            parseValid = false;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                    ApplyVisibility();
            };

            Clean(columns);
            Clean(rows);

            if (implicitColumn == null)
                implicitColumn = GridTrack.Auto();

            if (implicitRow == null)
                implicitRow = GridTrack.Auto();
        }

        // A track added straight into the list by the inspector's own array handling arrives with every
        // field at zero, which is a fixed track of no width - invisible, and easy to read as a bug in the
        // grid rather than as an empty track. Anything that lands that way is turned into a share instead.
        private static void Clean(List<GridTrack> tracks)
        {
            for (int i = 0; i < tracks.Count; i++)
            {
                if (tracks[i] == null)
                    tracks[i] = GridTrack.Flexible();
            }
        }
#endif
    }
}
