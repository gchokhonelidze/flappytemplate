using UnityEngine;

namespace FlappyTemplate
{
    // Everything you would want to do to a Ui Grid from code, in the order you would want to do it. Drop
    // this on the grid itself, press the keys, watch the panels move.
    //
    // The one worth reading is Layout: a whole arrangement is one string, so switching between two of them
    // is an assignment rather than a pile of SetActive calls and anchor maths. See README.md next to this
    // file for what goes in one.
    public class UiGridExample : MonoBehaviour
    {
        [Tooltip("Leave empty to use the Ui Grid on this object.")]
        [SerializeField]
        private UiGrid grid;

        // The names are the panels' object names. A layout naming something the grid has not got is not an
        // error - those cells are simply held open - so these can be written before the panels exist.
        //
        // The cols and rows lines are the other half of it. Without them the tracks stay as the inspector
        // left them, and the one column the narrow layout has left standing would still be the fixed-width
        // sidebar the wide one needed - a 200 unit strip down the side of the screen.
        //
        // The header and footer rows are given a size rather than written as auto, which is what the same
        // layout in CSS would say. Auto means "as big as the items in it need", and a plain panel does not
        // need anything - it has no text to measure and no sprite to fit, so an auto row of plain panels
        // comes out at nothing and the grid looks like it lost a row. Write auto[64] for a row that hugs
        // its contents but never falls below 64, or put a Layout Element on what is inside it.
        [TextArea(3, 8)]
        [SerializeField]
        private string wide =
            "cols: 200 1fr 240\n" +
            "rows: 64 1fr 48\n" +
            "header header header\n" +
            "nav    body   aside\n" +
            "footer footer footer";

        [TextArea(3, 8)]
        [SerializeField]
        private string narrow =
            "cols: 1fr\n" +
            "rows: 64 1fr 48\n" +
            "header\n" +
            "body\n" +
            "footer";

        [Tooltip("Swap the two layouts over as the window changes shape, which is the usual reason to have two.")]
        [SerializeField]
        private bool followAspect = true;

        [Tooltip("Wider than this and the wide layout is used.")]
        [SerializeField]
        private float wideAbove = 1f;

        private bool isWide;

        void Awake()
        {
            if (grid == null)
                grid = GetComponent<UiGrid>();
        }

        void Start()
        {
            // Whatever the aspect is at the first frame, applied without waiting for it to change.
            isWide = !IsWide();
            Follow();
        }

        void Update()
        {
            Follow();

            if (Input.GetKeyDown(KeyCode.Alpha1))
                ShowWide();

            if (Input.GetKeyDown(KeyCode.Alpha2))
                ShowNarrow();

            if (Input.GetKeyDown(KeyCode.Alpha3))
                ShowEverything();

            if (Input.GetKeyDown(KeyCode.Alpha4))
                WidenSidebar();

            if (Input.GetKeyDown(KeyCode.Alpha5))
                Print();
        }

        /// <summary>The whole arrangement in one assignment: named panels are shown and placed, the rest hidden.</summary>
        public void ShowWide() => grid.Layout = wide;

        public void ShowNarrow() => grid.Layout = narrow;

        /// <summary>Drops the layout. Every panel comes back and places itself by its own Column and Row.</summary>
        public void ShowEverything() => grid.ClearLayout();

        /// <summary>A layout can also be written inline, and rows separated by a slash rather than a newline.</summary>
        public void ShowGameOver() => grid.SetLayout("cols: 1fr 1fr / header header / . prompt / footer footer");

        /// <summary>Sizes in a layout take limits too, which is the whole of CSS's minmax in one token.</summary>
        // The sidebar never squeezes below its icons and never grows past a readable width, the body takes
        // what is left, and none of it needs the track list to have been set up beforehand.
        public void ShowCapped() => grid.SetLayout("cols: 1fr[160..400] 3fr / nav body");

        // Tracks are ordinary objects on a list, so anything about them can be changed - the size, the mode
        // it is measured in, its floor and its ceiling. Nothing watches that list, so say when you are done.
        public void WidenSidebar()
        {
            if (grid.Columns.Count == 0)
                return;

            var first = grid.Columns[0];
            first.Mode = EGridTrack.Fixed;
            first.Size = first.Size >= 300f ? 120f : 320f;

            grid.Rebuild();
        }

        /// <summary>Rebuilding the track list outright, which is what a "three even columns" button does.</summary>
        public void ThreeEvenColumns()
        {
            grid.Columns.Clear();
            grid.Columns.Add(GridTrack.Flexible());
            grid.Columns.Add(GridTrack.Flexible());
            grid.Columns.Add(GridTrack.Flexible());

            // A sidebar that must never squeeze below its icons and never grow past a readable width.
            grid.Columns[0].Min = 160f;
            grid.Columns[0].Max = 400f;

            grid.ColumnGap = 12f;
            grid.Rebuild();
        }

        // Reads out what the grid is arranged as right now, in the same form Layout takes. The way to get a
        // layout string is to arrange the panels in the inspector and print this rather than to type one.
        public void Print()
        {
            Debug.Log($"Layout is now:\n{grid.ReadLayout()}");
            Debug.Log($"The sidebar is {(grid.Shows("nav") ? "showing" : "hidden")}.");
        }

        private void Follow()
        {
            if (!followAspect)
                return;

            bool wanted = IsWide();
            if (wanted == isWide)
                return;

            isWide = wanted;

            // Setting the same layout twice is cheap but not free - it walks the children to show and hide
            // them - so it is done on the change rather than every frame.
            grid.Layout = wanted ? wide : narrow;
        }

        private bool IsWide() => Screen.height > 0 && (float)Screen.width / Screen.height >= wideAbove;
    }
}
