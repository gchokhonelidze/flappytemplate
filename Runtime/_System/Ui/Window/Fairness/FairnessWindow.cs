using System;
using System.Collections.Generic;
using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // The fairness dialog, built into a UiWindow: the seed pair the game is rolling from, the pair before it,
    // and the two ways a player may change them.
    //
    //     var fairness = FairnessWindow.Create(canvas);
    //     fairness.Show();
    //
    // Everything it shows comes from MainState.Seeds, which the server fills in and refreshes over the socket
    // as ON_SEED. Nothing is polled and nothing is invented: the window redraws when OnSeed fires and sits
    // still the rest of the time.
    //
    // The two buttons are the whole point of the dialog, and they are not the same button twice:
    //
    // - **Randomize** emits RANDOMIZE, which asks the server for a new pair - a new server seed as well as a
    //   new client seed. It is the one that makes the *next* rolls unknowable to everyone, including the house.
    // - The small one beside the box emits RANDOMIZE_CLIENTSALT_ONLY, which keeps the server's seed and
    //   replaces only the half the player owns, with whatever was typed.
    //
    // Both are refused while a round is in play, which is the server's rule rather than this window's: a seed
    // pair changed mid-bet would make that bet uncheckable. The controls lock themselves rather than letting a
    // press go out and be rejected.
    //
    // The layout is a UiGrid rather than arithmetic: one column, an auto row per block, and a nested grid per
    // block. A SHA-512 wraps to three lines and the window comes back the height that took, without anything
    // here having to measure it.
    [AddComponentMenu("UI/Fairness Window")]
    [RequireComponent(typeof(UiWindow))]
    public class FairnessWindow : MonoBehaviour
    {
        // What each cell answers to in the content grid's layout. One word each, and that is a requirement
        // rather than a habit: a layout is stored as text, so a name with a space in it comes back out as two
        // cells.
        private const string NewSeedArea = "newseed";
        private const string RandomizeArea = "randomize";
        private const string CurrentArea = "currenthead";
        private const string PreviousArea = "previoushead";
        private const string LoaderArea = "loader";
        private const string EntryArea = "entry";

        // The blocks under the headings, in the order they are drawn. Named rather than numbered at the call
        // sites, since which pair a row belongs to is the whole difference between two of them.
        private const int NonceEntry = 0;
        private const int ClientSeedEntry = 1;
        private const int ServerShaEntry = 2;
        private const int PrevClientSeedEntry = 3;
        private const int PrevServerSeedEntry = 4;
        private const int BetsMadeEntry = 5;
        private const int EntryCount = 6;

        // Entries below this belong to the current pair and the rest to the previous one, which is where the
        // second heading goes.
        private const int PreviousFrom = 3;

        private static readonly char[] HexDigits = "0123456789abcdef".ToCharArray();

        [SerializeField]
        private FairnessWindowStyle style = new FairnessWindowStyle();

        [Header("Labels")]
        // Serialized rather than translated, the same as the two windows next door: the template has no
        // opinion about the game's language, and a game that has one already knows where its strings live.
        [SerializeField]
        private string newClientSeedLabel = "New client seed";

        [SerializeField]
        private string randomizeLabel = "Randomize";

        [SerializeField]
        private string currentPairLabel = "Current seed pair";

        [SerializeField]
        private string previousPairLabel = "Previous seed pair";

        [SerializeField]
        private string nonceLabel = "Nonce";

        [SerializeField]
        private string clientSeedLabel = "Client seed";

        [Tooltip("Stands in for the client seed on a shared or multiplayer game, where the roll is seeded from a block hash rather than from anything one player sent.")]
        [SerializeField]
        private string blockHashLabel = "Bitcoin last block hash";

        [SerializeField]
        private string serverShaLabel = "Server seed's SHA512 hash";

        [SerializeField]
        private string serverSeedLabel = "Server seed";

        [SerializeField]
        private string betsMadeLabel = "Bets made with pair";

        [Tooltip("Printed where a value the server has not sent would go - the previous pair, before there has been one.")]
        [SerializeField]
        private string emptyLabel = "N/A";

        [Header("Blocks")]
        [Tooltip("The client seed box and its renew button. Dropped on a shared or multiplayer game whatever this says: the roll is seeded from a block hash there, and nothing the player types would reach it.")]
        [SerializeField]
        private bool showClientSeedBox = true;

        [Tooltip("The Randomize button. Dropped on a shared or multiplayer game, for the same reason.")]
        [SerializeField]
        private bool showRandomize = true;

        [SerializeField]
        private bool showCurrentPair = true;

        [SerializeField]
        private bool showPreviousPair = true;

        [Tooltip("Longest client seed the box will take. Sixteen is what the web front allows.")]
        [Min(1)]
        [SerializeField]
        private int clientSeedLength = 16;

        [Header("Behaviour")]
        [Tooltip("Redraw when the server sends a new seed pair, and when the round starts or ends.")]
        [SerializeField]
        private bool followState = true;

        [Tooltip("Emit SEED_INFO when the window opens. On, and it should stay on: the pair the session started with is several bets old by the time a player opens this, and the nonce has moved with every one of them.")]
        [SerializeField]
        private bool requestOnOpen = true;

        [Tooltip("Lock the controls while a round is in play. On, and it should stay on: the server refuses a seed change mid-bet, since a pair changed under a bet would make that bet uncheckable.")]
        [SerializeField]
        private bool lockWhileRunning = true;

        [Tooltip("Resize the window to exactly the blocks it is showing.")]
        [SerializeField]
        private bool fitWindowHeight = true;

        [Header("Events")]
        [Tooltip("A new pair has arrived and the fields are filled in.")]
        public UnityEvent<SeedDto> OnSeeds = new UnityEvent<SeedDto>();

        [Tooltip("The Randomize button, as the request goes out.")]
        public UnityEvent OnRandomizeRequested = new UnityEvent();

        [Tooltip("The renew button, with the client seed that was sent.")]
        public UnityEvent<string> OnClientSeedRequested = new UnityEvent<string>();

        // Parts, and the flag saying they exist, are deliberately not serialized - the same choice the two
        // windows next door make. Everything is found by name before it is made, so a rebuild after a script
        // reload finds the hierarchy that is already there rather than building a second one beside it.
        private UiWindow window;
        private UiGrid contentGrid;

        private UiGrid newSeed;
        private TextMeshProUGUI newSeedCaption;
        private UiGrid newSeedRow;
        private Padlock padlock;
        private RoundedBox inputBox;
        private RectTransform inputArea;
        private TMP_InputField input;
        private TextMeshProUGUI inputText;
        private TextMeshProUGUI inputPlaceholder;
        private RoundedBox renewBox;
        private Button renewButton;
        private Arrow renewArrow;

        private RoundedBox randomizeBox;
        private Button randomizeButton;
        private UiGrid randomizeRow;
        private Arrow randomizeArrow;
        private TextMeshProUGUI randomizeText;

        private UiGrid currentHeading;
        private TextMeshProUGUI currentHeadingText;
        private UiGrid previousHeading;
        private TextMeshProUGUI previousHeadingText;

        private readonly List<Entry> entries = new List<Entry>();

        private RectTransform loader;
        private UiGrid loaderGrid;
        private readonly RoundedBox[] dots = new RoundedBox[3];
        private readonly List<Tween> pulses = new List<Tween>();

        // Row lists are built per layout pass rather than fixed: a gap between two tracks is there whether or
        // not anything is in them, so a block that is switched off has to take its row away with it.
        private readonly List<GridTrack> contentRows = new List<GridTrack>();

        // What is showing, decided in Refresh and said to the grid in Arrange.
        private bool hasData;
        private bool single;
        private bool locked;
        private bool controlsOn;
        private bool randomizeOn;
        private bool currentOn;
        private bool previousOn;
        private bool loaderOn;
        private readonly bool[] entryOn = new bool[EntryCount];

        private SeedDto preview;

        // The client seed the box was last filled in from, so a pair the server has replaced resets it and a
        // broadcast that left it alone does not throw away what the player was typing.
        private string filledFrom;

        // A request is out and the answer has not arrived. The controls stay locked until it does, so a
        // second press cannot go out against a pair that is already being replaced.
        private bool pending;

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

        /// <summary>Colours, sizes and fonts of the box, the buttons and the rows. Edit and call
        /// <see cref="Rebuild"/>, or assign a whole new one.</summary>
        public FairnessWindowStyle Style
        {
            get => style;
            set
            {
                style = value ?? new FairnessWindowStyle();
                Rebuild();
            }
        }

        /// <summary>The pair being shown: the state's, or a sample in a scene with no template running at all.
        /// Null while the server has not sent one.</summary>
        public SeedDto Seeds
        {
            get
            {
                var manager = StateManager.Inst;
                if (manager != null)
                    return manager.MainState != null ? manager.MainState.Seeds : null;

                // No StateManager anywhere means a scene built to look at the window rather than to play in,
                // where a row of dots that never stops is no use to anyone.
                return preview ??= Sample();
            }
        }

        /// <summary>What is in the client seed box. Setting it does not send anything - the renew button, or
        /// <see cref="RenewClientSeed"/>, is what does that.</summary>
        public string ClientSeed
        {
            get
            {
                EnsureBuilt();
                return input != null ? input.text : string.Empty;
            }
            set
            {
                EnsureBuilt();

                if (input != null)
                    input.SetTextWithoutNotify(value ?? string.Empty);
            }
        }

        /// <summary>Whether the controls are locked as things stand: a round in play, no pair yet, or a
        /// request already on its way.</summary>
        public bool IsLocked => locked;

        /// <summary>Whether a seed request is out and unanswered.</summary>
        public bool IsPending => pending;

        /// <summary>The client seed box, for a game that would rather drive it itself.</summary>
        public TMP_InputField Input
        {
            get
            {
                EnsureBuilt();
                return input;
            }
        }

        public Button RandomizeButton
        {
            get
            {
                EnsureBuilt();
                return randomizeButton;
            }
        }

        public Button RenewButton
        {
            get
            {
                EnsureBuilt();
                return renewButton;
            }
        }

        /// <summary>Builds the whole thing - window, seeds and all - under a parent.</summary>
        // Added before the object wakes, for the reason UiWindowBuilder.Add exists: a component on an active
        // object runs its Awake there and then, and the rows would go into a window that had already put
        // itself away.
        public static FairnessWindow Create(Transform parent, string name = "Fairness", string title = "Fairness")
        {
            UiWindowBuilder.Create(parent, name)
                .Size(480f, 620f)
                .Title(title)
                .Add(out FairnessWindow fairness)
                .Done();

            // Awake has done this already in play mode. In the editor nothing else ever will, and a window
            // built from a context menu that came out empty would be a poor way to find that out.
            if (fairness != null)
                fairness.EnsureBuilt();

            return fairness;
        }

        void Awake()
        {
            EnsureBuilt();
        }

        void OnEnable()
        {
            EnsureBuilt();

            // The answer can have arrived while the window was closed, and a closed window is not listening -
            // so a request that was out when it was put away is not still out now. Without this, a dialog
            // closed mid-request comes back on a row of dots that nothing will ever stop.
            pending = false;

            Listen(true);

            // This is where the dialog opening is heard: UiWindow.Open switches the object on and Close
            // switches it off again, so being enabled is the same event as being opened - and it catches a
            // window opened straight through UiWindow.Open as well as through Show.
            if (requestOnOpen)
                Request();

            Refresh();
        }

        void OnDisable()
        {
            Listen(false);
            StopPulse();
        }

        void OnDestroy()
        {
            StopPulse();
        }

        /// <summary>Makes whatever is missing and lays it out. Safe to call as often as you like.</summary>
        public void EnsureBuilt()
        {
            if (built)
                return;

            Rebuild();
        }

        /// <summary>Builds the box, the buttons and the rows from scratch, then redraws. Call after changing
        /// the style from code.</summary>
        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            if (style == null)
                style = new FairnessWindowStyle();

            var host = Window;
            if (host == null)
                return;

            host.EnsureBuilt();
            host.ApplyLayout();

            BuildParts(host.Content);

            built = true;
            Refresh();
        }

        /// <summary>Opens the window on whatever pair the state holds, and asks the server for a fresh one.</summary>
        public void Show()
        {
            EnsureBuilt();
            Refresh();
            Window.Open();
        }

        /// <summary>Asks the server for the current pair, without opening anything.</summary>
        // Emits SEED_INFO and returns. The answer comes back as an ordinary ON_SEED broadcast, so this does not
        // lock the controls the way the two buttons do: what is on screen is already the pair the server last
        // sent, and there is no reason to stop reading it while a newer one is on its way.
        public void Request()
        {
            if (Emitter.Inst != null)
                Emitter.Inst.OnSeedInfo();
        }

        /// <summary>Asks the server for a whole new pair - a new server seed as well as a new client seed.
        /// What the Randomize button does.</summary>
        // The request goes out and the answer comes back as an ordinary ON_SEED broadcast, so the window is
        // never showing a pair the server has not confirmed. Without a socket - a scene with no template
        // running - there is nobody to ask, so the sample is rolled over directly and there is something to
        // look at.
        public void Randomize()
        {
            EnsureBuilt();

            if (locked)
                return;

            OnRandomizeRequested.Invoke();

            if (Emitter.Inst != null)
            {
                pending = true;
                Emitter.Inst.OnRandomize(null);
                Refresh();
                return;
            }

            Roll(null, true);
            Refresh();
        }

        /// <summary>Sends the client seed in the box and keeps the server's. What the small button beside the
        /// box does.</summary>
        public void RenewClientSeed()
        {
            EnsureBuilt();

            if (locked)
                return;

            var salt = ClientSeed ?? string.Empty;
            OnClientSeedRequested.Invoke(salt);

            if (Emitter.Inst != null)
            {
                pending = true;
                Emitter.Inst.OnRandomizeClientSaltOnly(salt);
                Refresh();
                return;
            }

            Roll(salt, false);
            Refresh();
        }

        /// <summary>Fills every row in from the state and lays the content out again.</summary>
        public void Refresh()
        {
            if (!built)
                return;

            var data = Seeds;

            // Which blocks are showing is worked out here and said to the grid in Arrange, as a layout.
            // Deliberately not SetActive: a UiGrid shows what its layout names and hides what it does not, and
            // it re-asserts that every time it is enabled - so a cell switched off behind the grid's back comes
            // back on the next time the window opens. The layout is the only thing a grid will not argue with.
            hasData = data != null;
            single = IsSingle;
            locked = pending || !hasData || (lockWhileRunning && Running);

            // The box and the Randomize button are a single-player idea. A shared or multiplayer round is
            // seeded from a block hash nobody can have known in advance, and nothing a player types would
            // reach it - so they are dropped rather than shown doing nothing.
            controlsOn = single && showClientSeedBox;
            randomizeOn = single && showRandomize;

            // The first pair of a session has nothing before it, and the server says so by sending both halves
            // of the previous one as null. Three rows of N/A and a bet count of zero is not information, so the
            // whole section goes until there is a pair to put in it.
            bool previous = hasData && showPreviousPair
                && (!string.IsNullOrEmpty(data.PrevClientSalt) || !string.IsNullOrEmpty(data.PrevServerSeed));

            entryOn[NonceEntry] = hasData && showCurrentPair && single;
            entryOn[ClientSeedEntry] = hasData && showCurrentPair;
            entryOn[ServerShaEntry] = hasData && showCurrentPair;
            entryOn[PrevClientSeedEntry] = previous;
            entryOn[PrevServerSeedEntry] = previous;
            entryOn[BetsMadeEntry] = previous && single;

            currentOn = Shown(0, PreviousFrom);
            previousOn = Shown(PreviousFrom, EntryCount);

            // While there is no pair at all, and while a request is out: in the second case the rows stay put
            // underneath, so the dialog does not empty itself out and come back a different height.
            loaderOn = !hasData || pending;

            if (hasData)
                Write(data);

            Pulse(loaderOn);
            Layout();

            // Two more passes over the next two frames. Everything the layout needs is measured inside
            // Layout, except what only the canvas can settle - a font that finished loading, a rect that had
            // no width yet because the window was activated this frame. Cheap, and it is the difference
            // between a dialog that fits and one with a hash hanging out of it.
            settle = 2;
        }

        void LateUpdate()
        {
            if (settle <= 0)
                return;

            settle--;
            FitWindow();
        }

        /// <summary>Sets every track, size and colour, then refits the window. Called by Refresh; separate
        /// because a game that has changed one style value wants this and not the rest.</summary>
        public void Layout()
        {
            if (!built)
                return;

            PaintContent();
            PaintNewSeed();
            PaintRandomize();
            PaintHeadings();
            PaintEntries();
            PaintLoader();
            Arrange();
            ApplyLock();
            FitWindow();
        }

        // ------------------------------------------------------------------ building

        private void BuildParts(RectTransform content)
        {
            contentGrid = GridOn(content);

            newSeed = Grid(content, "New Seed");
            newSeedCaption = UiWindowParts.Label(newSeed.transform, "Caption");
            newSeedRow = Grid(newSeed.transform, "Row");
            padlock = BuildPadlock(newSeedRow.transform, "Lock");
            BuildInput(newSeedRow.transform, "Input");
            renewBox = UiWindowParts.Box(newSeedRow.transform, "Renew");
            renewButton = Hook(renewBox, RenewClientSeed);
            renewArrow = BuildArrow(renewBox.transform, "Arrow");

            randomizeBox = UiWindowParts.Box(content, "Randomize");
            randomizeButton = Hook(randomizeBox, Randomize);
            randomizeRow = Grid(randomizeBox.transform, "Row");
            randomizeArrow = BuildArrow(randomizeRow.transform, "Arrow");
            randomizeText = UiWindowParts.Label(randomizeRow.transform, "Label");

            currentHeading = Grid(content, "Current Heading");
            currentHeadingText = UiWindowParts.Label(currentHeading.transform, "Label");
            previousHeading = Grid(content, "Previous Heading");
            previousHeadingText = UiWindowParts.Label(previousHeading.transform, "Label");

            entries.Clear();
            for (int i = 0; i < EntryCount; i++)
                entries.Add(BuildEntry(content, "Entry " + i.ToString(CultureInfo.InvariantCulture)));

            loader = Rect(Grid(content, "Loader"));
            loaderGrid = loader.GetComponent<UiGrid>();
            for (int i = 0; i < dots.Length; i++)
                dots[i] = UiWindowParts.Box(loader, "Dot " + i.ToString(CultureInfo.InvariantCulture));

            // What every cell answers to in the content grid's layout. Set once, here, where the parts are:
            // a name is a property of the panel, and Arrange only draws the picture that uses them.
            Named(Rect(newSeed), NewSeedArea);
            Named(randomizeBox.rectTransform, RandomizeArea);
            Named(Rect(currentHeading), CurrentArea);
            Named(Rect(previousHeading), PreviousArea);
            Named(loader, LoaderArea);

            for (int i = 0; i < entries.Count; i++)
                Named(Rect(entries[i].Root), EntryArea + i.ToString(CultureInfo.InvariantCulture));
        }

        // A caption over a value: every row of both pairs is one of these. The value is a cell of its own
        // rather than a second line of the caption, so a hash wraps inside the column and the row is as tall
        // as the wrapping made it.
        private class Entry
        {
            public UiGrid Root;
            public TextMeshProUGUI Caption;
            public TextMeshProUGUI Value;
        }

        // A padlock, drawn from a ring and a rounded box rather than fetched from an atlas - the same choice
        // the close cross and the reset arrow make next door, and for the same reason: it costs no atlas entry
        // and stays sharp at any size. The shackle is made first so the body draws over its lower half, which
        // is what turns a circle into an arch.
        private class Padlock
        {
            public RectTransform Root;
            public RoundedBox Shackle;
            public RoundedBox Body;
            public Image Picture;
        }

        // The circular arrow on both buttons: a ring, a notch painted out of it in the button's own colour,
        // and a diamond for the head.
        private class Arrow
        {
            public RectTransform Root;
            public RoundedBox Ring;
            public RoundedBox Gap;
            public RoundedBox Head;
            public Image Picture;
        }

        private Entry BuildEntry(Transform parent, string name)
        {
            var entry = new Entry { Root = Grid(parent, name) };
            entry.Caption = UiWindowParts.Label(entry.Root.transform, "Caption");
            entry.Value = UiWindowParts.Label(entry.Root.transform, "Value");
            return entry;
        }

        private Padlock BuildPadlock(Transform parent, string name)
        {
            var made = new Padlock { Root = UiWindowParts.Rect(parent, name) };
            made.Shackle = UiWindowParts.Box(made.Root, "Shackle");
            made.Body = UiWindowParts.Box(made.Root, "Body");
            made.Picture = UiWindowParts.Picture(made.Root, "Picture");
            return made;
        }

        private Arrow BuildArrow(Transform parent, string name)
        {
            var arrow = new Arrow { Root = UiWindowParts.Rect(parent, name) };
            arrow.Ring = UiWindowParts.Box(arrow.Root, "Ring");
            arrow.Gap = UiWindowParts.Box(arrow.Root, "Gap");
            arrow.Head = UiWindowParts.Box(arrow.Root, "Head");
            arrow.Picture = UiWindowParts.Picture(arrow.Root, "Picture");
            return arrow;
        }

        // The box the player types a client seed into. A TMP_InputField needs three things that have to agree:
        // a viewport with a mask on it, a text object inside that viewport, and a placeholder beside it. The
        // caret is TMP's own and appears inside the viewport at run time.
        private void BuildInput(Transform parent, string name)
        {
            inputBox = UiWindowParts.Box(parent, name);
            inputArea = UiWindowParts.Rect(inputBox.transform, "Text Area");

            // Without the mask a seed longer than the box draws straight out of both ends of it. RectMask2D
            // rather than Mask: it costs no stencil pass, and TMP clips against it like any other graphic.
            if (inputArea.GetComponent<RectMask2D>() == null)
                inputArea.gameObject.AddComponent<RectMask2D>();

            inputText = UiWindowParts.Label(inputArea, "Text");
            inputPlaceholder = UiWindowParts.Label(inputArea, "Placeholder");

            input = inputBox.GetComponent<TMP_InputField>();
            if (input == null)
                input = inputBox.gameObject.AddComponent<TMP_InputField>();

            input.textViewport = inputArea;
            input.textComponent = inputText;
            input.placeholder = inputPlaceholder;
            input.targetGraphic = inputBox;
            input.transition = Selectable.Transition.None;

            // Single line, which is also what settles the wrapping on the text object - TMP_InputField writes
            // that itself from the line type, so nothing here has to.
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.richText = false;
            input.characterLimit = Mathf.Max(1, clientSeedLength);
            input.onFocusSelectAll = true;
        }

        private Button Hook(RoundedBox box, UnityAction handler)
        {
            var button = box.GetComponent<Button>();
            if (button == null)
                button = box.gameObject.AddComponent<Button>();

            button.targetGraphic = box;

            // Removed before it is added, every time: AddListener is not serialized, so this runs on every
            // load, and a UnityEvent compares a listener by its target and method rather than by the delegate
            // object - which is what keeps a second build from firing the handler twice.
            button.onClick.RemoveListener(handler);
            button.onClick.AddListener(handler);
            return button;
        }

        // ------------------------------------------------------------------ tracks and colours

        private void PaintContent()
        {
            Columns(contentGrid, GridTrack.Flexible());
            contentGrid.RowGap = style.RowGap;
            contentGrid.ColumnGap = 0f;
            contentGrid.padding = new RectOffset(0, 0, 0, 0);
        }

        private void PaintNewSeed()
        {
            // A caption over a row of three: the padlock, the box, and the button that sends what is in it.
            Columns(newSeed, GridTrack.Flexible());
            Rows(newSeed, GridTrack.Auto(), GridTrack.Fixed(style.InputHeight));
            newSeed.RowGap = style.CaptionGap;
            newSeed.ColumnGap = 0f;
            newSeed.padding = new RectOffset(0, 0, 0, 0);
            Put(newSeedCaption.rectTransform, 0, 0);
            Put(Rect(newSeedRow), 0, 1);

            Label(newSeedCaption, style.CaptionFont, style.CaptionSize, style.CaptionColor, style.CaptionStyle);
            newSeedCaption.alignment = TextAlignmentOptions.Left;
            newSeedCaption.text = newClientSeedLabel;

            Columns(newSeedRow,
                GridTrack.Fixed(style.LockSize),
                GridTrack.Flexible(),
                GridTrack.Fixed(style.RenewSize.x));
            Rows(newSeedRow, GridTrack.Flexible());
            newSeedRow.ColumnGap = style.InputGap;
            newSeedRow.padding = new RectOffset(0, 0, 0, 0);

            Middle(padlock.Root, 0, 0, new Vector2(style.LockSize, style.LockSize));
            Put(inputBox.rectTransform, 1, 0);
            Middle(renewBox.rectTransform, 2, 0, style.RenewSize);

            PaintPadlock();
            PaintInput();

            Paint(renewBox, style.ButtonFill, style.ButtonCornerRadius);
            renewBox.raycastTarget = true;
            PaintArrow(renewArrow, style.ButtonFill);
            UiWindowParts.Pin(renewArrow.Root, new Vector2(0.5f, 0.5f), new Vector2(style.ArrowSize, style.ArrowSize), Vector2.zero);
        }

        private void PaintPadlock()
        {
            float span = style.LockSize;
            bool sprite = style.LockIcon != null;

            padlock.Picture.gameObject.SetActive(sprite);
            padlock.Picture.sprite = style.LockIcon;
            padlock.Picture.color = style.LockColor;
            padlock.Picture.preserveAspect = true;
            padlock.Picture.raycastTarget = false;
            UiWindowParts.Stretch(padlock.Picture.rectTransform, 0f, 0f, 0f, 0f);

            padlock.Shackle.gameObject.SetActive(!sprite);
            padlock.Body.gameObject.SetActive(!sprite);

            if (sprite)
                return;

            // A ring above the body, with the body drawn over its lower half. Nothing is cut: the body is
            // opaque and later in the hierarchy, which is all an arch needs.
            float thickness = Mathf.Max(0.5f, style.LockThickness);

            UiWindowParts.Pin(
                padlock.Shackle.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(span * 0.52f, span * 0.52f),
                new Vector2(0f, span * 0.16f));
            padlock.Shackle.FillGradientMode = EFillGradient.None;
            padlock.Shackle.FillColor = Color.clear;
            padlock.Shackle.SetCornerRadius(100000f);
            padlock.Shackle.SetBorderSize(thickness);
            padlock.Shackle.SetBorderColor(style.LockColor);
            padlock.Shackle.EdgeSoftness = 1f;
            padlock.Shackle.raycastTarget = false;

            UiWindowParts.Pin(
                padlock.Body.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(span * 0.82f, span * 0.56f),
                new Vector2(0f, -span * 0.2f));
            Paint(padlock.Body, style.LockColor, span * 0.14f);
        }

        private void PaintInput()
        {
            Paint(inputBox, locked ? style.InputLockedFill : style.InputFill, style.InputCornerRadius);
            inputBox.raycastTarget = true;

            // The viewport is inset rather than the text, because the mask clips to the viewport: a text
            // object stretched to the box's own edges would be clipped exactly where the padding says the
            // text should stop, and a seed scrolled to its end would sit against the rounded corner.
            UiWindowParts.Stretch(inputArea, style.InputPadding, 0f, style.InputPadding, 0f);

            Label(inputText, style.ValueFont, style.InputTextSize, style.InputTextColor, FontStyles.Normal);
            inputText.alignment = TextAlignmentOptions.Left;
            UiWindowParts.Stretch(inputText.rectTransform, 0f, 0f, 0f, 0f);

            Label(inputPlaceholder, style.ValueFont, style.InputTextSize, style.InputPlaceholderColor, FontStyles.Italic);
            inputPlaceholder.alignment = TextAlignmentOptions.Left;
            inputPlaceholder.text = newClientSeedLabel;
            UiWindowParts.Stretch(inputPlaceholder.rectTransform, 0f, 0f, 0f, 0f);

            input.characterLimit = Mathf.Max(1, clientSeedLength);
            input.customCaretColor = true;
            input.caretColor = style.CaretColor;
            input.caretWidth = Mathf.RoundToInt(Mathf.Max(1f, style.CaretWidth));
            input.selectionColor = style.SelectionColor;
        }

        private void PaintRandomize()
        {
            Paint(randomizeBox, style.ButtonFill, style.ButtonCornerRadius);
            randomizeBox.raycastTarget = true;

            // The arrow and the word held together in the middle of the button: a flexible track on each side
            // and the pair between them, which is what a grid has instead of a margin of auto.
            UiWindowParts.Stretch(Rect(randomizeRow), 0f, 0f, 0f, 0f);
            Columns(randomizeRow,
                GridTrack.Flexible(),
                GridTrack.Fixed(style.ArrowSize),
                GridTrack.Auto(),
                GridTrack.Flexible());
            Rows(randomizeRow, GridTrack.Flexible());
            randomizeRow.ColumnGap = style.ArrowGap;
            randomizeRow.padding = new RectOffset(0, 0, 0, 0);

            Middle(randomizeArrow.Root, 1, 0, new Vector2(style.ArrowSize, style.ArrowSize));
            Put(randomizeText.rectTransform, 2, 0);

            PaintArrow(randomizeArrow, style.ButtonFill);

            Label(randomizeText, style.CaptionFont, style.ButtonTextSize, style.ButtonTextColor, style.CaptionStyle);
            randomizeText.text = randomizeLabel;
            randomizeText.alignment = TextAlignmentOptions.Left;
        }

        // A circular arrow out of three boxes: a ring, a notch cut out of it in the button's own colour, and a
        // diamond for the head. Cheaper than an atlas entry and sharp at any size - the one thing to know is
        // that the notch is painted, not cut, so it only disappears against a flat button.
        private void PaintArrow(Arrow arrow, Color behind)
        {
            float span = style.ArrowSize;
            bool sprite = style.ArrowIcon != null;

            arrow.Picture.gameObject.SetActive(sprite);
            arrow.Picture.sprite = style.ArrowIcon;
            arrow.Picture.color = style.ButtonTextColor;
            arrow.Picture.preserveAspect = true;
            arrow.Picture.raycastTarget = false;
            UiWindowParts.Stretch(arrow.Picture.rectTransform, 0f, 0f, 0f, 0f);

            arrow.Ring.gameObject.SetActive(!sprite);
            arrow.Gap.gameObject.SetActive(!sprite);
            arrow.Head.gameObject.SetActive(!sprite);

            if (sprite)
                return;

            float thickness = Mathf.Max(0.5f, style.ArrowThickness);
            float radius = (span - thickness) * 0.5f;

            UiWindowParts.Pin(arrow.Ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(span, span), Vector2.zero);
            arrow.Ring.FillGradientMode = EFillGradient.None;
            arrow.Ring.FillColor = Color.clear;
            arrow.Ring.SetCornerRadius(100000f);
            arrow.Ring.SetBorderSize(thickness);
            arrow.Ring.SetBorderColor(style.ButtonTextColor);
            arrow.Ring.EdgeSoftness = 1f;
            arrow.Ring.raycastTarget = false;

            var notch = new Vector2(Mathf.Cos(55f * Mathf.Deg2Rad), Mathf.Sin(55f * Mathf.Deg2Rad)) * radius;
            UiWindowParts.Pin(arrow.Gap.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(thickness * 2.6f, thickness * 2.6f), notch);
            arrow.Gap.FillGradientMode = EFillGradient.None;
            arrow.Gap.FillColor = behind;
            arrow.Gap.SetBorderSize(0f);
            arrow.Gap.SetCornerRadius(0f);
            arrow.Gap.raycastTarget = false;

            UiWindowParts.Pin(arrow.Head.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(thickness * 2.1f, thickness * 2.1f), new Vector2(0f, radius));
            arrow.Head.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            arrow.Head.FillGradientMode = EFillGradient.None;
            arrow.Head.FillColor = style.ButtonTextColor;
            arrow.Head.SetBorderSize(0f);
            arrow.Head.SetCornerRadius(1f);
            arrow.Head.raycastTarget = false;
        }

        private void PaintHeadings()
        {
            PaintHeading(currentHeading, currentHeadingText, currentPairLabel);
            PaintHeading(previousHeading, previousHeadingText, previousPairLabel);
        }

        // The gap above a heading is padding on the heading's own cell rather than a larger row gap, because a
        // row gap is between every pair of rows and this one is only above the two headings.
        private void PaintHeading(UiGrid host, TextMeshProUGUI label, string text)
        {
            Columns(host, GridTrack.Flexible());
            Rows(host, GridTrack.Auto());
            host.RowGap = 0f;
            host.ColumnGap = 0f;
            host.padding = new RectOffset(0, 0, Mathf.RoundToInt(style.SectionGap), 0);
            Put(label.rectTransform, 0, 0);

            Label(label, style.HeadingFont, style.HeadingSize, style.HeadingColor, style.HeadingStyle);
            label.alignment = TextAlignmentOptions.Left;
            label.text = text;
        }

        private void PaintEntries()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                Columns(entry.Root, GridTrack.Flexible());
                Rows(entry.Root, GridTrack.Auto(), GridTrack.Auto());
                entry.Root.RowGap = style.CaptionGap;
                entry.Root.ColumnGap = 0f;
                entry.Root.padding = new RectOffset(0, 0, 0, 0);
                Put(entry.Caption.rectTransform, 0, 0);
                Put(entry.Value.rectTransform, 0, 1);

                Label(entry.Caption, style.CaptionFont, style.CaptionSize, style.CaptionColor, style.CaptionStyle);
                entry.Caption.alignment = TextAlignmentOptions.Left;

                // The long strings - both seeds and the hash - are drawn smaller and wrap; the nonce and the
                // bet count are numbers and are drawn like any other value.
                bool wide = Wide(i);
                Label(entry.Value, style.ValueFont, wide ? style.HashSize : style.ValueSize, style.ValueColor,
                    wide ? FontStyles.Normal : style.ValueStyle);
                entry.Value.alignment = TextAlignmentOptions.Left;
            }
        }

        private static bool Wide(int index) =>
            index == ClientSeedEntry || index == ServerShaEntry
            || index == PrevClientSeedEntry || index == PrevServerSeedEntry;

        private void PaintLoader()
        {
            // The dots, held in the middle by a flexible track on each side.
            float dot = style.LoaderDotSize;
            Columns(loaderGrid,
                GridTrack.Flexible(),
                GridTrack.Fixed(dot),
                GridTrack.Fixed(dot),
                GridTrack.Fixed(dot),
                GridTrack.Flexible());
            Rows(loaderGrid, GridTrack.Flexible());
            loaderGrid.ColumnGap = style.LoaderDotGap;
            loaderGrid.padding = new RectOffset(0, 0, 0, 0);

            for (int i = 0; i < dots.Length; i++)
            {
                Paint(dots[i], style.LoaderColor, 100000f);
                Middle(dots[i].rectTransform, 1 + i, 0, new Vector2(dot, dot));
            }
        }

        // Both buttons and the box, locked or not. This is the one thing that is *not* said as a layout: a
        // locked control is still there to be read, so it is painted and made uninteractable rather than
        // taken out of the arrangement.
        private void ApplyLock()
        {
            var fill = locked ? style.ButtonLockedFill : style.ButtonFill;

            randomizeBox.FillColor = fill;
            renewBox.FillColor = fill;

            // The notch is painted in the button's own colour, so it has to follow the button being dimmed or
            // it comes back as a bright dash across the ring.
            if (randomizeArrow.Gap != null)
                randomizeArrow.Gap.FillColor = fill;

            if (renewArrow.Gap != null)
                renewArrow.Gap.FillColor = fill;

            randomizeButton.interactable = !locked;
            renewButton.interactable = !locked;

            inputBox.FillColor = locked ? style.InputLockedFill : style.InputFill;
            input.readOnly = locked;
            input.interactable = !locked;
        }

        // ------------------------------------------------------------------ what is showing

        // Says the arrangement as a layout - a picture of the grid, one area name per cell - rather than by
        // switching cells on and off and placing them by number.
        //
        // That is not a stylistic preference. A UiGrid takes a layout as the whole truth about which of its
        // children are showing, and re-asserts it every time it is enabled and every time its child list
        // changes; a grid with no layout takes *every* child as showing and does the same. So a cell hidden
        // with SetActive comes back the next time the window opens - the loader under the buttons, the nonce
        // row on a game that has no nonce. Naming the cells is the only way to be believed.
        private void Arrange()
        {
            contentRows.Clear();
            var content = UiGridLayout.Build().Columns(GridTrack.Flexible());

            if (controlsOn)
                Row(content, NewSeedArea, GridTrack.Auto());

            if (randomizeOn)
                Row(content, RandomizeArea, GridTrack.Fixed(style.RandomizeHeight));

            if (loaderOn)
                Row(content, LoaderArea, GridTrack.Fixed(style.LoaderHeight));

            if (currentOn)
            {
                Row(content, CurrentArea, GridTrack.Auto());
                Entries(content, 0, PreviousFrom);
            }

            if (previousOn)
            {
                Row(content, PreviousArea, GridTrack.Auto());
                Entries(content, PreviousFrom, EntryCount);
            }

            // Held open at one row: a layout of none would leave the grid on its own list, and its own list is
            // not what says what is showing here.
            contentGrid.SetLayout(content
                .Rows(contentRows.ToArray())
                .Size(1, Mathf.Max(1, contentRows.Count))
                .Done());
        }

        private void Entries(UiGridLayout.Builder content, int from, int to)
        {
            for (int i = from; i < to && i < entries.Count; i++)
            {
                if (entryOn[i])
                    Row(content, EntryArea + i.ToString(CultureInfo.InvariantCulture), GridTrack.Auto());
            }
        }

        private void Row(UiGridLayout.Builder content, string area, GridTrack track)
        {
            content.Row(area);
            contentRows.Add(track);
        }

        private bool Shown(int from, int to)
        {
            for (int i = from; i < to && i < entryOn.Length; i++)
            {
                if (entryOn[i])
                    return true;
            }

            return false;
        }

        // ------------------------------------------------------------------ the data

        private void Write(SeedDto data)
        {
            // A single-player roll is seeded from a client salt and counted by a nonce. A shared or
            // multiplayer round is seeded from a block hash nobody can have known in advance, where a nonce
            // per player would mean nothing - so the same two rows are captioned differently there.
            var pairLabel = single ? clientSeedLabel : blockHashLabel;

            Fill(NonceEntry, nonceLabel, data.Nonce.ToString(CultureInfo.InvariantCulture));
            Fill(ClientSeedEntry, pairLabel, data.ClientSalt);
            Fill(ServerShaEntry, serverShaLabel, data.ServerSeedSha512);
            Fill(PrevClientSeedEntry, pairLabel, data.PrevClientSalt);
            Fill(PrevServerSeedEntry, serverSeedLabel, data.PrevServerSeed);
            Fill(BetsMadeEntry, betsMadeLabel, data.TotalBetsMade.ToString(CultureInfo.InvariantCulture));

            FillInput(data);
        }

        private void Fill(int index, string caption, string value)
        {
            if (index >= entries.Count)
                return;

            var entry = entries[index];
            entry.Caption.text = caption;
            entry.Value.text = string.IsNullOrEmpty(value) ? emptyLabel : value;
        }

        // The box follows the pair, and only the pair: a broadcast that left the client seed alone must not
        // throw away what the player was half way through typing.
        private void FillInput(SeedDto data)
        {
            if (input == null)
                return;

            var salt = data.ClientSalt ?? string.Empty;
            if (filledFrom == salt)
                return;

            filledFrom = salt;
            input.SetTextWithoutNotify(salt);
        }

        private SystemDto System
        {
            get
            {
                var manager = StateManager.Inst;
                return manager != null && manager.MainState != null ? manager.MainState.SystemState : null;
            }
        }

        // A round is in play, or there is no word yet on whether one is - which is the same thing as far as
        // sending a seed change goes. Without a StateManager at all there is no round to be in the middle of,
        // and the window is being looked at rather than played.
        private bool Running
        {
            get
            {
                var system = System;
                if (system != null)
                    return system.Running;

                return StateManager.Inst != null;
            }
        }

        private bool IsSingle
        {
            get
            {
                var system = System;
                return system == null || system.GameType.GetValueOrDefault(EGameType.SINGLE) == EGameType.SINGLE;
            }
        }

        private void Listen(bool on)
        {
            var manager = StateManager.Inst;
            if (manager == null || manager.Events == null || !followState)
                return;

            if (on && !listening)
            {
                manager.Events.OnSeed.AddListener(HandleSeed);
                manager.Events.OnSystem.AddListener(HandleSystem);
                manager.Events.OnSystemRunning.AddListener(HandleRunning);
                listening = true;
            }
            else if (!on && listening)
            {
                manager.Events.OnSeed.RemoveListener(HandleSeed);
                manager.Events.OnSystem.RemoveListener(HandleSystem);
                manager.Events.OnSystemRunning.RemoveListener(HandleRunning);
                listening = false;
            }
        }

        private void HandleSeed(SeedDto value)
        {
            // Whatever was asked for has been answered - a new pair, or the same pair with a new client half.
            // Either way the controls are free again.
            pending = false;
            Fill();

            if (value != null)
                OnSeeds.Invoke(value);
        }

        // The round starting or ending is what locks and unlocks the controls, so both ways of hearing about
        // it come back here. GameType arrives the same way, and can turn the top half of the dialog off.
        private void HandleSystem(SystemDto value) => Fill();

        private void HandleRunning(bool running) => Fill();

        // Refresh, with whatever it throws kept out of the way of the socket callback that got here. A row
        // that could not be written - a payload shaped in a way this did not expect, a game's own listener
        // throwing - is a dialog with a gap in it, and a dialog with a gap in it beats a broadcast that took
        // the rest of the state down with it. The exception still goes to the console, where it belongs.
        private void Fill()
        {
            try
            {
                Refresh();
            }
            catch (Exception error)
            {
                Debug.LogException(error, this);
            }
        }

        // ------------------------------------------------------------------ the loader

        private void Pulse(bool on)
        {
            StopPulse();

            if (!on || !isActiveAndEnabled || style.LoaderPulse <= 0f)
                return;

            for (int i = 0; i < dots.Length; i++)
            {
                var rect = dots[i].rectTransform;
                rect.localScale = Vector3.one;

                // Built with DOTween.To rather than DOScale: the shortcuts live in DOTween's UI module, which
                // is compiled into the project's own assembly and cannot be reached from a package. UiWindow
                // and the bet info dialog do the same thing for the same reason.
                //
                // Unscaled, like every other tween in this folder - a dialog that opens over a paused game
                // should not be waiting on time that is not running.
                pulses.Add(DOTween
                    .To(() => rect.localScale, v => rect.localScale = v, Vector3.one * 1.45f, style.LoaderPulse)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .SetDelay(i * style.LoaderPulse * 0.3f)
                    .SetUpdate(true));
            }
        }

        private void StopPulse()
        {
            foreach (var tween in pulses)
            {
                if (tween != null && tween.IsActive())
                    tween.Kill();
            }

            pulses.Clear();

            foreach (var dot in dots)
            {
                if (dot != null)
                    dot.rectTransform.localScale = Vector3.one;
            }
        }

        // ------------------------------------------------------------------ the window around it

        // The window is sized from the content out rather than the other way round: how tall a fairness dialog
        // has to be depends on how far a SHA-512 wrapped, which nothing here knows in advance. The content
        // area is a grid, so the window can measure it itself - and hold the answer to what the screen has
        // room for, scrolling the rest.
        //
        // Twice, and that is not belt and braces. A label reports the height it needs at the width it has, and
        // its width is what the first pass settles - so on the way into a window that has just been activated
        // a wrapped hash measures as one line, the content is fitted to that, and the rows under it are left
        // hanging below the panel. The second pass measures against the widths the first wrote.
        private void FitWindow()
        {
            var host = Window;
            if (!fitWindowHeight || host == null || !isActiveAndEnabled)
                return;

            host.Fit();
            host.Fit();
        }

        // ------------------------------------------------------------------ the sample

        // A pair that reads the way the design was drawn, for a scene with no template running in it: a dialog
        // built from a menu has to look like the dialog rather than like a row of dots that never stops. A real
        // game never sees this - the moment there is a StateManager, only the server's seeds are shown.
        private static SeedDto Sample() => new SeedDto
        {
            ClientSalt = "2af1853b",
            Nonce = 2,
            ServerSeedSha512 =
                "2d7da949d826aa4f2eda18cc41421cce37d67d76431009d6b396a96dcd64e3a6fb17db3c42bd181db8eef8daa723afc3f21464c6d5beb7cfb05c6f67c9102a10",
            PrevClientSalt = "2af1853b",
            PrevServerSeed = "bd57aa1bebbc456a928448df3d94a017",
            TotalBetsMade = 0,
        };

        // What the server would have done, done locally, so the buttons do something in a scene that has no
        // socket to send them down. Only ever reached when there is no Emitter at all.
        private void Roll(string clientSalt, bool serverToo)
        {
            var data = preview ??= Sample();

            data.PrevClientSalt = data.ClientSalt;
            data.TotalBetsMade = data.Nonce;

            if (serverToo)
                data.PrevServerSeed = Hex(32);

            data.ClientSalt = string.IsNullOrEmpty(clientSalt) ? Hex(8) : clientSalt;
            data.ServerSeedSha512 = Hex(128);
            data.Nonce = 0;
        }

        private static string Hex(int length)
        {
            var text = new char[length];
            for (int i = 0; i < length; i++)
                text[i] = HexDigits[UnityEngine.Random.Range(0, HexDigits.Length)];

            return new string(text);
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

        private static UiGridItem Item(RectTransform rect)
        {
            var item = rect.GetComponent<UiGridItem>();
            if (item == null)
                item = rect.gameObject.AddComponent<UiGridItem>();

            return item;
        }

        /// <summary>Puts something in a cell, stretched to fill it.</summary>
        // Every cell of the nested grids is placed by hand rather than left to the flow: a caption and its
        // value are a fixed pair, and the flow would only know the order they were made in.
        private static void Put(RectTransform rect, int column, int row, int span = 1)
        {
            var item = Item(rect);
            item.PlaceAt(column, row);
            item.Span(span, 1);
            item.OverrideAlign = false;
        }

        /// <summary>Puts something of its own size in the middle of a cell. For anything square - a padlock,
        /// an arrow, a dot - which a stretch would pull out of shape.</summary>
        private static void Middle(RectTransform rect, int column, int row, Vector2 size)
        {
            var item = Item(rect);
            item.PlaceAt(column, row);
            item.Span(1, 1);
            item.OverrideAlign = true;
            item.HorizontalAlign = EGridAlign.Center;
            item.VerticalAlign = EGridAlign.Center;
            UiWindowParts.Measured(rect, size);
        }

        /// <summary>Gives a cell the name the content grid's layout knows it by. The layout places it; this
        /// only says which cell of the picture is which panel.</summary>
        private static void Named(RectTransform rect, string area)
        {
            var item = Item(rect);
            item.Area = area;
            item.OverrideAlign = false;
        }

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
            label.raycastTarget = false;
        }
    }
}
