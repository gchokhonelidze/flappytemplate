using UnityEditor;
using UnityEngine;

namespace FlappyTemplate.Editor
{
    // Four numbers - column, row, and a span each way - describe a rectangle, and a rectangle is a thing
    // to drag rather than a thing to type. So the parent grid is drawn here as it really is and the block
    // this item covers is dragged out across it, which sets all four at once and cannot produce a
    // placement that does not fit.
    //
    // The numbers are still there underneath for the cases a drag cannot do: an animator driving a
    // placement, a row that does not exist yet, a value pasted from somewhere else.
    [CustomEditor(typeof(UiGridItem))]
    [CanEditMultipleObjects]
    public class UiGridItemEditor : UnityEditor.Editor
    {
        private const float MapMaxHeight = 220f;
        private const float FallbackWidth = 480f;
        private const float FallbackHeight = 320f;

        private static readonly GUIContent AreaContent = new GUIContent("Area", "The name this panel answers to in the grid's Layout. Blank means the object's own name, which is usually all you need.");
        private static readonly GUIContent AutoContent = new GUIContent("Auto Place", "Let the grid's flow find a cell. Off, this item holds the cell below and the others are carried around it.");
        private static readonly GUIContent ColumnContent = new GUIContent("Column", "Counted from the left, starting at 0.");
        private static readonly GUIContent RowContent = new GUIContent("Row", "Counted from the top, starting at 0.");
        private static readonly GUIContent SpanContent = new GUIContent("Span", "How many columns across and rows down this covers.");
        private static readonly GUIContent OverrideContent = new GUIContent("Override Align", "Use this item's own alignment instead of the grid's.");

        private static GUIStyle label;

        private SerializedProperty area;
        private SerializedProperty autoPlace;
        private SerializedProperty column;
        private SerializedProperty row;
        private SerializedProperty columnSpan;
        private SerializedProperty rowSpan;
        private SerializedProperty overrideAlign;
        private SerializedProperty horizontalAlign;
        private SerializedProperty verticalAlign;

        private bool dragging;
        private int anchorColumn;
        private int anchorRow;

        private void OnEnable()
        {
            area = serializedObject.FindProperty("area");
            autoPlace = serializedObject.FindProperty("autoPlace");
            column = serializedObject.FindProperty("column");
            row = serializedObject.FindProperty("row");
            columnSpan = serializedObject.FindProperty("columnSpan");
            rowSpan = serializedObject.FindProperty("rowSpan");
            overrideAlign = serializedObject.FindProperty("overrideAlign");
            horizontalAlign = serializedObject.FindProperty("horizontalAlign");
            verticalAlign = serializedObject.FindProperty("verticalAlign");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var item = (UiGridItem)target;
            var grid = item.Grid;

            // The name comes first because it is what the panel is, as far as a layout is concerned;
            // everything below it is where it goes when no layout is saying.
            DrawArea(item, grid);

            if (grid == null)
            {
                EditorGUILayout.HelpBox(
                    "The parent of this object is not a Ui Grid, so nothing here is read. Placement is only ever about the grid directly above an item.",
                    MessageType.Warning);
            }
            else if (!serializedObject.isEditingMultipleObjects)
            {
                DrawMap(grid, item);
            }

            // The layout gives this panel a cell and a span outright, so the four numbers below are not
            // what is being followed. They are left readable rather than hidden: they are what the panel
            // goes back to the moment the layout stops naming it.
            bool byLayout = grid != null && !serializedObject.isEditingMultipleObjects && grid.Template != null && grid.Template.Contains(item.Area);

            using (new EditorGUI.DisabledScope(byLayout))
            {
                EditorGUILayout.PropertyField(autoPlace, AutoContent);

                using (new EditorGUI.DisabledScope(autoPlace.boolValue && !autoPlace.hasMultipleDifferentValues))
                {
                    EditorGUILayout.PropertyField(column, ColumnContent);
                    EditorGUILayout.PropertyField(row, RowContent);
                }

                Pair(SpanContent, columnSpan, rowSpan);
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(overrideAlign, OverrideContent);

            using (new EditorGUI.DisabledScope(!overrideAlign.boolValue && !overrideAlign.hasMultipleDifferentValues))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(horizontalAlign, new GUIContent("Across"));
                EditorGUILayout.PropertyField(verticalAlign, new GUIContent("Down"));
                EditorGUI.indentLevel--;
            }

            // A span is honoured whether the cell was chosen by hand or by the flow, so it is left enabled
            // above; the note is here because a span with auto placement on reads as a contradiction.
            if (autoPlace.boolValue && (columnSpan.intValue > 1 || rowSpan.intValue > 1))
            {
                EditorGUILayout.HelpBox(
                    "Auto placement looks for a hole this shape rather than for a single cell, so the span still applies.",
                    MessageType.None);
            }

            serializedObject.ApplyModifiedProperties();
        }

