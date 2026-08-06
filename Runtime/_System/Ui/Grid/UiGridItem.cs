using UnityEngine;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // Where one panel sits in the grid above it. Optional: a child with no UiGridItem is carried by the
    // flow into the first free cell and given the grid's own alignment, which is what makes dropping a
    // panel in and having it land somewhere sensible the default.
    //
    // Placement lives here rather than in a list on the grid so that it travels with the panel - a cell
    // dragged to another grid, saved as a prefab or duplicated keeps what it was told, and a list on the
    // grid would need repairing every time a child was reordered or removed.
    [ExecuteAlways]
    [AddComponentMenu("Layout/Ui Grid Item")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class UiGridItem : MonoBehaviour
    {
        [Tooltip("The name this panel answers to in the grid's Layout. Leave it blank to be known by the object's own name, which is usually what you want - name the object 'header' and it is the header.")]
        [SerializeField]
        private string area;

        [Tooltip("Let the grid's flow find a cell for this. Off, the column and row below are used exactly, and the flow carries the other items around it.")]
        [SerializeField]
        private bool autoPlace = true;

        [Tooltip("Counted from the left, starting at 0.")]
        [SerializeField]
        private int column;

        [Tooltip("Counted from the top, starting at 0. A row past the last one defined adds implicit rows to reach it.")]
        [SerializeField]
        private int row;

        [Tooltip("How many columns this covers. Spans are honoured while auto-placing too - the flow looks for a hole of the right shape.")]
        [SerializeField]
        private int columnSpan = 1;

        [SerializeField]
        private int rowSpan = 1;

        [Tooltip("Use this item's own alignment instead of the grid's.")]
        [SerializeField]
        private bool overrideAlign;

        [SerializeField]
        private EGridAlign horizontalAlign = EGridAlign.Stretch;

        [SerializeField]
        private EGridAlign verticalAlign = EGridAlign.Stretch;

        /// <summary>The name this panel answers to in a layout. The object's own name when none is set.</summary>
        // Falling back to the object name rather than demanding a second one: the hierarchy is already a
        // list of names, and keeping two in step is work that buys nothing. The field is here for when the
        // object name cannot be the name - a space in it, or two panels that share a name on purpose.
        public string Area
        {
            get => string.IsNullOrEmpty(area) ? name : area;
            set => Set(ref area, value);
        }

        /// <summary>What is written in the field, without the fallback. Empty means "use the object name".</summary>
        public string AreaOverride => area;

        public bool AutoPlace
        {
            get => autoPlace;
            set => Set(ref autoPlace, value);
        }

        public int Column
        {
            get => column;
            set => Set(ref column, Mathf.Max(0, value));
        }

        public int Row
        {
            get => row;
            set => Set(ref row, Mathf.Max(0, value));
        }

        public int ColumnSpan
        {
            get => columnSpan;
            set => Set(ref columnSpan, Mathf.Max(1, value));
        }

        public int RowSpan
        {
            get => rowSpan;
            set => Set(ref rowSpan, Mathf.Max(1, value));
        }

        public bool OverrideAlign
        {
            get => overrideAlign;
            set => Set(ref overrideAlign, value);
        }

        public EGridAlign HorizontalAlign
        {
            get => horizontalAlign;
            set => Set(ref horizontalAlign, value);
        }

        public EGridAlign VerticalAlign
        {
            get => verticalAlign;
            set => Set(ref verticalAlign, value);
        }

        /// <summary>The grid this belongs to, or null if the parent is not one.</summary>
        // Only the direct parent counts. A grid further up the hierarchy lays out its own children, not
        // this one's - the panel between them is what would have to be placed there.
        public UiGrid Grid
        {
            get
            {
                var parent = transform.parent;
                return parent == null ? null : parent.GetComponent<UiGrid>();
            }
        }

        /// <summary>Puts this item in a cell by hand, turning auto-placement off.</summary>
        public void PlaceAt(int column, int row)
        {
            this.autoPlace = false;
            this.column = Mathf.Max(0, column);
            this.row = Mathf.Max(0, row);
            MarkDirty();
        }

        /// <summary>Sets how many cells this covers, in each direction.</summary>
        public void Span(int columns, int rows)
        {
            columnSpan = Mathf.Max(1, columns);
            rowSpan = Mathf.Max(1, rows);
            MarkDirty();
        }

        private void OnEnable() => MarkDirty();

        private void OnDisable() => MarkDirty();

        // A panel dragged into or out of a grid is the same event to both grids, and neither hears about
        // it from the other: the one it left rebuilds because its child list changed, and the one it
        // joined is told from here.
        private void OnTransformParentChanged() => MarkDirty();

        private void MarkDirty()
        {
            var grid = Grid;
            if (grid != null)
                grid.Rebuild();
        }

        private void Set<T>(ref T field, T value)
        {
            if (Equals(field, value))
                return;

            field = value;
            MarkDirty();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            column = Mathf.Max(0, column);
            row = Mathf.Max(0, row);
            columnSpan = Mathf.Max(1, columnSpan);
            rowSpan = Mathf.Max(1, rowSpan);

            // The grid has already been laid out by the time an inspector edit lands, and a layout is only
            // rebuilt when something asks - so without this a placement typed into the inspector would sit
            // there doing nothing until the next thing that happened to disturb the grid.
            var grid = Grid;
            if (grid != null && grid.isActiveAndEnabled)
                LayoutRebuilder.MarkLayoutForRebuild((RectTransform)grid.transform);
        }
#endif
    }
}
