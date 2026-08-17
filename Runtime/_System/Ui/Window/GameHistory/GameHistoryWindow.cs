using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // The game history dialog, built into a UiWindow: one round of a shared game, everybody who bet on it, and
    // the seeds it was rolled from.
    //
    //     var rounds = GameHistoryWindow.Create(canvas);
    //     rounds.BetInfo = betInfoWindow;   // so a row can be pressed for the bet behind it
    //     rounds.Show(roundId);
    //
    // Show emits GAME_HISTORY_INFO through Emitter and opens the window on a row of pulsing dots. The answer
    // arrives as ON_GAME_HISTORY_ID, which the socket puts in MainState.GameHistoryById and announces as
    // OnGameHistoryById; this listens for that and fills itself in. It is the shared-game counterpart of
    // BetInfoWindow next door, and the two work together: a round lists its transactions, and pressing one
    // opens the bet info dialog on that bet.
    //
    // What the round *was* is the game's business and cannot be known here - a multiplier, a colour, a card -
    // so whatever is parented into Outcome is laid out as a strip across the top and OnRound is where a game
    // fills it in. The rest is what every shared round carries however it was played: who bet, what they bet,
    // what came back, and the totals underneath.
    //
    // Amounts are shown in the currency each player bet in, which in a shared game is rarely the same one
    // twice. The toggle beside the outcome converts the lot to dollars at the rate the server sent with each
    // transaction, so a round can be read either as what everyone staked or as what it was all worth.
    [AddComponentMenu("UI/Game History Window")]
    [RequireComponent(typeof(UiWindow))]
    public class GameHistoryWindow : MonoBehaviour
    {
        // What each cell answers to in its grid's layout. One word each, and that is a requirement rather than
        // a habit: a layout is stored as text, so a name with a space in it comes back out as two cells.
        private const string HeaderArea = "header";
        private const string OutcomeArea = "outcome";
        private const string ToggleArea = "toggle";
        private const string ListArea = "list";
        private const string LoaderArea = "loader";
        private const string StatsArea = "stats";
        private const string SeedsArea = "seeds";
        private const string BarArea = "bar";
        private const string DetailsArea = "details";
        private const string VerifyArea = "verify";
        private const string EmptyArea = "none";
        private const string SeedArea = "seed";
        private const string RowArea = "r";

        // What a row answers to while it is spare. The layout never mentions it, which is what keeps it hidden.
        private const string SpareArea = "";

        [SerializeField]
        private GameHistoryWindowStyle style = new GameHistoryWindowStyle();

        [Header("Labels")]
        // Serialized rather than translated, the same as the windows next door: the template has no opinion
        // about the game's language, and a game that has one already knows where its strings live. Every one
        // of them goes through Translator.Label, so a caption left as it came translates anyway.
        [SerializeField]
        private string totalBetCountLabel = "Total bet count";

        [SerializeField]
        private string totalBetAmountLabel = "Total bet amount";

        [SerializeField]
        private string totalProfitLabel = "Total profit";

        [SerializeField]
        private string detailsLabel = "Details";

        [SerializeField]
        private string verifyLabel = "Verify";

        [SerializeField]
        private string clientSeedLabel = "Client seed";

        [SerializeField]
        private string serverSeedLabel = "Server seed";

        [SerializeField]
        private string serverShaLabel = "Server SHA-512";

        [Tooltip("Printed where the server seed would be while the round's seed is still in play and cannot be given out.")]
        [SerializeField]
        private string hiddenLabel = "Hidden";

        [Tooltip("Shown in place of the list on a round nobody bet on.")]
        [SerializeField]
        private string emptyLabel = "No bets on this round";

        [Tooltip("On the currency toggle while the amounts are in dollars.")]
        [SerializeField]
        private string usdLabel = "$";

        [Tooltip("On the currency toggle while the amounts are in the currency each bet was made in.")]
        [SerializeField]
        private string coinLabel = "¤";

        [Header("Blocks")]
        [Tooltip("The strip across the top the game draws the round into. It is only laid out once something has been parented into Outcome.")]
        [SerializeField]
        private bool showOutcome = true;

        [Tooltip("The button that swaps every amount between its own currency and dollars.")]
        [SerializeField]
        private bool showCurrencyToggle = true;

        [SerializeField]
        private bool showTransactions = true;

        [Tooltip("The three totals under the list: how many bets there were, what they came to, and what the round paid.")]
        [SerializeField]
        private bool showTotals = true;

        [Tooltip("The Details button and the seeds it opens.")]
        [SerializeField]
        private bool showDetails = true;

        [Tooltip("The Verify button. It appears only on a round the server sent a verify url with.")]
        [SerializeField]
        private bool showVerify = true;

        [SerializeField]
        private bool detailsOpen = false;

        [Tooltip("Start with every amount converted to dollars rather than shown in the currency it was bet in.")]
        [SerializeField]
        private bool usdView = false;

        [Header("Behaviour")]
        [Tooltip("Fill in when the server sends a round.")]
        [SerializeField]
        private bool followState = true;

        [Tooltip("Ask the server for the round when Show is given an id. Off leaves the asking to the game.")]
        [SerializeField]
        private bool requestOnShow = true;

        [Tooltip("Ignore a round whose id is not the one that was asked for. Off takes whatever arrives, which is what a window opened without an id wants.")]
        [SerializeField]
        private bool matchRequestedId = true;

        [Tooltip("Put the player's own row at the top, ahead of the sort. Off leaves it wherever its winnings put it.")]
        [SerializeField]
        private bool mineFirst = true;

        [Tooltip("Fetch the currency and avatar pictures the server sends as urls. Off leaves the drawn stand-ins showing.")]
        [SerializeField]
        private bool loadImages = true;

        [Tooltip("Resize the window to exactly the blocks it is showing.")]
        [SerializeField]
        private bool fitWindowHeight = true;

        [Tooltip("Close the seeds when the window closes, so a dialog reopened on another round starts the way it first opened.")]
        [SerializeField]
        private bool collapseDetailsOnClose = true;

        [Tooltip("The bet info dialog a pressed row opens. Nothing is found for you: a scene has as many bet info windows in it as somebody put there. Leave it empty and a row only raises On Picked.")]
        [SerializeField]
        private BetInfoWindow betInfo;

        [Header("Events")]
        [Tooltip("A round has arrived and the window is filled in. Where a game draws its own view of it into Outcome.")]
        public UnityEvent<GameHistoryByIdDto> OnRound = new UnityEvent<GameHistoryByIdDto>();

        [Tooltip("An id has been asked of the server, before the answer.")]
        public UnityEvent<string> OnRequested = new UnityEvent<string>();

        [Tooltip("A row was pressed, carrying that bet's id. Raised before the bet info window is opened, and whether or not it is.")]
        public UnityEvent<string> OnPicked = new UnityEvent<string>();

        [Tooltip("The verify url, as the browser is being sent to it.")]
        public UnityEvent<string> OnVerify = new UnityEvent<string>();

        public UnityEvent<bool> OnDetailsToggled = new UnityEvent<bool>();

        public UnityEvent<bool> OnCurrencyToggled = new UnityEvent<bool>();

        // Parts, and the flag saying they exist, are deliberately not serialized - the same choice the windows
        // next door make. Everything is found by name before it is made, so a rebuild after a script reload
        // finds the hierarchy that is already there rather than building a second one beside it.
        private UiWindow window;

        private UiGrid header;
        private UiGrid outcome;
        private RoundedBox toggleBox;
        private Button toggleButton;
        private TextMeshProUGUI toggleText;

        private RectTransform list;
        private ScrollRect scroller;
        private RectMask2D clip;
        private Image grab;
        private UiGrid listGrid;
        private TextMeshProUGUI emptyText;

        private RectTransform loader;
        private UiGrid loaderGrid;
        private readonly RoundedBox[] dots = new RoundedBox[3];
        private readonly List<Tween> pulses = new List<Tween>();

        private UiGrid stats;
        private readonly Stat[] totals = new Stat[3];

        private RoundedBox seedsCard;
        private UiGrid seedsGrid;
        private readonly List<Seed> seeds = new List<Seed>();

        private RectTransform bar;
        private UiGrid barGrid;
        private RoundedBox detailsBox;
        private Button detailsButton;
        private TextMeshProUGUI detailsText;
        private RoundedBox verifyBox;
        private Button verifyButton;
        private TextMeshProUGUI verifyText;

        private readonly List<Row> live = new List<Row>();
        private readonly List<Row> spare = new List<Row>();

        // Row lists are built per layout pass rather than fixed: a gap between two tracks is there whether or
        // not anything is in them, so a block that is switched off has to take its row away with it.
        private readonly List<GridTrack> contentRows = new List<GridTrack>();
        private readonly List<GridTrack> listRows = new List<GridTrack>();
        private readonly List<GridTrack> seedRows = new List<GridTrack>();
        private readonly List<Entry> entries = new List<Entry>();

        // What is showing, decided in Refresh and said to the grids in Arrange.
        private bool hasData;
        private bool headerOn;
        private bool outcomeOn;
        private bool toggleOn;
        private bool listOn;
        private bool totalsOn;
        private bool seedsOn;
        private bool detailsOn;
        private bool verifyOn;
        private readonly bool[] seedOn = new bool[3];

        private GameHistoryByIdDto round;
        private GameHistoryDto summary;
        private GameHistoryByIdDto preview;
        private string requestedId = string.Empty;
        private bool built;
        private bool listening;

        // Bumped whenever the window is pointed at another round, so a picture that arrives after the player
        // has moved on lands nowhere rather than on the wrong coin.
        private int stamp;

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

        /// <summary>Colours, sizes and fonts of the list and the blocks around it. Edit and call
        /// <see cref="Rebuild"/>, or assign a whole new one.</summary>
        public GameHistoryWindowStyle Style
        {
            get => style;
            set
            {
                style = value ?? new GameHistoryWindowStyle();
                Rebuild();
            }
        }

        /// <summary>What is being shown: the server's round, or a sample in a scene with no template running at
        /// all. Null while the answer is still on its way.</summary>
        public GameHistoryByIdDto Round
        {
            get
            {
                if (round != null)
                    return round;

                // The answer can have arrived while the window was closed, and a closed window is not
                // listening - so the state is asked again rather than opening on a loader for a round the
                // template already has. Only for the id that was asked for: anything else is another window's
                // answer.
                var known = Known();
                if (known != null && !string.IsNullOrEmpty(requestedId) && known.Id == requestedId)
                    return known;

                // No StateManager anywhere means a scene built to look at the window rather than to play in,
                // where a row of dots that never stops is no use to anyone.
                if (StateManager.Inst == null)
                    return preview ??= Sample();

                return null;
            }
        }

        /// <summary>The round's own line in the history strip, when the strip handed one over. It is where the
        /// totals under the list come from on a server that only fills them in on the summary.</summary>
        public GameHistoryDto Summary
        {
            get => summary;
            set
            {
                summary = value;
                Refresh();
            }
        }

        /// <summary>The id <see cref="Show(string)"/> was last given, or empty.</summary>
        public string RequestedId => requestedId;

        /// <summary>A strip across the top for the game's own view of the round - what the wheel landed on,
        /// where the ball fell. Parent anything into it and the window grows around it.</summary>
        // The one thing the template cannot fill in for a game, so it is a container rather than a shape: a
        // grid of one column, so several things stacked into it come out in order, and an auto row each so each
        // is as tall as it says it needs to be.
        //
        // Deliberately the only way in. The window's content is arranged by a layout that names its cells, and
        // a grid hides what its layout does not name - so a panel parented straight into the content would be
        // switched off the moment the layout was set again.
        public RectTransform Outcome
        {
            get
            {
                EnsureBuilt();
                return Rect(outcome);
            }
        }

        /// <summary>The scroller the list of transactions runs in, for a game that wants to drive it. Enabled
        /// only while there are more rows than the list is allowed to be tall.</summary>
        public ScrollRect Scroller
        {
            get
            {
                EnsureBuilt();
                return scroller;
            }
        }

        /// <summary>The bet info dialog a pressed row opens. Null opens nothing - a press still raises
        /// <see cref="OnPicked"/>, which is all a game driving its own dialog needs.</summary>
        public BetInfoWindow BetInfo
        {
            get => betInfo;
            set => betInfo = value;
        }

        public Button DetailsButton
        {
            get
            {
                EnsureBuilt();
                return detailsButton;
            }
        }

        public Button VerifyButton
        {
            get
            {
                EnsureBuilt();
                return verifyButton;
            }
        }

        /// <summary>The currency toggle, for a game that would rather drive it from its own control.</summary>
        public Button CurrencyButton
        {
            get
            {
                EnsureBuilt();
                return toggleButton;
            }
        }

        /// <summary>Whether the seeds are showing. Setting it opens or closes them and refits the window.</summary>
        public bool DetailsOpen
        {
            get => detailsOpen;
            set
            {
                if (detailsOpen == value)
                    return;

                detailsOpen = value;
                Refresh();
                OnDetailsToggled.Invoke(value);
            }
        }

        /// <summary>Whether every amount is converted to dollars at the rate the server sent with each
        /// transaction. Off shows each bet in the currency it was made in.</summary>
        public bool UsdView
        {
            get => usdView;
            set
            {
                if (usdView == value)
                    return;

                usdView = value;
                Refresh();
                OnCurrencyToggled.Invoke(value);
            }
        }

        /// <summary>Builds the whole thing - window, list and all - under a parent.</summary>
        // Added before the object wakes, for the reason UiWindowBuilder.Add exists: a component on an active
        // object runs its Awake there and then, and the list would go into a window that had already put itself
        // away.
        public static GameHistoryWindow Create(Transform parent, string name = "Game History", string title = "Game history")
        {
            UiWindowBuilder.Create(parent, name)
                .Size(560f, 700f)
                .Title(title)
                .Add(out GameHistoryWindow rounds)
                .Done();

            // Awake has done this already in play mode. In the editor nothing else ever will, and a window
            // built from a context menu that came out empty would be a poor way to find that out.
            if (rounds != null)
                rounds.EnsureBuilt();

            return rounds;
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

        /// <summary>Builds the header, the list, the totals, the seeds and the buttons from scratch, then
        /// redraws. Call after changing the style from code.</summary>
        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            if (style == null)
                style = new GameHistoryWindowStyle();

            var host = Window;
            if (host == null)
                return;

            host.EnsureBuilt();
            host.ApplyLayout();

            BuildParts(host.Content);

            built = true;
            Refresh();
        }

        /// <summary>Asks the server for a round and opens the window on it.</summary>
        // The request goes out and the window opens on the loader rather than waiting for the answer and
        // opening on a filled list: a dialog that appears half a second after the press reads as a dropped
        // press. Where the state already holds this round it is used at once and nothing is asked.
        public void Show(string id)
        {
            requestedId = id ?? string.Empty;
            round = null;
            stamp++;

            // The summary belongs to whichever round it was handed over with, so a window pointed at another
            // one drops it rather than printing the last round's totals under this round's bets.
            if (summary != null && summary.Id != requestedId)
                summary = null;

            var known = Known();
            if (known != null && (!matchRequestedId || known.Id == requestedId))
                round = known;

            if (round == null && requestOnShow && !string.IsNullOrEmpty(requestedId) && Emitter.Inst != null)
            {
                Emitter.Inst.OnGameHistoryInfo(requestedId);
                OnRequested.Invoke(requestedId);
            }

            EnsureBuilt();
            Fill();
            Window.Open();
        }

        /// <summary>Opens on a round the history strip handed over: the summary is shown at once - the outcome
        /// and the totals - while the transactions are asked for.</summary>
        public void Show(GameHistoryDto value)
        {
            summary = value;
            Show(value != null ? value.Id : string.Empty);
        }

        /// <summary>Opens the window on a round the game already has, without asking the server.</summary>
        public void Show(GameHistoryByIdDto value)
        {
            Apply(value);
            Window.Open();
        }

        /// <summary>Opens on whatever the state last received - for a window driven by the game's own request
        /// rather than by this one's.</summary>
        public void Show()
        {
            requestedId = string.Empty;
            Apply(Known());
            Window.Open();
        }

        /// <summary>Asks the server for a round without opening anything.</summary>
        public void Request(string id)
        {
            if (string.IsNullOrEmpty(id) || Emitter.Inst == null)
                return;

            requestedId = id;
            Emitter.Inst.OnGameHistoryInfo(id);
            OnRequested.Invoke(id);
        }

        /// <summary>Back to the loader, for a window about to be pointed at another round.</summary>
        public void Clear()
        {
            round = null;
            summary = null;
            requestedId = string.Empty;
            stamp++;
            Refresh();
        }

        public void ToggleDetails() => DetailsOpen = !detailsOpen;

        public void ToggleCurrency() => UsdView = !usdView;

        /// <summary>Sends the browser to the server's verifier with this round's seeds in the query, which is
        /// what makes a shared roll checkable from outside the game.</summary>
        public void Verify()
        {
            var data = Round;
            if (data == null || string.IsNullOrEmpty(data.VerifyUrl))
                return;

            var url = VerifyUrl(data);
            OnVerify.Invoke(url);
            Application.OpenURL(url);
        }

        /// <summary>What a row does when it is pressed: raises <see cref="OnPicked"/>, then opens the bet info
        /// dialog on that bet. Public so a game can drive it from its own control.</summary>
        public void Pick(string betId)
        {
            if (string.IsNullOrEmpty(betId))
                return;

            OnPicked.Invoke(betId);

            if (betInfo == null)
                return;

            // Show asks the server for the bet and opens on a loader, which is the right thing here: a round's
            // transaction is a summary, and the dialog wants the whole thing with its seeds.
            betInfo.Show(betId);
        }

        /// <summary>Fills the list and the totals in from the round and lays the window out again.</summary>
        public void Refresh()
        {
            if (!built)
                return;

            var data = Round;

            // Which blocks are showing is worked out here and said to the grids in Arrange, as a layout.
            // Deliberately not SetActive: a UiGrid shows what its layout names and hides what it does not, and
            // it re-asserts that every time it is enabled - so a cell switched off behind the grid's back comes
            // back on the next time the window opens. The layout is the only thing a grid will not argue with.
            hasData = data != null;
            outcomeOn = showOutcome && Rect(outcome).childCount > 0;
            toggleOn = hasData && showCurrencyToggle;
            headerOn = outcomeOn || toggleOn;
            listOn = hasData && showTransactions;
            totalsOn = hasData && showTotals;
            detailsOn = hasData && showDetails;
            verifyOn = hasData && showVerify && !string.IsNullOrEmpty(data.VerifyUrl);

            Write(data);

            seedsOn = detailsOn && detailsOpen && Shown(seedOn);

            Pulse(!hasData);
            Layout();

            // Two more passes over the next two frames. Everything the layout needs is measured inside Layout,
            // except what only the canvas can settle - a font that finished loading, a picture that arrived, a
            // rect that had no width yet because the window was activated this frame.
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
            PaintHeader();
            PaintList();
            PaintTotals();
            PaintSeeds();
            PaintBar();
            Arrange();
            FitWindow();
        }

        // ------------------------------------------------------------------ building

        private void BuildParts(RectTransform content)
        {
            GridOn(content);

            header = Grid(content, "Header");
            outcome = Grid(Rect(header), "Outcome");

            toggleBox = UiWindowParts.Box(Rect(header), "Currency");
            toggleButton = Hook(toggleBox, ToggleCurrency);
            toggleText = UiWindowParts.Label(toggleBox.transform, "Label");

            list = UiWindowParts.Rect(content, "Transactions");

            scroller = list.GetComponent<ScrollRect>();
            if (scroller == null)
                scroller = list.gameObject.AddComponent<ScrollRect>();

            clip = list.GetComponent<RectMask2D>();
            if (clip == null)
                clip = list.gameObject.AddComponent<RectMask2D>();

            // Something for a finger to take hold of. A ScrollRect is only offered a drag - or a wheel - if the
            // pointer lands on a raycast target that is the scroller itself or something under it, and a drag
            // that starts in the gap between two rows lands on nothing at all. This is what it lands on:
            // invisible, on the same object as the ScrollRect, and behind every row in it.
            grab = list.GetComponent<Image>();
            if (grab == null)
                grab = list.gameObject.AddComponent<Image>();

            grab.color = new Color(0f, 0f, 0f, 0f);
            grab.raycastTarget = true;

            listGrid = Grid(list, "Rows");
            emptyText = UiWindowParts.Label(Rect(listGrid), "Empty");

            loader = Rect(Grid(content, "Loader"));
            loaderGrid = loader.GetComponent<UiGrid>();
            for (int i = 0; i < dots.Length; i++)
                dots[i] = UiWindowParts.Box(loader, "Dot " + i);

            stats = Grid(content, "Totals");
            for (int i = 0; i < totals.Length; i++)
            {
                totals[i] = new Stat
                {
                    Caption = UiWindowParts.Label(Rect(stats), "Caption " + i),
                    Value = UiWindowParts.Label(Rect(stats), "Value " + i),
                };
            }

            seedsCard = UiWindowParts.Box(content, "Seeds");
            seedsGrid = GridOn(seedsCard.rectTransform);

            seeds.Clear();
            for (int i = 0; i < seedOn.Length; i++)
            {
                var seed = new Seed { Root = Grid(seedsCard.transform, "Seed " + i) };
                seed.Caption = UiWindowParts.Label(Rect(seed.Root), "Caption");
                seed.Value = UiWindowParts.Label(Rect(seed.Root), "Value");
                seeds.Add(seed);
            }

            bar = Rect(Grid(content, "Bar"));
            barGrid = bar.GetComponent<UiGrid>();

            detailsBox = UiWindowParts.Box(bar, "Details");
            detailsButton = Hook(detailsBox, ToggleDetails);
            detailsText = UiWindowParts.Label(detailsBox.transform, "Label");

            verifyBox = UiWindowParts.Box(bar, "Verify");
            verifyButton = Hook(verifyBox, Verify);
            verifyText = UiWindowParts.Label(verifyBox.transform, "Label");

            // Rows already under the grid are taken over rather than left there, which is what makes Rebuild
            // safe to call twice - on a window saved as a prefab, or one rebuilt after a script reload.
            Collect();

            // Same Remove-then-Add as the buttons, and here for the same reason: AddListener is not
            // serialized, so this belongs where the parts are wired rather than where they are made.
            var host = Window;
            host.OnClosed.RemoveListener(HandleClosed);
            host.OnClosed.AddListener(HandleClosed);

            // What every cell answers to in its grid's layout. Set once, here, where the parts are: a name is a
            // property of the panel, and Arrange only draws the picture that uses them.
            Named(Rect(header), HeaderArea);
            Named(Rect(outcome), OutcomeArea);
            Named(list, ListArea);
            Named(loader, LoaderArea);
            Named(Rect(stats), StatsArea);
            Named(seedsCard.rectTransform, SeedsArea);
            Named(bar, BarArea);
            Named(emptyText.rectTransform, EmptyArea);

            for (int i = 0; i < seeds.Count; i++)
                Named(Rect(seeds[i].Root), SeedArea + i.ToString(CultureInfo.InvariantCulture));
        }

        // A window that closed with its seeds open would come back open on them - and come back the height the
        // last round's seeds needed, which is not the height this one's do.
        private void HandleClosed()
        {
            if (collapseDetailsOnClose)
                detailsOpen = false;
        }

        // One player's bets on this round: the mark that says the row can be pressed, who made it, and what it
        // cost against what it paid. The row is the button - the whole strip, not a target the size of a
        // fingernail - and the mark beside the avatar is what says so.
        private class Row
        {
            public string Id;
            public RoundedBox Plate;
            public UiGrid Grid;
            public Button Button;
            public Mark Mark;
            public Badge Avatar;
            public TextMeshProUGUI Name;
            public UiGrid Amounts;
            public TextMeshProUGUI BetValue;
            public Badge BetCoin;
            public TextMeshProUGUI WinValue;
            public Badge WinCoin;
        }

        // One line of the totals: a caption on the left and a figure on the right.
        private class Stat
        {
            public TextMeshProUGUI Caption;
            public TextMeshProUGUI Value;
        }

        // One seed: a caption over a string long enough to wrap.
        private class Seed
        {
            public UiGrid Root;
            public TextMeshProUGUI Caption;
            public TextMeshProUGUI Value;
        }

        // A round or rounded plate with a letter on it and a picture over the top: a currency coin, a player's
        // avatar. The plate is what is on screen until - or unless - the server's image arrives, so a window
        // with no network still reads as a window.
        private class Badge
        {
            public RectTransform Root;
            public RoundedBox Plate;
            public TextMeshProUGUI Letter;
            public Image Picture;
        }

        // The plate at the head of a row, drawn from a box and two bars rather than fetched from an atlas - the
        // same choice the close cross and the bet info tick make, and for the same reason: it costs no atlas
        // entry and stays sharp at any size.
        private class Mark
        {
            public RectTransform Root;
            public RoundedBox Plate;
            public RoundedBox BarA;
            public RoundedBox BarB;
            public Image Picture;
        }

        // Everything already under the rows grid is taken over rather than left there. Nothing is thrown away:
        // a row is made of the same parts whatever round it is showing.
        private void Collect()
        {
            live.Clear();
            spare.Clear();

            var rows = Rect(listGrid);
            for (int i = 0; i < rows.childCount; i++)
            {
                var child = rows.GetChild(i);
                var plate = child.GetComponent<RoundedBox>();

                if (plate == null || !child.name.StartsWith("Row ", StringComparison.Ordinal))
                    continue;

                spare.Add(Adopt(plate));
            }
        }

        // The parts of one row, found by name before they are made - so this is both how a row is built and how
        // one that came back from a prefab is picked up again.
        private Row Adopt(RoundedBox plate)
        {
            var row = new Row
            {
                Plate = plate,
                Grid = GridOn(plate.rectTransform),
            };

            row.Button = Hook(plate, null);
            row.Mark = BuildMark(plate.transform, "Mark");
            row.Avatar = BuildBadge(plate.transform, "Avatar");
            row.Name = UiWindowParts.Label(plate.transform, "Name");

            row.Amounts = Grid(plate.transform, "Amounts");
            row.BetValue = UiWindowParts.Label(Rect(row.Amounts), "Bet");
            row.BetCoin = BuildBadge(Rect(row.Amounts), "Bet Coin");
            row.WinValue = UiWindowParts.Label(Rect(row.Amounts), "Win");
            row.WinCoin = BuildBadge(Rect(row.Amounts), "Win Coin");

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

        private Badge BuildBadge(Transform parent, string name)
        {
            var badge = new Badge { Root = UiWindowParts.Rect(parent, name) };
            badge.Plate = UiWindowParts.Box(badge.Root, "Plate");
            badge.Letter = UiWindowParts.Label(badge.Plate.transform, "Letter");
            badge.Picture = UiWindowParts.Picture(badge.Root, "Picture");
            return badge;
        }

        private Mark BuildMark(Transform parent, string name)
        {
            var mark = new Mark { Root = UiWindowParts.Rect(parent, name) };
            mark.Plate = UiWindowParts.Box(mark.Root, "Plate");
            mark.BarA = UiWindowParts.Box(mark.Root, "Bar A");
            mark.BarB = UiWindowParts.Box(mark.Root, "Bar B");
            mark.Picture = UiWindowParts.Picture(mark.Root, "Picture");
            return mark;
        }

        private Button Hook(RoundedBox box, UnityAction handler)
        {
            var button = box.GetComponent<Button>();
            if (button == null)
                button = box.gameObject.AddComponent<Button>();

            button.targetGraphic = box;

            if (handler == null)
                return button;

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
            var contentGrid = GridOn(Window.Content);
            Columns(contentGrid, GridTrack.Flexible());
            contentGrid.RowGap = style.SectionGap;
            contentGrid.ColumnGap = 0f;
            contentGrid.padding = new RectOffset(0, 0, 0, 0);
        }

        private void PaintHeader()
        {
            header.RowGap = 0f;
            header.ColumnGap = style.ColumnGap;
            header.padding = new RectOffset(0, 0, 0, 0);

            // The toggle's width on each side, so the outcome sits in the middle of the window rather than in
            // the middle of what the toggle left over.
            Columns(header,
                GridTrack.Fixed(style.ToggleSize.x),
                GridTrack.Flexible(),
                GridTrack.Fixed(style.ToggleSize.x));

            Rows(header, GridTrack.Auto());

            // One column and an auto row, so whatever a game stacks into Outcome comes out in hierarchy order
            // with each block as tall as it reports itself to be. Rows past the first are implicit, and
            // implicit rows are auto - a game adding a second view does not have to say so here.
            Columns(outcome, GridTrack.Flexible());
            Rows(outcome, GridTrack.Auto());
            outcome.RowGap = style.SectionGap;
            outcome.ColumnGap = 0f;
            outcome.padding = new RectOffset(0, 0, 0, 0);

            Named(toggleBox.rectTransform, ToggleArea, style.ToggleSize);
            Paint(toggleBox, style.ToggleFill, style.ToggleCornerRadius);
            toggleBox.raycastTarget = true;

            UiWindowParts.Stretch(toggleText.rectTransform, 2f, 0f, 2f, 0f);
            Label(toggleText, style.CaptionFont, style.ToggleTextSize, style.ToggleTextColor, style.CaptionStyle);
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

            Label(emptyText, style.CaptionFont, style.CaptionSize, style.CaptionColor, style.CaptionStyle);
            emptyText.text = Translator.Label(emptyLabel);

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

        // The shape of one row: the mark, the avatar, the name, and the two amounts stacked on the right. Said
        // here rather than when the row was made, so a style edited from code reaches rows that already exist.
        private void PaintRow(Row row)
        {
            int pad = Mathf.RoundToInt(style.RowPadding);
            row.Grid.padding = new RectOffset(pad, pad, pad, pad);
            row.Grid.ColumnGap = style.IconGap;
            row.Grid.RowGap = 0f;
            row.Plate.raycastTarget = true;

            bool markOn = style.MarkSize > 0f;
            bool avatarOn = style.AvatarSize > 0f;

            Columns(row.Grid,
                GridTrack.Fixed(markOn ? style.MarkSize : 0f),
                GridTrack.Fixed(avatarOn ? style.AvatarSize : 0f),
                GridTrack.Flexible(),
                GridTrack.Auto());

            Rows(row.Grid, GridTrack.Flexible());

            Middle(row.Mark.Root, 0, 0, new Vector2(style.MarkSize, style.MarkSize));
            Middle(row.Avatar.Root, 1, 0, new Vector2(style.AvatarSize, style.AvatarSize));
            Put(row.Name.rectTransform, 2, 0);
            Put(Rect(row.Amounts), 3, 0);

            Label(row.Name, style.ValueFont, style.RowNameSize, style.RowTextColor, style.ValueStyle);
            row.Name.alignment = TextAlignmentOptions.Left;
            row.Name.overflowMode = TextOverflowModes.Ellipsis;

            // The amounts: what was staked over what came back, each with its coin beside it. A flexible track
            // in front holds the pair against the right edge however wide the figures come out.
            row.Amounts.ColumnGap = style.IconGap;
            row.Amounts.RowGap = 0f;
            row.Amounts.padding = new RectOffset(0, 0, 0, 0);
            Columns(row.Amounts, GridTrack.Flexible(), GridTrack.Auto(), GridTrack.Fixed(style.CoinSize));
            Rows(row.Amounts, GridTrack.Flexible(), GridTrack.Flexible());

            Put(row.BetValue.rectTransform, 1, 0);
            Middle(row.BetCoin.Root, 2, 0, new Vector2(style.CoinSize, style.CoinSize));
            Put(row.WinValue.rectTransform, 1, 1);
            Middle(row.WinCoin.Root, 2, 1, new Vector2(style.CoinSize, style.CoinSize));

            Label(row.BetValue, style.ValueFont, style.RowAmountSize, style.RowTextColor, style.ValueStyle);
            Label(row.WinValue, style.ValueFont, style.RowAmountSize, style.RowTextColor, style.ValueStyle);
            row.BetValue.alignment = TextAlignmentOptions.Right;
            row.WinValue.alignment = TextAlignmentOptions.Right;

            Coin(row.BetCoin, style.CoinSize);
            Coin(row.WinCoin, style.CoinSize);
            Avatar(row.Avatar, style.AvatarSize);
            Drum(row.Mark, style.MarkSize);
        }

        private void PaintTotals()
        {
            stats.RowGap = style.StatRowGap;
            stats.ColumnGap = style.ColumnGap;
            stats.padding = new RectOffset(0, 0, 0, 0);
            Columns(stats, GridTrack.Flexible(), GridTrack.Auto());
            Rows(stats, GridTrack.Auto(), GridTrack.Auto(), GridTrack.Auto());

            for (int i = 0; i < totals.Length; i++)
            {
                var line = totals[i];

                Put(line.Caption.rectTransform, 0, i);
                Put(line.Value.rectTransform, 1, i);

                Label(line.Caption, style.CaptionFont, style.StatSize, style.StatCaptionColor, style.CaptionStyle);
                Label(line.Value, style.ValueFont, style.StatSize, style.StatValueColor, style.ValueStyle);

                line.Caption.alignment = TextAlignmentOptions.Left;
                line.Value.alignment = TextAlignmentOptions.Right;
            }
        }

        private void PaintSeeds()
        {
            Paint(seedsCard, style.CardFill, style.CardCornerRadius);

            int pad = Mathf.RoundToInt(style.CardPadding);
            seedsGrid.padding = new RectOffset(pad, pad, pad, pad);
            seedsGrid.RowGap = style.SeedRowGap;
            seedsGrid.ColumnGap = 0f;
            Columns(seedsGrid, GridTrack.Flexible());

            foreach (var seed in seeds)
            {
                seed.Root.padding = new RectOffset(0, 0, 0, 0);
                seed.Root.RowGap = style.CaptionGap;
                seed.Root.ColumnGap = 0f;
                Columns(seed.Root, GridTrack.Flexible());
                Rows(seed.Root, GridTrack.Auto(), GridTrack.Auto());

                Put(seed.Caption.rectTransform, 0, 0);
                Put(seed.Value.rectTransform, 0, 1);

                Label(seed.Caption, style.CaptionFont, style.CaptionSize, style.CaptionColor, style.CaptionStyle);
                Label(seed.Value, style.ValueFont, style.HashSize, style.ValueColor, FontStyles.Normal);

                seed.Caption.alignment = TextAlignmentOptions.Left;
                seed.Value.alignment = TextAlignmentOptions.Left;
            }
        }

        private void PaintBar()
        {
            barGrid.ColumnGap = style.ColumnGap;
            barGrid.padding = new RectOffset(0, 0, 0, 0);

            PaintButton(detailsBox, detailsText, detailsLabel, style.DetailsSize, DetailsArea);
            PaintButton(verifyBox, verifyText, verifyLabel, style.VerifySize, VerifyArea);
        }

        private void PaintButton(RoundedBox box, TextMeshProUGUI label, string text, Vector2 size, string area)
        {
            Named(box.rectTransform, area, size);
            Paint(box, style.ButtonFill, style.ButtonCornerRadius);
            box.raycastTarget = true;

            UiWindowParts.Stretch(label.rectTransform, 6f, 0f, 6f, 0f);
            Label(label, style.CaptionFont, style.ButtonTextSize, style.ButtonTextColor, style.CaptionStyle);
            label.text = Translator.Label(text);
        }

        // ------------------------------------------------------------------ what is showing

        // Says the arrangement as a layout - a picture of the grid, one area name per cell - rather than by
        // switching cells on and off and placing them by number. A UiGrid takes a layout as the whole truth
        // about which of its children are showing and re-asserts it every time it is enabled, so a cell hidden
        // with SetActive comes back the next time the window opens.
        private void Arrange()
        {
            ArrangeList();
            ArrangeSeeds();

            // The bar is one row of two buttons with the gap between them. A button that is not showing leaves
            // an empty cell rather than moving the other one, so Verify stays on the right whatever Details is
            // doing.
            barGrid.SetLayout(UiGridLayout.Build()
                .Columns(
                    GridTrack.Fixed(style.DetailsSize.x),
                    GridTrack.Flexible(),
                    GridTrack.Fixed(style.VerifySize.x))
                .Rows(GridTrack.Fixed(Mathf.Max(style.DetailsSize.y, style.VerifySize.y)))
                .Row(
                    detailsOn ? DetailsArea : UiGridLayout.Empty,
                    UiGridLayout.Empty,
                    verifyOn ? VerifyArea : UiGridLayout.Empty)
                .Done());

            // The header, the list, the totals, the seeds and the bar down the content area, each as tall as it
            // needs to be. The list is the exception: it is given a height rather than measured, because being
            // the thing that scrolls is what keeps the totals and the buttons on screen under it.
            contentRows.Clear();
            var content = UiGridLayout.Build().Columns(GridTrack.Flexible());

            if (headerOn)
            {
                content.Row(HeaderArea);
                contentRows.Add(style.OutcomeHeight > 0f && outcomeOn
                    ? GridTrack.Fixed(Mathf.Max(style.OutcomeHeight, style.ToggleSize.y))
                    : GridTrack.Auto());
            }

            if (listOn)
            {
                content.Row(ListArea);
                contentRows.Add(GridTrack.Fixed(ListHeight()));
            }
            else if (!hasData)
            {
                content.Row(LoaderArea);
                contentRows.Add(GridTrack.Fixed(style.LoaderHeight));
            }

            if (totalsOn)
            {
                content.Row(StatsArea);
                contentRows.Add(GridTrack.Auto());
            }

            if (seedsOn)
            {
                content.Row(SeedsArea);
                contentRows.Add(GridTrack.Auto());
            }

            if (detailsOn || verifyOn)
            {
                content.Row(BarArea);
                contentRows.Add(GridTrack.Auto());
            }

            GridOn(Window.Content).SetLayout(content.Rows(contentRows.ToArray()).Done());

            // The header's own row, which only has to say whether the toggle is in it.
            header.SetLayout(UiGridLayout.Build()
                .Columns(
                    GridTrack.Fixed(style.ToggleSize.x),
                    GridTrack.Flexible(),
                    GridTrack.Fixed(style.ToggleSize.x))
                .Rows(GridTrack.Auto())
                .Row(
                    UiGridLayout.Empty,
                    outcomeOn ? OutcomeArea : UiGridLayout.Empty,
                    toggleOn ? ToggleArea : UiGridLayout.Empty)
                .Done());
        }

        // The rows the round ended up with, as a layout of one column. The content rect is sized by hand rather
        // than by a fitter: a ScrollRect measures its travel against the rect it is given, and a fitter racing
        // the grid over the same rect is how a list ends up unable to reach its last row.
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

            // Nothing to clip and nothing to catch while every row fits. A mask that is off costs nothing, and
            // an invisible graphic that is off is a drag that reaches the window behind it.
            bool scrolling = wanted > ListHeight() + 0.5f;
            scroller.enabled = scrolling;
            clip.enabled = scrolling;
            grab.enabled = scrolling;

            if (!scrolling)
                rows.anchoredPosition = Vector2.zero;
        }

        private void ArrangeSeeds()
        {
            seedRows.Clear();
            var layout = UiGridLayout.Build().Columns(GridTrack.Flexible());

            for (int i = 0; i < seeds.Count; i++)
            {
                if (!seedOn[i])
                    continue;

                layout.Row(SeedArea + i.ToString(CultureInfo.InvariantCulture));
                seedRows.Add(GridTrack.Auto());
            }

            // Held open at one row: a layout of none would leave the grid on its own list, and its own list is
            // not what says what is showing here.
            seedsGrid.SetLayout(layout.Rows(seedRows.ToArray()).Size(1, Mathf.Max(1, seedRows.Count)).Done());
        }

        // How tall the rows come to, all told.
        private float Content()
        {
            int count = Mathf.Max(1, live.Count);
            return count * style.RowHeight + (count - 1) * style.RowGap;
        }

        // How tall the list is allowed to be: what it needs, held between the two ends of the style. The floor
        // is there so a round with one bet on it does not open a window a third of the height of the last one.
        private float ListHeight()
        {
            float wanted = Content();
            float most = style.ListMaxHeight > 0f ? style.ListMaxHeight : wanted;
            return Mathf.Max(style.ListMinHeight, Mathf.Min(wanted, most));
        }

        private static bool Shown(bool[] flags)
        {
            foreach (var flag in flags)
            {
                if (flag)
                    return true;
            }

            return false;
        }

        // ------------------------------------------------------------------ the data

        private void Write(GameHistoryByIdDto data)
        {
            toggleText.text = Translator.Label(usdView ? usdLabel : coinLabel);

            WriteTotals(data);
            WriteSeeds(data);
            WriteRows(data);
        }

        // The three figures under the list, all of them in dollars: a shared round is played in as many
        // currencies as there are players, and the only thing they can all be added up in is the one the server
        // converted them to.
        //
        // Taken from the round where it filled them in and from the strip's own summary where it did not - the
        // web front reads them off the history line for exactly that reason.
        private void WriteTotals(GameHistoryByIdDto data)
        {
            var line = Totals(data);
            int decimals = UsdDecimals();

            totals[0].Caption.text = Translator.Label(totalBetCountLabel);
            totals[0].Value.text = line.Count.ToString(CultureInfo.InvariantCulture);

            totals[1].Caption.text = Translator.Label(totalBetAmountLabel);
            totals[1].Value.text = Money(line.Bet, decimals) + Small(" $");

            totals[2].Caption.text = Translator.Label(totalProfitLabel);
            totals[2].Value.text = Money(line.Win, decimals) + Small(" $");
        }

        private Line Totals(GameHistoryByIdDto data)
        {
            var result = new Line();

            if (data != null && (data.TotalBetCount > 0 || Number(data.TotalBetAmountUsd) != 0m))
            {
                result.Count = data.TotalBetCount;
                result.Bet = Number(data.TotalBetAmountUsd);
                result.Win = Number(data.TotalWinAmountUsd);
                return result;
            }

            if (summary != null)
            {
                result.Count = summary.TotalBetCount;
                result.Bet = Number(summary.TotalBetAmountUsd);
                result.Win = Number(summary.TotalWinAmountUsd);
            }

            return result;
        }

        private struct Line
        {
            public int Count;
            public decimal Bet;
            public decimal Win;
        }

        // The seed the round was rolled from, the seed the server kept, and the hash it published before the
        // roll. Between them they are what makes a shared round checkable after the fact, which is the only
        // reason the dialog has a Details half at all.
        private void WriteSeeds(GameHistoryByIdDto data)
        {
            string salt = data != null ? data.ClientSalt : null;
            string server = data != null ? data.ServerSeed : null;
            string sha = data != null ? data.ServerSeedSha512 : null;

            WriteSeed(0, clientSeedLabel, salt ?? string.Empty, data != null);
            WriteSeed(1, serverSeedLabel, string.IsNullOrEmpty(server) ? Translator.Label(hiddenLabel) : server, data != null);
            WriteSeed(2, serverShaLabel, sha ?? string.Empty, data != null);
        }

        private void WriteSeed(int index, string caption, string value, bool shown)
        {
            if (index >= seeds.Count || index >= seedOn.Length)
                return;

            var field = seeds[index];
            seedOn[index] = shown;
            field.Caption.text = Translator.Label(caption);
            field.Value.text = value;
        }

        // Every transaction on the round, in the order the design puts them: the player's own first, then the
        // rest by what they came away with. A row is kept for as long as it is needed and handed back when it
        // is not - a round of forty bets and one of two are the same list with a different layout on it.
        private void WriteRows(GameHistoryByIdDto data)
        {
            entries.Clear();

            if (data != null && data.Transactions != null)
            {
                string me = Me();

                foreach (var pair in data.Transactions)
                {
                    var bet = pair.Value;
                    if (bet == null)
                        continue;

                    var entry = new Entry
                    {
                        Id = pair.Key,
                        Bet = bet,
                        Mine = !string.IsNullOrEmpty(me) && bet.PlayerId == me,
                    };

                    Sum(bet, out entry.Wagered, out entry.Won);
                    entry.NetUsd = (entry.Won - entry.Wagered) * Number(bet.RateUsd);
                    entries.Add(entry);
                }
            }

            entries.Sort(Compare);

            // Every row that is no longer needed goes back to the pool before any is taken out of it, or a
            // round of two bets after one of forty would make two more rows rather than reusing two of the
            // forty that are already there.
            for (int i = entries.Count; i < live.Count; i++)
                Retire(live[i]);

            if (live.Count > entries.Count)
                live.RemoveRange(entries.Count, live.Count - entries.Count);

            for (int i = 0; i < entries.Count; i++)
            {
                if (i >= live.Count)
                    live.Add(Rent(i));

                WriteRow(live[i], entries[i], i);
            }
        }

        private void WriteRow(Row row, Entry entry, int index)
        {
            var bet = entry.Bet;

            row.Id = entry.Id;
            row.Plate.name = "Row " + index.ToString(CultureInfo.InvariantCulture);

            PaintRow(row);
            Named(row.Plate.rectTransform, RowArea + index.ToString(CultureInfo.InvariantCulture));

            // Green from a payout of one: the bet came back whole or better. Below it the player is down on the
            // round, whatever the win amount says on its own.
            bool won = entry.Wagered > 0m ? entry.Won >= entry.Wagered : entry.Won > 0m;
            Paint(row.Plate, won ? style.RowWinFill : style.RowLoseFill, style.RowCornerRadius);
            row.Plate.raycastTarget = true;

            // The player's own row, outlined. Everything else about the two is the same, which is the point:
            // one row in forty has to be findable without being a different kind of row.
            row.Plate.SetBorderSize(entry.Mine ? style.RowMineBorderSize : 0f);
            row.Plate.SetBorderColor(style.RowMineBorder);

            string name = string.IsNullOrEmpty(bet.PlayerName) ? bet.PlayerId : bet.PlayerName;
            row.Name.text = name;

            int decimals = Decimals(bet);
            decimal rate = usdView ? Number(bet.RateUsd) : 1m;

            // The figure alone: the coin beside it is what says which currency it is in, and printing the code
            // as well would say it twice.
            row.BetValue.text = Money(entry.Wagered * rate, decimals);
            row.WinValue.text = Money(entry.Won * rate, decimals);

            string coin = usdView ? "$" : bet.Currency;

            Letter(row.Avatar, name);
            Letter(row.BetCoin, coin);
            Letter(row.WinCoin, coin);

            // No picture for a dollar figure: the coin the server sent is the currency the bet was made in, and
            // drawing it beside a converted amount would say the round was played in it.
            string image = usdView ? null : bet.CurrencyImage;
            Fetch(row.Avatar, bet.PlayerImage);
            Fetch(row.BetCoin, image);
            Fetch(row.WinCoin, image);

            // Rebound every time rather than only when the row is made: a row is handed from one player to the
            // next as the list is reused, and a listener left over from the last one would open that bet.
            row.Button.onClick.RemoveAllListeners();

            string picked = entry.Id;
            row.Button.onClick.AddListener(() => Pick(picked));
        }

        private void Retire(Row row)
        {
            if (row == null || row.Plate == null)
                return;

            row.Id = null;
            row.Button.onClick.RemoveAllListeners();
            UiWindowParts.Item(row.Plate.rectTransform).Area = SpareArea;
            spare.Add(row);
        }

        // What one player staked on the round, and what came back. A transaction is a list of chunks - a bet on
        // red, a bet on the number, an increase on either - and the row shows the two totals rather than the
        // parts, which is what the bet info dialog is for.
        private static void Sum(BetInfoDto bet, out decimal wagered, out decimal won)
        {
            wagered = 0m;
            won = 0m;

            if (bet.Bets == null)
                return;

            foreach (var chunk in bet.Bets)
            {
                if (chunk == null)
                    continue;

                var amount = Number(chunk.Amount);
                wagered += amount;
                won += amount * Number(chunk.Payout);
            }
        }

        private struct Entry
        {
            public string Id;
            public BetInfoDto Bet;
            public decimal Wagered;
            public decimal Won;
            public decimal NetUsd;
            public bool Mine;
        }

        private int Compare(Entry left, Entry right)
        {
            if (mineFirst && left.Mine != right.Mine)
                return left.Mine ? -1 : 1;

            return right.NetUsd.CompareTo(left.NetUsd);
        }

        private GameHistoryByIdDto Known()
        {
            var manager = StateManager.Inst;
            if (manager == null || manager.MainState == null)
                return null;

            return manager.MainState.GameHistoryById;
        }

        private string Me()
        {
            var manager = StateManager.Inst;
            var system = manager != null && manager.MainState != null ? manager.MainState.SystemState : null;
            return system != null ? system.Me : null;
        }

        private void Apply(GameHistoryByIdDto value)
        {
            round = value;
            stamp++;

            EnsureBuilt();
            Fill();

            if (value != null)
                OnRound.Invoke(value);
        }

        // Refresh, with whatever it throws kept out of the way of the window opening. A row that could not be
        // written - a payload shaped in a way this did not expect, a game's own listener throwing - is a dialog
        // with a gap in it, and a dialog with a gap in it beats a press that did nothing. The exception still
        // goes to the console, where it belongs.
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

        private void Listen(bool on)
        {
            var manager = StateManager.Inst;
            if (manager == null || manager.Events == null || !followState)
                return;

            if (on && !listening)
            {
                manager.Events.OnGameHistoryById.AddListener(HandleRound);
                listening = true;
            }
            else if (!on && listening)
            {
                manager.Events.OnGameHistoryById.RemoveListener(HandleRound);
                listening = false;
            }
        }

        // Every window listening hears every answer, so one opened on a particular round has to let the others
        // past rather than showing whichever came back last.
        private void HandleRound(GameHistoryByIdDto value)
        {
            if (value == null)
                return;

            if (matchRequestedId && !string.IsNullOrEmpty(requestedId) && value.Id != requestedId)
                return;

            Apply(value);
        }

        // ------------------------------------------------------------------ formatting

        private int Decimals(BetInfoDto data)
        {
            // A converted figure is a figure in dollars whatever currency it started in, so it is printed to the
            // style's own count rather than to the currency's - two places, the way the web front shows them.
            if (usdView)
                return Mathf.Clamp(style.UsdDecimals, 0, 18);

            if (style.Decimals >= 0)
                return Mathf.Clamp(style.Decimals, 0, 18);

            // Zero is what the field says when the server has not filled it in, and printing money to no
            // decimals at all would turn every satoshi into 0. Eight is what the web front falls back to.
            return data.DecimalPoints > 0 ? Mathf.Min(data.DecimalPoints, 18) : 8;
        }

        // What the totals are printed to: whatever the system says a figure takes, since that is what the web
        // front reads them off. The style's own number stands in where there is no socket running.
        private int UsdDecimals()
        {
            var manager = StateManager.Inst;
            var system = manager != null && manager.MainState != null ? manager.MainState.SystemState : null;
            int points = system != null && system.DecimalPoints.HasValue ? system.DecimalPoints.Value : style.UsdDecimals;
            return Mathf.Clamp(points, 0, 18);
        }

        // Money is truncated rather than rounded, and only then trimmed - the same order the web front does it
        // in, so the two show the same number for the same bet.
        private string Money(decimal value, int decimals)
        {
            decimal scale = 1m;
            for (int i = 0; i < decimals; i++)
                scale *= 10m;

            decimal cut = decimal.Truncate(value * scale) / scale;
            string text = cut.ToString("F" + decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

            if (!style.TrimZeros || text.IndexOf('.') < 0)
                return text;

            text = text.TrimEnd('0');
            return text.EndsWith(".", StringComparison.Ordinal) ? text + "0" : text;
        }

        private static decimal Number(string raw) =>
            decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : 0m;

        /// <summary>The currency code beside an amount: the same text, smaller.</summary>
        private string Small(string text) =>
            "<size=" + Mathf.RoundToInt(style.SmallTextScale * 100f).ToString(CultureInfo.InvariantCulture) + "%>" + text + "</size>";

        // The query the verifier expects: both seeds in base64 and the house edge the round was rolled under.
        // No nonce, and that is the difference from a single-player bet - a shared round is one roll for
        // everybody, so there is nothing to count.
        private string VerifyUrl(GameHistoryByIdDto data)
        {
            return data.VerifyUrl
                + "?cs=" + Base64(data.ClientSalt)
                + "&ss=" + Base64(data.ServerSeed)
                + "&he=" + data.HouseEdge;
        }

        private static string Base64(string text) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(text ?? string.Empty));

        // ------------------------------------------------------------------ pictures and glyphs

        private void Coin(Badge badge, float size) =>
            PaintBadge(badge, style.CoinFill, 100000f, style.CoinLetterColor, size * 0.55f);

        private void Avatar(Badge badge, float size) =>
            PaintBadge(badge, style.AvatarFill, 100000f, style.AvatarLetterColor, size * 0.5f);

        private void PaintBadge(Badge badge, Color fill, float radius, Color letterColor, float letterSize)
        {
            if (badge == null)
                return;

            UiWindowParts.Stretch(badge.Plate.rectTransform, 0f, 0f, 0f, 0f);
            Paint(badge.Plate, fill, radius);

            UiWindowParts.Stretch(badge.Letter.rectTransform, 0f, 0f, 0f, 0f);
            Label(badge.Letter, style.CaptionFont, Mathf.Max(1f, letterSize), letterColor, FontStyles.Bold);

            UiWindowParts.Stretch(badge.Picture.rectTransform, 0f, 0f, 0f, 0f);
            badge.Picture.preserveAspect = true;
            badge.Picture.color = Color.white;
            badge.Picture.raycastTarget = false;
            badge.Picture.gameObject.SetActive(badge.Picture.sprite != null);
            badge.Letter.gameObject.SetActive(badge.Picture.sprite == null);
        }

        private void Letter(Badge badge, string from)
        {
            if (badge == null)
                return;

            badge.Letter.text = string.IsNullOrEmpty(from)
                ? string.Empty
                : from.Substring(0, 1).ToUpperInvariant();
        }

        // The plate and its letter stay behind the picture rather than being replaced by it, so a url that
        // never answers leaves something readable instead of a hole.
        private void Fetch(Badge badge, string url)
        {
            if (badge == null)
                return;

            if (!loadImages || string.IsNullOrEmpty(url))
            {
                badge.Picture.sprite = null;
                badge.Picture.gameObject.SetActive(false);
                badge.Letter.gameObject.SetActive(true);
                return;
            }

            int asked = stamp;
            UiRemoteImage.Load(url, sprite =>
            {
                // Two ways this can arrive too late: the window has been destroyed, or it has been pointed at
                // another round since, and this picture belongs to a row that is showing somebody else.
                if (this == null || badge.Picture == null || asked != stamp)
                    return;

                badge.Picture.sprite = sprite;
                badge.Picture.gameObject.SetActive(sprite != null);
                badge.Letter.gameObject.SetActive(sprite == null);
            });
        }

        // A stack of bars in a rounded plate - the mark that says a row has a bet behind it worth opening.
        private void Drum(Mark mark, float span)
        {
            if (mark == null)
                return;

            bool sprite = UseSprite(mark, style.MarkIcon);

            mark.Plate.gameObject.SetActive(!sprite && span > 0f);
            mark.BarA.gameObject.SetActive(!sprite && span > 0f);
            mark.BarB.gameObject.SetActive(!sprite && span > 0f);

            if (sprite || span <= 0f)
                return;

            UiWindowParts.Stretch(mark.Plate.rectTransform, 0f, 0f, 0f, 0f);
            Paint(mark.Plate, style.MarkFill, style.MarkCornerRadius);

            // Two lines across the middle third of the plate, measured as fractions of it so the mark keeps its
            // shape at any size.
            float half = span * 0.22f;
            Stroke(mark.BarA, new Vector2(-half, span * 0.1f), new Vector2(half, span * 0.1f), style.MarkThickness, style.MarkColor);
            Stroke(mark.BarB, new Vector2(-half, -span * 0.12f), new Vector2(half, -span * 0.12f), style.MarkThickness, style.MarkColor);
        }

        private bool UseSprite(Mark mark, Sprite icon)
        {
            bool on = icon != null;

            mark.Picture.gameObject.SetActive(on);
            mark.Picture.sprite = icon;
            mark.Picture.preserveAspect = true;
            mark.Picture.raycastTarget = false;
            UiWindowParts.Stretch(mark.Picture.rectTransform, 0f, 0f, 0f, 0f);
            return on;
        }

        // One bar from one point to another: the length, the angle and the middle worked out from the two ends,
        // so a glyph is written as the shape it is rather than as three numbers a bar.
        private void Stroke(RoundedBox bar, Vector2 from, Vector2 to, float thickness, Color color)
        {
            var span = to - from;
            float length = Mathf.Max(span.magnitude, thickness);

            UiWindowParts.Pin(
                bar.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(length, thickness),
                (from + to) * 0.5f);

            bar.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(span.y, span.x) * Mathf.Rad2Deg);
            Paint(bar, color, 100000f);
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
                // is compiled into the project's own assembly and cannot be reached from a package. Unscaled,
                // like every other tween in this folder - a dialog that opens over a paused game should not be
                // waiting on time that is not running.
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

        // The window is sized from its contents out rather than the other way round: how tall a game history
        // dialog has to be depends on how many bets were on the round and on what the game put in Outcome.
        //
        // Twice, and that is not belt and braces. A label reports the height it needs at the width it has, and
        // its width is what the first pass settles - so on the way into a window that has just been activated a
        // wrapped seed measures as one line, and the buttons under it are left hanging below the panel.
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

        /// <summary>Puts something of its own size in the middle of a cell. For anything square - a coin, an
        /// avatar - which a stretch would pull out of shape.</summary>
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

        private static void Named(RectTransform rect, string area, Vector2 size) => UiWindowParts.Name(rect, area, size);

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

        // A round that reads the way the design was drawn, for a scene with no template running in it: a dialog
        // built from a menu has to look like the dialog rather than like a row of dots that never stops. A real
        // game never sees this - the moment there is a StateManager, only the server's rounds are shown.
        private static GameHistoryByIdDto Sample()
        {
            var made = new GameHistoryByIdDto
            {
                Id = "019f150cc55377c8b0894c99b556a7df",
                TotalBetCount = 3,
                TotalBetAmountUsd = "12.4",
                TotalWinAmountUsd = "18.6",
                ClientSalt = "0000000000000000000253d2b4b2f6d61a2b6f1a5f0d5b0e7f6c9d3a1b8e4c72",
                ServerSeedSha512 = "9f2c4e6a8b0d1f3a5c7e9b2d4f6a8c0e1b3d5f7a9c1e3b5d7f9a1c3e5b7d9f1a",
                HouseEdge = "1",
            };

            made.Transactions["019f150cc55377c8b0894c99b556a7df"] = Bet("dev", "DEV-TEAM", "usdt", 2, "10", "2.4", "1");
            made.Transactions["019f150cc55377c8b0894c99b556a7e0"] = Bet("p2", "Player Two", "btc", 8, "0.0001", "0", "62000");
            made.Transactions["019f150cc55377c8b0894c99b556a7e1"] = Bet("p3", "Player Three", "eth", 6, "0.05", "3.5", "3400");

            return made;
        }

        private static BetInfoDto Bet(string id, string name, string currency, int decimals, string amount, string payout, string rate) =>
            new BetInfoDto
            {
                PlayerId = id,
                PlayerName = name,
                Currency = currency,
                DecimalPoints = decimals,
                RateUsd = rate,
                Bets = new[] { new BetChunkDto { Amount = amount, Payout = payout, Win = Number(payout) >= 1m } },
            };
    }
}