        // The name is the whole interface between a panel and a layout, so it is the first thing here and it
        // says outright what it resolves to - a blank field is not nothing, it is the object's own name, and
        // that is impossible to guess from an empty box.
        private void DrawArea(UiGridItem item, UiGrid grid)
        {
            EditorGUILayout.PropertyField(area, AreaContent);

            if (serializedObject.isEditingMultipleObjects)
                return;

            if (string.IsNullOrEmpty(area.stringValue))
                EditorGUILayout.LabelField(" ", $"Known as \"{item.name}\"", EditorStyles.miniLabel);

            var template = grid != null ? grid.Template : null;
            if (template == null)
                return;

            if (template.Contains(item.Area))
            {
                template.TryGetArea(item.Area, out var block);
                EditorGUILayout.LabelField(" ", $"The layout puts this at column {block.x}, row {block.y}, spanning {block.width} × {block.height}.", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"The grid's layout does not name \"{item.Area}\", so this panel is hidden while that layout is set. Rename it, add it to the layout, or clear the layout to bring it back.",
                    MessageType.Info);
            }
        }

        private void DrawMap(UiGrid grid, UiGridItem item)
        {
            var size = SourceSize(grid);
            var snapshot = grid.Snapshot(size);
            if (!snapshot.IsValid)
                return;

            float estimate = Mathf.Max(140f, EditorGUIUtility.currentViewWidth - 48f);
            float guess = Mathf.Min(estimate / size.x, MapMaxHeight / size.y);

            var area = GUILayoutUtility.GetRect(10f, size.y * guess, GUILayout.ExpandWidth(true));
            float scale = Mathf.Min(area.width / size.x, area.height / size.y);
            var box = new Rect(area.x, area.y, size.x * scale, size.y * scale);

            int id = GUIUtility.GetControlID(FocusType.Passive);

            // While the layout names this panel, the layout is where its cell comes from - so the map shows
            // where that put it and takes no drags. Dragging here would write numbers nothing reads.
            bool byLayout = grid.Template != null && grid.Template.Contains(item.Area);

            if (Event.current.type == EventType.Repaint)
                Paint(snapshot, box, scale, item);
            else if (!byLayout)
                Input(snapshot, box, scale, id);

            // A panel sharing a cell is drawn either on top of or underneath the other one, and neither
            // looks like anything but a single panel - so it is said here, where the placement that caused
            // it is being read.
            if (Shares(snapshot, item, out string other))
            {
                EditorGUILayout.HelpBox(
                    $"This shares its cell with {other}, so one of the two is hidden behind the other. Move one of them, or turn Auto Place on to have the grid find it a cell of its own.",
                    MessageType.Warning);
            }

            EditorGUILayout.LabelField(
                byLayout ? "Placed by the grid's Layout. Edit the layout to move it." : "Drag across the map to set the cell and the span at once.",
                EditorStyles.centeredGreyMiniLabel);
        }

