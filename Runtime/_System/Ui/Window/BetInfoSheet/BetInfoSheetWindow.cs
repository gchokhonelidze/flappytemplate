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
    // The bet info sheet, built into a UiWindow: the other way of drawing one bet. Where BetInfoWindow next door
    // lays a bet out as a card of paired fields with its seeds behind a Details button, this lays it out as one
    // long sheet - the result across the top, the bet's id, then a heading and an outlined box per thing worth
    // knowing: what was staked and what it made, the three seeds, and the parts the bet was made of.
    //
    //     var sheet = BetInfoSheetWindow.Create(canvas);
    //     sheet.OnTransaction.AddListener(bet => sheet.SetResult("Result: " + bet.Outcome["result"]));
    //     sheet.Show(betId);
    //
    // It reads exactly what BetInfoWindow reads: Show emits BET_INFO through Emitter and opens on a row of
    // pulsing dots, the answer arrives as ON_BET_INFO_ID in MainState.BetInfoById and is announced as
    // OnBetInfoById, and the sheet fills itself in from that. Nothing is polled, and nothing is shown that the
    // server has not sent. The sheet is as tall as what is on it, and past the window's Max Height the window
    // scrolls it - nothing is a press away.
    //
    // What the bet *was* is the game's business - a roll, a multiplier, where the ball fell - so the top of the
    // sheet is left to it. SetResult writes a line of text into the pill the design draws there; anything richer
    // is parented into Outcome and laid out under it. The coin on the left swaps every amount on the sheet
    // between the currency the bet was made in and its value in dollars.
    [AddComponentMenu("UI/Bet Info Sheet Window")]
    [RequireComponent(typeof(UiWindow))]
    public class BetInfoSheetWindow : MonoBehaviour
    {
        // What each cell answers to in its grid's layout. One word each, and that is a requirement rather than
        // a habit: a layout is stored as text, so a name with a space in it comes back out as two cells.
        private const string HeaderArea = "header";
        private const string ToggleArea = "toggle";
        private const string MiddleArea = "middle";
        private const string ResultArea = "result";
        private const string OutcomeArea = "outcome";
        private const string IdArea = "id";
        private const string LoaderArea = "loader";
        private const string StatsArea = "stats";
        private const string ServerArea = "server";
        private const string ClientArea = "client";
        private const string HashArea = "hash";
        private const string BetsArea = "bets";
        private const string BarArea = "bar";
        private const string VerifyArea = "verify";
        private const string PlayerArea = "player";
        private const string HeadArea = "head";
        private const string PlacesArea = "places";
        private const string PlaceArea = "p";

        // What a place line answers to while it is spare. The layout never mentions it, which keeps it hidden.
        private const string SpareArea = "";

        // The most boxes any one drawn icon is made of - the list, three squares and three lines.
        private const int IconParts = 6;

        [SerializeField]
        private BetInfoSheetWindowStyle style = new BetInfoSheetWindowStyle();

        [Header("Labels")]
        // Serialized rather than translated, the same as the windows next door: every one of them goes through
        // Translator.Label, so a caption left as it came translates anyway and one typed over is left alone.
        [SerializeField]
        private string idLabel = "Round ID";

        [SerializeField]
        private string statisticsLabel = "Statistics";

        [SerializeField]
        private string totalBetCountLabel = "Total bet count";

        [SerializeField]
        private string totalBetAmountLabel = "Total bet amount";

        [SerializeField]
        private string totalProfitLabel = "Total profit";

        [SerializeField]
        private string serverSeedLabel = "Server seed";

        [SerializeField]
        private string clientSeedLabel = "Client seed";

        [Tooltip("Stands in for the client seed on a shared or multiplayer game, where the roll is seeded from a block hash rather than from anything one player sent.")]
        [SerializeField]
        private string blockHashLabel = "Bitcoin last block hash";

        [SerializeField]
        private string serverShaLabel = "Server seed's SHA512 hash";

        [SerializeField]
        private string betsLabel = "Round Bets";

        [Tooltip("What a row is called when the bet came as one piece rather than as a list of places.")]
        [SerializeField]
        private string betLabel = "Bet";

        [SerializeField]
        private string verifyLabel = "Verify";

        [Tooltip("Printed where the server seed would be while the bet's seed is still in play and cannot be given out.")]
        [SerializeField]
        private string hiddenLabel = "Hidden";

        [Tooltip("Printed in a box the server sent nothing for - a bet seeded without a client seed, say.")]
        [SerializeField]
        private string missingLabel = "N/A";

        [Tooltip("On the currency toggle while the amounts are in dollars. While they are in the bet's own currency it shows a coin.")]
        [SerializeField]
        private string usdLabel = "$";

        [Header("Blocks")]
        [Tooltip("The coin in the top left that swaps every amount between the bet's own currency and dollars. Shown only while there is a rate to convert at.")]
        [SerializeField]
        private bool showCurrencyToggle = true;

        [Tooltip("The pill SetResult writes into, and whatever the game parents into Outcome under it.")]
        [SerializeField]
        private bool showOutcome = true;

        [SerializeField]
        private bool showId = true;

        [Tooltip("The three totals: how many parts the bet was made of, what was staked, and what it made.")]
        [SerializeField]
        private bool showStatistics = true;

        [SerializeField]
        private bool showServerSeed = true;

        [SerializeField]
        private bool showClientSeed = true;

        [SerializeField]
        private bool showServerHash = true;

        [Tooltip("The parts the bet was made of - a row per place on a shared game, the bet itself on any other.")]
        [SerializeField]
        private bool showBets = true;

        [Tooltip("The Verify button under the sheet. It appears only on a transaction the server sent a verify url with.")]
        [SerializeField]
        private bool showVerify = true;

        [Tooltip("Start with every amount converted to dollars rather than shown in the currency it was bet in.")]
        [SerializeField]
        private bool usdView = false;

        [Header("Behaviour")]
        [Tooltip("Fill in when the server sends a transaction.")]
        [SerializeField]
        private bool followState = true;

        [Tooltip("Ask the server for the bet when Show is given an id. Off leaves the asking to the game.")]
        [SerializeField]
        private bool requestOnShow = true;

        [Tooltip("Ignore a transaction whose id is not the one that was asked for. Off takes whatever arrives, which is what a window opened without an id wants.")]
        [SerializeField]
        private bool matchRequestedId = true;

        [Tooltip("Fetch the currency picture the server sends as a url. Off leaves the drawn coins showing.")]
        [SerializeField]
        private bool loadImages = true;

        [Tooltip("Resize the window to the sheet it is showing. Past the window's Max Height the body scrolls instead, which is the window's own business.")]
        [SerializeField]
        private bool fitWindowHeight = true;

        [Header("Events")]
        [Tooltip("A transaction has arrived and the sheet is filled in. Where a game calls SetResult, or draws its own view into Outcome.")]
        public UnityEvent<TransactionPublic> OnTransaction = new UnityEvent<TransactionPublic>();

        [Tooltip("An id has been asked of the server, before the answer.")]
        public UnityEvent<string> OnRequested = new UnityEvent<string>();

        [Tooltip("The verify url, as the browser is being sent to it.")]
        public UnityEvent<string> OnVerify = new UnityEvent<string>();

        public UnityEvent<bool> OnCurrencyToggled = new UnityEvent<bool>();

        // Parts, and the flag saying they exist, are deliberately not serialized - the same choice the windows
        // next door make. Everything is found by name before it is made, so a rebuild after a script reload
        // finds the hierarchy that is already there rather than building a second one beside it.
        private UiWindow window;

        private UiGrid header;
        private RoundedBox toggleBox;
        private Button toggleButton;
        private RoundedBox toggleCoin;
        private TextMeshProUGUI toggleText;
        private UiGrid middle;
        private RoundedBox resultBox;
        private TextMeshProUGUI resultText;
        private UiGrid outcome;

        private TextMeshProUGUI idText;

        private RectTransform loader;
        private UiGrid loaderGrid;
        private readonly RoundedBox[] dots = new RoundedBox[3];
        private readonly List<Tween> pulses = new List<Tween>();

        private Section stats;
        private readonly TextMeshProUGUI[] totalCaptions = new TextMeshProUGUI[3];
        private readonly TextMeshProUGUI[] totalValues = new TextMeshProUGUI[3];
        private Section serverSeed;
        private Section clientSeed;
        private Section serverHash;
        private Section bets;

        private RectTransform bar;
        private UiGrid barGrid;
        private RoundedBox verifyBox;
        private Button verifyButton;
        private TextMeshProUGUI verifyText;

        private PlayerRow player;
        private readonly List<PlaceLine> lines = new List<PlaceLine>();
        private readonly List<PlaceLine> spareLines = new List<PlaceLine>();

        // Row lists are built per layout pass rather than fixed: a gap between two tracks is there whether or
        // not anything is in them, so a block that is switched off has to take its row away with it.
        private readonly List<GridTrack> contentRows = new List<GridTrack>();
        private readonly List<GridTrack> middleRows = new List<GridTrack>();
        private readonly List<GridTrack> listRows = new List<GridTrack>();
        private readonly List<GridTrack> placeRows = new List<GridTrack>();
        private readonly List<Part> parts = new List<Part>();

        // What is showing, decided in Refresh and said to the grids in Arrange.
        private bool hasData;
        private bool toggleOn;
        private bool resultOn;
        private bool outcomeOn;
        private bool headerOn;
        private bool idOn;
        private bool verifyOn;

        // Whether the server sent the places the bet was spread over, and whether they are showing under it.
        private bool hasPlaces;
        private bool placesOpen;

        // Dollars per unit of the bet's currency, or zero when nothing has said. Worked out once per refresh.
        private decimal rate;

        private TransactionPublic transaction;
        private TransactionPublic preview;
        private string requestedId = string.Empty;
        private string result = string.Empty;
        private bool built;
        private bool listening;

        // Bumped whenever the window is pointed at another bet, so a picture that arrives after the player has
        // moved on lands nowhere rather than on the wrong coin.
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

        /// <summary>Colours, sizes and fonts of the sheet. Edit and call <see cref="Rebuild"/>, or assign a
        /// whole new one.</summary>
        public BetInfoSheetWindowStyle Style
        {
            get => style;
            set
            {
                style = value ?? new BetInfoSheetWindowStyle();
                Rebuild();
            }
        }

        /// <summary>What is being shown: the server's transaction, or a sample in a scene with no template
        /// running at all. Null while the answer is still on its way.</summary>
        public TransactionPublic Transaction
        {
            get
            {
                if (transaction != null)
                    return transaction;

                // The answer can have arrived while the window was closed, and a closed window is not
                // listening - so the state is asked again rather than opening on a loader for a bet the
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

        /// <summary>The id <see cref="Show(string)"/> was last given, or empty.</summary>
        public string RequestedId => requestedId;

        /// <summary>The text in the pill across the top. Empty hides the pill. Set it from
        /// <see cref="OnTransaction"/> - it is cleared whenever the window is pointed at another bet.</summary>
        public string Result
        {
            get => result;
            set => SetResult(value);
        }

        /// <summary>A strip under the result for the game's own view of the bet - what the dice rolled, where
        /// the ball fell. Parent anything into it and the sheet grows around it.</summary>
        // A container rather than a shape, for the reason the windows next door give theirs: a grid of one
        // column, so several things stacked into it come out in order, and an auto row each so each is as tall
        // as it says it needs to be. And deliberately the only way in - the content is arranged by a layout that
        // names its cells, and a panel parented straight into it would be hidden the next time that was set.
        public RectTransform Outcome
        {
            get
            {
                EnsureBuilt();
                return Rect(outcome);
            }
        }

        /// <summary>The coin in the top left, for a game that would rather drive it from its own control.</summary>
        public Button CurrencyButton
        {
            get
            {
                EnsureBuilt();
                return toggleButton;
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

        /// <summary>The pill <see cref="SetResult"/> writes into, for anything the style does not reach.</summary>
        public RoundedBox ResultBox
        {
            get
            {
                EnsureBuilt();
                return resultBox;
            }
        }

        /// <summary>Whether every amount is converted to dollars. Off shows them in the currency the bet was
        /// made in. Has no effect on a bet nothing has given a rate for - there is nothing to convert at.</summary>
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

        /// <summary>Whether the places the bet was spread over are showing under the player's row. Only a bet
        /// the server sent places for can be opened; on any other this stays false.</summary>
        public bool PlacesOpen
        {
            get => placesOpen;
            set
            {
                value = value && hasPlaces;
                if (placesOpen == value)
                    return;

                placesOpen = value;
                Refresh();
            }
        }

        /// <summary>What a press on the player's row does: opens the places under it, or closes them.</summary>
        public void TogglePlaces() => PlacesOpen = !placesOpen;

        /// <summary>Builds the whole thing - window, sheet and all - under a parent.</summary>
        // Added before the object wakes, for the reason UiWindowBuilder.Add exists: a component on an active
        // object runs its Awake there and then, and the sheet would go into a window that had already put
        // itself away.
        //
        // The panel is painted here, once, as the window is made: the design is a charcoal sheet with no
        // caption, which is a look rather than a layout, so it belongs to the panel's own RoundedBox and stays
        // there for anybody to change - nothing in this component paints over it afterwards.
        public static BetInfoSheetWindow Create(Transform parent, string name = "Bet Info Sheet", string title = "Bet info")
        {
            UiWindowBuilder.Create(parent, name)
                .Size(760f, 720f)
                .Title(title)
                .NoCaption()
                .Padding(22f, 18f, 22f, 22f)
                .Panel(Charcoal)
                .Add(out BetInfoSheetWindow sheet)
                .Done();

            if (sheet == null)
                return null;

            // Held at the height the design is drawn at, so a bet made of many parts scrolls inside a dialog of a
            // sensible size rather than growing to the edge of the screen first.
            sheet.Window.MaxHeight = 720f;

            // Awake has done this already in play mode. In the editor nothing else ever will, and a window
            // built from a context menu that came out empty would be a poor way to find that out.
            sheet.EnsureBuilt();
            return sheet;
        }

        /// <summary>The flat dark panel the design is drawn on. Public so a window made some other way - the
        /// menu, a prefab - can be given the same look in one line.</summary>
        public static void Charcoal(RoundedBox panel)
        {
            if (panel == null)
                return;

            panel.FillGradientMode = EFillGradient.None;
            panel.FillColor = new Color(0.106f, 0.106f, 0.106f);
            panel.SetCornerRadius(18f);
            panel.SetBorderSize(1f);
            panel.SetBorderColor(new Color(1f, 1f, 1f, 0.06f));
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

        /// <summary>Builds the header, the sections and the list from scratch, then redraws. Call after
        /// changing the style from code.</summary>
        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            if (style == null)
                style = new BetInfoSheetWindowStyle();

            var host = Window;
            if (host == null)
                return;

            host.EnsureBuilt();
            host.ApplyLayout();

            BuildParts(host.Content);

            built = true;
            Refresh();
        }

        /// <summary>Asks the server for a bet and opens the window on it.</summary>
        // The request goes out and the window opens on the loader rather than waiting for the answer: a dialog
        // that appears half a second after the press reads as a dropped press. Where the state already holds
        // this bet it is used at once and nothing is asked.
        public void Show(string id)
        {
            requestedId = id ?? string.Empty;
            transaction = null;
            result = string.Empty;
            placesOpen = false;
            stamp++;

            var known = Known();
            if (known != null && (!matchRequestedId || known.Id == requestedId))
            {
                // Through Apply rather than straight into the field: the result was cleared above, and
                // OnTransaction is where a game writes it back - a bet the state already had still needs its
                // result drawn.
                Apply(known);
                Window.Open();
                return;
            }

            if (requestOnShow && !string.IsNullOrEmpty(requestedId) && Emitter.Inst != null)
            {
                Emitter.Inst.OnBetInfo(requestedId);
                OnRequested.Invoke(requestedId);
            }

            EnsureBuilt();
            Fill();
            Window.Open();
        }

        /// <summary>Opens the window on a transaction the game already has, without asking the server.</summary>
        public void Show(TransactionPublic value)
        {
            result = string.Empty;
            placesOpen = false;
            Apply(value);
            Window.Open();
        }

        /// <summary>Opens on whatever the state last received - for a window driven by the game's own request
        /// rather than by this one's.</summary>
        public void Show()
        {
            requestedId = string.Empty;
            result = string.Empty;
            placesOpen = false;
            Apply(Known());
            Window.Open();
        }

        /// <summary>Asks the server for a bet without opening anything.</summary>
        public void Request(string id)
        {
            if (string.IsNullOrEmpty(id) || Emitter.Inst == null)
                return;

            requestedId = id;
            Emitter.Inst.OnBetInfo(id);
            OnRequested.Invoke(id);
        }

        /// <summary>Back to the loader, for a window about to be pointed at another bet.</summary>
        public void Clear()
        {
            transaction = null;
            requestedId = string.Empty;
            result = string.Empty;
            placesOpen = false;
            stamp++;
            Refresh();
        }

        /// <summary>Writes a line into the pill across the top - "Result: 2", "x3.45", "Red". Empty hides it.
        /// Rich text works, so a game can colour part of it.</summary>
        public void SetResult(string text)
        {
            result = text ?? string.Empty;
            Refresh();
        }

        public void ToggleCurrency() => UsdView = !usdView;

        /// <summary>Sends the browser to the server's verifier with this bet's seeds in the query, which is what
        /// makes a roll checkable from outside the game.</summary>
        public void Verify()
        {
            var data = Transaction;
            if (data == null || string.IsNullOrEmpty(data.VerifyUrl))
                return;

            var url = VerifyUrl(data);
            OnVerify.Invoke(url);
            Application.OpenURL(url);
        }

        /// <summary>Fills the sheet in from the transaction and lays the window out again.</summary>
        public void Refresh()
        {
            if (!built)
                return;

            var data = Transaction;

            // Which blocks are showing is worked out here and said to the grids in Arrange, as a layout.
            // Deliberately not SetActive: a UiGrid shows what its layout names and hides what it does not, and
            // it re-asserts that every time it is enabled - so a cell switched off behind the grid's back comes
            // back on the next time the window opens.
            hasData = data != null;
            rate = hasData ? Rate(data) : 0m;
            toggleOn = hasData && showCurrencyToggle && rate > 0m;
            resultOn = showOutcome && !string.IsNullOrEmpty(ResultText(data));
            outcomeOn = showOutcome && Rect(outcome).childCount > 0;
            headerOn = toggleOn || resultOn || outcomeOn;
            idOn = showId && !string.IsNullOrEmpty(hasData ? data.Id : requestedId);
            verifyOn = hasData && showVerify && !string.IsNullOrEmpty(data.VerifyUrl);

            Write(data);

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
            PaintSections();
            PaintBar();
            Arrange();
            FitWindow();
        }

        // Converted only when there is something to convert at. A toggle left on dollars and then pointed at a
        // bet with no rate shows that bet in its own currency rather than as a row of zeroes.
        private bool InUsd => usdView && rate > 0m;

        // ------------------------------------------------------------------ building

        private void BuildParts(RectTransform content)
        {
            GridOn(content);

            header = Grid(content, "Header");

            toggleBox = UiWindowParts.Box(Rect(header), "Currency");
            toggleButton = Hook(toggleBox, ToggleCurrency);
            toggleCoin = UiWindowParts.Box(toggleBox.transform, "Coin");
            toggleText = UiWindowParts.Label(toggleBox.transform, "Label");

            middle = Grid(Rect(header), "Middle");
            resultBox = UiWindowParts.Box(Rect(middle), "Result");
            resultText = UiWindowParts.Label(resultBox.transform, "Label");
            outcome = Grid(Rect(middle), "Outcome");

            idText = UiWindowParts.Label(content, "Id");

            loader = Rect(Grid(content, "Loader"));
            loaderGrid = loader.GetComponent<UiGrid>();
            for (int i = 0; i < dots.Length; i++)
                dots[i] = UiWindowParts.Box(loader, "Dot " + i);

            stats = BuildSection(content, "Statistics");
            // "Total i" is the caption rather than a new name, so a sheet built before the figures moved under
            // their captions picks its old labels up as the captions instead of leaving them loose in the grid.
            for (int i = 0; i < totalCaptions.Length; i++)
            {
                totalCaptions[i] = UiWindowParts.Label(Rect(stats.Inner), "Total " + i);
                totalValues[i] = UiWindowParts.Label(Rect(stats.Inner), "Total " + i + " Value");
            }

            serverSeed = BuildSection(content, "Server Seed");
            serverSeed.Value = UiWindowParts.Label(Rect(serverSeed.Inner), "Value");

            clientSeed = BuildSection(content, "Client Seed");
            clientSeed.Value = UiWindowParts.Label(Rect(clientSeed.Inner), "Value");

            serverHash = BuildSection(content, "Server Hash");
            serverHash.Value = UiWindowParts.Label(Rect(serverHash.Inner), "Value");

            bets = BuildSection(content, "Bets");

            bar = Rect(Grid(content, "Bar"));
            barGrid = bar.GetComponent<UiGrid>();

            verifyBox = UiWindowParts.Box(bar, "Verify");
            verifyButton = Hook(verifyBox, Verify);
            verifyText = UiWindowParts.Label(verifyBox.transform, "Label");

            // Found by name before it is made, like everything else here, which is what makes Rebuild safe to
            // call twice - on a window saved as a prefab, or one rebuilt after a script reload.
            BuildPlayer();

            // What every cell answers to in its grid's layout. Set once, here, where the parts are: a name is a
            // property of the panel, and Arrange only draws the picture that uses them.
            Named(Rect(header), HeaderArea);
            Named(Rect(middle), MiddleArea);
            Named(Rect(outcome), OutcomeArea);
            Named(idText.rectTransform, IdArea);
            Named(loader, LoaderArea);
            Named(Rect(stats.Root), StatsArea);
            Named(Rect(serverSeed.Root), ServerArea);
            Named(Rect(clientSeed.Root), ClientArea);
            Named(Rect(serverHash.Root), HashArea);
            Named(Rect(bets.Root), BetsArea);
            Named(bar, BarArea);
        }

        // One heading and the outlined box under it - the shape every block of the sheet past the id has. What
        // goes in the box is the caller's: three totals, one long string, or the list.
        private class Section
        {
            public UiGrid Root;
            public UiGrid Heading;
            public Icon Icon;
            public TextMeshProUGUI Caption;
            public RoundedBox Box;
            public UiGrid Inner;
            public TextMeshProUGUI Value;
        }

        // The small mark in front of a heading, drawn from a handful of boxes rather than fetched from an atlas -
        // the same choice the close cross and the bet info tick make, and for the same reason: it costs no atlas
        // entry and stays sharp at any size. A sprite in the style takes its place.
        private class Icon
        {
            public RectTransform Root;
            public readonly RoundedBox[] Parts = new RoundedBox[IconParts];
            public Image Picture;
        }

        // The player's line in Round Bets, as the web front draws it: the avatar and the name, what was staked,
        // what came back, and the chevron that opens the places the bet was spread over underneath. The whole
        // plate is the button - a strip, not a target the size of the chevron.
        private class PlayerRow
        {
            public RoundedBox Plate;
            public UiGrid Grid;
            public Button Button;
            public UiGrid Head;
            public UiGrid Who;
            public Badge Avatar;
            public TextMeshProUGUI Name;
            public TextMeshProUGUI BetValue;
            public TextMeshProUGUI WinValue;
            public RectTransform Chevron;
            public RoundedBox ChevronA;
            public RoundedBox ChevronB;
            public UiGrid Places;
        }

        // One place under the player's line: where it was placed and at what multiplier, what it cost and what
        // came back - in the same columns as the line above, so the figures sit under the figures.
        private class PlaceLine
        {
            public UiGrid Root;
            public TextMeshProUGUI Name;
            public TextMeshProUGUI BetValue;
            public TextMeshProUGUI WinValue;
        }

        // A round plate with a letter on it and a picture over the top: a currency coin. The plate is what is on
        // screen until - or unless - the server's image arrives, so a window with no network still reads as a
        // window.
        private class Badge
        {
            public RectTransform Root;
            public RoundedBox Plate;
            public TextMeshProUGUI Letter;
            public Image Picture;
        }

        private Section BuildSection(Transform parent, string name)
        {
            var section = new Section { Root = Grid(parent, name) };

            section.Heading = Grid(Rect(section.Root), "Heading");
            section.Icon = BuildIcon(Rect(section.Heading), "Icon");
            section.Caption = UiWindowParts.Label(Rect(section.Heading), "Caption");

            section.Box = UiWindowParts.Box(Rect(section.Root), "Box");
            section.Inner = GridOn(section.Box.rectTransform);
            return section;
        }

        private Icon BuildIcon(Transform parent, string name)
        {
            var icon = new Icon { Root = UiWindowParts.Rect(parent, name) };

            for (int i = 0; i < IconParts; i++)
                icon.Parts[i] = UiWindowParts.Box(icon.Root, "Part " + i.ToString(CultureInfo.InvariantCulture));

            icon.Picture = UiWindowParts.Picture(icon.Root, "Picture");
            return icon;
        }

        private void BuildPlayer()
        {
            var list = Rect(bets.Inner);

            // The rows a sheet built before Round Bets showed the player - one per place, "Row 0" and on - are
            // thrown away rather than left loose in the list: nothing names them any more.
            for (int i = list.childCount - 1; i >= 0; i--)
            {
                var child = list.GetChild(i);
                if (child.name.StartsWith("Row ", StringComparison.Ordinal))
                    UiWindowParts.Discard(child.gameObject);
            }

            var plate = UiWindowParts.Box(list, "Player");
            player = new PlayerRow
            {
                Plate = plate,
                Grid = GridOn(plate.rectTransform),
            };

            player.Button = Hook(plate, TogglePlaces);
            player.Head = Grid(plate.transform, "Head");
            player.Who = Grid(Rect(player.Head), "Who");
            player.Avatar = BuildBadge(Rect(player.Who), "Avatar");
            player.Name = UiWindowParts.Label(Rect(player.Who), "Name");
            player.BetValue = UiWindowParts.Label(Rect(player.Head), "Bet");
            player.WinValue = UiWindowParts.Label(Rect(player.Head), "Win");
            player.Chevron = UiWindowParts.Rect(Rect(player.Head), "Chevron");
            player.ChevronA = UiWindowParts.Box(player.Chevron, "Bar A");
            player.ChevronB = UiWindowParts.Box(player.Chevron, "Bar B");
            player.Places = Grid(plate.transform, "Places");

            // Lines already there are taken over rather than left: a place line is made of the same parts
            // whatever it is showing.
            lines.Clear();
            spareLines.Clear();

            var places = Rect(player.Places);
            for (int i = 0; i < places.childCount; i++)
            {
                var child = places.GetChild(i) as RectTransform;
                if (child != null && child.name.StartsWith("Place ", StringComparison.Ordinal))
                    RetireLine(AdoptLine(child));
            }

            Named(plate.rectTransform, PlayerArea);
            Named(Rect(player.Head), HeadArea);
            Named(places, PlacesArea);
        }

        private PlaceLine AdoptLine(RectTransform root)
        {
            var line = new PlaceLine { Root = GridOn(root) };
            line.Name = UiWindowParts.Label(root, "Name");
            line.BetValue = UiWindowParts.Label(root, "Bet");
            line.WinValue = UiWindowParts.Label(root, "Win");
            return line;
        }

        private PlaceLine RentLine(int index)
        {
            while (spareLines.Count > 0)
            {
                int last = spareLines.Count - 1;
                var kept = spareLines[last];
                spareLines.RemoveAt(last);

                if (kept != null && kept.Root != null)
                    return kept;
            }

            return AdoptLine(UiWindowParts.Rect(Rect(player.Places), "Place " + index.ToString(CultureInfo.InvariantCulture)));
        }

        private void RetireLine(PlaceLine line)
        {
            if (line == null || line.Root == null)
                return;

            UiWindowParts.Item(Rect(line.Root)).Area = SpareArea;
            spareLines.Add(line);
        }

        private Badge BuildBadge(Transform parent, string name)
        {
            var badge = new Badge { Root = UiWindowParts.Rect(parent, name) };
            badge.Plate = UiWindowParts.Box(badge.Root, "Plate");
            badge.Letter = UiWindowParts.Label(badge.Plate.transform, "Letter");
            badge.Picture = UiWindowParts.Picture(badge.Root, "Picture");
            return badge;
        }

        private Button Hook(RoundedBox box, UnityAction handler)
        {
            var button = box.GetComponent<Button>();
            if (button == null)
                button = box.gameObject.AddComponent<Button>();

            button.targetGraphic = box;

            // Removed before it is added, every time: AddListener is not serialized, so this runs on every load,
            // and a UnityEvent compares a listener by its target and method rather than by the delegate object -
            // which is what keeps a second build from firing the handler twice.
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

            Label(idText, style.RoundIdSize, style.RoundIdValueColor, FontStyles.Normal);
            idText.alignment = TextAlignmentOptions.Left;

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

        private void PaintHeader()
        {
            header.RowGap = 0f;
            header.ColumnGap = 0f;
            header.padding = new RectOffset(0, 0, 0, 0);

            // The coin's cell on the left and an empty one the same width on the right, where the window's close
            // button floats - so the result sits in the middle of the window rather than in the middle of what
            // the coin left over.
            Columns(header, GridTrack.Fixed(style.HeaderSide), GridTrack.Flexible(), GridTrack.Fixed(style.HeaderSide));
            Rows(header, GridTrack.Auto());

            // The currency button: a coin while the amounts are in the bet's own currency, the dollar sign while
            // they are in dollars. Always the state it is in, never the one a press would go to.
            float size = style.ToggleSize;
            Named(toggleBox.rectTransform, ToggleArea, new Vector2(size, size));
            Paint(toggleBox, style.ToggleFill, 100000f);
            toggleBox.raycastTarget = true;

            float rim = Mathf.Max(1f, size * 0.06f);
            UiWindowParts.Stretch(toggleCoin.rectTransform, size * 0.2f, size * 0.2f, size * 0.2f, size * 0.2f);
            Paint(toggleCoin, style.ToggleCoinFill, 100000f);
            toggleCoin.SetBorderSize(rim);
            toggleCoin.SetBorderColor(style.ToggleCoinRim);
            toggleCoin.gameObject.SetActive(!InUsd);

            UiWindowParts.Stretch(toggleText.rectTransform, 0f, 0f, 0f, 0f);
            Label(toggleText, style.ToggleTextSize, style.ToggleTextColor, FontStyles.Bold);
            toggleText.gameObject.SetActive(InUsd);

            middle.padding = new RectOffset(0, 0, 0, 0);
            middle.ColumnGap = 0f;
            middle.RowGap = style.HeadingGap;
            Columns(middle, GridTrack.Flexible());

            // The pill is as wide as its text and no wider, held in the middle of the column - a cell of its own
            // size rather than a stretch, so the measured width is the drawn one.
            Paint(resultBox, style.ResultFill, 100000f);
            resultBox.SetBorderSize(style.ResultBorderSize);
            resultBox.SetBorderColor(style.ResultBorder);

            Label(resultText, style.ResultTextSize, style.ResultTextColor, style.ResultTextStyle);
            UiWindowParts.Stretch(resultText.rectTransform, style.ResultPadding.x, 0f, style.ResultPadding.x, 0f);
            resultText.textWrappingMode = TextWrappingModes.NoWrap;

            float width = resultText.GetPreferredValues(resultText.text).x + style.ResultPadding.x * 2f;
            float height = Mathf.Max(style.ResultHeight, style.ResultTextSize + style.ResultPadding.y * 2f);
            Named(resultBox.rectTransform, ResultArea, new Vector2(Mathf.Ceil(width), height));

            // One column and an auto row, so whatever a game stacks into Outcome comes out in hierarchy order with
            // each block as tall as it reports itself to be.
            Columns(outcome, GridTrack.Flexible());
            Rows(outcome, GridTrack.Auto());
            outcome.RowGap = style.HeadingGap;
            outcome.ColumnGap = 0f;
            outcome.padding = new RectOffset(0, 0, 0, 0);
        }

        private void PaintSections()
        {
            PaintSection(stats, ESectionIcon.Statistics, style.StatisticsIcon);
            PaintSection(serverSeed, ESectionIcon.Server, style.ServerSeedIcon);
            PaintSection(clientSeed, ESectionIcon.Client, style.ClientSeedIcon);
            PaintSection(serverHash, ESectionIcon.Hash, style.HashIcon);
            PaintSection(bets, ESectionIcon.List, style.BetsIcon);

            // The totals: three columns across the box, each a caption with its figure under it - the first held
            // left, the second in the middle and the third right, which is how the design spaces them. A caption
            // too long for its third wraps, and the row of figures moves down with it.
            Columns(stats.Inner, GridTrack.Flexible(), GridTrack.Flexible(), GridTrack.Flexible());
            Rows(stats.Inner, GridTrack.Auto(), GridTrack.Auto());
            stats.Inner.ColumnGap = style.IconGap;
            stats.Inner.RowGap = style.StatGap;

            var align = new[] { TextAlignmentOptions.Left, TextAlignmentOptions.Center, TextAlignmentOptions.Right };

            for (int i = 0; i < totalCaptions.Length; i++)
            {
                Put(totalCaptions[i].rectTransform, i, 0);
                Put(totalValues[i].rectTransform, i, 1);
                Label(totalCaptions[i], style.StatCaptionSize, style.StatCaptionColor, style.StatCaptionStyle);
                Label(totalValues[i], style.StatSize, style.ValueColor, style.StatValueStyle);
                totalCaptions[i].alignment = align[i];
                totalValues[i].alignment = align[i];
            }

            // The three seeds: one long string each, left-aligned and wrapping, the row as tall as the wrapping
            // made it.
            foreach (var section in new[] { serverSeed, clientSeed, serverHash })
            {
                Columns(section.Inner, GridTrack.Flexible());
                Rows(section.Inner, GridTrack.Auto());
                Put(section.Value.rectTransform, 0, 0);
                Label(section.Value, style.ValueSize, style.ValueColor, style.ValueStyle);
                section.Value.alignment = TextAlignmentOptions.Left;
            }

            // The list: the player's row, laid out by Arrange.
            Columns(bets.Inner, GridTrack.Flexible());
            bets.Inner.RowGap = 0f;
        }

        private void PaintSection(Section section, ESectionIcon kind, Sprite sprite)
        {
            bool iconOn = style.IconSize > 0f;

            section.Root.padding = new RectOffset(0, 0, 0, 0);
            section.Root.RowGap = style.HeadingGap;
            section.Root.ColumnGap = 0f;
            Columns(section.Root, GridTrack.Flexible());
            Rows(section.Root, GridTrack.Auto(), GridTrack.Auto());
            Put(Rect(section.Heading), 0, 0);
            Put(section.Box.rectTransform, 0, 1);

            // The heading: the mark in a cell of its own size and the caption taking the rest. With the marks
            // off the first column closes to nothing and so does the gap after it.
            section.Heading.padding = new RectOffset(0, 0, 0, 0);
            section.Heading.RowGap = 0f;
            section.Heading.ColumnGap = iconOn ? style.IconGap : 0f;
            Columns(section.Heading, GridTrack.Fixed(iconOn ? style.IconSize : 0f), GridTrack.Flexible());
            Rows(section.Heading, GridTrack.Auto());

            Middle(section.Icon.Root, 0, 0, iconOn ? new Vector2(style.IconSize, style.IconSize) : Vector2.zero);
            Draw(section.Icon, kind, sprite, iconOn ? style.IconSize : 0f);

            Put(section.Caption.rectTransform, 1, 0);
            Label(section.Caption, style.HeadingSize, style.HeadingColor, style.HeadingStyle);
            section.Caption.alignment = TextAlignmentOptions.Left;

            // The box: a thin rule round whatever is in it, and nothing behind it unless the style says so.
            Paint(section.Box, style.BoxFill, style.BoxCornerRadius);
            section.Box.SetBorderSize(style.BoxBorderSize);
            section.Box.SetBorderColor(style.BoxBorder);

            int x = Mathf.RoundToInt(style.BoxPaddingX);
            int y = Mathf.RoundToInt(style.BoxPaddingY);
            section.Inner.padding = new RectOffset(x, x, y, y);
            section.Inner.RowGap = 0f;
            section.Inner.ColumnGap = 0f;
        }

        // The four columns the player's line and every place line under it share: who, what was staked, what came
        // back, and the chevron's room - so a place's figures sit under the player's.
        private GridTrack[] RowColumns() => new[]
        {
            GridTrack.Flexible(),
            GridTrack.Flexible(),
            GridTrack.Flexible(),
            GridTrack.Fixed(Mathf.Max(style.ChevronSize, 1f) + style.RowPadding),
        };

        // The shape of the player's line and the places under it. Said here rather than when they were made, so a
        // style edited from code reaches a row that already exists.
        private void PaintPlayer()
        {
            int pad = Mathf.RoundToInt(style.RowPadding);

            Paint(player.Plate, style.RowFill, style.RowCornerRadius);
            player.Plate.raycastTarget = true;
            player.Grid.padding = new RectOffset(0, 0, 0, 0);
            player.Grid.RowGap = 0f;
            player.Grid.ColumnGap = 0f;
            Columns(player.Grid, GridTrack.Flexible());

            // A press does something only where there are places to open, and a Button that is not interactable
            // is what tells the cursor so. No tint either way: the plate is a row, not a button that greys out.
            player.Button.transition = Selectable.Transition.None;
            player.Button.interactable = hasPlaces;

            var head = player.Head;
            head.padding = new RectOffset(pad, pad, 0, 0);
            head.ColumnGap = style.RowColumnGap;
            head.RowGap = 0f;
            Columns(head, RowColumns());
            Rows(head, GridTrack.Flexible());

            Put(Rect(player.Who), 0, 0);
            Put(player.BetValue.rectTransform, 1, 0);
            Put(player.WinValue.rectTransform, 2, 0);
            Middle(player.Chevron, 3, 0, new Vector2(style.ChevronSize, style.ChevronSize));

            // The avatar and the name held together in the middle of the first column, which is how the web front
            // draws them: a flexible track on each side and the pair between.
            var who = player.Who;
            who.padding = new RectOffset(0, 0, 0, 0);
            who.ColumnGap = style.RowColumnGap;
            who.RowGap = 0f;
            Columns(who, GridTrack.Flexible(), GridTrack.Fixed(style.AvatarSize), GridTrack.Auto(), GridTrack.Flexible());
            Rows(who, GridTrack.Flexible());
            Middle(player.Avatar.Root, 1, 0, new Vector2(style.AvatarSize, style.AvatarSize));
            Put(player.Name.rectTransform, 2, 0);

            Label(player.Name, style.RowNameSize, style.RowTextColor, style.ValueStyle);
            player.Name.alignment = TextAlignmentOptions.Left;
            player.Name.textWrappingMode = TextWrappingModes.NoWrap;
            player.Name.overflowMode = TextOverflowModes.Ellipsis;

            Label(player.BetValue, style.RowAmountSize, style.RowTextColor, style.ValueStyle);
            Label(player.WinValue, style.RowAmountSize, style.RowTextColor, style.ValueStyle);
            player.BetValue.textWrappingMode = TextWrappingModes.NoWrap;
            player.WinValue.textWrappingMode = TextWrappingModes.NoWrap;

            PaintAvatar(player.Avatar);

            // The chevron: two strokes meeting at the bottom, turned over while the places are open.
            float c = style.ChevronSize;
            Stroke(player.ChevronA, new Vector2(-0.42f * c, 0.2f * c), new Vector2(0f, -0.2f * c), style.ChevronThickness, style.ChevronColor);
            Stroke(player.ChevronB, new Vector2(0f, -0.2f * c), new Vector2(0.42f * c, 0.2f * c), style.ChevronThickness, style.ChevronColor);
            player.ChevronA.gameObject.SetActive(hasPlaces);
            player.ChevronB.gameObject.SetActive(hasPlaces);
            player.Chevron.localRotation = placesOpen ? Quaternion.Euler(0f, 0f, 180f) : Quaternion.identity;

            player.Places.padding = new RectOffset(pad, pad, 0, pad);
            player.Places.RowGap = 0f;
            player.Places.ColumnGap = 0f;
            Columns(player.Places, GridTrack.Flexible());
        }

        private void PaintLine(PlaceLine line)
        {
            line.Root.padding = new RectOffset(0, 0, 0, 0);
            line.Root.ColumnGap = style.RowColumnGap;
            line.Root.RowGap = 0f;
            Columns(line.Root, RowColumns());
            Rows(line.Root, GridTrack.Flexible());

            Put(line.Name.rectTransform, 0, 0);
            Put(line.BetValue.rectTransform, 1, 0);
            Put(line.WinValue.rectTransform, 2, 0);

            foreach (var label in new[] { line.Name, line.BetValue, line.WinValue })
            {
                Label(label, style.PlaceTextSize, style.PlaceTextColor, FontStyles.Normal);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        private void PaintBar()
        {
            barGrid.ColumnGap = 0f;
            barGrid.padding = new RectOffset(0, 0, 0, 0);

            Named(verifyBox.rectTransform, VerifyArea, style.VerifySize);
            Paint(verifyBox, style.VerifyFill, style.VerifyCornerRadius);
            verifyBox.SetBorderSize(style.BoxBorderSize);
            verifyBox.SetBorderColor(style.VerifyBorder);
            verifyBox.raycastTarget = true;

            UiWindowParts.Stretch(verifyText.rectTransform, 6f, 0f, 6f, 0f);
            Label(verifyText, style.VerifyTextSize, style.VerifyTextColor, FontStyles.Normal);
            verifyText.text = Translator.Label(verifyLabel);
        }

        // ------------------------------------------------------------------ what is showing

        // Says the arrangement as a layout - a picture of the grid, one area name per cell - rather than by
        // switching cells on and off. A UiGrid takes a layout as the whole truth about which of its children are
        // showing and re-asserts it every time it is enabled, so a cell hidden with SetActive comes back the
        // next time the window opens.
        private void Arrange()
        {
            ArrangeHeader();
            ArrangeList();

            // The bar is one button held in the middle, there only for a bet that can be verified.
            barGrid.SetLayout(UiGridLayout.Build()
                .Columns(GridTrack.Flexible(), GridTrack.Fixed(style.VerifySize.x), GridTrack.Flexible())
                .Rows(GridTrack.Fixed(style.VerifySize.y))
                .Row(UiGridLayout.Empty, verifyOn ? VerifyArea : UiGridLayout.Empty, UiGridLayout.Empty)
                .Done());

            // Every block down the content area, each as tall as it needs to be. Nothing is given a height of its
            // own: the window measures the lot and scrolls whatever is past its Max Height, which is the point of
            // laying the bet out as one sheet.
            contentRows.Clear();
            var content = UiGridLayout.Build().Columns(GridTrack.Flexible());

            Add(content, headerOn, HeaderArea);
            Add(content, idOn, IdArea);

            if (!hasData)
            {
                content.Row(LoaderArea);
                contentRows.Add(GridTrack.Fixed(style.LoaderHeight));
            }

            Add(content, hasData && showStatistics, StatsArea);
            Add(content, hasData && showServerSeed, ServerArea);
            Add(content, hasData && showClientSeed, ClientArea);
            Add(content, hasData && showServerHash, HashArea);
            Add(content, hasData && showBets, BetsArea);
            Add(content, verifyOn, BarArea);

            // Held open at one row: a layout of none would leave the grid on its own list, and its own list is
            // not what says what is showing here.
            GridOn(Window.Content).SetLayout(content.Rows(contentRows.ToArray()).Size(1, Mathf.Max(1, contentRows.Count)).Done());
        }

        private void Add(UiGridLayout.Builder layout, bool shown, string area)
        {
            if (!shown)
                return;

            layout.Row(area);
            contentRows.Add(GridTrack.Auto());
        }

        private void ArrangeHeader()
        {
            header.SetLayout(UiGridLayout.Build()
                .Columns(GridTrack.Fixed(style.HeaderSide), GridTrack.Flexible(), GridTrack.Fixed(style.HeaderSide))
                .Rows(GridTrack.Auto())
                .Row(
                    toggleOn ? ToggleArea : UiGridLayout.Empty,
                    resultOn || outcomeOn ? MiddleArea : UiGridLayout.Empty,
                    UiGridLayout.Empty)
                .Done());

            middleRows.Clear();
            var layout = UiGridLayout.Build().Columns(GridTrack.Flexible());

            if (resultOn)
            {
                layout.Row(ResultArea);
                middleRows.Add(GridTrack.Auto());
            }

            if (outcomeOn)
            {
                layout.Row(OutcomeArea);
                middleRows.Add(style.OutcomeHeight > 0f ? GridTrack.Fixed(style.OutcomeHeight) : GridTrack.Auto());
            }

            middle.SetLayout(layout.Rows(middleRows.ToArray()).Size(1, Mathf.Max(1, middleRows.Count)).Done());
        }

        // The list is the one player who made the bet; the plate is their line, and under it the places while
        // they are open. Each said as a layout, like everything else here.
        private void ArrangeList()
        {
            bets.Inner.SetLayout(UiGridLayout.Build()
                .Columns(GridTrack.Flexible())
                .Rows(GridTrack.Auto())
                .Row(PlayerArea)
                .Done());

            bool open = placesOpen && hasPlaces && lines.Count > 0;

            listRows.Clear();
            var plate = UiGridLayout.Build().Columns(GridTrack.Flexible()).Row(HeadArea);
            listRows.Add(GridTrack.Fixed(style.RowHeight));

            if (open)
            {
                plate.Row(PlacesArea);
                listRows.Add(GridTrack.Auto());
            }

            player.Grid.SetLayout(plate.Rows(listRows.ToArray()).Done());

            placeRows.Clear();
            var places = UiGridLayout.Build().Columns(GridTrack.Flexible());

            for (int i = 0; i < lines.Count; i++)
            {
                places.Row(PlaceArea + i.ToString(CultureInfo.InvariantCulture));
                placeRows.Add(GridTrack.Fixed(style.PlaceHeight));
            }

            player.Places.SetLayout(places.Rows(placeRows.ToArray()).Size(1, Mathf.Max(1, placeRows.Count)).Done());
        }

        // ------------------------------------------------------------------ the data

        private void Write(TransactionPublic data)
        {
            toggleText.text = Translator.Label(usdLabel);
            resultText.text = ResultText(data);

            string id = data != null ? data.Id : requestedId;
            idText.text = Tint(Translator.Label(idLabel) + style.HeadingSuffix, style.RoundIdCaptionColor) + "  " + id;

            // A single-player roll is seeded from the salt the player sent. A shared or multiplayer one is seeded
            // from a block hash nobody can have known in advance, and is captioned as what it is.
            bool single = data == null || data.GameType == EGameType.SINGLE;

            stats.Caption.text = Heading(statisticsLabel);
            serverSeed.Caption.text = Heading(serverSeedLabel);
            clientSeed.Caption.text = Heading(single ? clientSeedLabel : blockHashLabel);
            serverHash.Caption.text = Heading(serverShaLabel);
            bets.Caption.text = Heading(betsLabel);

            ReadParts(data);
            WriteTotals(data);
            WriteSeeds(data);
            WritePlayer(data);
        }

        // What the pill says: what the game set, or - on the sample a window built from the menu shows - what
        // the design says, so the dialog arrives looking like the dialog.
        private string ResultText(TransactionPublic data)
        {
            if (!string.IsNullOrEmpty(result))
                return result;

            return data != null && data == preview ? "Result: 2" : string.Empty;
        }

        // How many parts the bet was made of, what was staked, and what it made - the same profit the bet info
        // window prints, win less stake. In the bet's own currency, or in dollars while the toggle says so.
        private void WriteTotals(TransactionPublic data)
        {
            if (data == null)
                return;

            decimal wagered = Number(data.BetAmount);
            decimal won = Number(data.WinAmount);

            // One for a bet made in one piece; one a place for a bet spread over several.
            int count = hasPlaces ? parts.Count : 1;
            Total(0, totalBetCountLabel, count.ToString(CultureInfo.InvariantCulture));
            Total(1, totalBetAmountLabel, Amount(wagered, data));
            Total(2, totalProfitLabel, Amount(won - wagered, data));
        }

        // A caption with the figure on the line under it, so it takes no colon: the layout already says which
        // figure is whose.
        private void Total(int index, string caption, string value)
        {
            totalCaptions[index].text = Translator.Label(caption);
            totalValues[index].text = value;
        }

        // The seed the server kept, the one the roll was salted with, and the hash the server published before
        // the roll - what makes a bet checkable after the fact. A server seed still in play arrives empty and
        // says so; anything else the server did not send says N/A rather than leaving a blank box.
        private void WriteSeeds(TransactionPublic data)
        {
            if (data == null)
                return;

            serverSeed.Value.text = string.IsNullOrEmpty(data.ServerSeed) ? Translator.Label(hiddenLabel) : data.ServerSeed;
            clientSeed.Value.text = string.IsNullOrEmpty(data.ClientSalt) ? Translator.Label(missingLabel) : data.ClientSalt;
            serverHash.Value.text = string.IsNullOrEmpty(data.ServerSeedSha512) ? Translator.Label(missingLabel) : data.ServerSeedSha512;
        }

        // What the bet was spread over. A shared game sends the places a transaction was made on - a bet on red,
        // a bet on 17 - as ON_BET_INFO, which the socket keeps in MainState.BetInfos under the transaction's id.
        // Where that is there, the player's row opens on one line a place; anywhere else there is nothing under
        // the row to open.
        private void ReadParts(TransactionPublic data)
        {
            parts.Clear();
            hasPlaces = false;

            if (data == null)
                return;

            var info = InfoFor(data);
            if (info == null || info.Bets == null)
                return;

            foreach (var chunk in info.Bets)
            {
                if (chunk == null)
                    continue;

                decimal amount = Number(chunk.Amount);
                decimal payout = Number(chunk.Payout);

                parts.Add(new Part
                {
                    Name = string.IsNullOrEmpty(chunk.BetId) ? Translator.Label(betLabel) : chunk.BetId,
                    Wagered = amount,
                    Payout = payout,
                    Won = amount * payout,
                });
            }

            hasPlaces = parts.Count > 0;
            placesOpen &= hasPlaces;
        }

        private struct Part
        {
            public string Name;
            public decimal Wagered;
            public decimal Payout;
            public decimal Won;
        }

        // The player who made the bet, what they staked and what came back - in the bet's own currency, or in
        // dollars while the toggle says so, the same way the totals read - and the places under it.
        private void WritePlayer(TransactionPublic data)
        {
            PaintPlayer();

            if (data == null)
                return;

            string name = string.IsNullOrEmpty(data.IPlayerName) ? data.IPlayerId : data.IPlayerName;
            player.Name.text = name;

            decimal wagered = Number(data.BetAmount);
            decimal won = Number(data.WinAmount);

            player.BetValue.text = Amount(wagered, data);
            player.WinValue.text = Amount(won, data);
            player.WinValue.color = Won(wagered, won) ? style.RowWinColor : style.RowLoseColor;

            Letter(player.Avatar, name);
            Fetch(player.Avatar, data.CImg);

            // A line is kept for as long as it is needed and handed back when it is not - a bet on twelve places
            // and one on two are the same list with a different layout on it.
            for (int i = parts.Count; i < lines.Count; i++)
                RetireLine(lines[i]);

            if (lines.Count > parts.Count)
                lines.RemoveRange(parts.Count, lines.Count - parts.Count);

            for (int i = 0; i < parts.Count; i++)
            {
                if (i >= lines.Count)
                    lines.Add(RentLine(i));

                WriteLine(lines[i], parts[i], data, i);
            }
        }

        private void WriteLine(PlaceLine line, Part part, TransactionPublic data, int index)
        {
            line.Root.name = "Place " + index.ToString(CultureInfo.InvariantCulture);
            PaintLine(line);
            Named(Rect(line.Root), PlaceArea + index.ToString(CultureInfo.InvariantCulture));

            line.Name.text = part.Name + "  " + Tint(Fixed(part.Payout, style.PayoutDecimals) + "x", style.RowPayoutColor);
            line.BetValue.text = Amount(part.Wagered, data);
            line.WinValue.text = Amount(part.Won, data);
            line.WinValue.color = Won(part.Wagered, part.Won) ? style.RowWinColor : style.PlaceTextColor;
        }

        // Green from a payout of one: the bet came back whole or better.
        private static bool Won(decimal wagered, decimal won) =>
            wagered > 0m ? won >= wagered : won > 0m;

        // The places a shared transaction was spread over, if the socket has them. The id is matched the way
        // ON_BET_INFO_INCREASE matches it - by what comes before the first dash.
        private static BetInfoDto InfoFor(TransactionPublic data)
        {
            var manager = StateManager.Inst;
            if (manager == null || manager.MainState == null || manager.MainState.BetInfos == null || string.IsNullOrEmpty(data.Id))
                return null;

            var infos = manager.MainState.BetInfos;
            if (infos.TryGetValue(data.Id, out var info))
                return info;

            int dash = data.Id.IndexOf('-');
            return dash > 0 && infos.TryGetValue(data.Id.Substring(0, dash), out info) ? info : null;
        }

        // Dollars per unit of the bet's currency. A shared transaction carries its own rate; any other bet is
        // converted at the rate the player's balance is in, and only when it is the same currency - a rate for
        // one coin applied to another would be a confident wrong number. Zero when neither says.
        private decimal Rate(TransactionPublic data)
        {
            if (data == preview)
                return 62000m;

            var info = InfoFor(data);
            if (info != null && Number(info.RateUsd) > 0m)
                return Number(info.RateUsd);

            var manager = StateManager.Inst;
            var balance = manager != null && manager.MainState != null ? manager.MainState.BalanceState : null;

            if (balance != null && string.Equals(balance.Currency, data.Currency, StringComparison.OrdinalIgnoreCase))
                return Number(balance.RateUsd);

            return 0m;
        }

        private TransactionPublic Known()
        {
            var manager = StateManager.Inst;
            if (manager == null || manager.MainState == null)
                return null;

            return manager.MainState.BetInfoById;
        }

        private void Apply(TransactionPublic value)
        {
            transaction = value;
            stamp++;

            EnsureBuilt();
            Fill();

            if (value == null)
                return;

            // The game's listener is where the result is written, and a listener that throws is the game's bug
            // rather than a reason for the dialog not to open - the same reasoning as Fill.
            try
            {
                OnTransaction.Invoke(value);
            }
            catch (Exception error)
            {
                Debug.LogException(error, this);
            }
        }

        // Refresh, with whatever it throws kept out of the way of the window opening. A field that could not be
        // written - a payload shaped in a way this did not expect - is a dialog with a gap in it, and a dialog
        // with a gap in it beats a press that did nothing. The exception still goes to the console.
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

        // The transaction itself, and the two events that change what is known about its parts: ON_BET_INFO
        // bringing them, and ON_BET_INFO_PAYOUT settling them. Either can land after the transaction does.
        private void Listen(bool on)
        {
            var manager = StateManager.Inst;
            if (manager == null || manager.Events == null || !followState)
                return;

            var events = manager.Events;

            if (on && !listening)
            {
                events.OnBetInfoById.AddListener(HandleBetInfo);
                events.OnBetInfo.AddListener(HandleParts);
                events.OnBetInfoPayout.AddListener(HandleParts);
                listening = true;
            }
            else if (!on && listening)
            {
                events.OnBetInfoById.RemoveListener(HandleBetInfo);
                events.OnBetInfo.RemoveListener(HandleParts);
                events.OnBetInfoPayout.RemoveListener(HandleParts);
                listening = false;
            }
        }

        // Every window listening hears every answer, so one opened on a particular bet has to let the others
        // past rather than showing whichever came back last.
        private void HandleBetInfo(TransactionPublic value)
        {
            if (value == null)
                return;

            if (matchRequestedId && !string.IsNullOrEmpty(requestedId) && value.Id != requestedId)
                return;

            Apply(value);
        }

        // The event does not say which transaction it was about, so the sheet simply reads its parts again. Only
        // while there is a bet on it: a sheet on the loader has nothing to read them into.
        private void HandleParts(BetInfoDto value)
        {
            if (transaction != null)
                Fill();
        }

        // ------------------------------------------------------------------ formatting

        private string Heading(string label) => Translator.Label(label) + style.HeadingSuffix;

        private static string Tint(string text, Color color) =>
            "<color=#" + ColorUtility.ToHtmlStringRGBA(color) + ">" + text + "</color>";

        // An amount as the totals print it: the currency code in front in the bet's own currency, the dollar
        // sign in front once converted - and in front of a bet that was made in dollars to begin with, which
        // reads as $ 1.5 rather than as USD 1.5.
        private string Amount(decimal value, TransactionPublic data)
        {
            if (InUsd)
                return Small(style.UsdPrefix) + Money(value * rate, UsdDecimals());

            string money = Money(value, Decimals(data));

            if (IsDollar(data.Currency))
                return Small(style.UsdPrefix) + money;

            return string.IsNullOrEmpty(data.Currency) ? money : Small(data.Currency.ToUpperInvariant() + " ") + money;
        }

        // Dollars themselves, not a coin pegged to them: USDT and USDC are currencies of their own, and the code
        // is what tells the player which one they bet in.
        private static bool IsDollar(string currency) =>
            string.Equals(currency, "usd", StringComparison.OrdinalIgnoreCase);

        private int Decimals(TransactionPublic data)
        {
            // A converted figure is a figure in dollars whatever currency it started in, so it is printed to the
            // dollar count rather than to the currency's.
            if (InUsd)
                return UsdDecimals();

            if (style.Decimals >= 0)
                return Mathf.Clamp(style.Decimals, 0, 18);

            // Zero is what the field says when the server has not filled it in, and printing money to no
            // decimals at all would turn every satoshi into 0. Eight is what the web front falls back to.
            return data != null && data.DecimalPoints > 0 ? Mathf.Min(data.DecimalPoints, 18) : 8;
        }

        // What a dollar figure is printed to: the style's own count when it has one, else whatever the system
        // says a figure takes, since that is what the web front reads them off. Two where there is neither.
        private int UsdDecimals()
        {
            if (style.UsdDecimals >= 0)
                return Mathf.Clamp(style.UsdDecimals, 0, 18);

            var manager = StateManager.Inst;
            var system = manager != null && manager.MainState != null ? manager.MainState.SystemState : null;
            int points = system != null && system.DecimalPoints.HasValue ? system.DecimalPoints.Value : 2;
            return Mathf.Clamp(points, 0, 18);
        }

        // Money is truncated rather than rounded, and only then trimmed - the same order the web front does it
        // in, so the same bet reads the same here and in the bet info dialog.
        private string Money(decimal value, int decimals)
        {
            string text = Fixed(value, decimals);

            if (!style.TrimZeros || text.IndexOf('.') < 0)
                return text;

            text = text.TrimEnd('0');
            return text.EndsWith(".", StringComparison.Ordinal) ? text + "0" : text;
        }

        private static string Fixed(decimal value, int decimals)
        {
            decimal scale = 1m;
            for (int i = 0; i < decimals; i++)
                scale *= 10m;

            decimal cut = decimal.Truncate(value * scale) / scale;
            return cut.ToString("F" + decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        }

        private static decimal Number(string raw) =>
            decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : 0m;

        /// <summary>The currency code or dollar sign beside an amount, at the style's scale.</summary>
        private string Small(string text)
        {
            if (string.IsNullOrEmpty(text) || style.SmallTextScale >= 0.999f)
                return text ?? string.Empty;

            return "<size=" + Mathf.RoundToInt(style.SmallTextScale * 100f).ToString(CultureInfo.InvariantCulture) + "%>" + text + "</size>";
        }

        // The query the verifier expects: both seeds in base64, the nonce, and the house edge the roll was made
        // under. The same string the bet info window builds, and the two have to agree.
        private string VerifyUrl(TransactionPublic data)
        {
            var nonce = string.IsNullOrEmpty(data.Nonce) ? "0" : data.Nonce;
            var nig = data.InGameNonce.HasValue
                ? "&nig=" + data.InGameNonce.Value.ToString(CultureInfo.InvariantCulture)
                : string.Empty;

            return data.VerifyUrl
                + "?cs=" + Base64(data.ClientSalt)
                + "&ss=" + Base64(data.ServerSeed)
                + "&n=" + nonce
                + nig
                + "&he=" + data.HouseEdge;
        }

        private static string Base64(string text) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(text ?? string.Empty));

        // ------------------------------------------------------------------ pictures

        // The player's picture on a rounded plate, with the first letter of their name on it until the picture
        // arrives - or for good, where the server sends none.
        private void PaintAvatar(Badge badge)
        {
            if (badge == null)
                return;

            float size = style.AvatarSize;

            UiWindowParts.Stretch(badge.Plate.rectTransform, 0f, 0f, 0f, 0f);
            Paint(badge.Plate, style.AvatarFill, style.AvatarCornerRadius);

            UiWindowParts.Stretch(badge.Letter.rectTransform, 0f, 0f, 0f, 0f);
            Label(badge.Letter, Mathf.Max(1f, size * 0.5f), style.AvatarLetterColor, FontStyles.Bold);

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

        // The plate and its letter stay behind the picture rather than being replaced by it, so a url that never
        // answers leaves something readable instead of a hole.
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
                // another bet since, and this picture is no longer the one that belongs there.
                if (this == null || badge.Picture == null || asked != stamp)
                    return;

                badge.Picture.sprite = sprite;
                badge.Picture.gameObject.SetActive(sprite != null);
                badge.Letter.gameObject.SetActive(sprite == null);
            });
        }

        // ------------------------------------------------------------------ the section marks

        private enum ESectionIcon
        {
            /// <summary>An axis and three bars.</summary>
            Statistics,

            /// <summary>Two stacked units with a light on each.</summary>
            Server,

            /// <summary>A screen on a stand.</summary>
            Client,

            /// <summary>A hash sign.</summary>
            Hash,

            /// <summary>Three bullets and three lines.</summary>
            List,
        }

        // Every mark is written as the shape it is, in fractions of its own size, so each keeps its proportions
        // at any IconSize. Parts a mark does not use are switched off - they are children of the icon rather than
        // cells of a grid, so nothing re-asserts them.
        private void Draw(Icon icon, ESectionIcon kind, Sprite sprite, float s)
        {
            bool picture = sprite != null && s > 0f;

            icon.Picture.gameObject.SetActive(picture);
            icon.Picture.sprite = sprite;
            icon.Picture.preserveAspect = true;
            icon.Picture.color = style.IconColor;
            icon.Picture.raycastTarget = false;
            UiWindowParts.Stretch(icon.Picture.rectTransform, 0f, 0f, 0f, 0f);

            foreach (var part in icon.Parts)
                part.gameObject.SetActive(false);

            if (picture || s <= 0f)
                return;

            float t = style.IconThickness;
            var p = icon.Parts;
            var c = style.IconColor;

            switch (kind)
            {
                case ESectionIcon.Statistics:
                    Block(p[0], new Vector2(-0.42f * s, 0f), new Vector2(t, 0.9f * s), t * 0.5f, c);
                    Column(p[1], -0.16f * s, 0.16f * s, 0.45f * s, s, c);
                    Column(p[2], 0.1f * s, 0.16f * s, 0.85f * s, s, c);
                    Column(p[3], 0.36f * s, 0.16f * s, 0.62f * s, s, c);
                    break;

                case ESectionIcon.Server:
                    Frame(p[0], new Vector2(0f, 0.23f * s), new Vector2(0.92f * s, 0.4f * s), 0.1f * s, t, c);
                    Frame(p[1], new Vector2(0f, -0.23f * s), new Vector2(0.92f * s, 0.4f * s), 0.1f * s, t, c);
                    Block(p[2], new Vector2(-0.22f * s, 0.23f * s), new Vector2(t * 1.3f, t * 1.3f), t, c);
                    Block(p[3], new Vector2(-0.22f * s, -0.23f * s), new Vector2(t * 1.3f, t * 1.3f), t, c);
                    break;

                case ESectionIcon.Client:
                    Frame(p[0], new Vector2(0f, 0.12f * s), new Vector2(0.94f * s, 0.62f * s), 0.08f * s, t, c);
                    Block(p[1], new Vector2(0f, -0.29f * s), new Vector2(t, 0.18f * s), 0f, c);
                    Block(p[2], new Vector2(0f, -0.42f * s), new Vector2(0.46f * s, t), t * 0.5f, c);
                    break;

                case ESectionIcon.Hash:
                    Stroke(p[0], new Vector2(-0.08f * s, 0.44f * s), new Vector2(-0.22f * s, -0.44f * s), t, c);
                    Stroke(p[1], new Vector2(0.24f * s, 0.44f * s), new Vector2(0.1f * s, -0.44f * s), t, c);
                    Stroke(p[2], new Vector2(-0.42f * s, 0.16f * s), new Vector2(0.44f * s, 0.16f * s), t, c);
                    Stroke(p[3], new Vector2(-0.44f * s, -0.16f * s), new Vector2(0.42f * s, -0.16f * s), t, c);
                    break;

                case ESectionIcon.List:
                    for (int i = 0; i < 3; i++)
                    {
                        float y = (0.32f - 0.32f * i) * s;
                        Frame(p[i * 2], new Vector2(-0.34f * s, y), new Vector2(0.2f * s, 0.2f * s), 0.03f * s, t * 0.8f, c);
                        Block(p[i * 2 + 1], new Vector2(0.14f * s, y), new Vector2(0.56f * s, t), t * 0.5f, c);
                    }

                    break;
            }
        }

        // A bar standing on the bottom edge of the mark, for the chart.
        private void Column(RoundedBox part, float x, float width, float height, float s, Color color) =>
            Block(part, new Vector2(x, -0.45f * s + height * 0.5f), new Vector2(width, height), width * 0.2f, color);

        private void Block(RoundedBox part, Vector2 centre, Vector2 size, float radius, Color color)
        {
            part.gameObject.SetActive(true);
            UiWindowParts.Pin(part.rectTransform, new Vector2(0.5f, 0.5f), size, centre);
            part.rectTransform.localRotation = Quaternion.identity;
            Paint(part, color, radius);
            part.EdgeSoftness = 1f;
        }

        // An outline with nothing in it - border only - which is what a RoundedBox gives for free.
        private void Frame(RoundedBox part, Vector2 centre, Vector2 size, float radius, float thickness, Color color)
        {
            part.gameObject.SetActive(true);
            UiWindowParts.Pin(part.rectTransform, new Vector2(0.5f, 0.5f), size, centre);
            part.rectTransform.localRotation = Quaternion.identity;
            Paint(part, Color.clear, radius);
            part.SetBorderSize(thickness);
            part.SetBorderColor(color);
            part.EdgeSoftness = 1f;
        }

        // One bar from one point to another: the length, the angle and the middle worked out from the two ends.
        private void Stroke(RoundedBox part, Vector2 from, Vector2 to, float thickness, Color color)
        {
            var span = to - from;
            float length = Mathf.Max(span.magnitude, thickness);

            part.gameObject.SetActive(true);
            UiWindowParts.Pin(part.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(length, thickness), (from + to) * 0.5f);
            part.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(span.y, span.x) * Mathf.Rad2Deg);
            Paint(part, color, 100000f);
            part.EdgeSoftness = 1f;
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

                // Built with DOTween.To rather than DOScale: the shortcuts live in DOTween's UI module, which is
                // compiled into the project's own assembly and cannot be reached from a package. Unscaled, like
                // every other tween in this folder - a dialog that opens over a paused game should not be waiting
                // on time that is not running.
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

        // The window is sized from the sheet out: how tall it has to be depends on how far the seeds wrapped and
        // how many parts the bet was made of, neither of which anything here knows in advance. Past the window's
        // Max Height it stops growing and scrolls, which is the window's own job and the whole of this design.
        //
        // Twice, and that is not belt and braces. A label reports the height it needs at the width it has, and
        // its width is what the first pass settles - so on the way into a window that has just been activated a
        // wrapped hash measures as one line. The second pass measures against the widths the first wrote.
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

        private static UiGrid GridOn(RectTransform rect) => UiWindowParts.Grid(rect);

        /// <summary>Puts something in a cell, stretched to fill it.</summary>
        private static void Put(RectTransform rect, int column, int row)
        {
            var item = UiWindowParts.Item(rect);
            item.PlaceAt(column, row);
            item.Span(1, 1);
            item.OverrideAlign = false;
        }

        /// <summary>Puts something of its own size in the middle of a cell. For anything square - a coin, a
        /// mark - which a stretch would pull out of shape.</summary>
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

        private void Label(TextMeshProUGUI label, float size, Color color, FontStyles fontStyle)
        {
            label.font = style.Font != null ? style.Font : label.font;
            label.fontSize = size;
            label.color = color;
            label.fontStyle = fontStyle;
            label.alignment = TextAlignmentOptions.Center;
            label.richText = true;
            label.raycastTarget = false;
        }

        // A bet that reads the way the design was drawn, for a scene with no template running in it: a dialog
        // built from a menu has to look like the dialog rather than like a row of dots that never stops. A real
        // game never sees this - the moment there is a StateManager, only the server's transactions are shown.
        private static TransactionPublic Sample() => new TransactionPublic
        {
            Id = "d9f77676f0a390e1",
            BetAmount = "0.0001",
            WinAmount = "0.0002",
            Payout = "2",
            Win = true,
            Currency = "btc",
            DecimalPoints = 8,
            GameName = "Plinko",
            IPlayerId = "dev",
            IPlayerName = "DEV-TEAM",
            Nonce = "42",
            ServerSeed = "6e43ebe3daf247c9892449b94be2d57a",
            ServerSeedSha512 = "5cca0e0e2291f73c5999d07c8d336905013026f2fff58fefb2464ef2068c8d4a5274784d4f625e6994005efea832d4f1048d14931174378209421f1d8eeaa9c6",
            HouseEdge = "1",
            GameType = EGameType.SINGLE,
            Finished = true,
            CreatedAt = 1756512186000L,
        };
    }
}
