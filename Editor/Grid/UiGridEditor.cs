using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace FlappyTemplate.Editor
{
    // Two lists of numbers do not look like a layout, and the layout is the whole point - so the grid is
    // drawn as the grid, at the proportions it will really have, and the numbers are what you get to when
    // you want to be exact rather than what you have to work through to get started.
    //
    // Everything in the picture is live: the dividers between tracks are draggable, an empty cell is a
    // button that builds a panel there, a panel can be dragged to another cell, and the track headers open
    // the menu that changes what kind of track they are. The lists underneath are the same values typed.
    //
    // The picture is measured by the component rather than by this file (UiGrid.Snapshot). A second copy
    // of the sizing rules living in the inspector would agree with the real one everywhere except where it
    // matters - a track that hit its ceiling, an item the flow had to move - and those are exactly the
    // cases you open the inspector to understand.
    [CustomEditor(typeof(UiGrid))]
    public class UiGridEditor : UnityEditor.Editor
    {
        private const float MapMaxHeight = 300f;
        private const float RowHeaderWidth = 44f;
        private const float ColumnHeaderHeight = 17f;
        private const float HeaderGap = 3f;

        // Half the width of a divider's grab zone. Wide enough to hit without aiming, narrow enough that
        // the cells either side of it are still mostly clickable - a thin track is the case that decides
        // this, and at 4 a 20 unit column keeps a usable middle.
        private const float HandleReach = 4f;

        private const float DragThreshold = 4f;

        // The map is a picture of the rect, and a rect that has never been laid out has no size to draw.
        private const float FallbackWidth = 480f;
        private const float FallbackHeight = 320f;

        private const string CellKindKey = "FlappyTemplate.UiGrid.CellKind";

        private static readonly GUIContent AddColumnContent = new GUIContent("Add Column", "Adds a flexible column on the right.");
        private static readonly GUIContent AddRowContent = new GUIContent("Add Row", "Adds a flexible row at the bottom.");
        private static readonly GUIContent FillContent = new GUIContent("Fill Empty Cells", "Puts a panel in every cell that has nothing in it.");
        private static readonly GUIContent GapContent = new GUIContent("Gap", "Space between columns and between rows, in canvas units.");
        private static readonly GUIContent FlowContent = new GUIContent("Auto Flow", "Which way items with no placement of their own are carried, and therefore which axis grows when they run out of room.");
        private static readonly GUIContent DenseContent = new GUIContent("Dense", "Let a later small item fall back into a hole an earlier large one skipped. Off, what is on screen follows the order in the hierarchy.");
        private static readonly GUIContent ImplicitRowContent = new GUIContent("Implicit Rows", "The shape of the rows the flow adds past the ones you defined.");
        private static readonly GUIContent ImplicitColumnContent = new GUIContent("Implicit Columns", "The shape of the columns the flow adds past the ones you defined.");
        private static readonly GUIContent HorizontalContent = new GUIContent("Align Across", "Where items sit in their cell horizontally, unless the item overrides it. Stretch writes their width.");
        private static readonly GUIContent VerticalContent = new GUIContent("Align Down", "Where items sit in their cell vertically, unless the item overrides it.");

        private static GUIStyle cellLabel;
        private static GUIStyle headerLabel;

        private SerializedProperty columns;
        private SerializedProperty rows;
        private SerializedProperty columnGap;
        private SerializedProperty rowGap;
        private SerializedProperty flow;
        private SerializedProperty dense;
        private SerializedProperty implicitRow;
        private SerializedProperty implicitColumn;
        private SerializedProperty horizontalAlign;
        private SerializedProperty verticalAlign;
        private SerializedProperty padding;

        private ReorderableList columnList;
        private ReorderableList rowList;

        private static bool showTracks = true;
        private static bool showSpacing;
        private static bool showFlow;
        private static EGridCellKind cellKind = EGridCellKind.RoundedBox;

        private int resizeAxis = -1;
        private int resizeIndex = -1;
        private float resizeOrigin;
        private float resizeStartPixels;
        private float resizeStartValue;
        private float resizeFree;

        private int dragCell = -1;
        private Vector2 dragOrigin;
        private bool dragging;
        private int dropColumn = -1;
        private int dropRow = -1;

        private void OnEnable()
        {
            columns = serializedObject.FindProperty("columns");
            rows = serializedObject.FindProperty("rows");
            columnGap = serializedObject.FindProperty("columnGap");
            rowGap = serializedObject.FindProperty("rowGap");
            flow = serializedObject.FindProperty("flow");
            dense = serializedObject.FindProperty("dense");
            implicitRow = serializedObject.FindProperty("implicitRow");
            implicitColumn = serializedObject.FindProperty("implicitColumn");
            horizontalAlign = serializedObject.FindProperty("horizontalAlign");
            verticalAlign = serializedObject.FindProperty("verticalAlign");
            padding = serializedObject.FindProperty("m_Padding");

            columnList = BuildList(columns, "Columns", 0);
            rowList = BuildList(rows, "Rows", 1);

            cellKind = (EGridCellKind)EditorPrefs.GetInt(CellKindKey, (int)EGridCellKind.RoundedBox);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var grid = (UiGrid)target;

            if (serializedObject.isEditingMultipleObjects)
            {
                EditorGUILayout.HelpBox("The map draws one grid at a time. The lists below still edit all of the selected grids.", MessageType.None);
            }
            else
            {
                DrawMap(grid);
                DrawActions(grid);
            }

            EditorGUILayout.Space();
            DrawTrackLists();
            DrawSpacing();
            DrawFlow();

            serializedObject.ApplyModifiedProperties();
        }

        // ---------------------------------------------------------------- map

        private struct Map
        {
            public Rect Area;

            /// <summary>The grid's own rect, to scale.</summary>
            public Rect Box;

            public float Scale;
            public UiGridSnapshot Snapshot;
        }

        private void DrawMap(UiGrid grid)
        {
            var size = SourceSize(grid);
            var snapshot = grid.Snapshot(size);
            if (!snapshot.IsValid)
                return;

            // The height has to be reserved before the real width is known, so it is reserved from an
            // estimate and the scale is worked out again from the rect that comes back. Getting the
            // estimate wrong only ever leaves a band of empty inspector, never a map drawn outside itself.
            float estimate = Mathf.Max(160f, EditorGUIUtility.currentViewWidth - 48f - RowHeaderWidth);
            float guess = Mathf.Min(estimate / size.x, MapMaxHeight / size.y);

            var area = GUILayoutUtility.GetRect(10f, size.y * guess + ColumnHeaderHeight + HeaderGap, GUILayout.ExpandWidth(true));

            float room = Mathf.Max(40f, area.width - RowHeaderWidth);
            float roomDown = Mathf.Max(40f, area.height - ColumnHeaderHeight - HeaderGap);
            float scale = Mathf.Min(room / size.x, roomDown / size.y);

            var map = new Map
            {
                Area = area,
                Box = new Rect(area.x + RowHeaderWidth, area.y + ColumnHeaderHeight + HeaderGap, size.x * scale, size.y * scale),
                Scale = scale,
                Snapshot = snapshot,
            };

            // Taken on every event so the id is the same one on the mouse up that it was on the mouse down;
            // an id handed out only on some events walks, and the drag it owns is dropped halfway through.
            int id = GUIUtility.GetControlID(FocusType.Passive);

            if (Event.current.type == EventType.Repaint)
                Paint(map);
            else
                Input(map, grid, id);

            // Outside the repaint branch: a control that appears on some events and not on others is a
            // layout that changes shape between the pass that measures it and the pass that draws it, and
            // the inspector says so loudly.
            if (AnyCollapsedAuto(map, grid))
            {
                EditorGUILayout.HelpBox(
                    "An Auto track came out empty. Auto asks the items in it how big they need to be, and a plain panel has no answer - give the track a Min, make it Fixed or Flexible, or put a Layout Element on what is inside it.",
                    MessageType.Info);
            }
        }

        private void Paint(Map map)
        {
            var snapshot = map.Snapshot;

            EditorGUI.DrawRect(map.Box, BackColor);

            for (int row = 0; row < snapshot.RowCount; row++)
            {
                for (int column = 0; column < snapshot.ColumnCount; column++)
                {
                    if (snapshot.CellAt(column, row) >= 0)
                        continue;

                    var empty = CellRect(map, column, row, 1, 1);
                    EditorGUI.DrawRect(empty, EmptyColor);
                    Frame(empty, LineColor);

                    if (empty.width > 22f && empty.height > 16f)
                        GUI.Label(empty, "+", CellStyle);
                }
            }

            for (int i = 0; i < snapshot.Cells.Length; i++)
            {
                var cell = snapshot.Cells[i];
                if (cell.Target == null)
                    continue;

                var rect = CellRect(map, cell.Column, cell.Row, cell.ColumnSpan, cell.RowSpan);
                EditorGUI.DrawRect(rect, cell.Explicit ? PinnedColor : FlowedColor);
                Frame(rect, LineColor);

                if (rect.height > 14f)
                    GUI.Label(rect, cell.Target.name, CellStyle);
            }

            if (dropColumn >= 0 && dropRow >= 0)
            {
                var drop = CellRect(map, dropColumn, dropRow, 1, 1);
                EditorGUI.DrawRect(drop, DropColor);
                Frame(drop, Color.white);
            }

            Frame(map.Box, LineColor);

            PaintHeaders(map);
            PaintHandles(map);
        }

        private void PaintHeaders(Map map)
        {
            var snapshot = map.Snapshot;

            for (int i = 0; i < snapshot.ColumnCount; i++)
                PaintHeader(HeaderRect(map, 0, i), 0, i, snapshot.ColumnSizes[i]);

            for (int i = 0; i < snapshot.RowCount; i++)
                PaintHeader(HeaderRect(map, 1, i), 1, i, snapshot.RowSizes[i]);
        }

        // The one place the header geometry is worked out, because a header that is drawn in one place and
        // clicked in another is worse than one that is hard to hit.
        private static Rect HeaderRect(Map map, int axis, int index)
        {
            var snapshot = map.Snapshot;

            if (axis == 0)
            {
                return new Rect(
                    map.Box.x + snapshot.ColumnPositions[index] * map.Scale,
                    map.Area.y,
                    Mathf.Max(1f, snapshot.ColumnSizes[index] * map.Scale),
                    ColumnHeaderHeight);
            }

            // A short row would leave its label unreadable and its menu unclickable, so the header keeps a
            // floor and is centred on the row instead - it is a handle for the row, not a picture of it,
            // and the row itself is drawn to scale to the right of it.
            float height = Mathf.Max(1f, snapshot.RowSizes[index] * map.Scale);
            float top = map.Box.y + snapshot.RowPositions[index] * map.Scale;

            // A tall row keeps its own band exactly; only a short one is grown, and then about its middle.
            return new Rect(
                map.Area.x,
                top + Mathf.Min(0f, (height - ColumnHeaderHeight) * 0.5f),
                RowHeaderWidth - 2f,
                Mathf.Max(ColumnHeaderHeight, height));
        }

        private void PaintHeader(Rect rect, int axis, int index, float measured)
        {
            var grid = (UiGrid)target;
            var track = grid.TrackAt(axis, index);
            bool defined = index < (axis == 0 ? columns : rows).arraySize;

            EditorGUI.DrawRect(rect, defined ? HeaderColor : ImplicitColor);
            GUI.Label(rect, new GUIContent(Label(track), Tooltip(track, measured, defined)), HeaderStyle);
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
        }

        private void PaintHandles(Map map)
        {
            var snapshot = map.Snapshot;

            for (int i = 0; i < snapshot.ColumnCount; i++)
            {
                float x = map.Box.x + (snapshot.ColumnPositions[i] + snapshot.ColumnSizes[i]) * map.Scale;
                var grab = new Rect(x - HandleReach, map.Area.y, HandleReach * 2f, map.Box.height + ColumnHeaderHeight + HeaderGap);
                EditorGUIUtility.AddCursorRect(grab, MouseCursor.ResizeHorizontal);
                EditorGUI.DrawRect(new Rect(x - 0.5f, map.Area.y, 1f, grab.height), resizeAxis == 0 && resizeIndex == i ? Color.white : GripColor);
            }

            for (int i = 0; i < snapshot.RowCount; i++)
            {
                float y = map.Box.y + (snapshot.RowPositions[i] + snapshot.RowSizes[i]) * map.Scale;
                var grab = new Rect(map.Area.x, y - HandleReach, map.Box.width + RowHeaderWidth, HandleReach * 2f);
                EditorGUIUtility.AddCursorRect(grab, MouseCursor.ResizeVertical);
                EditorGUI.DrawRect(new Rect(map.Area.x, y - 0.5f, grab.width, 1f), resizeAxis == 1 && resizeIndex == i ? Color.white : GripColor);
            }
        }

        // ---------------------------------------------------------------- input

        private void Input(Map map, UiGrid grid, int id)
        {
            var e = Event.current;

            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (!map.Area.Contains(e.mousePosition) || GUIUtility.hotControl != 0)
                        break;

                    if (Hit(map, e.mousePosition, true, out int headerAxis, out int headerIndex))
                    {
                        TrackMenu(headerAxis, headerIndex);
                        e.Use();
                        break;
                    }

                    if (Hit(map, e.mousePosition, false, out int axis, out int index))
                    {
                        BeginResize(map, axis, index);
                        GUIUtility.hotControl = id;
                        e.Use();
                        break;
                    }

                    if (map.Box.Contains(e.mousePosition))
                    {
                        Cell(map, e.mousePosition, out int column, out int row);
                        int cell = map.Snapshot.CellAt(column, row);

                        if (e.button == 1)
                        {
                            CellMenu(grid, map, column, row, cell);
                            e.Use();
                            break;
                        }

                        if (cell < 0)
                        {
                            UiGridMenu.CreateCell(grid, column, row, cellKind);
                            e.Use();

                            // The hierarchy just changed underneath a half-drawn inspector. Bailing out
                            // here lets it be drawn again from the grid as it now is.
                            GUIUtility.ExitGUI();
                            break;
                        }

                        dragCell = cell;
                        dragOrigin = e.mousePosition;
                        dragging = false;
                        GUIUtility.hotControl = id;
                        e.Use();
                    }

                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != id)
                        break;

                    if (resizeAxis >= 0)
                    {
                        Resize(map, e);
                        e.Use();
                        break;
                    }

                    if (dragCell >= 0)
                    {
                        if (!dragging && (e.mousePosition - dragOrigin).magnitude < DragThreshold)
                            break;

                        dragging = true;
                        if (map.Box.Contains(e.mousePosition))
                            Cell(map, e.mousePosition, out dropColumn, out dropRow);
                        else
                            dropColumn = dropRow = -1;

                        e.Use();
                    }

                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl != id)
                        break;

                    // The snapshot is remade for every event, so the index the drag started on is only
                    // good if the grid still holds as many items as it did then.
                    if (dragCell >= 0 && dragCell < map.Snapshot.Cells.Length)
                    {
                        var moved = map.Snapshot.Cells[dragCell];

                        // A press that never moved is a click, and a click on a panel is a request to look
                        // at it - which is what the hierarchy would have been used for otherwise.
                        if (!dragging || dropColumn < 0)
                            Selection.activeGameObject = moved.Target != null ? moved.Target.gameObject : null;
                        else if (dropColumn != moved.Column || dropRow != moved.Row)
                            Move(moved.Target, dropColumn, dropRow);
                    }

                    GUIUtility.hotControl = 0;
                    resizeAxis = -1;
                    resizeIndex = -1;
                    dragCell = -1;
                    dragging = false;
                    dropColumn = -1;
                    dropRow = -1;
                    e.Use();
                    break;
            }
        }

        // Headers and dividers overlap at the corners, so which is asked first decides which wins there;
        // the header is asked first because a menu opened by mistake is undone by pressing escape and a
        // drag started by mistake has already moved something.
        private bool Hit(Map map, Vector2 mouse, bool headers, out int axis, out int index)
        {
            var snapshot = map.Snapshot;
            axis = -1;
            index = -1;

            if (headers)
            {
                // Backwards, because where two short headers overlap the one drawn last is the one on top,
                // and the one on top is the one being aimed at.
                if (mouse.y < map.Box.y && mouse.x >= map.Box.x)
                {
                    for (int i = snapshot.ColumnCount - 1; i >= 0; i--)
                    {
                        if (HeaderRect(map, 0, i).Contains(mouse))
                        {
                            axis = 0;
                            index = i;
                            return true;
                        }
                    }
                }

                if (mouse.x < map.Box.x)
                {
                    for (int i = snapshot.RowCount - 1; i >= 0; i--)
                    {
                        if (HeaderRect(map, 1, i).Contains(mouse))
                        {
                            axis = 1;
                            index = i;
                            return true;
                        }
                    }
                }

                return false;
            }

            for (int i = 0; i < snapshot.ColumnCount; i++)
            {
                float x = map.Box.x + (snapshot.ColumnPositions[i] + snapshot.ColumnSizes[i]) * map.Scale;
                if (Mathf.Abs(mouse.x - x) <= HandleReach)
                {
                    axis = 0;
                    index = i;
                    return true;
                }
            }

            for (int i = 0; i < snapshot.RowCount; i++)
            {
                float y = map.Box.y + (snapshot.RowPositions[i] + snapshot.RowSizes[i]) * map.Scale;
                if (Mathf.Abs(mouse.y - y) <= HandleReach)
                {
                    axis = 1;
                    index = i;
                    return true;
                }
            }

            return false;
        }

        private void Cell(Map map, Vector2 mouse, out int column, out int row)
        {
            var snapshot = map.Snapshot;
            column = snapshot.ColumnCount - 1;
            row = snapshot.RowCount - 1;

            float x = (mouse.x - map.Box.x) / map.Scale;
            for (int i = 0; i < snapshot.ColumnCount; i++)
            {
                // The gap after a track counts as part of it, so a click never falls between two cells.
                if (x < snapshot.ColumnPositions[i] + snapshot.ColumnSizes[i] + columnGap.floatValue)
                {
                    column = i;
                    break;
                }
            }

            float y = (mouse.y - map.Box.y) / map.Scale;
            for (int i = 0; i < snapshot.RowCount; i++)
            {
                if (y < snapshot.RowPositions[i] + snapshot.RowSizes[i] + rowGap.floatValue)
                {
                    row = i;
                    break;
                }
            }

            column = Mathf.Clamp(column, 0, snapshot.ColumnCount - 1);
            row = Mathf.Clamp(row, 0, snapshot.RowCount - 1);
        }

        // ---------------------------------------------------------------- editing

        private void BeginResize(Map map, int axis, int index)
        {
            Materialise(axis, index);

            var track = TrackProperty(axis, index);
            var mode = track.FindPropertyRelative("mode");
            var size = track.FindPropertyRelative("size");

            resizeStartPixels = axis == 0 ? map.Snapshot.ColumnSizes[index] : map.Snapshot.RowSizes[index];

            // Dragging an Auto track is a statement that its size should be decided here rather than by
            // its contents, so it becomes a Fixed track at the size it already had - the drag then starts
            // from where the pointer is instead of from wherever the contents happened to leave it.
            if ((EGridTrack)mode.enumValueIndex == EGridTrack.Auto)
            {
                mode.enumValueIndex = (int)EGridTrack.Fixed;
                size.floatValue = Mathf.Round(resizeStartPixels);
                serializedObject.ApplyModifiedProperties();
            }

            resizeAxis = axis;
            resizeIndex = index;
            resizeOrigin = axis == 0 ? Event.current.mousePosition.x : Event.current.mousePosition.y;
            resizeStartValue = size.floatValue;
            resizeFree = Free(map.Snapshot, axis);
        }

        private void Resize(Map map, Event e)
        {
            float now = resizeAxis == 0 ? e.mousePosition.x : e.mousePosition.y;
            float wanted = Mathf.Max(0f, resizeStartPixels + (now - resizeOrigin) / Mathf.Max(0.0001f, map.Scale));

            var track = TrackProperty(resizeAxis, resizeIndex);
            var mode = track.FindPropertyRelative("mode");
            var size = track.FindPropertyRelative("size");

            switch ((EGridTrack)mode.enumValueIndex)
            {
                case EGridTrack.Percent:
                    size.floatValue = resizeFree > 1f ? Mathf.Round(wanted / resizeFree * 1000f) * 0.1f : 0f;
                    break;

                case EGridTrack.Flexible:
                    // A weight has no size of its own - it is only ever a ratio - so the drag scales it by
                    // how much bigger the track was asked to get. A track sitting at nothing has no ratio
                    // to scale, and there is nothing to do but give it a real size.
                    if (resizeStartPixels > 1f)
                    {
                        size.floatValue = Mathf.Max(0.01f, resizeStartValue * wanted / resizeStartPixels);
                    }
                    else
                    {
                        mode.enumValueIndex = (int)EGridTrack.Fixed;
                        size.floatValue = Mathf.Round(wanted);
                    }

                    break;

                default:
                    size.floatValue = Mathf.Round(wanted);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
            ((UiGrid)target).Rebuild();
        }

        private void Move(RectTransform item, int column, int row)
        {
            if (item == null)
                return;

            var placement = item.GetComponent<UiGridItem>();
            if (placement == null)
                placement = Undo.AddComponent<UiGridItem>(item.gameObject);

            Undo.RecordObject(placement, "Move Grid Cell");
            placement.PlaceAt(column, row);
            EditorUtility.SetDirty(placement);
        }

        private void TrackMenu(int axis, int index)
        {
            var grid = (UiGrid)target;
            var list = axis == 0 ? columns : rows;
            var current = grid.TrackAt(axis, index);
            bool defined = index < list.arraySize;
            float measured = Measured(axis, index);

            var menu = new GenericMenu();

            foreach (EGridTrack mode in Enum.GetValues(typeof(EGridTrack)))
            {
                var chosen = mode;
                menu.AddItem(new GUIContent(mode.ToString()), current.Mode == mode, () => SetMode(axis, index, chosen, measured));
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Fit To Size/Fixed at current size"), false, () => SetMode(axis, index, EGridTrack.Fixed, measured));
            menu.AddSeparator(string.Empty);

            menu.AddItem(new GUIContent(axis == 0 ? "Insert Left" : "Insert Above"), false, () => Insert(axis, index, false));
            menu.AddItem(new GUIContent(axis == 0 ? "Insert Right" : "Insert Below"), false, () => Insert(axis, index, true));
            menu.AddItem(new GUIContent("Duplicate"), false, () => Duplicate(axis, index));

            if (defined && list.arraySize > 1)
                menu.AddItem(new GUIContent("Delete"), false, () => Delete(axis, index));
            else
                menu.AddDisabledItem(new GUIContent("Delete"));

            menu.ShowAsContext();
        }

        private void CellMenu(UiGrid grid, Map map, int column, int row, int cell)
        {
            var menu = new GenericMenu();

            if (cell >= 0)
            {
                var item = map.Snapshot.Cells[cell].Target;

                menu.AddItem(new GUIContent("Select"), false, () => Selection.activeGameObject = item != null ? item.gameObject : null);
                menu.AddItem(new GUIContent("Pin Here"), false, () => Move(item, column, row));
                menu.AddItem(new GUIContent("Let It Flow"), false, () => Release(item));
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Span/Wider"), false, () => Resize(item, 1, 0));
                menu.AddItem(new GUIContent("Span/Narrower"), false, () => Resize(item, -1, 0));
                menu.AddItem(new GUIContent("Span/Taller"), false, () => Resize(item, 0, 1));
                menu.AddItem(new GUIContent("Span/Shorter"), false, () => Resize(item, 0, -1));
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Delete Panel"), false, () =>
                {
                    if (item != null)
                        Undo.DestroyObjectImmediate(item.gameObject);

                    grid.Rebuild();
                });
            }
            else
            {
                foreach (EGridCellKind kind in Enum.GetValues(typeof(EGridCellKind)))
                {
                    var chosen = kind;
                    menu.AddItem(new GUIContent("Add " + ObjectNames.NicifyVariableName(kind.ToString())), false,
                        () => UiGridMenu.CreateCell(grid, column, row, chosen));
                }
            }

            menu.ShowAsContext();
        }

        private void Release(RectTransform item)
        {
            if (item == null)
                return;

            var placement = item.GetComponent<UiGridItem>();
            if (placement == null)
                return;

            Undo.RecordObject(placement, "Auto Place Grid Cell");
            placement.AutoPlace = true;
            EditorUtility.SetDirty(placement);
        }

        private void Resize(RectTransform item, int columns, int rows)
        {
            if (item == null)
                return;

            var placement = item.GetComponent<UiGridItem>();
            if (placement == null)
                placement = Undo.AddComponent<UiGridItem>(item.gameObject);

            Undo.RecordObject(placement, "Span Grid Cell");
            placement.Span(placement.ColumnSpan + columns, placement.RowSpan + rows);
            EditorUtility.SetDirty(placement);
        }

        // ---------------------------------------------------------------- track properties

        // A track past the end of the list is an implicit one - the flow made it, and it is drawn from the
        // template rather than stored. Editing it has to make it real first, or the edit would land on the
        // template and move every other implicit track with it.
        private void Materialise(int axis, int index)
        {
            var list = axis == 0 ? columns : rows;
            var template = axis == 0 ? implicitColumn : implicitRow;

            while (list.arraySize <= index)
            {
                int at = list.arraySize;
                list.InsertArrayElementAtIndex(at);
                Copy(template, list.GetArrayElementAtIndex(at));
            }

            serializedObject.ApplyModifiedProperties();
        }

        private SerializedProperty TrackProperty(int axis, int index)
        {
            var list = axis == 0 ? columns : rows;
            if (index < list.arraySize)
                return list.GetArrayElementAtIndex(index);

            return axis == 0 ? implicitColumn : implicitRow;
        }

        private void SetMode(int axis, int index, EGridTrack mode, float measured)
        {
            serializedObject.Update();
            Materialise(axis, index);

            var track = TrackProperty(axis, index);
            track.FindPropertyRelative("mode").enumValueIndex = (int)mode;

            // The size means something different in every mode, so carrying the old number over would be
            // carrying a percentage into canvas units. Each mode is given the value that keeps the track
            // the size it is on screen right now, so switching mode never moves anything.
            var size = track.FindPropertyRelative("size");
            switch (mode)
            {
                case EGridTrack.Fixed:
                    size.floatValue = Mathf.Round(measured);
                    break;

                case EGridTrack.Percent:
                    float free = Free(((UiGrid)target).Snapshot(SourceSize((UiGrid)target)), axis);
                    size.floatValue = free > 1f ? Mathf.Round(measured / free * 1000f) * 0.1f : 50f;
                    break;

                case EGridTrack.Flexible:
                    size.floatValue = 1f;
                    break;
            }

            serializedObject.ApplyModifiedProperties();
            ((UiGrid)target).Rebuild();
        }

        private void Insert(int axis, int index, bool after)
        {
            serializedObject.Update();

            var list = axis == 0 ? columns : rows;
            int at = Mathf.Clamp(after ? index + 1 : index, 0, list.arraySize);

            list.InsertArrayElementAtIndex(at);

            // An inserted element copies the one before it, or arrives at every field zero when there is
            // nothing before it - a fixed track of no width, which reads as the grid having lost a column
            // rather than as an empty track. Written outright either way.
            Set(list.GetArrayElementAtIndex(at), EGridTrack.Flexible, 1f, 0f, 0f);

            serializedObject.ApplyModifiedProperties();
            ((UiGrid)target).Rebuild();
        }

        private void Duplicate(int axis, int index)
        {
            serializedObject.Update();
            Materialise(axis, index);

            var list = axis == 0 ? columns : rows;
            var source = list.GetArrayElementAtIndex(index);

            list.InsertArrayElementAtIndex(index);
            Copy(source, list.GetArrayElementAtIndex(index + 1));

            serializedObject.ApplyModifiedProperties();
            ((UiGrid)target).Rebuild();
        }

        private void Delete(int axis, int index)
        {
            serializedObject.Update();

            var list = axis == 0 ? columns : rows;
            if (index < list.arraySize && list.arraySize > 1)
                list.DeleteArrayElementAtIndex(index);

            serializedObject.ApplyModifiedProperties();
            ((UiGrid)target).Rebuild();
        }

        private static void Copy(SerializedProperty from, SerializedProperty to)
        {
            to.FindPropertyRelative("mode").enumValueIndex = from.FindPropertyRelative("mode").enumValueIndex;
            to.FindPropertyRelative("size").floatValue = from.FindPropertyRelative("size").floatValue;
            to.FindPropertyRelative("min").floatValue = from.FindPropertyRelative("min").floatValue;
            to.FindPropertyRelative("max").floatValue = from.FindPropertyRelative("max").floatValue;
        }

        private static void Set(SerializedProperty track, EGridTrack mode, float size, float min, float max)
        {
            track.FindPropertyRelative("mode").enumValueIndex = (int)mode;
            track.FindPropertyRelative("size").floatValue = size;
            track.FindPropertyRelative("min").floatValue = min;
            track.FindPropertyRelative("max").floatValue = max;
        }

        // ---------------------------------------------------------------- panels

        private void DrawActions(UiGrid grid)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(AddColumnContent, EditorStyles.miniButtonLeft))
                    Insert(0, columns.arraySize, false);

                if (GUILayout.Button(AddRowContent, EditorStyles.miniButtonRight))
                    Insert(1, rows.arraySize, false);

                GUILayout.Space(6f);

                if (GUILayout.Button(FillContent, EditorStyles.miniButtonLeft, GUILayout.Width(104f)))
                {
                    if (UiGridMenu.Fill(grid, cellKind) > 0)
                        GUIUtility.ExitGUI();
                }

                EditorGUI.BeginChangeCheck();
                cellKind = (EGridCellKind)EditorGUILayout.EnumPopup(cellKind, EditorStyles.miniPullDown, GUILayout.Width(96f));
                if (EditorGUI.EndChangeCheck())
                    EditorPrefs.SetInt(CellKindKey, (int)cellKind);
            }

            EditorGUILayout.LabelField(
                "Click an empty cell to add a panel. Drag a panel to move it, a divider to resize a track, and click a header for what kind of track it is.",
                EditorStyles.wordWrappedMiniLabel);
        }

        private void DrawTrackLists()
        {
            showTracks = EditorGUILayout.BeginFoldoutHeaderGroup(showTracks, "Tracks");
            if (showTracks)
            {
                columnList.DoLayoutList();
                rowList.DoLayoutList();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawSpacing()
        {
            showSpacing = EditorGUILayout.BeginFoldoutHeaderGroup(showSpacing, "Spacing");
            if (showSpacing)
            {
                var rect = EditorGUILayout.GetControlRect();
                var content = EditorGUI.PrefixLabel(rect, GapContent);

                float half = (content.width - 4f) * 0.5f;
                float labels = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 14f;
                EditorGUI.PropertyField(new Rect(content.x, content.y, half, content.height), columnGap, new GUIContent("X"));
                EditorGUI.PropertyField(new Rect(content.x + half + 4f, content.y, half, content.height), rowGap, new GUIContent("Y"));
                EditorGUIUtility.labelWidth = labels;

                EditorGUILayout.PropertyField(padding);
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawFlow()
        {
            showFlow = EditorGUILayout.BeginFoldoutHeaderGroup(showFlow, "Flow and Alignment");
            if (showFlow)
            {
                EditorGUILayout.PropertyField(flow, FlowContent);
                EditorGUILayout.PropertyField(dense, DenseContent);

                Inline(ImplicitRowContent, implicitRow);
                Inline(ImplicitColumnContent, implicitColumn);

                EditorGUILayout.PropertyField(horizontalAlign, HorizontalContent);
                EditorGUILayout.PropertyField(verticalAlign, VerticalContent);
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // A track is four numbers that only mean anything together, so it is drawn as one line wherever it
        // appears rather than as a foldout holding four rows.
        private static void Inline(GUIContent label, SerializedProperty track)
        {
            var content = EditorGUI.PrefixLabel(EditorGUILayout.GetControlRect(), label);
            Row(content, track, false);
        }

        private static void Row(Rect rect, SerializedProperty track, bool showLimits)
        {
            var mode = track.FindPropertyRelative("mode");
            var size = track.FindPropertyRelative("size");

            float modeWidth = Mathf.Min(84f, rect.width * 0.42f);
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, modeWidth, rect.height), mode, GUIContent.none);

            float x = rect.x + modeWidth + 4f;
            float rest = rect.width - modeWidth - 4f;
            float sizeWidth = showLimits ? rest * 0.36f : rest;

            using (new EditorGUI.DisabledScope((EGridTrack)mode.enumValueIndex == EGridTrack.Auto))
            {
                float labels = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 12f;
                EditorGUI.PropertyField(new Rect(x, rect.y, sizeWidth, rect.height), size, new GUIContent(Unit((EGridTrack)mode.enumValueIndex)));
                EditorGUIUtility.labelWidth = labels;
            }

            if (!showLimits)
                return;

            float limit = (rest - sizeWidth - 8f) * 0.5f;
            var min = track.FindPropertyRelative("min");
            var max = track.FindPropertyRelative("max");

            float wide = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 26f;
            EditorGUI.PropertyField(new Rect(x + sizeWidth + 4f, rect.y, limit, rect.height), min, new GUIContent("min"));
            EditorGUI.PropertyField(new Rect(x + sizeWidth + limit + 8f, rect.y, limit, rect.height), max, new GUIContent("max"));
            EditorGUIUtility.labelWidth = wide;
        }

        private ReorderableList BuildList(SerializedProperty property, string title, int axis)
        {
            var list = new ReorderableList(serializedObject, property, true, true, true, true)
            {
                elementHeight = EditorGUIUtility.singleLineHeight + 6f,
            };

            list.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(new Rect(rect.x, rect.y, 90f, rect.height), $"{title}  ({property.arraySize})", EditorStyles.boldLabel);
            };

            list.drawElementCallback = (rect, index, active, focused) =>
            {
                if (index >= property.arraySize)
                    return;

                rect.y += 3f;
                rect.height = EditorGUIUtility.singleLineHeight;

                var number = new Rect(rect.x, rect.y, 16f, rect.height);
                GUI.Label(number, index.ToString(), EditorStyles.miniLabel);

                Row(new Rect(rect.x + 18f, rect.y, rect.width - 18f, rect.height), property.GetArrayElementAtIndex(index), true);
            };

            list.onAddCallback = _ =>
            {
                int at = property.arraySize;
                property.InsertArrayElementAtIndex(at);
                Set(property.GetArrayElementAtIndex(at), EGridTrack.Flexible, 1f, 0f, 0f);
                serializedObject.ApplyModifiedProperties();
            };

            // The last one cannot go: a grid with no columns has nowhere to put anything, and the flow
            // would be measuring against a track list it has to invent anyway.
            list.onCanRemoveCallback = _ => property.arraySize > 1;

            return list;
        }

        // ---------------------------------------------------------------- helpers

        private static Vector2 SourceSize(UiGrid grid)
        {
            var rect = ((RectTransform)grid.transform).rect.size;
            if (rect.x > 1f && rect.y > 1f)
                return rect;

            return new Vector2(FallbackWidth, FallbackHeight);
        }

        private Rect CellRect(Map map, int column, int row, int columnSpan, int rowSpan)
        {
            var snapshot = map.Snapshot;
            int lastColumn = Mathf.Min(column + columnSpan, snapshot.ColumnCount) - 1;
            int lastRow = Mathf.Min(row + rowSpan, snapshot.RowCount) - 1;

            float x = snapshot.ColumnPositions[column];
            float y = snapshot.RowPositions[row];
            float width = snapshot.ColumnPositions[lastColumn] + snapshot.ColumnSizes[lastColumn] - x;
            float height = snapshot.RowPositions[lastRow] + snapshot.RowSizes[lastRow] - y;

            return new Rect(
                map.Box.x + x * map.Scale,
                map.Box.y + y * map.Scale,
                Mathf.Max(1f, width * map.Scale),
                Mathf.Max(1f, height * map.Scale));
        }

        /// <summary>The space the tracks of one axis are dividing up, gaps already taken out.</summary>
        private float Free(UiGridSnapshot snapshot, int axis)
        {
            var grid = (UiGrid)target;
            int count = axis == 0 ? snapshot.ColumnCount : snapshot.RowCount;
            float gaps = (axis == 0 ? columnGap.floatValue : rowGap.floatValue) * Mathf.Max(0, count - 1);
            float pad = axis == 0 ? grid.padding.horizontal : grid.padding.vertical;

            return Mathf.Max(0f, (axis == 0 ? snapshot.Size.x : snapshot.Size.y) - pad - gaps);
        }

        private float Measured(int axis, int index)
        {
            var grid = (UiGrid)target;
            var snapshot = grid.Snapshot(SourceSize(grid));
            var sizes = axis == 0 ? snapshot.ColumnSizes : snapshot.RowSizes;

            return index >= 0 && index < sizes.Length ? sizes[index] : 0f;
        }

        private bool AnyCollapsedAuto(Map map, UiGrid grid)
        {
            for (int axis = 0; axis < 2; axis++)
            {
                var sizes = axis == 0 ? map.Snapshot.ColumnSizes : map.Snapshot.RowSizes;
                for (int i = 0; i < sizes.Length; i++)
                {
                    if (sizes[i] < 0.5f && grid.TrackAt(axis, i).Mode == EGridTrack.Auto)
                        return true;
                }
            }

            return false;
        }

        private static string Label(GridTrack track)
        {
            switch (track.Mode)
            {
                case EGridTrack.Fixed:
                    return track.Size.ToString("0");

                case EGridTrack.Percent:
                    return track.Size.ToString("0.#") + "%";

                case EGridTrack.Auto:
                    return "auto";

                default:
                    return track.Size.ToString("0.##") + "fr";
            }
        }

        private static string Unit(EGridTrack mode)
        {
            switch (mode)
            {
                case EGridTrack.Fixed:
                    return "u";

                case EGridTrack.Percent:
                    return "%";

                case EGridTrack.Auto:
                    return "-";

                default:
                    return "fr";
            }
        }

        private static string Tooltip(GridTrack track, float measured, bool defined)
        {
            string limits = track.Min > 0f || track.Max > 0f
                ? $"  (min {track.Min:0}, max {(track.Max > 0f ? track.Max.ToString("0") : "none")})"
                : string.Empty;

            return $"{track.Mode}{limits}\nMeasures {measured:0.#} units here.\n"
                + (defined ? "Click for track options." : "Added by the flow. Click to make it a real track.");
        }

        private static void Frame(Rect rect, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }

        private static GUIStyle CellStyle
        {
            get
            {
                if (cellLabel == null)
                {
                    cellLabel = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        clipping = TextClipping.Clip,
                        wordWrap = false,
                    };
                    cellLabel.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.92f, 0.94f, 0.98f) : new Color(0.12f, 0.14f, 0.18f);
                }

                return cellLabel;
            }
        }

        private static GUIStyle HeaderStyle
        {
            get
            {
                if (headerLabel == null)
                {
                    headerLabel = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        clipping = TextClipping.Clip,
                        wordWrap = false,
                    };
                }

                return headerLabel;
            }
        }

        private static Color BackColor => EditorGUIUtility.isProSkin ? new Color(0.16f, 0.16f, 0.17f) : new Color(0.72f, 0.72f, 0.74f);
        private static Color EmptyColor => EditorGUIUtility.isProSkin ? new Color(0.21f, 0.21f, 0.23f) : new Color(0.82f, 0.82f, 0.84f);
        private static Color FlowedColor => EditorGUIUtility.isProSkin ? new Color(0.30f, 0.38f, 0.48f) : new Color(0.62f, 0.72f, 0.86f);
        private static Color PinnedColor => EditorGUIUtility.isProSkin ? new Color(0.34f, 0.46f, 0.36f) : new Color(0.66f, 0.82f, 0.66f);
        private static Color DropColor => new Color(1f, 0.78f, 0.35f, 0.45f);
        private static Color HeaderColor => EditorGUIUtility.isProSkin ? new Color(0.26f, 0.26f, 0.28f) : new Color(0.78f, 0.78f, 0.80f);
        private static Color ImplicitColor => EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.24f, 0.7f) : new Color(0.86f, 0.86f, 0.88f, 0.7f);
        private static Color LineColor => EditorGUIUtility.isProSkin ? new Color(0f, 0f, 0f, 0.35f) : new Color(0f, 0f, 0f, 0.22f);
        private static Color GripColor => EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.22f) : new Color(0f, 0f, 0f, 0.28f);
    }
}