        private void Paint(UiGridSnapshot snapshot, Rect box, float scale, UiGridItem item)
        {
            EditorGUI.DrawRect(box, BackColor);

            var mine = Mine(snapshot, item);

            for (int row = 0; row < snapshot.RowCount; row++)
            {
                for (int column = 0; column < snapshot.ColumnCount; column++)
                {
                    var rect = Cell(snapshot, box, scale, column, row, 1, 1);
                    int at = snapshot.CellAt(column, row);
                    bool ours = mine.Target != null && mine.Contains(column, row);

                    EditorGUI.DrawRect(rect, ours ? SelfColor : at >= 0 ? OtherColor : EmptyColor);
                    Frame(rect, LineColor);

                    if (at >= 0 && !ours && rect.height > 13f)
                    {
                        var occupant = snapshot.Cells[at];
                        if (occupant.Column == column && occupant.Row == row && occupant.Target != null)
                            GUI.Label(rect, occupant.Target.name, Style);
                    }
                }
            }

            if (mine.Target != null)
            {
                var block = Cell(snapshot, box, scale, mine.Column, mine.Row, mine.ColumnSpan, mine.RowSpan);
                Frame(block, Color.white);
                GUI.Label(block, $"{mine.Column},{mine.Row}", Style);
            }

            EditorGUIUtility.AddCursorRect(box, MouseCursor.Arrow);
        }

        private void Input(UiGridSnapshot snapshot, Rect box, float scale, int id)
        {
            var e = Event.current;

            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (e.button != 0 || !box.Contains(e.mousePosition) || GUIUtility.hotControl != 0)
                        break;

                    Locate(snapshot, box, scale, e.mousePosition, out anchorColumn, out anchorRow);
                    dragging = true;
                    GUIUtility.hotControl = id;
                    Write(anchorColumn, anchorRow, anchorColumn, anchorRow);
                    e.Use();
                    break;

                case EventType.MouseDrag:
                    if (!dragging || GUIUtility.hotControl != id)
                        break;

                    Locate(snapshot, box, scale, e.mousePosition, out int overColumn, out int overRow);
                    Write(anchorColumn, anchorRow, overColumn, overRow);
                    e.Use();
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl != id)
                        break;

