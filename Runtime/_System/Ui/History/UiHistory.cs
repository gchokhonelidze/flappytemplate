using System.Collections.Generic;
using System.Globalization;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // The strip of recent bets: the last few rounds as a row of chips over the game, or a column of them down
    // the side of it. Clicking one opens the bet info window on that bet.
    //
    //     var strip = UiHistory.Create(canvas);       // or drop the component on a rect
    //     strip.ElementPrefab = myChip;               // and this, which is not optional
    //
    // That is the whole of the usual case. With nothing else touched it seeds itself from MainState.History,
    // listens for OnHistory, animates each arrival, shows as many bets as fit and drops the oldest.
    //
    // The strip draws nothing of its own. What an element is made of, how big it is and what it says about the
    // round are all the prefab's: the strip hands it the whole HistoryDto through UiHistoryElement.Write and
    // stays out of it. So there is no chip in here to colour, no size to set and no value to format - a game
    // that wants a multiplier in green derives from UiHistoryElement, gives itself the fields it wants and reads
    // Data. Without a prefab there is nothing to show, and the strip says so once.
    //
    // What is left to decide is the strip: which way it runs, which end the newest bet lands on, what happens
    // when it fills up, and the gap between one element and the next.
    //
    // The one thing it will not do for itself is find a dialog to open. Point Bet Info at the window a click
    // should open and it opens that one; leave it empty and a click raises OnPicked and nothing else. Nothing is
    // searched for, and that is a decision rather than an omission: a scene holds as many bet info windows as
    // somebody put in it - a game's own, plus whatever an example or a test built for itself - and a strip that
    // guessed would open one of them off screen about half the time.
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

        // What Sample writes its made-up multiplier under. A game's own element reads whatever key its server
        // sends, so the preview can only pick the one most of them use and be plainly wrong about the rest.
        private const string SampleKey = "multiplier";

        // The name a spare answers to. The layout never mentions it, which is what keeps it hidden.
        private const string SpareArea = "";

        [SerializeField]
        private UiHistoryStyle style = new UiHistoryStyle();

        [Header("Element")]
        [Tooltip("The game's own element: a prefab with a UiHistoryElement on its root - your own class derived from it, which is where a bet gets its look and its size. Without one the strip has nothing to draw a bet with.")]
        [SerializeField]
        private UiHistoryElement elementPrefab;

        [Header("Strip")]
        [SerializeField]
        private EHistoryFlow flow = EHistoryFlow.Horizontal;

        [SerializeField]
        private EHistoryOrder order = EHistoryOrder.NewestLast;

        [Tooltip("Where the elements sit while there are too few to fill the strip.")]
        [SerializeField]
        private EHistoryAlign align = EHistoryAlign.Center;

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

        [Header("Behaviour")]
        [Tooltip("Seed from MainState.History and fill in as ON_HISTORY arrives. Off leaves the feeding to the game, through Add and Set.")]
        [SerializeField]
        private bool followState = true;

        [Tooltip("Ignore a bet whose id the strip is already showing, and take the newer copy of it instead of adding a second chip.")]
        [SerializeField]
        private bool dedupe = true;

        [Tooltip("Put the bets in the order the server says they happened rather than the order they were handed over. The history array is not sorted, so without this a seeded strip can come out the opposite way round from the arrivals that follow it. Off only makes sense on a feed with no Created At.")]
        [SerializeField]
        private bool sortByTime = true;

        [Tooltip("Animate arrivals. Off puts each new element straight where it belongs.")]
        [SerializeField]
        private bool animateArrivals = true;

        [Tooltip("The dialog a clicked element opens. Nothing is found for you: a scene has as many bet info windows in it as somebody put there, and a strip that guessed would sometimes open one parked off screen. Leave it empty and a click only raises On Picked.")]
        [SerializeField]
        private BetInfoWindow betInfo;

        [Tooltip("Fill the strip with made-up bets when there is no socket running, so it looks like a history strip in the editor rather than like an empty rect. Needs an element prefab, the same as a real bet does.")]
        [SerializeField]
        private bool preview = true;

        [Min(1)]
        [SerializeField]
        private int sampleCount = 11;

        [Header("Events")]
        [Tooltip("An element was clicked. Raised before the bet info window is opened, and whether or not it is.")]
        public UnityEvent<HistoryDto> OnPicked = new UnityEvent<HistoryDto>();

        [Tooltip("An element has been filled in with a bet - the newly arrived one, or an old one being rewritten. Where a game decorates an element the prefab does not.")]
        public UnityEvent<UiHistoryElement> OnElement = new UnityEvent<UiHistoryElement>();

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
        private bool warned;
        private bool missing;
        private int held = -1;
        private bool dirty;
        private Vector2 measured;
        private bool listening;
        private Tweener follow;

        // How big one element is, measured off the prefab once per arrangement: along the flow it is the track,
        // across it, it is the difference between an element that keeps its own height and one stretched to the
        // strip.
        private float stepAlong;
        private float stepCross;

        /// <summary>The feel of the strip: the gap, the arrival and the scrolling. Change anything on it and
        /// call <see cref="ApplyStyle"/>.</summary>
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

        /// <summary>The game's own element, and the only thing the strip has to draw a bet with. Setting it
        /// rebuilds: elements left over from the last prefab are thrown away rather than reused, because a
        /// prefab is not a chip with different colours.</summary>
        public UiHistoryElement ElementPrefab
        {
            get => elementPrefab;
            set
            {
                if (elementPrefab == value)
                    return;

                elementPrefab = value;
                missing = false;
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

        public bool AnimateArrivals
        {
            get => animateArrivals;
            set => animateArrivals = value;
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

        /// <summary>The dialog a clicked element opens. Null opens nothing - a click still raises
        /// <see cref="OnPicked"/>, which is all a game driving its own dialog needs.</summary>
        public BetInfoWindow BetInfo
        {
            get => betInfo;
            set => betInfo = value;
        }

        /// <summary>Builds a strip under a parent, ready to be given an element and fed.</summary>
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

            // Nothing here is serialized - not the parts, not the bets - so a script reload leaves a strip in the
            // editor with its elements still in the hierarchy and nothing to put in them. Seeding again is what
            // it would have done on enable, and it is why a strip in the editor goes on looking like a strip
            // across a recompile rather than emptying itself.
            Seed();
        }

        /// <summary>Writes the gap, the flow and the scroll settings, then lays the strip out.</summary>
        public void ApplyStyle()
        {
            if (!built)
                return;

            // No padding, ever. The elements are centred in the strip by the layout, and an inset that only shows
            // up once the strip is full is one more thing nobody would have guessed at.
            var padding = grid.padding;
            padding.left = 0;
            padding.top = 0;
            padding.right = 0;
            padding.bottom = 0;
            grid.padding = padding;

            grid.ColumnGap = style.Gap;
            grid.RowGap = style.Gap;
            grid.Flow = flow == EHistoryFlow.Horizontal ? EGridFlow.Row : EGridFlow.Column;

            scroller.scrollSensitivity = style.ScrollSensitivity;
            scroller.inertia = style.ScrollInertia;
            scroller.decelerationRate = style.ScrollDeceleration;
            scroller.movementType = ScrollRect.MovementType.Clamped;

            // Every element written again, not only the ones whose bet changed: a prefab that reads something
            // outside the bet - the player's own id, a currency, a setting - has just been given the chance to
            // read it again, and nothing else would have told it to.
            for (int i = 0; i < live.Count; i++)
            {
                var element = live[i];
                if (element != null && element.Data != null)
                    Fill(element, element.Data);
            }

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

            // Sorted before trimming, or the bet dropped for being oldest would be whichever happened to be at
            // the front of the list rather than the one that actually is.
            Sort();
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

            Sort();
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
        /// quick way to see an element without a server.</summary>
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
                    IPlayerId = "player-" + i.ToString(CultureInfo.InvariantCulture),
                    IPlayerName = "Player " + i.ToString(CultureInfo.InvariantCulture),
                    N = i + 1,
                    CreatedAt = 1700000000000L + i * 60000L,
                };

                data._Outcome = new GenericDictionary<string, string>
                {
                    { SampleKey, multiplier.ToString("0.00", CultureInfo.InvariantCulture) },
                };

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

            if (betInfo == null)
            {
                // Said once rather than once a click: a strip wired to OnPicked and nothing else is a perfectly
                // ordinary thing to build, and a log line per press would be noise. Said at all because the
                // other reading - a Bet Info field somebody meant to fill in - looks exactly the same from here.
                if (!warned)
                {
                    warned = true;
                    Debug.LogWarning(
                        $"{name}: no bet info window to open - point Bet Info at one. A click still raises OnPicked, so ignore this if the game opens its own.",
                        this
                    );
                }

                return;
            }

            if (string.IsNullOrEmpty(data.Id))
            {
                // A press that does nothing and says nothing is the worst kind of bug to be handed: it looks
                // like the click was missed.
                Debug.LogWarning($"{name}: that bet has no id, so there is nothing to open the bet info window on.", this);
                return;
            }

            // Show asks the server for the bet and opens on a loader, which is the right thing here: the
            // history payload is a summary, and the dialog wants the whole transaction with its seeds.
            betInfo.Show(data.Id);
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
        // safe to call twice. Elements left over from another prefab - the field was changed, or the strip was
        // built by an older version of the game - are thrown away instead: a prefab is not a chip that can be
        // restyled into a different one.
        private void Collect()
        {
            live.Clear();
            spare.Clear();
            shown.Clear();
            next.Clear();

            var found = content.GetComponentsInChildren<UiHistoryElement>(true);

            for (int i = 0; i < found.Length; i++)
            {
                var element = found[i];
                if (element == null || element.transform.parent != content)
                    continue;

                if (element.Source != elementPrefab)
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

            var element = Spawn();
            element.Adopt(this);
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
            element.Source = elementPrefab;

            // A template kept switched off in the scene is an ordinary way to hold an element - and Instantiate
            // copies that, so a strip built from one would fill up with elements nobody can see.
            if (!element.gameObject.activeSelf)
                element.gameObject.SetActive(true);

            return element;
        }

        // ------------------------------------------------------------------ placing

        private void Arrange()
        {
            bool horizontal = flow == EHistoryFlow.Horizontal;
            bool scrolling = overflow == EHistoryOverflow.Scroll;

            if (elementPrefab == null)
            {
                Blank();
                return;
            }

            // Said again if the prefab is taken away again. It is one warning per time there is nothing to draw
            // with, not one per session.
            missing = false;

            stepAlong = Step(horizontal);
            stepCross = Step(!horizontal);

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

        // Nothing to draw a bet with. The elements go back to the pool, the grid gets the one layout that hides
        // them, and it is said once: an empty rect on a component that looks configured reads as a strip that is
        // broken rather than as an Element Prefab nobody filled in.
        private void Blank()
        {
            for (int i = 0; i < live.Count; i++)
                Retire(live[i]);

            live.Clear();
            display.Clear();
            shown.Clear();
            arrival = null;
            held = 0;
            measured = Rect.rect.size;

            Nothing();
            Scroll(flow == EHistoryFlow.Horizontal, false);

            if (missing)
                return;

            missing = true;
            Debug.LogWarning(
                $"{name}: no Element Prefab, so there is nothing to draw a bet with. Point it at a prefab with a UiHistoryElement on its root.",
                this
            );
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

        // How many elements the strip has room for. Everything, unless it is clamping and the prefab has a size
        // along the flow to divide the room by - with an element that sizes itself there is nothing to count
        // with, and guessing would be worse than clipping.
        private int Room()
        {
            if (overflow != EHistoryOverflow.Clamp || stepAlong <= 0f)
                return int.MaxValue;

            var size = Rect.rect.size;
            float available = flow == EHistoryFlow.Horizontal ? size.x : size.y;

            // No width yet - the first frame of a canvas, or a strip whose parent has not been laid out. Showing
            // everything is the safer answer: LateUpdate arranges again as soon as there is a size, and one
            // frame of a full strip beats one frame of an empty one.
            if (available <= 1f)
                return int.MaxValue;

            int fit = Mathf.FloorToInt((available + style.Gap) / (stepAlong + style.Gap));
            return Mathf.Max(1, fit);
        }

        // How big one element is along an axis, and the prefab is the only one that knows: nothing here sizes an
        // element any more. Its own layout answer first - a Layout Element, or a label wide enough to have asked
        // for one - and the size it was drawn at where there is none.
        private float Step(bool horizontal)
        {
            if (elementPrefab == null)
                return 0f;

            var rect = elementPrefab.Rect;
            float step = horizontal ? LayoutUtility.GetPreferredWidth(rect) : LayoutUtility.GetPreferredHeight(rect);

            if (step <= 0f)
                step = horizontal ? rect.rect.width : rect.rect.height;

            return Mathf.Max(0f, step);
        }

        private void Name(bool horizontal)
        {
            bool crossFixed = stepCross > 0f;

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
            }

            for (int i = 0; i < spare.Count; i++)
            {
                if (spare[i] != null)
                    UiWindowParts.Item(spare[i].Rect).Area = SpareArea;
            }
        }

        // One track per element, plus a flexible one at whichever end the strip is not packed against. A
        // scrolling strip gets neither: its content is exactly as long as its elements, and a spacer in it would
        // be room to scroll into that has nothing in it.
        private void Shape(bool horizontal, bool scrolling)
        {
            if (live.Count == 0)
            {
                Nothing();
                return;
            }

            tracks.Clear();
            cells.Clear();

            bool spacing = !scrolling;

            if (spacing && align != EHistoryAlign.Start)
            {
                tracks.Add(GridTrack.Flexible());
                cells.Add(UiGridLayout.Empty);
            }

            for (int i = 0; i < live.Count; i++)
            {
                tracks.Add(stepAlong > 0f ? GridTrack.Fixed(stepAlong) : GridTrack.Auto());
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

        // One empty cell rather than no layout at all. A grid with no layout shows every child it has, which on
        // this one would be every spare element in the pool.
        private void Nothing()
        {
            grid.SetLayout(UiGridLayout
                .Build()
                .Columns(GridTrack.Flexible())
                .Rows(GridTrack.Flexible())
                .Row(UiGridLayout.Empty)
                .Done());
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
            element.Bind(data);
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

        // ------------------------------------------------------------------ the game's own payload

        /// <summary>A value out of a bet's own outcome payload, or empty. Both shapes of it are looked at - the
        /// flat one the socket fills in and the raw JSON it came as. What an element reads to find out what the
        /// round did.</summary>
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

        // Oldest first, by the time the server stamped each bet with. ON_HISTORY arrives as an array in whatever
        // order the server built it - newest first, as it happens, which is the opposite of the order arrivals
        // come in one at a time. Sorting is the only thing that makes a seeded strip and the bets that follow it
        // agree about which end is new.
        //
        // Insertion sort, and deliberately: the list is fifteen long, it is very nearly sorted already, and it
        // has to be stable so that bets sharing a timestamp - or a feed with no timestamps at all - keep the
        // order they arrived in.
        private void Sort()
        {
            if (!sortByTime)
                return;

            for (int i = 1; i < items.Count; i++)
            {
                var data = items[i];
                int at = i - 1;

                while (at >= 0 && items[at] != null && data != null && items[at].CreatedAt > data.CreatedAt)
                {
                    items[at + 1] = items[at];
                    at--;
                }

                items[at + 1] = data;
            }
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
            sampleCount = Mathf.Max(1, sampleCount);

            if (built && Application.isPlaying)
            {
                ApplyStyle();
                return;
            }

            // OnValidate is not allowed to activate, create or destroy anything, and both building the strip and
            // setting a grid's layout do some of that. Deferring by a frame puts it back on ordinary editor time,
            // where all of it is allowed.
            //
            // EnsureBuilt rather than a check on the flag: the flag is not serialized, so the first inspector
            // edit after a script reload finds a strip that is built on screen and not built as far as this class
            // knows. Bailing out there is what would make a gap edited in the editor appear to do nothing.
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null)
                    return;

                EnsureBuilt();
                ApplyStyle();
            };
        }
#endif
    }
}
