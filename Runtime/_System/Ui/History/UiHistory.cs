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
    // The strip of recent bets: the last few rounds as a row of chips over the game, or a column of them down
    // the side of it. Clicking one opens the bet info window on that bet.
    //
    //     var strip = UiHistory.Create(canvas);       // or drop the component on a rect
    //
    // That is the whole of the usual case. With no other setting touched it seeds itself from
    // MainState.History, listens for OnHistory, animates each arrival, shows as many bets as fit and drops the
    // oldest, prints the nonce - or the tail of the bet id where there is no nonce - and opens the bet info
    // dialog it finds in the scene when one is clicked.
    //
    // What a game is expected to change, in the order it usually wants to:
    //
    //     strip.Style.ElementSize = new Vector2(120f, 60f);    // shape
    //     Text Key = "multiplier", Text Decimals = 2           // what a chip says, from the inspector
    //     strip.Text = data => Multiplier(data) + "x";         // or from code, for anything a key cannot say
    //     strip.Classify = data => data.WinAmount != "0" ? "win" : "loss";
    //     Element Prefab                                       // a chip of the game's own instead of ours
    //
    // Nothing here knows what a round of any particular game means, which is why the two questions it cannot
    // answer - what a chip says and which case it is - are a serialized key each and a delegate each. Neither
    // has to be answered: the fallbacks are the nonce and the amounts.
    //
    // The layout is a UiGrid with one track per element, and every element the strip is not currently showing
    // is a spare kept in the same grid under a name the layout does not mention. That is not an implementation
    // detail worth hiding: a grid shows exactly what its layout names and hides the rest, re-asserting it every
    // time it is enabled, so naming is the only way to hide something in one that it will not argue with.
    [AddComponentMenu("UI/Ui History")]
    [RequireComponent(typeof(RectTransform))]
    public class UiHistory : MonoBehaviour
    {
        private const string ContentName = "Content";
        private const string ElementName = "Element";
        private const string LabelName = "Value";
        private const string AccentName = "Accent";

        // The name a spare answers to. The layout never mentions it, which is what keeps it hidden.
        private const string SpareArea = "";

        // Stands in for the player's own id while there is no server to say what it is, so an editor preview can
        // still show what a marked bet looks like.
        private const string SampleMe = "me";

        [SerializeField]
        private UiHistoryStyle style = new UiHistoryStyle();

        [Header("Element")]
        [Tooltip("The game's own element: a prefab with a UiHistoryElement on its root - the component itself for anything the base behaviour already does, or your own class derived from it. Empty draws the built-in chip.")]
        [SerializeField]
        private UiHistoryElement elementPrefab;

        [Tooltip("Write the scenario's colours onto each element. Off leaves a prefab exactly as it was drawn, which is what a prefab that already knows what a win looks like wants.")]
        [SerializeField]
        private bool paintElements = true;

        [Header("Strip")]
        [SerializeField]
        private EHistoryFlow flow = EHistoryFlow.Horizontal;

        [SerializeField]
        private EHistoryOrder order = EHistoryOrder.NewestLast;

        [Tooltip("Where the elements sit while there are too few to fill the strip.")]
        [SerializeField]
        private EHistoryAlign align = EHistoryAlign.End;

        [SerializeField]
        private EHistoryOverflow overflow = EHistoryOverflow.Clamp;

        [Tooltip("How many bets are kept at most. The oldest is dropped past this. Zero keeps everything the game hands over, which only a scrolling strip wants. The socket itself keeps the last fifteen.")]
        [Min(0)]
        [SerializeField]
        private int capacity = 15;

        [Tooltip("Cut anything that reaches past the strip's own rect. Always on while scrolling, whatever this says.")]
        [SerializeField]
        private bool clip = true;

        [Tooltip("Slide a scrolling strip back to the newest bet as one arrives.")]
        [SerializeField]
        private bool followNewest = true;

        [Header("Value")]
        [Tooltip("A key in the bet's own outcome payload to print - \"multiplier\", \"crash\", whatever the game called it. Empty falls back to the nonce, and then to the tail of the bet id.")]
        [SerializeField]
        private string textKey = string.Empty;

        [Tooltip("How the value is printed, as a string.Format pattern - \"{0}x\", \"x{0}\", \"{0}%\". Empty prints it as it came.")]
        [SerializeField]
        private string textFormat = string.Empty;

        [Tooltip("Decimals to cut a numeric value to. Negative leaves it exactly as the server sent it.")]
        [SerializeField]
        private int textDecimals = -1;

        [Tooltip("Pad the whole part with zeros to this many digits, so 2.83 prints as 02.83 and a strip of numbers lines up. Zero pads nothing.")]
        [Min(0)]
        [SerializeField]
        private int textPad = 0;

        [Tooltip("How much of the bet id to print when there is neither a value nor a nonce.")]
        [Min(1)]
        [SerializeField]
        private int idLength = 4;

        [Tooltip("Take those characters from the end of the id. The end of an id varies where the start of one often does not.")]
        [SerializeField]
        private bool idFromEnd = true;

        [Header("Scenario")]
        [Tooltip("A key in the outcome payload whose value names the scenario - for a game whose server already says \"won\" or \"lost\". Empty compares the amounts: more back than staked is win, some back is push, nothing back is loss.")]
        [SerializeField]
        private string scenarioKey = string.Empty;

        [Header("Behaviour")]
        [Tooltip("Seed from MainState.History and fill in as ON_HISTORY arrives. Off leaves the feeding to the game, through Add and Set.")]
        [SerializeField]
        private bool followState = true;

        [Tooltip("Ignore a bet whose id the strip is already showing, and take the newer copy of it instead of adding a second chip.")]
        [SerializeField]
        private bool dedupe = true;

        [Tooltip("Animate arrivals. Off puts each new element straight where it belongs.")]
        [SerializeField]
        private bool animateArrivals = true;

        [Tooltip("Open the bet info dialog when an element is clicked.")]
        [SerializeField]
        private bool openBetInfo = true;

        [Tooltip("Which dialog to open. Empty finds one in the scene, including a closed one.")]
        [SerializeField]
        private BetInfoWindow betInfo;

        [SerializeField]
        private bool findBetInfo = true;

        [Tooltip("Fill the strip with made-up bets when there is no socket running, so it looks like a history strip in the editor rather than like an empty rect.")]
        [SerializeField]
        private bool preview = true;

        [Min(1)]
        [SerializeField]
        private int sampleCount = 11;

        [Header("Events")]
        [Tooltip("An element was clicked. Raised before the bet info window is opened, and whether or not it is.")]
        public UnityEvent<HistoryDto> OnPicked = new UnityEvent<HistoryDto>();

        [Tooltip("An element has been filled in with a bet - the newly arrived one, or an old one being rewritten. Where a game decorates an element the inspector cannot reach.")]
        public UnityEvent<UiHistoryElement> OnElement = new UnityEvent<UiHistoryElement>();

        /// <summary>What a chip says, for anything a key in the outcome cannot answer. Return null to fall back
        /// to the ordinary rules.</summary>
        public Func<HistoryDto, string> Text;

        /// <summary>Which scenario a bet is - the name of one of the style's scenarios. Return null to fall
        /// back to Scenario Key, and then to the amounts.</summary>
        public Func<HistoryDto, string> Classify;

        /// <summary>Whether a bet gets the accent bar. Unset, the player's own bets do.</summary>
        public Func<HistoryDto, bool> Mark;

        // Parts and the flag saying they exist are deliberately not serialized, the same as the windows next
        // door: everything is found by name before it is made, so a rebuild after a script reload finds the
        // hierarchy that is already there rather than building a second one beside it.
        private RectTransform content;
        private UiGrid grid;
        private ContentSizeFitter fitter;
        private ScrollRect scroller;
        private RectMask2D mask;
        private Image grab;
        private bool built;

        private readonly List<HistoryDto> items = new List<HistoryDto>();
        private readonly List<HistoryDto> display = new List<HistoryDto>();
        private readonly List<UiHistoryElement> live = new List<UiHistoryElement>();
        private readonly List<UiHistoryElement> spare = new List<UiHistoryElement>();
        private Dictionary<string, UiHistoryElement> shown = new Dictionary<string, UiHistoryElement>();
        private Dictionary<string, UiHistoryElement> next = new Dictionary<string, UiHistoryElement>();

        private readonly List<GridTrack> tracks = new List<GridTrack>();
        private readonly List<string> cells = new List<string>();

        private HistoryDto arrival;
        private int held = -1;
        private bool dirty;
        private Vector2 measured;
        private bool listening;
        private Tweener follow;

        /// <summary>The look of the strip. Change anything on it and call <see cref="ApplyStyle"/>.</summary>
        public UiHistoryStyle Style
        {
            get => style ??= new UiHistoryStyle();
            set
            {
                style = value ?? new UiHistoryStyle();
                ApplyStyle();
            }
        }

        /// <summary>The bets the strip holds, oldest first - however they are being shown.</summary>
        public IReadOnlyList<HistoryDto> Items => items;

        /// <summary>The elements on screen, in the order they are shown. Rebuilt whenever the strip is laid
        /// out, so it is worth reading rather than keeping.</summary>
        public IReadOnlyList<UiHistoryElement> Elements => live;

        public int Count => items.Count;

        public RectTransform Rect => (RectTransform)transform;

        /// <summary>The grid the elements are placed in, and their parent. For anything the settings here do
        /// not reach.</summary>
        public UiGrid Grid
        {
            get
            {
                EnsureBuilt();
                return grid;
            }
        }

        /// <summary>The scroller. Only doing anything while Overflow is Scroll.</summary>
        public ScrollRect Scroller
        {
            get
            {
                EnsureBuilt();
                return scroller;
            }
        }

        public EHistoryFlow Flow
        {
            get => flow;
            set
            {
                if (flow == value)
                    return;

                flow = value;
                ApplyStyle();
            }
        }

        public EHistoryOrder Order
        {
            get => order;
            set
            {
                if (order == value)
                    return;

                order = value;
                Touch();
            }
        }

        public EHistoryAlign Align
        {
            get => align;
            set
            {
                if (align == value)
                    return;

                align = value;
                Touch();
            }
        }

        public EHistoryOverflow Overflow
        {
            get => overflow;
            set
            {
                if (overflow == value)
                    return;

                overflow = value;
                ApplyStyle();
            }
        }

        /// <summary>The game's own element. Setting it rebuilds the strip: the elements of the other kind are
        /// thrown away rather than restyled, because a prefab is not a chip with different colours.</summary>
        public UiHistoryElement ElementPrefab
        {
            get => elementPrefab;
            set
            {
                if (elementPrefab == value)
                    return;

                elementPrefab = value;
                Rebuild();
            }
        }

        /// <summary>Seed from MainState.History and fill in as bets arrive. Off hands the feeding to the
        /// game.</summary>
        public bool FollowState
        {
            get => followState;
            set
            {
                if (followState == value)
                    return;

                // Unsubscribed before the flag changes and subscribed after it, because Listen asks the flag
                // whether it is allowed to do anything at all.
                if (!value)
                    Listen(false);

                followState = value;

                if (value && isActiveAndEnabled)
                {
                    Listen(true);
                    Seed();
                }
            }
        }

        /// <summary>Fill the strip with made-up bets while there is no socket running.</summary>
        public bool Preview
        {
            get => preview;
            set => preview = value;
        }

        /// <summary>A key in the bet's outcome payload to print. Empty falls back to the nonce, and then to the
        /// tail of the id.</summary>
        public string TextKey
        {
            get => textKey;
            set
            {
                textKey = value ?? string.Empty;
                Refresh();
            }
        }

        /// <summary>How the value is printed, as a string.Format pattern - "{0}x". Empty prints it as it
        /// came.</summary>
        public string TextFormat
        {
            get => textFormat;
            set
            {
                textFormat = value ?? string.Empty;
                Refresh();
            }
        }

        /// <summary>Decimals to cut a numeric value to. Negative leaves it as the server sent it.</summary>
        public int TextDecimals
        {
            get => textDecimals;
            set
            {
                textDecimals = value;
                Refresh();
            }
        }

        /// <summary>Zeros to pad the whole part out to, so 2.83 prints as 02.83.</summary>
        public int TextPad
        {
            get => textPad;
            set
            {
                textPad = Mathf.Max(0, value);
                Refresh();
            }
        }

        /// <summary>A key in the outcome payload whose value names the scenario. Empty compares the
        /// amounts.</summary>
        public string ScenarioKey
        {
            get => scenarioKey;
            set
            {
                scenarioKey = value ?? string.Empty;
                Refresh();
            }
        }

        public bool AnimateArrivals
        {
            get => animateArrivals;
            set => animateArrivals = value;
        }

        public bool OpenBetInfo
        {
            get => openBetInfo;
            set => openBetInfo = value;
        }

        /// <summary>How many bets are kept at most. Zero keeps all of them.</summary>
        public int Capacity
        {
            get => capacity;
            set
            {
                capacity = Mathf.Max(0, value);
                Trim();
                Touch();
            }
        }

        /// <summary>The dialog a clicked element opens. Left empty, one is found in the scene - including a
        /// closed one, which every dialog is until it is opened.</summary>
        public BetInfoWindow BetInfo
        {
            get
            {
                if (betInfo != null || !findBetInfo)
                    return betInfo;

                betInfo = FindAnyObjectByType<BetInfoWindow>(FindObjectsInactive.Include);
                return betInfo;
            }
            set => betInfo = value;
        }

        /// <summary>Builds a strip under a parent, ready to be fed.</summary>
        public static UiHistory Create(Transform parent, string name = "History", float width = 640f, float height = 64f)
        {
            var created = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)created.transform;

            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.sizeDelta = new Vector2(width, height);

            if (parent != null)
                created.layer = parent.gameObject.layer;

            var history = created.AddComponent<UiHistory>();

            // Awake has done this in play mode. In the editor nothing else ever will, and a strip that arrived
            // as an empty rect would be read as broken.
            history.EnsureBuilt();
            return history;
        }

        void Awake()
        {
            EnsureBuilt();
        }

        void OnEnable()
        {
            EnsureBuilt();
            Listen(true);
            Seed();
        }

        void OnDisable()
        {
            Listen(false);
            Stop();
        }

        void OnDestroy()
        {
            Stop();
        }

        /// <summary>Makes whatever is missing. Safe to call as often as you like.</summary>
        public void EnsureBuilt()
        {
            // The parts are named in the guard rather than trusting the flag: a strip saved by a version that
            // had fewer of them comes back with the flag set and half a hierarchy.
            if (built && content != null && grid != null && scroller != null)
                return;

            Rebuild();
        }

        /// <summary>Builds the strip from scratch and lays it out. Call after changing the style from code, or
        /// after swapping the element prefab.</summary>
        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            if (style == null)
                style = new UiHistoryStyle();

            BuildParts();
            Collect();

            built = true;

            ApplyStyle();
        }

        /// <summary>Writes every colour, gap and scroll setting, then lays the strip out.</summary>
        public void ApplyStyle()
        {
            if (!built)
                return;

            var padding = grid.padding;
            padding.left = Mathf.RoundToInt(style.PaddingLeft);
            padding.top = Mathf.RoundToInt(style.PaddingTop);
            padding.right = Mathf.RoundToInt(style.PaddingRight);
            padding.bottom = Mathf.RoundToInt(style.PaddingBottom);
            grid.padding = padding;

            grid.ColumnGap = style.Gap;
            grid.RowGap = style.Gap;
            grid.Flow = flow == EHistoryFlow.Horizontal ? EGridFlow.Row : EGridFlow.Column;

            // The elements the strip drew itself follow the style; a game's own prefab does not, and that is the
            // point of the flag on the element.
            for (int i = 0; i < live.Count; i++)
                Dress(live[i]);

            for (int i = 0; i < spare.Count; i++)
                Dress(spare[i]);

            scroller.scrollSensitivity = style.ScrollSensitivity;
            scroller.inertia = style.ScrollInertia;
            scroller.decelerationRate = style.ScrollDeceleration;
            scroller.movementType = ScrollRect.MovementType.Clamped;

            grid.Rebuild();
            Layout();
        }

        /// <summary>Binds the bets to elements, names the cells and sets the layout, there and then.</summary>
        public void Layout()
        {
            if (!built)
                return;

            dirty = false;
            Arrange();
        }

        // What every change to the data ends in. One arrangement per frame however many bets arrived in it: the
        // socket delivers a round of history one event at a time, and laying the strip out fifteen times to end
        // up with the arrangement of the fifteenth is fourteen arrangements nobody sees. In the editor, where
        // there is no frame coming, it happens now.
        private void Touch()
        {
            if (!built)
                return;

            if (Application.isPlaying)
            {
                dirty = true;
                return;
            }

            Arrange();
        }

        // ------------------------------------------------------------------ the data

        /// <summary>Adds a bet as the newest one, dropping the oldest past Capacity. What OnHistory ends in, and
        /// what a game feeding the strip itself calls.</summary>
        public void Add(HistoryDto data)
        {
            if (data == null)
                return;

            EnsureBuilt();

            if (dedupe)
            {
                int at = IndexOf(data.Id);
                if (at >= 0)
                {
                    // The same bet again - a payout that came in after the round, most often. It takes the newer
                    // copy where it already is rather than jumping to the end of the strip.
                    items[at] = data;
                    Touch();
                    return;
                }
            }

            items.Add(data);
            Trim();

            arrival = animateArrivals ? data : null;
            Touch();
        }

        public void AddRange(IEnumerable<HistoryDto> values)
        {
            if (values == null)
                return;

            foreach (var value in values)
                Add(value);
        }

        /// <summary>Replaces everything with these, oldest first. Nothing is animated - this is a strip being
        /// filled in, not a bet arriving.</summary>
        public void Set(IEnumerable<HistoryDto> values)
        {
            EnsureBuilt();

            items.Clear();

            if (values != null)
            {
                foreach (var value in values)
                {
                    if (value == null)
                        continue;

                    if (dedupe && IndexOf(value.Id) >= 0)
                        continue;

                    items.Add(value);
                }
            }

            Trim();
            arrival = null;
            Touch();
        }

        public void Clear()
        {
            items.Clear();
            arrival = null;

            if (built)
                Touch();
        }

        public bool Remove(string id)
        {
            int at = IndexOf(id);
            if (at < 0)
                return false;

            items.RemoveAt(at);
            Touch();
            return true;
        }

        /// <summary>Writes every element again from the bet it is showing - for after the game has changed what
        /// a chip should say, or after the server has said who the player is.</summary>
        public void Refresh()
        {
            EnsureBuilt();

            for (int i = 0; i < live.Count; i++)
            {
                var element = live[i];
                if (element != null && element.Data != null)
                    Fill(element, element.Data);
            }

            Touch();
        }

        /// <summary>The element showing that bet, or null.</summary>
        public UiHistoryElement ElementFor(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            return shown.TryGetValue(id, out var element) ? element : null;
        }

        /// <summary>Fills the strip with made-up bets. What the editor preview and the create menu use, and a
        /// quick way to see a style without a server.</summary>
        public void Sample(int count)
        {
            var made = new List<HistoryDto>(Mathf.Max(0, count));

            for (int i = 0; i < count; i++)
            {
                // Fixed arithmetic rather than Random, so the same strip looks the same every time it is
                // rebuilt - a preview that reshuffles on every recompile is a preview nobody can style against.
                bool win = i % 3 != 1;
                decimal multiplier = 1.05m + (i * 37 % 91) * 0.11m;

                var data = new HistoryDto
                {
                    Id = "sample-" + (1000 + i * 7).ToString(CultureInfo.InvariantCulture),
                    GameName = "Sample",
                    BetAmount = "1",
                    WinAmount = win ? multiplier.ToString(CultureInfo.InvariantCulture) : "0",
                    Currency = "USD",
                    IPlayerId = i % 4 == 0 ? SampleMe : "other-" + i.ToString(CultureInfo.InvariantCulture),
                    IPlayerName = "Player " + i.ToString(CultureInfo.InvariantCulture),
                    N = i + 1,
                    CreatedAt = 1700000000000L + i * 60000L,
                };

                if (!string.IsNullOrEmpty(textKey))
                {
                    data._Outcome = new GenericDictionary<string, string>();
                    data._Outcome[textKey] = multiplier.ToString("0.00", CultureInfo.InvariantCulture);
                }

                made.Add(data);
            }

            Set(made);
        }

        // ------------------------------------------------------------------ clicking

        /// <summary>What an element does when it is clicked: raises OnPicked, then opens the bet info dialog on
        /// that bet. Public so a game can drive it from its own control.</summary>
        public void Pick(UiHistoryElement element)
        {
            var data = element != null ? element.Data : null;
            if (data == null)
                return;

            OnPicked.Invoke(data);

            if (!openBetInfo)
                return;

            var window = BetInfo;
            if (window == null || string.IsNullOrEmpty(data.Id))
                return;

            // Show asks the server for the bet and opens on a loader, which is the right thing here: the
            // history payload is a summary, and the dialog wants the whole transaction with its seeds.
            window.Show(data.Id);
        }

        /// <summary>Slides a scrolling strip to the end the newest bet is at.</summary>
        public void ScrollToNewest(bool animated = true)
        {
            if (!built || overflow != EHistoryOverflow.Scroll)
                return;

            bool horizontal = flow == EHistoryFlow.Horizontal;

            // Normalised scroll runs left to right and bottom to top, while a grid's rows run downwards - so
            // the newest being first means one end horizontally and the other vertically.
            float target = horizontal
                ? (order == EHistoryOrder.NewestFirst ? 0f : 1f)
                : (order == EHistoryOrder.NewestFirst ? 1f : 0f);

            Stop();

            float duration = animated ? style.FollowDuration : 0f;
            if (duration <= 0f)
            {
                Place(target, horizontal);
                return;
            }

            // Whatever the flick was doing is over: two things moving the same strip at once reads as a stutter.
            scroller.velocity = Vector2.zero;

            float from = horizontal ? scroller.horizontalNormalizedPosition : scroller.verticalNormalizedPosition;
            follow = DOTween
                .To(() => from, value => Place(value, horizontal), target, duration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(style.UnscaledTime);
        }

        // ------------------------------------------------------------------ building

        private void BuildParts()
        {
            var self = Rect;

            content = UiWindowParts.Find<RectTransform>(self, ContentName);
            if (content == null)
            {
                var created = new GameObject(ContentName, typeof(RectTransform));
                content = (RectTransform)created.transform;
                content.SetParent(self, false);
                content.localScale = Vector3.one;
                created.layer = gameObject.layer;
            }

            grid = UiWindowParts.Grid(content);

            fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = content.gameObject.AddComponent<ContentSizeFitter>();

            scroller = GetComponent<ScrollRect>();
            if (scroller == null)
                scroller = gameObject.AddComponent<ScrollRect>();

            scroller.content = content;
            scroller.viewport = self;
            scroller.horizontalScrollbar = null;
            scroller.verticalScrollbar = null;

            mask = GetComponent<RectMask2D>();
            if (mask == null)
                mask = gameObject.AddComponent<RectMask2D>();

            // A drag that starts on a chip reaches the scroller by itself - a Button handles the press but not
            // the drag, so the event carries on up to the ScrollRect above it. A drag that starts in a gap
            // between chips lands on nothing at all, and neither does the wheel over one. This is what it lands
            // on: invisible, on the same object as the ScrollRect, and enabled only while the strip scrolls.
            grab = GetComponent<Image>();
            if (grab == null)
                grab = gameObject.AddComponent<Image>();

            grab.color = new Color(0f, 0f, 0f, 0f);
            grab.raycastTarget = true;
        }

        // Everything already under the grid is taken over rather than left there, which is what makes Rebuild
        // safe to call twice. Elements of the wrong kind - built-in chips on a strip that has since been given
        // a prefab, or the other way about - are thrown away instead.
        private void Collect()
        {
            live.Clear();
            spare.Clear();
            shown.Clear();
            next.Clear();

            bool wantNative = elementPrefab == null;
            var found = content.GetComponentsInChildren<UiHistoryElement>(true);

            for (int i = 0; i < found.Length; i++)
            {
                var element = found[i];
                if (element == null || element.transform.parent != content)
                    continue;

                if (element.Native != wantNative)
                {
                    UiWindowParts.Discard(element.gameObject);
                    continue;
                }

                element.Adopt(this);
                element.Release();
                spare.Add(element);
            }
        }

        private UiHistoryElement Rent()
        {
            while (spare.Count > 0)
            {
                int last = spare.Count - 1;
                var kept = spare[last];
                spare.RemoveAt(last);

                if (kept != null)
                    return kept;
            }

            var element = elementPrefab != null ? Spawn() : Chip();
            element.Adopt(this);
            Dress(element);
            return element;
        }

        // The prefab is typed, so there is nothing to check here: an element always has the component, and
        // RequireComponent means it always has a RectTransform for the grid to place.
        private UiHistoryElement Spawn()
        {
            var element = Instantiate(elementPrefab, content, false);
            element.name = ElementName;
            element.gameObject.layer = gameObject.layer;
            element.Rect.localScale = Vector3.one;
            element.Native = false;

            return element;
        }

        // The built-in chip: a rounded plate, a label in the middle of it, and a bar along the bottom for the
        // bets that are the player's own.
        private UiHistoryElement Chip()
        {
            var created = new GameObject(ElementName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RoundedBox));
            var rect = (RectTransform)created.transform;
            rect.SetParent(content, false);
            rect.localScale = Vector3.one;
            created.layer = gameObject.layer;

            var element = created.AddComponent<UiHistoryElement>();
            element.Native = true;
            element.Plate = created.GetComponent<RoundedBox>();

            var label = UiWindowParts.Label(rect, LabelName);
            label.raycastTarget = false;
            element.Label = label;

            var accent = UiWindowParts.Box(rect, AccentName);
            accent.raycastTarget = false;
            element.Accent = accent;

            // Off until something marks the bet. A fresh rect is a hundred units square in the middle of its
            // parent, so an accent nobody has painted yet would be a box across the whole chip.
            accent.gameObject.SetActive(false);

            return element;
        }

        // Shape and text metrics, which are one look for the whole strip - as against the scenario's colours,
        // which are what tells one bet from another and are written in Fill.
        private void Dress(UiHistoryElement element)
        {
            if (element == null || !element.Native)
                return;

            if (element.Plate is RoundedBox box)
            {
                box.SetCornerRadius(style.CornerRadius);
                box.EdgeSoftness = style.EdgeSoftness;
            }

            var label = element.Label;
            if (label == null)
                return;

            UiWindowParts.Stretch(label.rectTransform, style.TextInset, style.TextInset, style.TextInset, style.TextInset);

            if (style.Font != null)
                label.font = style.Font;

            label.fontSize = style.TextSize;
            label.fontStyle = style.TextStyle;
            label.alignment = style.TextAlignment;
            // No wrapping: a chip is one short value, and a multiplier broken over two lines is worse than one
            // that shrank to fit.
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;

            // Autosizing is a floor and a ceiling rather than a size, so the ceiling is the size asked for and a
            // value too long for the chip shrinks instead of spilling out of it.
            label.enableAutoSizing = style.ShrinkText;
            if (!style.ShrinkText)
                return;

            label.fontSizeMax = style.TextSize;
            label.fontSizeMin = Mathf.Max(1f, style.TextSize * 0.5f);
        }

        // ------------------------------------------------------------------ placing

        private void Arrange()
        {
            bool horizontal = flow == EHistoryFlow.Horizontal;
            bool scrolling = overflow == EHistoryOverflow.Scroll;

            Choose();

            // Elements are kept with the bet they are showing rather than reassigned by position, so a strip
            // that shifts along only writes the one chip that is new. A prefab holding state of its own - a
            // little chart, a running animation - keeps it as it slides.
            next.Clear();
            live.Clear();

            for (int i = 0; i < display.Count; i++)
            {
                var data = display[i];
                string key = Key(data, i);

                while (next.ContainsKey(key))
                    key += "*";

                if (!shown.TryGetValue(key, out var element) || element == null)
                {
                    element = Rent();
                    Fill(element, data);
                }
                else if (!ReferenceEquals(element.Data, data))
                {
                    Fill(element, data);
                }

                shown.Remove(key);
                next[key] = element;
                live.Add(element);
            }

            foreach (var pair in shown)
                Retire(pair.Value);

            shown.Clear();

            var swap = shown;
            shown = next;
            next = swap;

            Name(horizontal);
            Shape(horizontal, scrolling);
            Scroll(horizontal, scrolling);

            if (arrival != null)
            {
                // By reference rather than by id: an arrival the strip had no room for is not on screen at all,
                // and a bet the server sent with no id at all is still the object that just arrived.
                var element = Holder(arrival);
                arrival = null;

                if (element != null)
                    element.Appear();

                if (scrolling && followNewest)
                    ScrollToNewest();
            }
            else if (scrolling && followNewest && live.Count != held)
            {
                // A strip that was filled in rather than added to - seeded from the state, or set by the game.
                // It belongs at the newest end, and jumping there is right where sliding would be a strip that
                // animates itself on the frame it appears.
                ScrollToNewest(false);
            }

            held = live.Count;
            measured = Rect.rect.size;
        }

        // Which bets are shown, in the order they are shown in: the newest that fit, oldest first, then turned
        // round if the newest belongs at the start.
        private void Choose()
        {
            display.Clear();

            int room = Room();
            int take = Mathf.Min(items.Count, room);

            for (int i = items.Count - take; i < items.Count; i++)
            {
                if (items[i] != null)
                    display.Add(items[i]);
            }

            if (order == EHistoryOrder.NewestFirst)
                display.Reverse();
        }

        // How many elements the strip has room for. Everything, unless it is clamping and there is a size along
        // the flow to divide the room by - with elements sizing themselves there is nothing to count with, and
        // guessing would be worse than clipping.
        private int Room()
        {
            if (overflow != EHistoryOverflow.Clamp)
                return int.MaxValue;

            bool horizontal = flow == EHistoryFlow.Horizontal;
            float step = horizontal ? style.ElementSize.x : style.ElementSize.y;
            if (step <= 0f)
                return int.MaxValue;

            var size = Rect.rect.size;
            float available = horizontal
                ? size.x - style.PaddingLeft - style.PaddingRight
                : size.y - style.PaddingTop - style.PaddingBottom;

            // No width yet - the first frame of a canvas, or a strip whose parent has not been laid out. Showing
            // everything is the safer answer: LateUpdate arranges again as soon as there is a size, and one
            // frame of a full strip beats one frame of an empty one.
            if (available <= 1f)
                return int.MaxValue;

            int fit = Mathf.FloorToInt((available + style.Gap) / (step + style.Gap));
            return Mathf.Max(1, fit);
        }

        private void Name(bool horizontal)
        {
            bool crossFixed = (horizontal ? style.ElementSize.y : style.ElementSize.x) > 0f;

            for (int i = 0; i < live.Count; i++)
            {
                var element = live[i];
                if (element == null)
                    continue;

                var item = UiWindowParts.Item(element.Rect);
                item.Area = Area(i);

                // Stretched along the flow, because the track is exactly one element wide; centred across it,
                // because an element with a size of its own should sit in the middle of a taller strip rather
                // than being pulled up to the top of it.
                item.OverrideAlign = crossFixed;
                if (crossFixed)
                {
                    item.HorizontalAlign = horizontal ? EGridAlign.Stretch : EGridAlign.Center;
                    item.VerticalAlign = horizontal ? EGridAlign.Center : EGridAlign.Stretch;
                }

                Measure(element.Rect);
            }

            for (int i = 0; i < spare.Count; i++)
            {
                if (spare[i] != null)
                    UiWindowParts.Item(spare[i].Rect).Area = SpareArea;
            }
        }

        // An auto track is as big as the items in it say they need to be, and a rect says nothing at all unless
        // something on it answers - so a size from the style is written as a Layout Element rather than only as
        // a sizeDelta. Zero on an axis writes nothing, which leaves a prefab's own answer standing.
        private void Measure(RectTransform rect)
        {
            var size = style.ElementSize;

            var element = rect.GetComponent<LayoutElement>();
            if (size.x <= 0f && size.y <= 0f)
                return;

            if (element == null)
                element = rect.gameObject.AddComponent<LayoutElement>();

            element.ignoreLayout = false;
            element.minWidth = size.x > 0f ? size.x : -1f;
            element.preferredWidth = size.x > 0f ? size.x : -1f;
            element.minHeight = size.y > 0f ? size.y : -1f;
            element.preferredHeight = size.y > 0f ? size.y : -1f;
        }

        // One track per element, plus a flexible one at whichever end the strip is not packed against. A
        // scrolling strip gets neither: its content is exactly as long as its elements, and a spacer in it would
        // be room to scroll into that has nothing in it.
        private void Shape(bool horizontal, bool scrolling)
        {
            if (live.Count == 0)
            {
                // One empty cell rather than no layout at all. A grid with no layout shows every child it has,
                // which on this one would be every spare element in the pool.
                grid.SetLayout(UiGridLayout
                    .Build()
                    .Columns(GridTrack.Flexible())
                    .Rows(GridTrack.Flexible())
                    .Row(UiGridLayout.Empty)
                    .Done());

                return;
            }

            tracks.Clear();
            cells.Clear();

            bool spacing = !scrolling;
            float along = horizontal ? style.ElementSize.x : style.ElementSize.y;

            if (spacing && align != EHistoryAlign.Start)
            {
                tracks.Add(GridTrack.Flexible());
                cells.Add(UiGridLayout.Empty);
            }

            for (int i = 0; i < live.Count; i++)
            {
                tracks.Add(along > 0f ? GridTrack.Fixed(along) : GridTrack.Auto());
                cells.Add(Area(i));
            }

            if (spacing && align != EHistoryAlign.End)
            {
                tracks.Add(GridTrack.Flexible());
                cells.Add(UiGridLayout.Empty);
            }

            // The cross axis is always one flexible track - the elements fill the strip across, or keep their
            // own size and are centred in it. Never a fixed track: that would pin the row to the top edge and
            // leave the rest of the strip empty under it.
            var arrangement = UiGridLayout.Build();

            if (horizontal)
            {
                arrangement.Columns(tracks.ToArray()).Rows(GridTrack.Flexible()).Row(cells.ToArray());
            }
            else
            {
                arrangement.Columns(GridTrack.Flexible()).Rows(tracks.ToArray());
                for (int i = 0; i < cells.Count; i++)
                    arrangement.Row(cells[i]);
            }

            grid.SetLayout(arrangement.Done());
        }

        private void Scroll(bool horizontal, bool scrolling)
        {
            if (scrolling)
            {
                // The content is as long as its elements along the flow and as wide as the strip across it, which
                // is the shape a ScrollRect expects and the one the fitter can write.
                fitter.horizontalFit = horizontal ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = horizontal ? ContentSizeFitter.FitMode.Unconstrained : ContentSizeFitter.FitMode.PreferredSize;
                fitter.enabled = true;

                if (horizontal)
                {
                    content.anchorMin = new Vector2(0f, 0f);
                    content.anchorMax = new Vector2(0f, 1f);
                    content.pivot = new Vector2(0f, 0.5f);
                    content.sizeDelta = new Vector2(content.sizeDelta.x, 0f);
                }
                else
                {
                    content.anchorMin = new Vector2(0f, 1f);
                    content.anchorMax = new Vector2(1f, 1f);
                    content.pivot = new Vector2(0.5f, 1f);
                    content.sizeDelta = new Vector2(0f, content.sizeDelta.y);
                }
            }
            else
            {
                fitter.enabled = false;
                UiWindowParts.Stretch(content, 0f, 0f, 0f, 0f);
            }

            scroller.enabled = scrolling;
            scroller.horizontal = scrolling && horizontal;
            scroller.vertical = scrolling && !horizontal;
            scroller.scrollSensitivity = style.ScrollSensitivity;
            scroller.inertia = style.ScrollInertia;
            scroller.decelerationRate = style.ScrollDeceleration;

            mask.enabled = clip || scrolling;
            grab.enabled = scrolling;

            if (scrolling)
                return;

            Stop();
            content.anchoredPosition = Vector2.zero;
        }

        private void Fill(UiHistoryElement element, HistoryDto data)
        {
            string scenario = Kind(data);

            element.Bind(data, Value(data), scenario, Marked(data));

            if (paintElements)
                element.Paint(style.Find(scenario));

            OnElement.Invoke(element);
        }

        private void Retire(UiHistoryElement element)
        {
            if (element == null)
                return;

            element.Release();
            UiWindowParts.Item(element.Rect).Area = SpareArea;
            spare.Add(element);
        }

        private static string Area(int index) => "e" + index.ToString(CultureInfo.InvariantCulture);

        private UiHistoryElement Holder(HistoryDto data)
        {
            for (int i = 0; i < live.Count; i++)
            {
                if (live[i] != null && ReferenceEquals(live[i].Data, data))
                    return live[i];
            }

            return null;
        }

        private string Key(HistoryDto data, int index)
        {
            if (data == null)
                return index.ToString(CultureInfo.InvariantCulture);

            return string.IsNullOrEmpty(data.Id)
                ? "#" + data.GetHashCode().ToString(CultureInfo.InvariantCulture)
                : data.Id;
        }

        private void Place(float value, bool horizontal)
        {
            if (horizontal)
                scroller.horizontalNormalizedPosition = value;
            else
                scroller.verticalNormalizedPosition = value;
        }

        private void Stop()
        {
            if (follow != null && follow.IsActive())
                follow.Kill();

            follow = null;
        }

        // ------------------------------------------------------------------ what a chip says

        /// <summary>A value out of a bet's own outcome payload, or empty. Both shapes of it are looked at - the
        /// flat one the socket fills in and the raw JSON it came as.</summary>
        public static string Read(HistoryDto data, string key)
        {
            if (data == null || string.IsNullOrEmpty(key))
                return string.Empty;

            if (data._Outcome != null && data._Outcome.TryGetValue(key, out var flat) && flat != null)
                return flat;

            if (data.Outcome != null && data.Outcome.TryGetValue(key, out var token) && token != null)
                return token.ToString();

            return string.Empty;
        }

        // What one chip says, in the order a game would try: its own answer, then the key it named in the
        // outcome, then the nonce, then the tail of the id. Something always comes out - a chip with nothing
        // written on it reads as a bug in the strip rather than as a bet with no value.
        private string Value(HistoryDto data)
        {
            if (data == null)
                return string.Empty;

            if (Text != null)
            {
                string own = Text(data);
                if (own != null)
                    return own;
            }

            string raw = Read(data, textKey);
            if (!string.IsNullOrEmpty(raw))
                return Print(raw);

            if (data.N.HasValue)
                return data.N.Value.ToString(CultureInfo.InvariantCulture);

            return Tail(data.Id);
        }

        // Truncated rather than rounded, the same way the rest of this package prints money: a multiplier the
        // server called 2.839 was 2.83, and rounding it up to 2.84 is showing the player a number that never
        // happened.
        private string Print(string raw)
        {
            string text = raw;

            if (textDecimals >= 0 && decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            {
                decimal scale = 1m;
                for (int i = 0; i < textDecimals; i++)
                    scale *= 10m;

                decimal cut = decimal.Truncate(value * scale) / scale;
                text = cut.ToString("F" + textDecimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
            }

            if (textPad > 0)
            {
                int dot = text.IndexOf('.');
                string whole = dot < 0 ? text : text.Substring(0, dot);
                string rest = dot < 0 ? string.Empty : text.Substring(dot);

                if (whole.Length < textPad)
                    whole = whole.PadLeft(textPad, '0');

                text = whole + rest;
            }

            if (string.IsNullOrEmpty(textFormat))
                return text;

            try
            {
                return string.Format(CultureInfo.InvariantCulture, textFormat, text);
            }
            catch (FormatException)
            {
                // A pattern typed wrong in the inspector is a typo, not a reason to throw once per element per
                // frame. The value goes up unformatted and the strip carries on.
                return text;
            }
        }

        private string Tail(string id)
        {
            if (string.IsNullOrEmpty(id))
                return string.Empty;

            int length = Mathf.Clamp(idLength, 1, id.Length);
            return idFromEnd ? id.Substring(id.Length - length, length) : id.Substring(0, length);
        }

        // Which case a bet is: the game's own answer, then the key it named in the outcome, then the amounts.
        private string Kind(HistoryDto data)
        {
            if (data == null)
                return string.Empty;

            if (Classify != null)
            {
                string own = Classify(data);
                if (own != null)
                    return own;
            }

            string named = Read(data, scenarioKey);
            if (!string.IsNullOrEmpty(named))
                return named;

            decimal bet = Number(data.BetAmount);
            decimal win = Number(data.WinAmount);

            if (win > bet)
                return "win";

            return win > 0m ? "push" : "loss";
        }

        // Whether a bet gets the accent bar. The player's own, by default - which is the one thing a shared
        // history strip is really for.
        private bool Marked(HistoryDto data)
        {
            if (data == null)
                return false;

            if (Mark != null)
                return Mark(data);

            var system = StateManager.Inst != null && StateManager.Inst.MainState != null
                ? StateManager.Inst.MainState.SystemState
                : null;

            if (system != null && !string.IsNullOrEmpty(system.Me))
                return data.IPlayerId == system.Me;

            // No server to say who the player is, so this is an editor preview and the sample's own id stands
            // in for one. In a running game with no Me yet, nothing is marked, which is right.
            return StateManager.Inst == null && data.IPlayerId == SampleMe;
        }

        private static decimal Number(string raw) =>
            decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : 0m;

        // ------------------------------------------------------------------ the feed

        private void Listen(bool on)
        {
            var manager = StateManager.Inst;
            if (manager == null || manager.Events == null || !followState)
                return;

            if (on && !listening)
            {
                manager.Events.OnHistory.AddListener(Add);
                listening = true;
            }
            else if (!on && listening)
            {
                manager.Events.OnHistory.RemoveListener(Add);
                listening = false;
            }
        }

        private void Seed()
        {
            var manager = StateManager.Inst;
            var history = manager != null && manager.MainState != null ? manager.MainState.History : null;

            if (followState && history != null && history.Count > 0)
            {
                Set(history);
                return;
            }

            if (preview && items.Count == 0 && manager == null)
                Sample(sampleCount);
        }

        private int IndexOf(string id)
        {
            if (string.IsNullOrEmpty(id))
                return -1;

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].Id == id)
                    return i;
            }

            return -1;
        }

        private void Trim()
        {
            if (capacity <= 0)
                return;

            while (items.Count > capacity)
                items.RemoveAt(0);
        }

        void LateUpdate()
        {
            if (!built)
                return;

            // Awake order across objects is nobody's to decide, so a strip enabled before the state manager
            // exists would have missed its chance to subscribe and would then sit empty for the whole session.
            // Asking again costs a null check a frame until there is something to ask.
            if (followState && !listening && Application.isPlaying)
            {
                Listen(true);

                if (listening)
                    Seed();
            }

            if (dirty)
            {
                dirty = false;
                Arrange();
                return;
            }

            // How many elements fit is worked out from the strip's own width, and nothing announces that it has
            // changed: an anchored strip is resized by its parent, by the canvas scaler, by the window it is in
            // being dragged narrower. Comparing it is cheaper than anything that would.
            var size = Rect.rect.size;
            if ((size - measured).sqrMagnitude < 0.25f)
                return;

            measured = size;

            if (overflow == EHistoryOverflow.Clamp)
                Arrange();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            capacity = Mathf.Max(0, capacity);
            idLength = Mathf.Max(1, idLength);
            sampleCount = Mathf.Max(1, sampleCount);

            if (!built)
                return;

            if (Application.isPlaying)
            {
                ApplyStyle();
                return;
            }

            // OnValidate is not allowed to activate or destroy anything, and setting a grid's layout does the
            // first of those. Deferring by a frame puts it back on ordinary editor time, where it is.
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null)
                    return;

                if (built)
                    ApplyStyle();
            };
        }
#endif
    }
}