                    dragging = false;
                    GUIUtility.hotControl = 0;
                    e.Use();
                    break;
            }
        }

        // The drag runs from wherever it started to wherever it is now, in either direction, so the block
        // is the two corners sorted rather than an origin and a length - which is what lets it be widened
        // to the left without first moving the item there.
        private void Write(int fromColumn, int fromRow, int toColumn, int toRow)
        {
            serializedObject.Update();

            autoPlace.boolValue = false;
            column.intValue = Mathf.Min(fromColumn, toColumn);
            row.intValue = Mathf.Min(fromRow, toRow);
            columnSpan.intValue = Mathf.Abs(toColumn - fromColumn) + 1;
            rowSpan.intValue = Mathf.Abs(toRow - fromRow) + 1;

            serializedObject.ApplyModifiedProperties();

            var item = (UiGridItem)target;
            var grid = item.Grid;
            if (grid != null)
                grid.Rebuild();
        }

        private static bool Shares(UiGridSnapshot snapshot, UiGridItem item, out string other)
        {
            other = null;

            var rect = (RectTransform)item.transform;
            var mine = Mine(snapshot, item);
            if (mine.Target == null)
                return false;

            for (int i = 0; i < snapshot.Cells.Length; i++)
            {
                var each = snapshot.Cells[i];
                if (each.Target == null || each.Target == rect || !each.Overlaps(mine))
                    continue;

                other = each.Target.name;
                return true;
            }

            return false;
        }

        private static UiGridCell Mine(UiGridSnapshot snapshot, UiGridItem item)
        {
            var rect = (RectTransform)item.transform;
            for (int i = 0; i < snapshot.Cells.Length; i++)
            {
                if (snapshot.Cells[i].Target == rect)
                    return snapshot.Cells[i];
            }

            return default;
        }

        private static void Locate(UiGridSnapshot snapshot, Rect box, float scale, Vector2 mouse, out int column, out int row)
        {
            column = snapshot.ColumnCount - 1;
            row = snapshot.RowCount - 1;

            float x = (mouse.x - box.x) / scale;
            for (int i = 0; i < snapshot.ColumnCount; i++)
            {
                if (x < snapshot.ColumnPositions[i] + snapshot.ColumnSizes[i])
                {
                    column = i;
                    break;
                }
            }

            float y = (mouse.y - box.y) / scale;
            for (int i = 0; i < snapshot.RowCount; i++)
            {
                if (y < snapshot.RowPositions[i] + snapshot.RowSizes[i])
                {
                    row = i;
                    break;
                }
            }

            column = Mathf.Clamp(column, 0, snapshot.ColumnCount - 1);
            row = Mathf.Clamp(row, 0, snapshot.RowCount - 1);
        }

        private static Rect Cell(UiGridSnapshot snapshot, Rect box, float scale, int column, int row, int columnSpan, int rowSpan)
        {
            int lastColumn = Mathf.Clamp(column + columnSpan - 1, 0, snapshot.ColumnCount - 1);
            int lastRow = Mathf.Clamp(row + rowSpan - 1, 0, snapshot.RowCount - 1);
            column = Mathf.Clamp(column, 0, snapshot.ColumnCount - 1);
            row = Mathf.Clamp(row, 0, snapshot.RowCount - 1);

            float x = snapshot.ColumnPositions[column];
            float y = snapshot.RowPositions[row];

            return new Rect(
                box.x + x * scale,
                box.y + y * scale,
                Mathf.Max(1f, (snapshot.ColumnPositions[lastColumn] + snapshot.ColumnSizes[lastColumn] - x) * scale),
                Mathf.Max(1f, (snapshot.RowPositions[lastRow] + snapshot.RowSizes[lastRow] - y) * scale));
        }

        private static void Pair(GUIContent title, SerializedProperty first, SerializedProperty second)
        {
            var content = EditorGUI.PrefixLabel(EditorGUILayout.GetControlRect(), title);
            float half = (content.width - 4f) * 0.5f;

            float labels = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 14f;
            EditorGUI.PropertyField(new Rect(content.x, content.y, half, content.height), first, new GUIContent("X"));
            EditorGUI.PropertyField(new Rect(content.x + half + 4f, content.y, half, content.height), second, new GUIContent("Y"));
            EditorGUIUtility.labelWidth = labels;
        }

        private static Vector2 SourceSize(UiGrid grid)
        {
            var rect = ((RectTransform)grid.transform).rect.size;
            if (rect.x > 1f && rect.y > 1f)
                return rect;

            return new Vector2(FallbackWidth, FallbackHeight);
        }

        private static void Frame(Rect rect, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }

        private static GUIStyle Style
        {
            get
            {
                if (label == null)
                {
                    label = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        clipping = TextClipping.Clip,
                        wordWrap = false,
                    };
                }

                return label;
            }
        }

        private static Color BackColor => EditorGUIUtility.isProSkin ? new Color(0.16f, 0.16f, 0.17f) : new Color(0.72f, 0.72f, 0.74f);
        private static Color EmptyColor => EditorGUIUtility.isProSkin ? new Color(0.21f, 0.21f, 0.23f) : new Color(0.82f, 0.82f, 0.84f);
        private static Color OtherColor => EditorGUIUtility.isProSkin ? new Color(0.28f, 0.30f, 0.33f) : new Color(0.74f, 0.76f, 0.80f);
        private static Color SelfColor => EditorGUIUtility.isProSkin ? new Color(0.34f, 0.46f, 0.36f) : new Color(0.64f, 0.82f, 0.66f);
        private static Color LineColor => EditorGUIUtility.isProSkin ? new Color(0f, 0f, 0f, 0.35f) : new Color(0f, 0f, 0f, 0.22f);
    }
}
