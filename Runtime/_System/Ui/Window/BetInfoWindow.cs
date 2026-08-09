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
    // The bet info dialog, built into a UiWindow: what one bet was, what it paid, who made it, and the seeds
    // it was rolled from.
    //
    //     var info = BetInfoWindow.Create(canvas);
    //     info.Show(betId);
    //
    // Show emits BET_INFO through Emitter and opens the window on a row of pulsing dots. The answer arrives
    // as ON_BET_INFO_ID, which the socket puts in MainState.BetInfoById and announces as OnBetInfoById; this
    // listens for that and fills itself in. Nothing is polled, and nothing is shown that the server has not
    // sent - the window has no opinion of its own about what a bet was worth.
    //
    // What one bet *meant* is the game's business and cannot be known here, so the fields stop at what every
    // transaction carries and the rest is left open: whatever is parented into Outcome is laid out as a
    // full-width block under them, and OnTransaction is where a game fills it in. TransactionPublic carries
    // the raw Outcome and Custom payloads for exactly that.
    //
    // The layout is a UiGrid rather than arithmetic: two columns, one auto row per block, and a nested grid
    // per field. Widths are settled before heights are measured, so a bet id that wraps to three lines and a
    // game's own outcome view both make the card taller without anything here having to measure them.
    [AddComponentMenu("UI/Bet Info Window")]
    [RequireComponent(typeof(UiWindow))]
    public class BetInfoWindow : MonoBehaviour
    {
        // Below this a timestamp cannot be milliseconds - it would be 1973 - so it is seconds. The server
        // sends milliseconds and the web front reads them as such; this only keeps a seconds feed from
        // printing 1970 for every bet ever made.
        private const long MillisecondFloor = 100000000000L;

        // What each cell answers to in its grid's layout. One word each, and that is a requirement rather than
        // a habit: a layout is stored as text, so a name with a space in it comes back out as two cells.
        private const string CardArea = "card";
        private const string SeedsArea = "seeds";
        private const string BarArea = "bar";
        private const string ProfitArea = "profit";
        private const string RuleArea = "rule";
        private const string BetArea = "bet";
        private const string PayoutArea = "payout";
        private const string PlayerArea = "player";
        private const string IdArea = "id";
        private const string GameArea = "game";
        private const string TimeArea = "time";
        private const string OutcomeArea = "outcome";
        private const string LoaderArea = "loader";
        private const string SeedArea = "seed";
        private const string DetailsArea = "details";
        private const string VerifyArea = "verify";

        [SerializeField]
        private BetInfoWindowStyle style = new BetInfoWindowStyle();

        [Header("Labels")]
        // Serialized rather than translated, the same as StatisticsWindow next door: the template has no
        // opinion about the game's language, and a game that has one already knows where its strings live.
        [SerializeField]
        private string profitLabel = "Profit";

        [SerializeField]
        private string betLabel = "Bet";

        [SerializeField]
        private string payoutLabel = "Payout";

        [SerializeField]
        private string playerLabel = "Player";

        [SerializeField]
        private string betIdLabel = "Bet's id";

        [SerializeField]
        private string gameLabel = "Game";

        [SerializeField]
        private string timeLabel = "Time";

        [SerializeField]
        private string detailsLabel = "Details";

        [SerializeField]
        private string verifyLabel = "Verify";

        [SerializeField]
        private string nonceLabel = "Nonce";

        [SerializeField]
        private string clientSeedLabel = "Client seed";

        [Tooltip("Stands in for the client seed on a shared or multiplayer game, where the roll is seeded from a block hash rather than from anything one player sent.")]
        [SerializeField]
        private string blockHashLabel = "Bitcoin last block hash";

        [SerializeField]
        private string serverSeedLabel = "Server seed";

        [SerializeField]
        private string serverShaLabel = "Server SHA-512";

        [Tooltip("Printed where the server seed would be while the bet's seed is still in play and cannot be given out.")]
        [SerializeField]
        private string hiddenLabel = "Hidden";

        [Header("Blocks")]
        [Tooltip("The banner across the top: what the bet came to, green above a payout of one and red below it.")]
        [SerializeField]
        private bool showProfit = true;

        [Tooltip("The bet amount and the payout multiplier, which share a row.")]
        [SerializeField]
        private bool showBetPayout = true;

        [Tooltip("The player and the bet's id, which share a row.")]
        [SerializeField]
        private bool showPlayer = true;

        [Tooltip("The game and the time, which share a row.")]
        [SerializeField]
        private bool showGame = true;

        [Tooltip("The Details button and the seeds it opens.")]
        [SerializeField]
        private bool showDetails = true;

        [Tooltip("The Verify button. It appears only on a transaction the server sent a verify url with.")]
        [SerializeField]
        private bool showVerify = true;

        [SerializeField]
        private bool detailsOpen = false;

        [Tooltip("Height of the block a game's own view is given. Zero measures whatever is parented into Outcome, which needs that thing to report a height of its own - a layout group, a label, a Layout Element. A plain panel reports nothing, so give it a height here.")]
        [Min(0f)]
        [SerializeField]
        private float outcomeHeight = 0f;

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

        [Tooltip("Fetch the currency, game and avatar pictures the server sends as urls. Off leaves the drawn stand-ins showing.")]
        [SerializeField]
        private bool loadImages = true;

        [Tooltip("Resize the window to exactly the blocks it is showing.")]
        [SerializeField]
        private bool fitWindowHeight = true;

        [Tooltip("Close the seeds when the window closes, so a dialog reopened on another bet starts the way it first opened. Off leaves them open, and the window comes back the height they need.")]
        [SerializeField]
        private bool collapseDetailsOnClose = true;

        [Header("Events")]
        [Tooltip("A transaction has arrived and the fields are filled in. Where a game fills in Outcome.")]
        public UnityEvent<TransactionPublic> OnTransaction = new UnityEvent<TransactionPublic>();

        [Tooltip("An id has been asked of the server, before the answer.")]
        public UnityEvent<string> OnRequested = new UnityEvent<string>();

        [Tooltip("The verify url, as the browser is being sent to it.")]
        public UnityEvent<string> OnVerify = new UnityEvent<string>();

        public UnityEvent<bool> OnDetailsToggled = new UnityEvent<bool>();

        // Parts, and the flag saying they exist, are deliberately not serialized - the same choice
        // StatisticsWindow makes. Everything is found by name before it is made, so a rebuild after a script
        // reload finds the hierarchy that is already there rather than building a second one beside it.
        private UiWindow window;

        private RoundedBox card;
        private UiGrid cardGrid;

        private RoundedBox profitBox;
        private UiGrid profitGrid;
        private TextMeshProUGUI profitCaption;
        private UiGrid profitRow;
        private Badge profitCoin;
        private TextMeshProUGUI profitAmount;

        private readonly RoundedBox[] rules = new RoundedBox[3];

        private Field bet;
        private Field payout;
        private Field player;
        private Field betId;
        private Field game;
        private Field time;

        private UiGrid outcome;

        private RectTransform loader;
        private UiGrid loaderGrid;
        private readonly RoundedBox[] dots = new RoundedBox[3];
        private readonly List<Tween> pulses = new List<Tween>();

        private RoundedBox seedsCard;
        private UiGrid seedsGrid;
        private readonly List<Field> seeds = new List<Field>();

        private RectTransform bar;
        private UiGrid barGrid;
        private RoundedBox detailsBox;
        private Button detailsButton;
        private TextMeshProUGUI detailsText;
        private RoundedBox verifyBox;
        private Button verifyButton;
        private TextMeshProUGUI verifyText;

        // Row lists are built per layout pass rather than fixed: a gap between two tracks is there whether or
        // not anything is in them, so a block that is switched off has to take its row away with it.
        private readonly List<GridTrack> cardRows = new List<GridTrack>();
        private readonly List<GridTrack> contentRows = new List<GridTrack>();
        private readonly List<GridTrack> seedRows = new List<GridTrack>();

        // What is showing, decided in Refresh and said to the grids in Arrange.
        private bool hasData;
        private bool profitOn;
        private bool betOn;
        private bool playerOn;
        private bool gameOn;
        private bool outcomeOn;
        private bool seedsOn;
        private bool detailsOn;
        private bool verifyOn;
        private readonly bool[] seedOn = new bool[4];

        private TransactionPublic transaction;
        private TransactionPublic preview;
        private string requestedId = string.Empty;
        private bool built;
        private bool listening;

        // Bumped whenever the window is pointed at another bet, so a picture that arrives after the player
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

        /// <summary>Colours, sizes and fonts of the card and its fields. Edit and call <see cref="Rebuild"/>,
        /// or assign a whole new one.</summary>
        public BetInfoWindowStyle Style
        {
            get => style;
            set
            {
                style = value ?? new BetInfoWindowStyle();
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
                // template already has. Only for the id that was asked for: anything else is another
                // window's answer.
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

        /// <summary>A full-width block under the fields for the game's own view of the bet - what the dice
        /// rolled, where the ball fell. Parent anything into it and the card grows around it.</summary>
        // The one thing the template cannot fill in for a game, so it is a container rather than a shape: a
        // grid of one column, so several things stacked into it come out in order, and an auto row each so
        // each is as tall as it says it needs to be.
        //
        // Deliberately the only way in. The card itself is arranged by a layout that names its cells, and a
        // grid hides what its layout does not name - so a panel parented straight into the card would be
        // switched off the moment the layout was set again. This one is named, and what is inside it is the
        // game's business.
        public RectTransform Outcome
        {
            get
            {
                EnsureBuilt();
                return Rect(outcome);
            }
        }

        /// <summary>The card behind the fields, for anything the style does not reach.</summary>
        public RoundedBox Card
        {
            get
            {
                EnsureBuilt();
                return card;
            }
        }

        /// <summary>The Details button, for a game that would rather drive it itself.</summary>
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

        /// <summary>Builds the whole thing - window, card and all - under a parent.</summary>
        // Added before the object wakes, for the reason UiWindowBuilder.Add exists: a component on an active
        // object runs its Awake there and then, and the card would go into a window that had already put
        // itself away.
        public static BetInfoWindow Create(Transform parent, string name = "Bet Info", string title = "Bet info")
        {
            UiWindowBuilder.Create(parent, name)
                .Size(560f, 620f)
                .Title(title)
                .Add(out BetInfoWindow info)
                .Done();

            // Awake has done this already in play mode. In the editor nothing else ever will, and a window
            // built from a context menu that came out empty would be a poor way to find that out.
            if (info != null)
                info.EnsureBuilt();

            return info;
        }

        void Awake()
        {
            EnsureBuilt();
        }

        void OnEnable()
        {
            EnsureBuilt();
            Listen(true);
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

        /// <summary>Builds the card, the fields, the seeds and the buttons from scratch, then redraws. Call
        /// after changing the style from code.</summary>
        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            if (style == null)
                style = new BetInfoWindowStyle();

            var host = Window;
            if (host == null)
                return;

            host.EnsureBuilt();
            host.ApplyStyle();

            BuildParts(host.Content);

            built = true;
            Refresh();
        }

        /// <summary>Asks the server for a bet and opens the window on it.</summary>
        // The request goes out and the window opens on the loader rather than waiting for the answer and
        // opening on a filled card: a dialog that appears half a second after the press reads as a dropped
        // press. Where the state already holds this bet - the player is looking at the same one again - it is
        // used at once and nothing is asked.
        public void Show(string id)
        {
            requestedId = id ?? string.Empty;
            transaction = null;
            stamp++;

            var known = Known();
            if (known != null && (!matchRequestedId || known.Id == requestedId))
                transaction = known;

            if (transaction == null && requestOnShow && !string.IsNullOrEmpty(requestedId) && Emitter.Inst != null)
            {
                Emitter.Inst.OnBetInfo(requestedId);
                OnRequested.Invoke(requestedId);
            }

            EnsureBuilt();
            Refresh();
            Window.Open();
        }

        /// <summary>Opens the window on a transaction the game already has, without asking the server.</summary>
        public void Show(TransactionPublic value)
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
            stamp++;
            Refresh();
        }

        public void ToggleDetails() => DetailsOpen = !detailsOpen;

        /// <summary>Sends the browser to the server's verifier with this bet's seeds in the query, which is
        /// what makes a roll checkable from outside the game.</summary>
        public void Verify()
        {
            var data = Transaction;
            if (data == null || string.IsNullOrEmpty(data.VerifyUrl))
                return;

            var url = VerifyUrl(data);
            OnVerify.Invoke(url);
            Application.OpenURL(url);
        }

        /// <summary>Fills every field in from the transaction and lays the card out again.</summary>
        public void Refresh()
        {
            if (!built)
                return;

            var data = Transaction;

            // Which blocks are showing is worked out here and said to the grids in Arrange, as a layout.
            // Deliberately not SetActive: a UiGrid shows what its layout names and hides what it does not, and
            // it re-asserts that every time it is enabled - so a cell switched off behind the grid's back comes
            // back on the next time the window opens. The layout is the only thing a grid will not argue with.
            hasData = data != null;
            profitOn = hasData && showProfit;
            betOn = hasData && showBetPayout;
            playerOn = hasData && showPlayer;
            gameOn = hasData && showGame;
            outcomeOn = hasData && Rect(outcome).childCount > 0;
            detailsOn = hasData && showDetails;
            verifyOn = hasData && showVerify && !string.IsNullOrEmpty(data.VerifyUrl);

            if (hasData)
                Write(data);

            seedsOn = detailsOn && detailsOpen && Shown(seedOn);

            Pulse(!hasData);
            Layout();

            // Two more passes over the next two frames. Everything the layout needs is measured inside
            // Layout, except what only the canvas can settle - a font that finished loading, a picture that
            // arrived, a rect that had no width yet because the window was activated this frame. Cheap, and
            // it is the difference between a dialog that fits and one with a card hanging out of it.
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

            PaintCard();
            PaintBlocks();
            PaintSeeds();
            PaintBar();
            Arrange();
            FitWindow();
        }

        // ------------------------------------------------------------------ building

        private void BuildParts(RectTransform content)
        {
            GridOn(content);

            card = UiWindowParts.Box(content, "Card");
            cardGrid = GridOn(card.rectTransform);

            profitBox = UiWindowParts.Box(card.transform, "Profit");
            profitGrid = GridOn(profitBox.rectTransform);
            profitCaption = UiWindowParts.Label(profitBox.transform, "Caption");
            profitRow = Grid(profitBox.transform, "Amount");
            profitCoin = BuildBadge(profitRow.transform, "Coin");
            profitAmount = UiWindowParts.Label(profitRow.transform, "Value");

            // One rule per gap between the pairs. Built up front and switched off when there is no gap for it
            // to sit in, rather than made and destroyed as the blocks come and go.
            for (int i = 0; i < rules.Length; i++)
                rules[i] = UiWindowParts.Box(card.transform, "Rule " + i);

            bet = BuildField(card.transform, "Bet", EFieldShape.CenteredIcon, EFieldIcon.Picture);
            payout = BuildField(card.transform, "Payout", EFieldShape.PlainValue, EFieldIcon.None);
            player = BuildField(card.transform, "Player", EFieldShape.CenteredIcon, EFieldIcon.Picture);
            betId = BuildField(card.transform, "Bet Id", EFieldShape.WideValue, EFieldIcon.Glyph);
            game = BuildField(card.transform, "Game", EFieldShape.PictureBeside, EFieldIcon.Picture);
            time = BuildField(card.transform, "Time", EFieldShape.CenteredIcon, EFieldIcon.Glyph);

            outcome = Grid(card.transform, "Outcome");

            loader = Rect(Grid(card.transform, "Loader"));
            loaderGrid = loader.GetComponent<UiGrid>();
            for (int i = 0; i < dots.Length; i++)
                dots[i] = UiWindowParts.Box(loader, "Dot " + i);

            seedsCard = UiWindowParts.Box(content, "Seeds");
            seedsGrid = GridOn(seedsCard.rectTransform);

            seeds.Clear();
            for (int i = 0; i < 4; i++)
                seeds.Add(BuildField(seedsCard.transform, "Seed " + i, EFieldShape.WideValue, EFieldIcon.None));

            bar = Rect(Grid(content, "Bar"));
            barGrid = bar.GetComponent<UiGrid>();

            detailsBox = UiWindowParts.Box(bar, "Details");
            detailsButton = Hook(detailsBox, ToggleDetails);
            detailsText = UiWindowParts.Label(detailsBox.transform, "Label");

            verifyBox = UiWindowParts.Box(bar, "Verify");
            verifyButton = Hook(verifyBox, Verify);
            verifyText = UiWindowParts.Label(verifyBox.transform, "Label");

            // Same Remove-then-Add as the buttons, and here for the same reason: AddListener is not
            // serialized, so this belongs where the parts are wired rather than where they are made.
            var host = Window;
            host.OnClosed.RemoveListener(HandleClosed);
            host.OnClosed.AddListener(HandleClosed);

            // What every cell answers to in its grid's layout. Set once, here, where the parts are: a name is
            // a property of the panel, and Arrange only draws the picture that uses them.
            Named(card.rectTransform, CardArea);
            Named(seedsCard.rectTransform, SeedsArea);
            Named(bar, BarArea);

            Named(profitBox.rectTransform, ProfitArea);

            for (int i = 0; i < rules.Length; i++)
                Named(rules[i].rectTransform, RuleArea + i.ToString(CultureInfo.InvariantCulture));

            Named(Rect(bet.Root), BetArea);
            Named(Rect(payout.Root), PayoutArea);
            Named(Rect(player.Root), PlayerArea);
            Named(Rect(betId.Root), IdArea);
            Named(Rect(game.Root), GameArea);
            Named(Rect(time.Root), TimeArea);
            Named(Rect(outcome), OutcomeArea);
            Named(loader, LoaderArea);

            for (int i = 0; i < seeds.Count; i++)
                Named(Rect(seeds[i].Root), SeedArea + i.ToString(CultureInfo.InvariantCulture));
        }

        // A window that closed with its seeds open would come back open on them - and come back the height
        // the last bet's seeds needed, which is not the height this one's do. The flag alone: the object is
        // already inactive by the time this fires, and OnEnable refreshes on the way back in.
        private void HandleClosed()
        {
            if (collapseDetailsOnClose)
                detailsOpen = false;
        }

        // What a field is made of. Every one is a caption and a value; what differs is whether something
        // stands beside the value, whether the value wraps, and - for the game - whether the picture stands
        // beside the caption as well.
        private enum EFieldShape
        {
            /// <summary>A caption over a value with nothing beside it.</summary>
            PlainValue,

            /// <summary>A caption over an icon and a value, the pair held in the middle.</summary>
            CenteredIcon,

            /// <summary>A caption over an icon and a value that takes the rest of the column and wraps.</summary>
            WideValue,

            /// <summary>A picture, with the caption and value stacked beside it.</summary>
            PictureBeside,
        }

        private enum EFieldIcon
        {
            None,

            /// <summary>A plate with a letter on it and the server's picture over the top.</summary>
            Picture,

            /// <summary>Something drawn from boxes - a tick, a clock.</summary>
            Glyph,
        }

        private class Field
        {
            public EFieldShape Shape;
            public UiGrid Root;
            public UiGrid Inner;
            public TextMeshProUGUI Caption;
            public TextMeshProUGUI Value;
            public Badge Picture;
            public Glyph Mark;
        }

        // A round or rounded plate with a letter on it and a picture over the top: a currency coin, a player's
        // avatar, a game's thumbnail. The plate is what is on screen until - or unless - the server's image
        // arrives, so a window with no network still reads as a window.
        private class Badge
        {
            public RectTransform Root;
            public RoundedBox Plate;
            public TextMeshProUGUI Letter;
            public Image Picture;
        }

        // A tick or a clock, drawn from a disc, a ring and two bars rather than fetched from an atlas - the
        // same choice the close cross and the reset arrow make next door, and for the same reason: it costs no
        // atlas entry and stays sharp at any size.
        private class Glyph
        {
            public RectTransform Root;
            public RoundedBox Plate;
            public RoundedBox Ring;
            public RoundedBox BarA;
            public RoundedBox BarB;
            public Image Picture;
        }

        private Field BuildField(Transform parent, string name, EFieldShape shape, EFieldIcon icon)
        {
            var field = new Field { Shape = shape, Root = Grid(parent, name) };

            if (shape == EFieldShape.PictureBeside)
            {
                field.Picture = BuildBadge(field.Root.transform, "Picture");
                field.Inner = Grid(field.Root.transform, "Text");
                field.Caption = UiWindowParts.Label(field.Inner.transform, "Caption");
                field.Value = UiWindowParts.Label(field.Inner.transform, "Value");
                return field;
            }

            field.Caption = UiWindowParts.Label(field.Root.transform, "Caption");
            field.Inner = Grid(field.Root.transform, "Value Row");

            // Before the label, because the two are cells of the same row and a row reads left to right.
            if (icon == EFieldIcon.Picture)
                field.Picture = BuildBadge(field.Inner.transform, "Picture");
            else if (icon == EFieldIcon.Glyph)
                field.Mark = BuildGlyph(field.Inner.transform, "Mark");

            field.Value = UiWindowParts.Label(field.Inner.transform, "Value");
            return field;
        }

        private Badge BuildBadge(Transform parent, string name)
        {
            var badge = new Badge { Root = UiWindowParts.Rect(parent, name) };
            badge.Plate = UiWindowParts.Box(badge.Root, "Plate");
            badge.Letter = UiWindowParts.Label(badge.Plate.transform, "Letter");
            badge.Picture = UiWindowParts.Picture(badge.Root, "Picture");
            return badge;
        }

        private Glyph BuildGlyph(Transform parent, string name)
        {
            var glyph = new Glyph { Root = UiWindowParts.Rect(parent, name) };
            glyph.Plate = UiWindowParts.Box(glyph.Root, "Plate");
            glyph.Ring = UiWindowParts.Box(glyph.Root, "Ring");
            glyph.BarA = UiWindowParts.Box(glyph.Root, "Bar A");
            glyph.BarB = UiWindowParts.Box(glyph.Root, "Bar B");
            glyph.Picture = UiWindowParts.Picture(glyph.Root, "Picture");
            return glyph;
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

        private void PaintCard()
        {
            var contentGrid = GridOn(Window.Content);
            Columns(contentGrid, GridTrack.Flexible());
            contentGrid.RowGap = style.SectionGap;
            contentGrid.ColumnGap = 0f;
            contentGrid.padding = new RectOffset(0, 0, 0, 0);

            Paint(card, style.CardFill, style.CardCornerRadius);
            Paint(seedsCard, style.CardFill, style.CardCornerRadius);

            int pad = Mathf.RoundToInt(style.CardPadding);
            cardGrid.padding = new RectOffset(pad, pad, pad, pad);
            cardGrid.RowGap = style.SectionGap;
            cardGrid.ColumnGap = style.ColumnGap;
            Columns(cardGrid, GridTrack.Flexible(), GridTrack.Flexible());

            seedsGrid.padding = new RectOffset(pad, pad, pad, pad);
            seedsGrid.RowGap = style.SeedRowGap;
            seedsGrid.ColumnGap = 0f;
            Columns(seedsGrid, GridTrack.Flexible());
        }

        private void PaintBlocks()
        {
            // The banner. Its height is its contents plus its padding rather than a number, so a longer
            // currency code or a larger font makes it taller instead of being clipped by it.
            int pad = Mathf.RoundToInt(style.CardPadding);
            profitGrid.padding = new RectOffset(pad, pad, pad, pad);
            profitGrid.RowGap = style.CaptionGap;
            profitGrid.ColumnGap = 0f;
            Columns(profitGrid, GridTrack.Flexible());
            Rows(profitGrid, GridTrack.Auto(), GridTrack.Auto());
            Put(profitCaption.rectTransform, 0, 0);
            Put(Rect(profitRow), 0, 1);

            // The fill is the transaction's business - green above a payout of one, red below it - so only
            // the shape is set here and whatever Write last chose is kept.
            Paint(profitBox, profitBox.FillColor, style.ProfitCornerRadius);

            Label(profitCaption, style.CaptionFont, style.ProfitCaptionSize, style.ProfitTextColor, style.CaptionStyle);
            Label(profitAmount, style.ValueFont, style.ProfitAmountSize, style.ProfitTextColor, style.ValueStyle);

            CenteredRow(profitRow, style.ProfitCoinSize, profitCoin.Root, profitAmount);
            Coin(profitCoin, style.ProfitCoinSize);

            foreach (var rule in rules)
            {
                Paint(rule, style.LineColor, 0f);
                rule.EdgeSoftness = 0f;
            }

            Shape(bet, style.CoinSize);
            Shape(payout, 0f);
            Shape(player, style.AvatarSize);
            Shape(betId, style.TickSize);
            Shape(game, 0f);
            Shape(time, style.ClockSize);

            Coin(bet.Picture, style.CoinSize);
            Avatar(player.Picture, style.AvatarSize);
            Thumbnail(game.Picture);
            Tick(betId.Mark, style.TickSize);
            Clock(time.Mark, style.ClockSize);

            // One column and an auto row, so whatever a game stacks in here comes out in hierarchy order with
            // each block as tall as it reports itself to be. Rows past the first are implicit, and implicit
            // rows are auto - a game adding a second view does not have to say so here.
            Columns(outcome, GridTrack.Flexible());
            Rows(outcome, GridTrack.Auto());
            outcome.RowGap = style.SectionGap;
            outcome.ColumnGap = 0f;
            outcome.padding = new RectOffset(0, 0, 0, 0);

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

        // The shape of one field: the tracks of the column it stands in, and the tracks of the row inside it.
        private void Shape(Field field, float iconSize)
        {
            field.Root.padding = new RectOffset(0, 0, 0, 0);
            field.Inner.padding = new RectOffset(0, 0, 0, 0);

            Label(field.Caption, style.CaptionFont, style.CaptionSize, style.CaptionColor, style.CaptionStyle);

            if (field.Shape == EFieldShape.PictureBeside)
            {
                // A thumbnail with the caption and the name stacked beside it, which is how the design draws
                // the game a bet was made in.
                Columns(field.Root, GridTrack.Fixed(style.GameImageSize.x), GridTrack.Flexible());
                Rows(field.Root, GridTrack.Auto());
                field.Root.ColumnGap = style.IconGap;
                Middle(field.Picture.Root, 0, 0, style.GameImageSize);

                Columns(field.Inner, GridTrack.Flexible());
                Rows(field.Inner, GridTrack.Auto(), GridTrack.Auto());
                field.Inner.RowGap = style.CaptionGap;
                field.Inner.ColumnGap = 0f;
                Put(Rect(field.Inner), 1, 0);
                Put(field.Caption.rectTransform, 0, 0);
                Put(field.Value.rectTransform, 0, 1);

                Label(field.Value, style.ValueFont, style.ValueSize, style.ValueColor, style.ValueStyle);
                field.Caption.alignment = TextAlignmentOptions.Left;
                field.Value.alignment = TextAlignmentOptions.Left;
                return;
            }

            Columns(field.Root, GridTrack.Flexible());
            Rows(field.Root, GridTrack.Auto(), GridTrack.Auto());
            field.Root.RowGap = style.CaptionGap;
            field.Root.ColumnGap = 0f;
            Put(field.Caption.rectTransform, 0, 0);
            Put(Rect(field.Inner), 0, 1);

            switch (field.Shape)
            {
                case EFieldShape.WideValue:
                    // The long strings: a bet id, a seed, a hash. The value takes whatever the icon leaves and
                    // wraps inside it, and the row is as tall as the wrapping made it.
                    Label(field.Value, style.ValueFont, style.HashSize, style.ValueColor, FontStyles.Normal);
                    Rows(field.Inner, GridTrack.Auto());
                    field.Inner.ColumnGap = style.IconGap;

                    if (field.Mark != null && iconSize > 0f)
                    {
                        Columns(field.Inner, GridTrack.Fixed(iconSize), GridTrack.Flexible());
                        Middle(field.Mark.Root, 0, 0, new Vector2(iconSize, iconSize));
                        Put(field.Value.rectTransform, 1, 0);
                    }
                    else
                    {
                        Columns(field.Inner, GridTrack.Flexible());
                        Put(field.Value.rectTransform, 0, 0);
                    }

                    break;

                case EFieldShape.CenteredIcon:
                    Label(field.Value, style.ValueFont, style.AmountSize, style.ValueColor, style.ValueStyle);
                    var icon = field.Picture != null ? field.Picture.Root : field.Mark != null ? field.Mark.Root : null;
                    CenteredRow(field.Inner, iconSize, icon, field.Value);
                    break;

                default:
                    Label(field.Value, style.ValueFont, style.AmountSize, style.ValueColor, style.ValueStyle);
                    Columns(field.Inner, GridTrack.Flexible());
                    Rows(field.Inner, GridTrack.Auto());
                    field.Inner.ColumnGap = 0f;
                    Put(field.Value.rectTransform, 0, 0);
                    break;
            }
        }

        // An icon and a label held together in the middle of the column: a flexible track on each side and the
        // pair between them, which is what a grid has instead of a margin of auto.
        private void CenteredRow(UiGrid row, float iconSize, RectTransform icon, TextMeshProUGUI label)
        {
            bool iconOn = icon != null && iconSize > 0f;

            row.ColumnGap = style.IconGap;
            row.padding = new RectOffset(0, 0, 0, 0);
            Rows(row, GridTrack.Auto());

            if (iconOn)
            {
                Columns(row, GridTrack.Flexible(), GridTrack.Fixed(iconSize), GridTrack.Auto(), GridTrack.Flexible());
                Middle(icon, 1, 0, new Vector2(iconSize, iconSize));
                Put(label.rectTransform, 2, 0);
            }
            else
            {
                Columns(row, GridTrack.Flexible(), GridTrack.Auto(), GridTrack.Flexible());
                Put(label.rectTransform, 1, 0);
            }

            label.alignment = TextAlignmentOptions.Left;
        }

        private void PaintSeeds()
        {
            foreach (var field in seeds)
            {
                Shape(field, 0f);
                field.Caption.alignment = TextAlignmentOptions.Left;
                field.Value.alignment = TextAlignmentOptions.Left;
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
            label.text = text;
        }

        // ------------------------------------------------------------------ what is showing

        // Says the arrangement as a layout - a picture of the grid, one area name per cell - rather than by
        // switching cells on and off and placing them by number.
        //
        // That is not a stylistic preference. A UiGrid takes a layout as the whole truth about which of its
        // children are showing, and re-asserts it every time it is enabled and every time its child list
        // changes; a grid with no layout takes *every* child as showing and does the same. So a cell hidden
        // with SetActive comes back the next time the window opens - the loader over the profit banner, the
        // seeds under a card that was not sized for them. Naming the cells is the only way to be believed.
        private void Arrange()
        {
            ArrangeCard();
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

            // The card, the seeds and the bar down the content area, each as tall as it needs to be.
            contentRows.Clear();
            var content = UiGridLayout.Build().Columns(GridTrack.Flexible());

            content.Row(CardArea);
            contentRows.Add(GridTrack.Auto());

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
        }

        private void ArrangeCard()
        {
            cardRows.Clear();

            var card = UiGridLayout.Build().Columns(GridTrack.Flexible(), GridTrack.Flexible());

            if (!hasData)
            {
                // Nothing to show yet: the dots, and nothing else named, so every field is out of the way
                // rather than blank.
                card.Row(LoaderArea, LoaderArea);
                cardRows.Add(GridTrack.Fixed(style.LoaderHeight));
            }
            else
            {
                int rule = 0;

                if (profitOn)
                {
                    card.Row(ProfitArea, ProfitArea);
                    cardRows.Add(GridTrack.Auto());
                }

                Pair(card, betOn, BetArea, PayoutArea, ref rule);
                Pair(card, playerOn, PlayerArea, IdArea, ref rule);
                Pair(card, gameOn, GameArea, TimeArea, ref rule);

                if (outcomeOn)
                {
                    card.Row(OutcomeArea, OutcomeArea);
                    cardRows.Add(outcomeHeight > 0f ? GridTrack.Fixed(outcomeHeight) : GridTrack.Auto());
                }
            }

            cardGrid.SetLayout(card.Rows(cardRows.ToArray()).Done());
        }

        private void Pair(UiGridLayout.Builder card, bool shown, string left, string right, ref int rule)
        {
            if (!shown)
                return;

            // A rule between one pair and the next, and never above the first: a line across the top of the
            // card rules off nothing.
            if (cardRows.Count > 0 && rule < rules.Length)
            {
                var name = RuleArea + rule.ToString(CultureInfo.InvariantCulture);
                card.Row(name, name);
                cardRows.Add(GridTrack.Fixed(style.LineThickness));
                rule++;
            }

            card.Row(left, right);
            cardRows.Add(GridTrack.Auto());
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

        private void Write(TransactionPublic data)
        {
            int decimals = Decimals(data);
            decimal wagered = Number(data.BetAmount);
            decimal won = Number(data.WinAmount);

            profitCaption.text = profitLabel;
            profitAmount.text = Amount(won - wagered, decimals, data.Currency);

            // Green from a payout of one: the bet came back whole or better. Below it the player is down on
            // the bet, whatever the win amount says on its own.
            profitBox.FillColor = Number(data.Payout) >= 1m ? style.ProfitPositiveFill : style.ProfitNegativeFill;

            bet.Caption.text = betLabel;
            bet.Value.text = Amount(wagered, decimals, data.Currency);

            payout.Caption.text = payoutLabel;
            payout.Value.text = Money(Number(data.Payout), style.PayoutDecimals) + Small(" x");

            player.Caption.text = playerLabel;
            player.Value.text = string.IsNullOrEmpty(data.IPlayerName) ? data.IPlayerId : data.IPlayerName;

            betId.Caption.text = betIdLabel;
            betId.Value.text = data.Id;

            game.Caption.text = gameLabel;
            game.Value.text = data.GameName;

            time.Caption.text = timeLabel;
            time.Value.text = Moment(data.CreatedAt);

            Letter(profitCoin, data.Currency);
            Letter(bet.Picture, data.Currency);
            Letter(player.Picture, player.Value.text);
            Letter(game.Picture, data.GameName);

            Fetch(profitCoin, data.CurrencyImage);
            Fetch(bet.Picture, data.CurrencyImage);
            Fetch(player.Picture, data.CImg);
            Fetch(game.Picture, data.GameImg);

            WriteSeeds(data);
        }

        // Nonce, the seed the player contributed, the seed the server kept, and the hash it published before
        // the roll. Between them they are what makes a bet checkable after the fact, which is the only reason
        // the dialog has a Details half at all.
        private void WriteSeeds(TransactionPublic data)
        {
            // A single-player roll is seeded from a client salt and counted by a nonce. A shared or
            // multiplayer round is seeded from a block hash nobody can have known in advance, where a nonce
            // per player would mean nothing.
            bool single = data.GameType == EGameType.SINGLE;

            Seed(0, nonceLabel, string.IsNullOrEmpty(data.Nonce) ? "0" : data.Nonce, single);
            Seed(1, single ? clientSeedLabel : blockHashLabel, data.ClientSalt, true);
            Seed(2, serverSeedLabel, string.IsNullOrEmpty(data.ServerSeed) ? hiddenLabel : data.ServerSeed, true);
            Seed(3, serverShaLabel, data.ServerSeedSha512 ?? string.Empty, true);
        }

        private void Seed(int index, string caption, string value, bool shown)
        {
            if (index >= seeds.Count || index >= seedOn.Length)
                return;

            var field = seeds[index];
            seedOn[index] = shown;
            field.Caption.text = caption;
            field.Value.text = value;
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
            Refresh();

            if (value != null)
                OnTransaction.Invoke(value);
        }

        private void Listen(bool on)
        {
            var manager = StateManager.Inst;
            if (manager == null || manager.Events == null || !followState)
                return;

            if (on && !listening)
            {
                manager.Events.OnBetInfoById.AddListener(HandleBetInfo);
                listening = true;
            }
            else if (!on && listening)
            {
                manager.Events.OnBetInfoById.RemoveListener(HandleBetInfo);
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

        // ------------------------------------------------------------------ formatting

        private int Decimals(TransactionPublic data)
        {
            if (style.Decimals >= 0)
                return Mathf.Clamp(style.Decimals, 0, 18);

            // Zero is what the field says when the server has not filled it in, and printing money to no
            // decimals at all would turn every satoshi into 0. Eight is what the web front falls back to.
            return data.DecimalPoints > 0 ? Mathf.Min(data.DecimalPoints, 18) : 8;
        }

        private string Amount(decimal value, int decimals, string currency)
        {
            string money = Money(value, decimals);
            return string.IsNullOrEmpty(currency) ? money : Small(currency + " ") + money;
        }

        // Money is truncated rather than rounded, and only then trimmed - the same order the web front does it
        // in, so the two show the same number for the same bet. A figure that will not parse is printed exactly
        // as it came, which is the only honest thing to do with one from the server.
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

        /// <summary>The currency code beside an amount, and the x after a payout: the same text, smaller.</summary>
        private string Small(string text) =>
            "<size=" + Mathf.RoundToInt(style.SmallTextScale * 100f).ToString(CultureInfo.InvariantCulture) + "%>" + text + "</size>";

        private string Moment(long created)
        {
            var moment = created < MillisecondFloor
                ? DateTimeOffset.FromUnixTimeSeconds(created)
                : DateTimeOffset.FromUnixTimeMilliseconds(created);

            if (style.LocalTime)
                moment = moment.ToLocalTime();

            // The day bold and the time beside it, the way the design draws it. The label itself is not bold,
            // so the tag makes the difference rather than undoing it.
            return "<b>" + moment.ToString(style.DayFormat, CultureInfo.InvariantCulture) + "</b> "
                + moment.ToString(style.TimeFormat, CultureInfo.InvariantCulture);
        }

        // The query the verifier expects: both seeds in base64, the nonce, and the house edge the roll was made
        // under. Deliberately unescaped past that, because it is the string the web front builds and the two
        // have to agree.
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

        // ------------------------------------------------------------------ pictures and glyphs

        private void Coin(Badge badge, float size) =>
            PaintBadge(badge, style.CoinFill, 100000f, style.CoinLetterColor, size * 0.5f);

        private void Avatar(Badge badge, float size) =>
            PaintBadge(badge, style.AvatarFill, 100000f, style.AvatarLetterColor, size * 0.5f);

        private void Thumbnail(Badge badge) =>
            PaintBadge(badge, style.GameImageFill, style.GameImageCornerRadius, style.AvatarLetterColor, style.GameImageSize.y * 0.4f);

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
                // another bet since - a different currency, a different game - and this picture is no longer
                // the one that belongs there.
                if (this == null || badge.Picture == null || asked != stamp)
                    return;

                badge.Picture.sprite = sprite;
                badge.Picture.gameObject.SetActive(sprite != null);
                badge.Letter.gameObject.SetActive(sprite == null);
            });
        }

        private void Tick(Glyph glyph, float span)
        {
            if (glyph == null)
                return;

            bool sprite = UseSprite(glyph, style.TickIcon);

            glyph.Plate.gameObject.SetActive(!sprite);
            glyph.Ring.gameObject.SetActive(false);
            glyph.BarA.gameObject.SetActive(!sprite);
            glyph.BarB.gameObject.SetActive(!sprite);

            if (sprite)
                return;

            UiWindowParts.Stretch(glyph.Plate.rectTransform, 0f, 0f, 0f, 0f);
            Paint(glyph.Plate, style.TickFill, 100000f);

            // Three points and two strokes between them, measured as fractions of the disc so the tick keeps
            // its shape at any size.
            var start = new Vector2(-0.24f, 0.03f) * span;
            var bottom = new Vector2(-0.06f, -0.17f) * span;
            var end = new Vector2(0.26f, 0.19f) * span;

            Stroke(glyph.BarA, start, bottom, style.TickThickness, style.TickMarkColor);
            Stroke(glyph.BarB, bottom, end, style.TickThickness, style.TickMarkColor);
        }

        private void Clock(Glyph glyph, float span)
        {
            if (glyph == null)
                return;

            bool sprite = UseSprite(glyph, style.ClockIcon);

            glyph.Plate.gameObject.SetActive(false);
            glyph.Ring.gameObject.SetActive(!sprite);
            glyph.BarA.gameObject.SetActive(!sprite);
            glyph.BarB.gameObject.SetActive(!sprite);

            if (sprite)
                return;

            // A rim and two hands. The rim is a circle with nothing in it - border only - which is what a
            // RoundedBox gives for free and a sprite would need an atlas entry for.
            UiWindowParts.Stretch(glyph.Ring.rectTransform, 0f, 0f, 0f, 0f);
            glyph.Ring.FillGradientMode = EFillGradient.None;
            glyph.Ring.FillColor = Color.clear;
            glyph.Ring.SetCornerRadius(100000f);
            glyph.Ring.SetBorderSize(style.ClockThickness);
            glyph.Ring.SetBorderColor(style.ClockColor);
            glyph.Ring.EdgeSoftness = 1f;
            glyph.Ring.raycastTarget = false;

            Stroke(glyph.BarA, Vector2.zero, new Vector2(0f, 0.27f) * span, style.ClockThickness, style.ClockColor);
            Stroke(glyph.BarB, Vector2.zero, new Vector2(0.18f, 0.06f) * span, style.ClockThickness, style.ClockColor);
        }

        private bool UseSprite(Glyph glyph, Sprite icon)
        {
            bool on = icon != null;

            glyph.Picture.gameObject.SetActive(on);
            glyph.Picture.sprite = icon;
            glyph.Picture.preserveAspect = true;
            glyph.Picture.raycastTarget = false;
            UiWindowParts.Stretch(glyph.Picture.rectTransform, 0f, 0f, 0f, 0f);
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
                // is compiled into the project's own assembly and cannot be reached from a package. UiWindow
                // and Toast do the same thing for the same reason.
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

        // The window is sized from the card out rather than the other way round: how tall a bet info dialog has
        // to be depends on how far the bet id wrapped and on what the game put in Outcome, neither of which
        // anything here knows in advance. The content area is a grid, so the window can measure it itself - and
        // hold the answer to what the screen has room for, scrolling the rest.
        //
        // Twice, and that is not belt and braces. A label reports the height it needs at the width it has, and
        // its width is what the first pass settles - so on the way into a window that has just been activated a
        // wrapped hash measures as one line, the card is fitted to that, and the seeds and the buttons under
        // them are left hanging below the panel. The second pass measures against the widths the first wrote.
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

        private static UiGridItem Item(RectTransform rect)
        {
            var item = rect.GetComponent<UiGridItem>();
            if (item == null)
                item = rect.gameObject.AddComponent<UiGridItem>();

            return item;
        }

        /// <summary>Puts something in a cell, stretched to fill it.</summary>
        // Every cell here is placed by hand rather than left to the flow: the row a block lands in depends on
        // which blocks above it are showing, and the flow would only know the order.
        private static void Put(RectTransform rect, int column, int row, int span = 1)
        {
            var item = Item(rect);
            item.PlaceAt(column, row);
            item.Span(span, 1);
            item.OverrideAlign = false;
        }

        /// <summary>Puts something of its own size in the middle of a cell. For anything square - a coin, a
        /// tick - which a stretch would pull out of shape.</summary>
        private static void Middle(RectTransform rect, int column, int row, Vector2 size)
        {
            var item = Item(rect);
            item.PlaceAt(column, row);
            item.Span(1, 1);
            Centred(item);
            Size(rect, size);
        }

        /// <summary>Gives a cell the name its grid's layout knows it by. The layout places it; this only says
        /// which cell of the picture is which panel.</summary>
        private static void Named(RectTransform rect, string area)
        {
            var item = Item(rect);
            item.Area = area;
            item.OverrideAlign = false;
        }

        /// <summary>The same, for a cell that keeps its own size and sits in the middle of what it is given.</summary>
        private static void Named(RectTransform rect, string area, Vector2 size)
        {
            var item = Item(rect);
            item.Area = area;
            Centred(item);
            Size(rect, size);
        }

        private static void Centred(UiGridItem item)
        {
            item.OverrideAlign = true;
            item.HorizontalAlign = EGridAlign.Center;
            item.VerticalAlign = EGridAlign.Center;
        }

        private static void Size(RectTransform rect, Vector2 size)
        {
            rect.sizeDelta = size;

            // An auto row is as tall as the items in it say they need to be, and a plate or a picture says
            // nothing at all - so a thumbnail taller than the text beside it would hang out of its row and
            // over the rule below. A LayoutElement is how a plain rect answers that question; the alignment
            // being Center means the grid still does not write the size, only reads what it wants.
            var element = rect.GetComponent<LayoutElement>();
            if (element == null)
                element = rect.gameObject.AddComponent<LayoutElement>();

            element.ignoreLayout = false;
            element.minWidth = size.x;
            element.minHeight = size.y;
            element.preferredWidth = size.x;
            element.preferredHeight = size.y;
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
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
        }

        // A bet that reads the way the design was drawn, for a scene with no template running in it: a dialog
        // built from a menu has to look like the dialog rather than like a row of dots that never stops. A real
        // game never sees this - the moment there is a StateManager, only the server's transactions are shown.
        private static TransactionPublic Sample() => new TransactionPublic
        {
            Id = "019f150cc55377c8b0894c99b556a7df",
            BetAmount = "0.00000001",
            WinAmount = "0",
            Payout = "0",
            Win = false,
            Currency = "btc",
            DecimalPoints = 8,
            GameName = "Dice",
            IPlayerId = "dev",
            IPlayerName = "DEV-TEAM",
            Nonce = "42",
            ClientSalt = "b8c1f0e2a4d67193",
            ServerSeedSha512 = "9f2c4e6a8b0d1f3a5c7e9b2d4f6a8c0e1b3d5f7a9c1e3b5d7f9a1c3e5b7d9f1a",
            HouseEdge = "1",
            GameType = EGameType.SINGLE,
            Finished = true,
            CreatedAt = 1756512186000L,
        };
    }
}
