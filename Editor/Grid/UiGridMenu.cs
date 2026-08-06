using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FlappyTemplate.Editor
{
    // Puts Grid in GameObject > UI next to the other panels, and holds the one place that knows how a cell
    // is built - the inspector's cell buttons come back here rather than each making their own.
    public static class UiGridMenu
    {
        // Rounded Box sits at 2003 and its image variant at 2004, so the grid follows them.
        private const int MenuPriority = 2005;

        // Big enough that the default two by two reads as a layout rather than as four buttons, and a
        // round number so the tracks in it come out to whole units.
        private static readonly Vector2 DefaultSize = new Vector2(480f, 320f);
        private const float CellRadius = 12f;

        [MenuItem("GameObject/UI (Canvas)/Grid", false, MenuPriority)]
        private static void CreateGrid(MenuCommand command)
        {
            var grid = Create(command.context as GameObject, 2, 2, true);

            Undo.SetCurrentGroupName("Create Grid");
            Selection.activeGameObject = grid.gameObject;
        }

        [MenuItem("GameObject/UI (Canvas)/Grid (Empty)", false, MenuPriority + 1)]
        private static void CreateEmptyGrid(MenuCommand command)
        {
            var grid = Create(command.context as GameObject, 2, 2, false);

            Undo.SetCurrentGroupName("Create Grid");
            Selection.activeGameObject = grid.gameObject;
        }

        /// <summary>Builds a grid of even tracks under the usual UI parent, optionally filled with panels.</summary>
        public static UiGrid Create(GameObject context, int columns, int rows, bool fill)
        {
            var parent = RoundedBoxMenu.ResolveParent(context);

            var host = ObjectFactory.CreateGameObject("Grid", typeof(RectTransform), typeof(UiGrid));
            var rect = (RectTransform)host.transform;
            rect.sizeDelta = DefaultSize;

            var grid = host.GetComponent<UiGrid>();
            grid.Columns.Clear();
            for (int i = 0; i < columns; i++)
                grid.Columns.Add(GridTrack.Flexible());

            grid.Rows.Clear();
            for (int i = 0; i < rows; i++)
                grid.Rows.Add(GridTrack.Flexible());

            GameObjectUtility.EnsureUniqueNameForSibling(host);
            RoundedBoxMenu.Parent(host, parent);

            if (fill)
            {
                for (int row = 0; row < rows; row++)
                {
                    for (int column = 0; column < columns; column++)
                        CreateCell(grid, column, row, EGridCellKind.RoundedBox);
                }
            }

            grid.Rebuild();
            return grid;
        }

        /// <summary>Adds one panel to a grid and pins it to a cell. Returns the panel.</summary>
        // Pinned rather than left to the flow because this is called from a click on a particular cell:
        // the panel has to land where it was asked for, and stay there when the next one is added.
        public static GameObject CreateCell(UiGrid grid, int column, int row, EGridCellKind kind)
        {
            if (grid == null)
                return null;

            GameObject cell;
            switch (kind)
            {
                case EGridCellKind.Image:
                    cell = ObjectFactory.CreateGameObject("Cell", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(UiGridItem));
                    break;

                case EGridCellKind.Empty:
                    cell = ObjectFactory.CreateGameObject("Cell", typeof(RectTransform), typeof(UiGridItem));
                    break;

                default:
                    // CanvasRenderer named outright rather than left to RequireComponent: without one the
                    // graphic has nothing to draw through, and it cannot be put back from the inspector.
                    cell = ObjectFactory.CreateGameObject("Cell", typeof(RectTransform), typeof(CanvasRenderer), typeof(RoundedBox), typeof(UiGridItem));
                    cell.GetComponent<RoundedBox>().SetCornerRadius(CellRadius);
                    break;
            }

            Undo.SetTransformParent(cell.transform, grid.transform, string.Empty);
            cell.layer = grid.gameObject.layer;

            var rect = (RectTransform)cell.transform;

            // Reparenting keeps the object where it is in the world, and the object was made at the world
            // origin - so under a canvas that is not sitting there, it arrives at whatever local position
            // that works out to. The layout writes x and y, which leaves z: a Screen Space - Camera canvas
            // is a plane distance away and scaled by the CanvasScaler, so z lands in the thousands negative
            // and the panel is behind the camera. anchoredPosition3D zeroes all three.
            rect.anchoredPosition3D = Vector3.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;

            // Something to see when the cell is not stretched: an alignment of Start on both axes leaves
            // the panel at its own size, and a rect that arrived at zero would be invisible in its cell.
            rect.sizeDelta = new Vector2(100f, 60f);

            cell.GetComponent<UiGridItem>().PlaceAt(column, row);

            GameObjectUtility.EnsureUniqueNameForSibling(cell);

            // A grid running a layout hides whatever the layout does not name, so a panel added to it and
            // left out of it would be built and vanish in the same click. It is written into the picture at
            // the cell it was asked for instead - after the rename above, since that is the name it answers
            // to. Done last, because setting the layout is what brings it back into view.
            var template = grid.Template;
            if (template != null)
            {
                Undo.RecordObject(grid, "Add Grid Cell");
                grid.SetLayout(template.With(UiGrid.AreaOf(cell.transform), column, row));
                EditorUtility.SetDirty(grid);
            }
            else
            {
                grid.Rebuild();
            }

            Undo.SetCurrentGroupName("Add Grid Cell");
            return cell;
        }

        /// <summary>Puts a panel in every cell of the grid that has nothing in it. Returns how many it made.</summary>
        public static int Fill(UiGrid grid, EGridCellKind kind)
        {
            if (grid == null)
                return 0;

            int made = 0;

            // One cell per look at the grid rather than one pass over a list of holes: pinning a panel into
            // a hole can move an auto-placed item into the next one, and filling from a list made before
            // any of that would drop a second panel on top of it.
            for (int guard = 0; guard < 256; guard++)
            {
                var snapshot = grid.Snapshot();
                int column = -1;
                int row = -1;

                for (int r = 0; r < snapshot.RowCount && row < 0; r++)
                {
                    for (int c = 0; c < snapshot.ColumnCount; c++)
                    {
                        if (snapshot.CellAt(c, r) >= 0)
                            continue;

                        column = c;
                        row = r;
                        break;
                    }
                }

                if (row < 0)
                    break;

                CreateCell(grid, column, row, kind);
                made++;
            }

            if (made > 0)
                Undo.SetCurrentGroupName("Fill Grid");

            return made;
        }
    }
}
