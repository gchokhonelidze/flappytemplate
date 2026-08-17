using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // The hotkeys dialog, built into a UiWindow: a drawn keyboard with the bound caps picked out, the list of what
    // each one does, and a button along the bottom that switches the whole feature on and off.
    //
    //     var keys = HotkeysWindow.Create(canvas);
    //     keys.Window.Open();
    //
    // Or leave it to the navbar, which finds one in the scene or builds it - see Ui/Navbar.
    //
    // There is nothing to fill in. The window reads <see cref="Hotkeys"/>, so a key bound anywhere in the game is
    // in the list the moment it is bound and gone the moment it is dropped - no list to keep in step, and no way
    // for the dialog and the game to disagree about what D does. Pressing a bound key while the window is open
    // lights its cap in both places, which is the other half of what the dialog is for: a player who cannot tell
    // whether the game heard them can hold a key and watch.
    //
    // The button along the bottom is the one thing here that changes anything. Hotkeys are off until the player
    // says otherwise - the server defaults the `keyboard` setting to 0 - and pressing it emits SETTING, so the
    // choice follows them to their next session and to the web front. Its caption says which way things stand
    // rather than what the press will do, which is how the web front's own button reads.
    //
    // A scene with no template running in it shows a few sample bindings, so the window can be laid out and styled
    // from a menu rather than from a running game. A real game never sees them: the moment anything is bound, or a
    // StateManager exists, only the real list is shown.
    [AddComponentMenu("UI/Hotkeys Window")]
    [RequireComponent(typeof(UiWindow))]
    public class HotkeysWindow : MonoBehaviour
    {
        // What each cell answers to in its grid's layout. One word each, and that is a requirement rather than a
        // habit: a layout is stored as text, so a name with a space in it comes back out as two cells.
        private const string BoardArea = "keyboard";
        private const string ListArea = "list";
        private const string FooterArea = "footer";
        private const string EmptyArea = "none";
        private const string RowArea = "r";

        [SerializeField]
        private HotkeysWindowStyle style = new HotkeysWindowStyle();

        [Header("Labels")]
        // Serialized rather than translated, the same as the windows next door: the template has no opinion about
        // the game's language, and a game that has one already knows where its strings live. Every one of them
        // goes through Translator.Label, so a caption left as it came translates anyway.
        [Tooltip("On the footer button while hotkeys are switched on.")]
        [SerializeField]
        private string onLabel = "Hotkeys on";

        [Tooltip("On the footer button while they are switched off. The caption says which way things stand, not what the press does - which is how the web front's button reads.")]
        [SerializeField]
        private string offLabel = "Hotkeys off";

        [Tooltip("Shown in place of the list while the game has bound nothing.")]
        [SerializeField]
        private string emptyLabel = "No hotkeys bound";

        [Header("Blocks")]
        [Tooltip("The drawn keyboard across the top, with every bound cap picked out.")]
        [SerializeField]
        private bool showKeyboard = true;

        [Tooltip("The list of what each bound key does.")]
        [SerializeField]
        private bool showList = true;

        [Tooltip("The button that switches hotkeys on and off. Off leaves the player no way to enable them, so only turn it off if the game offers that somewhere else.")]
        [SerializeField]
        private bool showToggle = true;

        [Header("Behaviour")]
        [Tooltip("Follow the bindings while the window is open: a key bound or dropped rebuilds the list, and a key pressed lights its cap.")]
        [SerializeField]
        private bool followBindings = true;

        [Tooltip("Resize the window to exactly the blocks it is showing.")]
        [SerializeField]
        private bool fitWindowHeight = true;

        [Header("Events")]
        [Tooltip("The footer button, carrying what hotkeys have just been switched to.")]
        public UnityEvent<bool> OnToggled = new UnityEvent<bool>();

        // Parts, and the flag saying they exist, are deliberately not serialized - the same choice the windows next
        // door make. Everything is found by name before it is made, so a rebuild after a script reload finds the
        // hierarchy that is already there rather than building a second one beside it.
        private UiWindow window;

        private RoundedBox boardCard;
        private readonly HotkeyKeyboard keyboard = new HotkeyKeyboard();

        private RectTransform list;
        private ScrollRect scroller;
        private RectMask2D clip;
        private Image grab;
        private UiGrid listGrid;
        private TextMeshProUGUI emptyText;

        private RoundedBox footerBox;
        private Button footerButton;
        private TextMeshProUGUI footerText;

        private readonly List<Row> live = new List<Row>();
        private readonly List<Row> spare = new List<Row>();

        // Row lists are built per layout pass rather than fixed: a gap between two tracks is there whether or not
        // anything is in them, so a block that is switched off has to take its row away with it.
        private readonly List<GridTrack> contentRows = new List<GridTrack>();
        private readonly List<GridTrack> listRows = new List<GridTrack>();

        // What the list is showing, which is the registry itself except in a scene with nothing running. The
        // dictionary is the same set by key, which is what the drawn keyboard colours its caps from - so the
        // picture and the list can never disagree about what is bound.
        private readonly List<Hotkey> shown = new List<Hotkey>();
        private readonly Dictionary<KeyCode, Hotkey> lookup = new Dictionary<KeyCode, Hotkey>();
        private List<Hotkey> preview;

        // What is showing, decided in Refresh and said to the grids in Arrange.
        private bool boardOn;
        private bool listOn;
        private bool footerOn;

        private bool built;
        private bool listening;

        // Frames left to check the fit on after a refresh - see the end of Refresh.
        private int settle;

        /// <summary>The window this is drawn into. Open, close, drag and theme it through that.</summary>
        public UiWindow Window
        {
            get
            {
                if (window == null)
                    window = GetComponent<UiWindow>();

                return window;
            }
        }

        /// <summary>Colours, sizes and fonts of the keyboard, the list and the button. Edit and call
        /// <see cref="Rebuild"/>, or assign a whole new one.</summary>
        public HotkeysWindowStyle Style
        {
            get => style;
            set
            {
                style = value ?? new HotkeysWindowStyle();
                Rebuild();
            }
        }

        /// <summary>The button along the bottom, for a game that would rather drive it from its own control.</summary>
        public Button ToggleButton
        {
            get
            {
                EnsureBuilt();
                return footerButton;
            }
        }

        /// <summary>The scroller the list runs in. Enabled only while there are more bindings than the list is
        /// allowed to be tall.</summary>
        public ScrollRect Scroller
        {
            get
            {
                EnsureBuilt();
                return scroller;
            }
        }

        /// <summary>Builds the whole thing - window, keyboard and all - under a parent.</summary>
        // Added before the object wakes, for the reason UiWindowBuilder.Add exists: a component on an active object
        // runs its Awake there and then, and the list would go into a window that had already put itself away.
        public static HotkeysWindow Create(Transform parent, string name = "Hotkeys", string title = "Hotkeys")
        {
            UiWindowBuilder.Create(parent, name)
                .Size(660f, 780f)
                .Title(title)
                .Add(out HotkeysWindow keys)
                .Done();

            // Awake has done this already in play mode. In the editor nothing else ever will, and a window built
            // from a context menu that came out empty would be a poor way to find that out.
            if (keys != null)
                keys.EnsureBuilt();

            return keys;
        }

        void Awake()
        {
            EnsureBuilt();
        }

        void OnEnable()
        {
            EnsureBuilt();
            Listen(true);

            // Every caption below goes through Translator.Label, so a language change is a repaint and nothing
            // more. Only while the dialog is open: a closed one refreshes on the way back in anyway.
            Translator.OnLocaleChanged += Refresh;

            Refresh();
        }

        void OnDisable()
        {
            Listen(false);
            Translator.OnLocaleChanged -= Refresh;
        }

        /// <summary>Makes whatever is missing and lays it out. Safe to call as often as you like.</summary>
        public void EnsureBuilt()
        {
            if (built)
                return;

            Rebuild();
        }

        /// <summary>Builds the keyboard, the list and the button from scratch, then redraws. Call after changing
        /// the style from code.</summary>
        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            if (style == null)
                style = new HotkeysWindowStyle();

            var host = Window;
            if (host == null)
                return;

            host.EnsureBuilt();
            host.ApplyLayout();

            BuildParts(host.Content);

            built = true;
            Refresh();
        }

        /// <summary>Reads the bindings again and lays the window out. What a key being bound or dropped calls.</summary>
        public void Refresh()
        {
            if (!built)
                return;

            Read();

            // Which blocks are showing is worked out here and said to the grids in Arrange, as a layout.
            // Deliberately not SetActive: a UiGrid shows what its layout names and hides what it does not, and it
            // re-asserts that every time it is enabled - so a cell switched off behind the grid's back comes back
            // on the next time the window opens. The layout is the only thing a grid will not argue with.
            boardOn = showKeyboard;
            listOn = showList;
            footerOn = showToggle;

            WriteRows();
            Layout();

            // Two more passes over the next two frames. Everything the layout needs is measured inside Layout,
            // except what only the canvas can settle - a font that finished loading, a rect that had no width yet
            // because the window was activated this frame.
            settle = 2;
        }

        void LateUpdate()
        {
            if (settle <= 0)
                return;

            settle--;
            FitWindow();
        }

        /// <summary>Sets every track, size and colour, then refits the window. Called by Refresh; separate because
        /// a game that has changed one style value wants this and not the rest.</summary>
        public void Layout()
        {
            if (!built)
                return;

            PaintContent();
            PaintBoard();
            PaintList();
            PaintFooter();
            Arrange();
            Tint();
            FitWindow();
        }

        /// <summary>Recolours the caps from what is held down, and nothing else. What a key press calls - a
        /// keyboard rebuilt sixty caps at a time on every press would be felt.</summary>
        public void Tint()
        {
            if (!built)
                return;

            keyboard.Tint(style, lookup);

            for (int i = 0; i < live.Count; i++)
                TintRow(live[i]);

            PaintFooterState();
        }

        /// <summary>Switches hotkeys on or off for the player, and emits SETTING so the choice is kept. What the
        /// footer button does.</summary>
        public void Toggle()
        {
            Hotkeys.Toggle();

            // Ahead of the setting coming back over the socket. Hotkeys.Enabled has already been written locally,
            // so this paints the answer the player is waiting for rather than the state they just left.
            Tint();
            OnToggled.Invoke(Hotkeys.Enabled);
        }

        // ------------------------------------------------------------------ building

        private void BuildParts(RectTransform content)
        {
            GridOn(content);

            boardCard = UiWindowParts.Box(content, "Keyboard");
            keyboard.Build(boardCard.rectTransform);

            list = UiWindowParts.Rect(content, "Bindings");

            scroller = list.GetComponent<ScrollRect>();
            if (scroller == null)
                scroller = list.gameObject.AddComponent<ScrollRect>();

            clip = list.GetComponent<RectMask2D>();
            if (clip == null)
                clip = list.gameObject.AddComponent<RectMask2D>();

            // Something for a finger to take hold of. A ScrollRect is only offered a drag - or a wheel - if the
            // pointer lands on a raycast target that is the scroller itself or something under it, and a drag that
            // starts in the gap between two rows lands on nothing at all. This is what it lands on: invisible, on
            // the same object as the ScrollRect, and behind every row in it.
            grab = list.GetComponent<Image>();
            if (grab == null)
                grab = list.gameObject.AddComponent<Image>();

            grab.color = new Color(0f, 0f, 0f, 0f);
            grab.raycastTarget = true;

            listGrid = Grid(list, "Rows");
            emptyText = UiWindowParts.Label(Rect(listGrid), "Empty");

            footerBox = UiWindowParts.Box(content, "Toggle");
            footerButton = footerBox.GetComponent<Button>();
            if (footerButton == null)
                footerButton = footerBox.gameObject.AddComponent<Button>();

            footerButton.targetGraphic = footerBox;

            // Removed before it is added, every time: AddListener is not serialized, so this runs on every load,
            // and a UnityEvent compares a listener by its target and method rather than by the delegate object -
            // which is what keeps a second build from firing the handler twice.
            footerButton.onClick.RemoveListener(Toggle);
            footerButton.onClick.AddListener(Toggle);

            footerText = UiWindowParts.Label(footerBox.transform, "Label");

            // Rows already under the grid are taken over rather than left there, which is what makes Rebuild safe
            // to call twice - on a window saved as a prefab, or one rebuilt after a script reload.
            Collect();

            // What every cell answers to in its grid's layout. Set once, here, where the parts are: a name is a
            // property of the panel, and Arrange only draws the picture that uses them.
            Named(boardCard.rectTransform, BoardArea);
            Named(list, ListArea);
            Named(footerBox.rectTransform, FooterArea);
            Named(emptyText.rectTransform, EmptyArea);
        }

        // One binding: what it does on the left, the key that does it on the right.
        private class Row
        {
            public RoundedBox Plate;
            public UiGrid Grid;
            public TextMeshProUGUI Caption;
            public RoundedBox Cap;
            public TextMeshProUGUI CapText;
            public Hotkey Binding;
        }

        // Everything already under the rows grid is taken over rather than left there. Nothing is thrown away: a
        // row is made of the same parts whatever key it is showing.
        private void Collect()
        {
            live.Clear();
            spare.Clear();

            var rows = Rect(listGrid);
            for (int i = 0; i < rows.childCount; i++)
            {
                var child = rows.GetChild(i);
                var plate = child.GetComponent<RoundedBox>();

                if (plate == null || !child.name.StartsWith("Row ", System.StringComparison.Ordinal))
                    continue;

                spare.Add(Adopt(plate));
            }
        }

        // The parts of one row, found by name before they are made - so this is both how a row is built and how one
        // that came back from a prefab is picked up again.
        private Row Adopt(RoundedBox plate)
        {
            var row = new Row
            {
                Plate = plate,
                Grid = GridOn(plate.rectTransform),
            };

            row.Caption = UiWindowParts.Label(plate.transform, "Caption");
            row.Cap = UiWindowParts.Box(plate.transform, "Cap");
            row.CapText = UiWindowParts.Label(row.Cap.transform, "Key");

            return row;
        }

        private Row Rent(int index)
        {
            while (spare.Count > 0)
            {
                int last = spare.Count - 1;
                var kept = spare[last];
                spare.RemoveAt(last);

                if (kept != null && kept.Plate != null)
                    return kept;
            }

            var plate = UiWindowParts.Box(Rect(listGrid), "Row " + index.ToString(CultureInfo.InvariantCulture));
            return Adopt(plate);
        }

        private void Retire(Row row)
        {
            if (row == null || row.Plate == null)
                return;

            // Out of the layout by name rather than by SetActive, for the reason above: a grid re-asserts its
            // layout, and a name the layout does not mention is a child it leaves alone and hidden.
            Named(row.Plate.rectTransform, string.Empty);
            spare.Add(row);
        }

        // ------------------------------------------------------------------ tracks and colours

        private void PaintContent()
        {
            var contentGrid = GridOn(Window.Content);
            Columns(contentGrid, GridTrack.Flexible());
            contentGrid.RowGap = style.SectionGap;
            contentGrid.ColumnGap = 0f;
            contentGrid.padding = new RectOffset(0, 0, 0, 0);
        }

        private void PaintBoard()
        {
            Paint(boardCard, style.KeyboardFill, style.KeyboardCornerRadius);
            keyboard.Layout(style);
        }

        private void PaintList()
        {
            scroller.viewport = list;
            scroller.content = Rect(listGrid);
            scroller.horizontal = false;
            scroller.vertical = true;
            scroller.movementType = ScrollRect.MovementType.Clamped;
            scroller.scrollSensitivity = style.ScrollSensitivity;
            scroller.inertia = style.ScrollInertia;
            scroller.decelerationRate = style.ScrollDeceleration;
            scroller.horizontalScrollbar = null;
            scroller.verticalScrollbar = null;

            listGrid.RowGap = style.RowGap;
            listGrid.ColumnGap = 0f;
            listGrid.padding = new RectOffset(0, 0, 0, 0);
            Columns(listGrid, GridTrack.Flexible());

            Label(emptyText, style.LabelFont, style.EmptySize, style.EmptyColor, FontStyles.Normal);
            emptyText.text = Translator.Label(emptyLabel);
        }

        // The shape of one row: the caption on the left and the key cap on the right. Said here rather than when
        // the row was made, so a style edited from code reaches rows that already exist.
        private void PaintRow(Row row)
        {
            int pad = Mathf.RoundToInt(style.RowPadding);
            row.Grid.padding = new RectOffset(pad, pad, pad, pad);
            row.Grid.ColumnGap = style.SectionGap;
            row.Grid.RowGap = 0f;

            Columns(row.Grid, GridTrack.Flexible(), GridTrack.Fixed(style.CapSize.x));
            Rows(row.Grid, GridTrack.Flexible());

            Put(row.Caption.rectTransform, 0, 0);
            Middle(row.Cap.rectTransform, 1, 0, style.CapSize);

            Label(row.Caption, style.LabelFont, style.LabelSize, style.LabelColor, style.LabelStyle);
            row.Caption.alignment = TextAlignmentOptions.Left;
            row.Caption.overflowMode = TextOverflowModes.Ellipsis;

            UiWindowParts.Stretch(row.CapText.rectTransform, 4f, 0f, 4f, 0f);
            Label(row.CapText, style.CapFont, style.CapTextSize, style.CapTextColor, style.CapTextStyle);

            // A key called Page Up has to fit the same cap a D does, and shrinking is better than spilling over
            // the edge of it.
            row.CapText.enableAutoSizing = true;
            row.CapText.fontSizeMin = Mathf.Max(8f, style.CapTextSize * 0.5f);
            row.CapText.fontSizeMax = style.CapTextSize;
            row.CapText.textWrappingMode = TextWrappingModes.NoWrap;

            row.Cap.SetCornerRadius(Mathf.Max(0f, style.CapCornerRadius));
            row.Cap.EdgeSoftness = 1.25f;
            row.Cap.raycastTarget = false;
            row.Plate.SetCornerRadius(Mathf.Max(0f, style.RowCornerRadius));
            row.Plate.EdgeSoftness = 1.25f;
            row.Plate.raycastTarget = false;
        }

        // What a row's colours say: the plate greys out and the cap fades for a binding whose Enabled is off, and
        // the cap lights while the key is held. Split out of PaintRow because this is the half a key press calls.
        private void TintRow(Row row)
        {
            if (row == null || row.Plate == null || row.Binding == null)
                return;

            bool live = Hotkeys.Enabled && row.Binding.Enabled;
            bool held = live && row.Binding.IsDown;

            row.Plate.FillGradientMode = EFillGradient.None;
            row.Plate.FillColor = live ? style.RowFill : style.RowDisabledFill;

            row.Cap.FillGradientMode = EFillGradient.None;
            row.Cap.FillColor = held ? style.CapDownFill : live ? style.CapFill : style.CapDisabledFill;

            row.Caption.color = live ? style.LabelColor : style.LabelDisabledColor;
        }

        private void PaintFooter()
        {
            Named(footerBox.rectTransform, FooterArea);

            footerBox.SetBorderSize(0f);
            footerBox.SetCornerRadius(Mathf.Max(0f, style.ButtonCornerRadius));
            footerBox.EdgeSoftness = 1.25f;
            footerBox.raycastTarget = true;

            UiWindowParts.Stretch(footerText.rectTransform, 8f, 0f, 8f, 0f);
            Label(footerText, style.ButtonFont, style.ButtonTextSize, style.ButtonTextColor, style.ButtonTextStyle);

            PaintFooterState();
        }

        // The caption says which way things stand rather than what the press will do, which is how the web front's
        // own button reads - and between the two of them a player who has seen one recognises the other.
        private void PaintFooterState()
        {
            if (footerBox == null || footerText == null)
                return;

            bool on = Hotkeys.Enabled;

            footerBox.FillGradientMode = EFillGradient.None;
            footerBox.FillColor = on ? style.ButtonOnFill : style.ButtonOffFill;
            footerText.text = Translator.Label(on ? onLabel : offLabel);
        }

        // ------------------------------------------------------------------ what is showing

        // Says the arrangement as a layout - a picture of the grid, one area name per cell - rather than by
        // switching cells on and off and placing them by number. A UiGrid takes a layout as the whole truth about
        // which of its children are showing and re-asserts it every time it is enabled, so a cell hidden with
        // SetActive comes back the next time the window opens.
        private void Arrange()
        {
            ArrangeList();

            contentRows.Clear();
            var content = UiGridLayout.Build().Columns(GridTrack.Flexible());

            if (boardOn)
            {
                content.Row(BoardArea);
                contentRows.Add(GridTrack.Fixed(keyboard.Height(style)));
            }

            if (listOn)
            {
                content.Row(ListArea);
                contentRows.Add(GridTrack.Fixed(ListHeight()));
            }

            if (footerOn)
            {
                content.Row(FooterArea);
                contentRows.Add(GridTrack.Fixed(Mathf.Max(1f, style.ButtonHeight)));
            }

            GridOn(Window.Content).SetLayout(content.Rows(contentRows.ToArray()).Done());
        }

        // The rows the registry ended up with, as a layout of one column. The content rect is sized by hand rather
        // than by a fitter: a ScrollRect measures its travel against the rect it is given, and a fitter racing the
        // grid over the same rect is how a list ends up unable to reach its last row.
        private void ArrangeList()
        {
            listRows.Clear();
            var layout = UiGridLayout.Build().Columns(GridTrack.Flexible());

            for (int i = 0; i < live.Count; i++)
            {
                layout.Row(RowArea + i.ToString(CultureInfo.InvariantCulture));
                listRows.Add(GridTrack.Fixed(style.RowHeight));
            }

            if (live.Count == 0)
            {
                layout.Row(EmptyArea);
                listRows.Add(GridTrack.Fixed(style.RowHeight));
            }

            listGrid.SetLayout(layout.Rows(listRows.ToArray()).Size(1, Mathf.Max(1, listRows.Count)).Done());

            float wanted = Content();
            var rows = Rect(listGrid);

            rows.anchorMin = new Vector2(0f, 1f);
            rows.anchorMax = new Vector2(1f, 1f);
            rows.pivot = new Vector2(0.5f, 1f);
            rows.sizeDelta = new Vector2(0f, wanted);
            rows.anchoredPosition = Vector2.zero;

            // Nothing to clip and nothing to catch while every row fits. A mask that is off costs nothing, and an
            // invisible graphic that is off is a drag that reaches the window behind it.
            bool scrolling = wanted > ListHeight() + 0.5f;
            scroller.enabled = scrolling;
            clip.enabled = scrolling;
            grab.enabled = scrolling;

            if (!scrolling)
                rows.anchoredPosition = Vector2.zero;
        }

        // How tall the rows come to, all told.
        private float Content()
        {
            int count = Mathf.Max(1, live.Count);
            return count * style.RowHeight + (count - 1) * style.RowGap;
        }

        // How tall the list is allowed to be: what it needs, held between the two ends of the style. The floor is
        // there so a game with one hotkey does not open a window a third of the height of one with eight.
        private float ListHeight()
        {
            float wanted = Content();
            float most = style.ListMaxHeight > 0f ? style.ListMaxHeight : wanted;
            return Mathf.Max(style.ListMinHeight, Mathf.Min(wanted, most));
        }

        // ------------------------------------------------------------------ the bindings

        // What the list is showing. The registry itself in a game, and a few sample bindings in a scene with
        // nothing running in it - a dialog built from a menu has to look like the dialog rather than like an empty
        // panel. The moment anything is really bound, or there is a StateManager to say a game is running, only the
        // real list is shown.
        private void Read()
        {
            shown.Clear();
            lookup.Clear();

            if (Hotkeys.Count > 0)
                shown.AddRange(Hotkeys.Bindings);
            else if (StateManager.Inst == null && Application.isEditor)
                shown.AddRange(preview ??= Sample());

            for (int i = 0; i < shown.Count; i++)
            {
                // One binding per key is the registry's own rule, so the last one in wins here as well rather than
                // throwing over a duplicate a sample list could otherwise introduce.
                if (shown[i] != null)
                    lookup[shown[i].Key] = shown[i];
            }
        }

        private void WriteRows()
        {
            // Every row that is no longer needed goes back to the pool before any is taken out of it, or a list of
            // two bindings after one of eight would make two more rows rather than reusing two of the eight that
            // are already there.
            for (int i = shown.Count; i < live.Count; i++)
                Retire(live[i]);

            if (live.Count > shown.Count)
                live.RemoveRange(shown.Count, live.Count - shown.Count);

            for (int i = 0; i < shown.Count; i++)
            {
                if (i >= live.Count)
                    live.Add(Rent(i));

                WriteRow(live[i], shown[i], i);
            }
        }

        private void WriteRow(Row row, Hotkey binding, int index)
        {
            row.Binding = binding;
            row.Plate.name = "Row " + index.ToString(CultureInfo.InvariantCulture);

            PaintRow(row);
            Named(row.Plate.rectTransform, RowArea + index.ToString(CultureInfo.InvariantCulture));

            // Through the translator rather than straight from the binding: "Cash out" is the en_US wording of a key
            // and comes back in the player's language, while a label nothing knows is printed as it was typed.
            row.Caption.text = Translator.Label(binding.Label);
            row.CapText.text = HotkeyCaps.Name(binding.Key);

            TintRow(row);
        }

        // ------------------------------------------------------------------ the state

        private void Listen(bool on)
        {
            if (!followBindings)
                return;

            if (on && !listening)
            {
                Hotkeys.OnChanged += Refresh;
                Hotkeys.OnPressed += HandlePressed;
                listening = true;
            }
            else if (!on && listening)
            {
                Hotkeys.OnChanged -= Refresh;
                Hotkeys.OnPressed -= HandlePressed;
                listening = false;
            }
        }

        // A key going down or coming up. Only the colours change, so only the colours are written - the list is the
        // same list it was a frame ago, and rebuilding it per press is work a player would feel.
        private void HandlePressed(Hotkey hotkey, bool down) => Tint();

        // ------------------------------------------------------------------ the window around it

        // The window is sized from its contents out rather than the other way round: how tall a hotkeys dialog has
        // to be depends on how many keys the game bound.
        //
        // Twice, and that is not belt and braces. A label reports the height it needs at the width it has, and its
        // width is what the first pass settles - so on the way into a window that has just been activated a
        // wrapped caption measures as one line, and the button under it is left hanging below the panel.
        private void FitWindow()
        {
            var host = Window;
            if (!fitWindowHeight || host == null || !isActiveAndEnabled)
                return;

            host.Fit();
            host.Fit();
        }

        // ------------------------------------------------------------------ small change

        // LayoutGroup keeps its own rect to itself, so a grid's rect is reached through its transform.
        private static RectTransform Rect(UiGrid grid) => (RectTransform)grid.transform;

        private static UiGrid Grid(Transform parent, string name) => GridOn(UiWindowParts.Rect(parent, name));

        private static UiGrid GridOn(RectTransform rect)
        {
            var grid = rect.GetComponent<UiGrid>();
            if (grid == null)
                grid = rect.gameObject.AddComponent<UiGrid>();

            return grid;
        }

        /// <summary>Puts something in a cell, stretched to fill it.</summary>
        private static void Put(RectTransform rect, int column, int row, int span = 1)
        {
            var item = UiWindowParts.Item(rect);
            item.PlaceAt(column, row);
            item.Span(span, 1);
            item.OverrideAlign = false;
        }

        /// <summary>Puts something of its own size in the middle of a cell. For the key cap, which a stretch would
        /// pull out of shape.</summary>
        private static void Middle(RectTransform rect, int column, int row, Vector2 size)
        {
            var item = UiWindowParts.Item(rect);
            item.PlaceAt(column, row);
            item.Span(1, 1);
            item.OverrideAlign = true;
            item.HorizontalAlign = EGridAlign.Center;
            item.VerticalAlign = EGridAlign.Center;
            UiWindowParts.Measured(rect, size);
        }

        private static void Named(RectTransform rect, string area) => UiWindowParts.Name(rect, area);

        private static void Columns(UiGrid grid, params GridTrack[] tracks)
        {
            grid.Columns.Clear();
            grid.Columns.AddRange(tracks);
            grid.Rebuild();
        }

        private static void Rows(UiGrid grid, params GridTrack[] tracks)
        {
            grid.Rows.Clear();
            grid.Rows.AddRange(tracks);
            grid.Rebuild();
        }

        private static void Paint(RoundedBox box, Color fill, float radius)
        {
            box.FillGradientMode = EFillGradient.None;
            box.FillColor = fill;
            box.SetBorderSize(0f);
            box.SetCornerRadius(radius);
            box.EdgeSoftness = 1.25f;
            box.raycastTarget = false;
        }

        private static void Label(TextMeshProUGUI label, TMP_FontAsset font, float size, Color color, FontStyles fontStyle)
        {
            label.font = font != null ? font : label.font;
            label.fontSize = size;
            label.color = color;
            label.fontStyle = fontStyle;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
        }

        // A list that reads the way the design was drawn, for a scene with no template running in it. These are not
        // bound to anything and never fire - they are three rows of text so the window can be styled from a menu.
        // A real game never sees them.
        private static List<Hotkey> Sample() => new List<Hotkey>
        {
            new Hotkey(KeyCode.D, "Min", null, null, null),
            new Hotkey(KeyCode.A, "Half the amount", null, null, null),
            new Hotkey(KeyCode.S, "Double the amount", null, null, null),
            new Hotkey(KeyCode.B, "Cash out", null, null, null),
        };
    }
}
