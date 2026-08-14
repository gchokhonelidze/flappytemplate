using UnityEngine;

namespace FlappyTemplate
{
    // Three navbars built at runtime under whatever this sits on: the ordinary one in the top right, a
    // labelled row across the bottom, and a column down the left with a button of the game's own in it.
    // Drop it on an empty RectTransform inside a canvas and press play, or use Build Now from the
    // component's context menu to see them without leaving the editor.
    //
    // It is here to be read as much as run. Between them the three cover most of what the bar can be told:
    // both flows, both ways of aligning, labels on and off, a slot with an event of its own, and a Home
    // button given an address by hand - which is the only way to see that button in the editor, where there
    // is no socket to send SystemDto.ReturnUrl over.
    //
    // The keys are the other half of it: 1 opens the statistics window through the bar, 2 the fairness
    // window, and 3 asks the top bar to leave for its return address.
    [AddComponentMenu("UI/Ui Navbar Example")]
    [RequireComponent(typeof(RectTransform))]
    public class UiNavbarExample : MonoBehaviour
    {
        [Tooltip("Stands in for the address the server sends, which no editor session has. Anything not http or https is refused by Navigator, so this is a real one.")]
        [SerializeField]
        private string returnUrl = "https://example.com/lobby";

        private UiNavbar corner;
        private UiNavbar labelled;
        private UiNavbar column;

        void Start()
        {
            Build();
        }

        void Update()
        {
            if (corner == null)
                return;

            if (Input.GetKeyDown(KeyCode.Alpha1))
                corner.ShowStatistics();

            if (Input.GetKeyDown(KeyCode.Alpha2))
                corner.ShowFairness();

            if (Input.GetKeyDown(KeyCode.Alpha3))
                corner.GoHome();
        }

        /// <summary>Builds the three bars. Runs from Start, and from the context menu in the editor.</summary>
        [ContextMenu("Build Now")]
        public void Build()
        {
            BuildCorner();
            BuildLabelled();
            BuildColumn();
        }

        // The default bar: home, statistics and fairness in the top right, glyphs only. The return address
        // is written on rather than waited for, so the Home button is there to be pressed in the editor -
        // a build leaves it empty and lets the socket fill it in.
        private void BuildCorner()
        {
            corner = UiNavbar.Create(transform, "Navbar");
            corner.ReturnUrl = returnUrl;
        }

        // The same three buttons across the bottom with their names under them, spread out over a bar that
        // is not fitted to them. Docking to Bottom and leaving the width alone is the shape a bar across a
        // phone screen wants.
        private void BuildLabelled()
        {
            labelled = UiNavbar.Create(transform, "Navbar Labelled");
            labelled.ReturnUrl = returnUrl;

            var style = labelled.Style;
            style.ShowLabels = true;
            style.ButtonSize = new Vector2(72f, 72f);
            style.IconScale = 0.62f;
            style.BarCornerRadius = 26f;

            // Through the property rather than onto the field, since assigning a style is what rebuilds the
            // bar around it.
            labelled.Style = style;

            // Docked to the bottom edge and told not to fit itself, so the width below is the bar's own and
            // the buttons are spread out across it. Fit To Buttons on would shrink it back to the three.
            labelled.DockTo = ERectAnchor.Bottom;
            labelled.DockOffset = new Vector2(0f, 24f);
            labelled.FitToButtons = false;
            labelled.Align = ENavbarAlign.Spread;

            labelled.Rect.sizeDelta = new Vector2(420f, 92f);
            labelled.Layout();
        }

        // A column down the left, with a fourth button that is the game's own: Custom draws nothing and
        // does nothing but fire, which is where a game hangs its settings dialog, its sound toggle, or the
        // switch between a horizontal and a vertical board.
        private void BuildColumn()
        {
            column = UiNavbar.Create(transform, "Navbar Column", ENavbarFlow.Vertical);
            column.DockTo = ERectAnchor.Left;

            var mine = new NavbarButton(ENavbarButton.Custom, "Sound");
            mine.OnPressed.AddListener(() => Debug.Log("Navbar: the game's own button was pressed."));

            column.Buttons.Add(mine);

            // The list was changed rather than the style, so the bar is told to build the button that is
            // now in it.
            column.Rebuild();
        }
    }
}
