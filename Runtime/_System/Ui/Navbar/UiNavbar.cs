using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // The row of buttons over the game: home, statistics, fairness, and whatever the game adds beside them.
    //
    //     var navbar = UiNavbar.Create(canvas);
    //
    // Like the windows next door it is one component on an empty RectTransform - the strip, the buttons and
    // the glyphs in them are all built from code the first time they are needed, so there is no prefab to
    // keep in step and no hierarchy to rebuild when a game decides its chrome looks different after all.
    //
    // What each button does:
    //
    // - **Home** leaves the game for SystemDto.ReturnUrl, which the server sends over the socket. A
    //   FlappyBet build is nearly always an iframe inside an operator's page, so the whole page is sent
    //   there rather than the frame the canvas is drawn in - pointing the frame at a lobby would draw that
    //   lobby inside the game's rectangle. Navigator is where that happens, and its readme section below
    //   is worth reading before this button is trusted in an unfamiliar embed.
    //
    //   The button **hides itself while there is no such address**, which is the usual case for a demo or a
    //   direct link. A Home button that led nowhere would be worse than no button, and the address arrives
    //   after the scene has loaded rather than with it, so this is watched rather than decided once.
    //
    // - **Statistics** and **Fairness** open the windows of those names, and close them again on a second
    //   press. Either is found in the scene if it is already there and built if it is not, so a bar dropped
    //   into an empty canvas works on its own; a window built by hand and dragged into the slot is used
    //   instead. While one is open its button takes the style's active colour, so a player can see which
    //   dialog they are looking at from the bar rather than from the dialog.
    //
    // - **Custom** does nothing of its own: it fires the slot's event and the bar's, for a button of the
    //   game's own. Adding a *built-in* one later is three lines - a value on ENavbarButton, a glyph in
    //   UiNavbarIcons.Draw and a case in Press - and that is the shape the switch to a vertical or
    //   horizontal game mode is meant to arrive in.
    //
    // The bar itself runs either way round - Flow, a row or a column - and pins itself to a corner or an
    // edge of whatever it is drawn in. That is the bar's own direction and has nothing to do with the game
    // being played in portrait or landscape.
    //
    // It runs in edit mode as well as in play mode, so the inspector is a live thing: a colour, a size, a
    // flow or a whole button changes the bar as it is typed rather than on the next press of play. That is
    // what [ExecuteAlways] is doing here - the parts the bar is made of are not authored, they are built
    // from these fields, and a field whose result cannot be seen is a field being guessed at.
    [AddComponentMenu("UI/Ui Navbar")]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class UiNavbar : MonoBehaviour
    {
        private const string ButtonName = "Button ";
        private const string IconName = "Icon";
        private const string PictureName = "Picture";
        private const string LabelName = "Label";

        [SerializeField]
        private UiNavbarStyle style = new UiNavbarStyle();

        [Header("Buttons")]
        [SerializeField]
        private List<NavbarButton> buttons = new List<NavbarButton>
        {
            new NavbarButton(ENavbarButton.Home, "Home"),
            new NavbarButton(ENavbarButton.Statistics, "Statistics"),
            new NavbarButton(ENavbarButton.Fairness, "Fairness"),
        };

        [Header("Layout")]
        [SerializeField]
        private ENavbarFlow flow = ENavbarFlow.Horizontal;

        [Tooltip("Where the buttons sit along a bar longer than they need. Only read while Fit To Buttons is off, since a fitted bar has nothing left over.")]
        [SerializeField]
        private ENavbarAlign align = ENavbarAlign.Center;

        [Tooltip("Size the bar to exactly the buttons showing in it, padding included. Wants a rect that is pinned rather than stretched - which is what Dock leaves it as.")]
        [SerializeField]
        private bool fitToButtons = true;

        [Tooltip("Pin the bar to a corner or an edge of whatever it is drawn in. Off leaves the rect exactly as the scene saved it.")]
        [SerializeField]
        private bool dock = true;

        [SerializeField]
        private ERectAnchor dockTo = ERectAnchor.TopRight;

        [Tooltip("From that corner, inwards. Both numbers are read as a distance from the edge they are nearest, so the same pair works in any corner.")]
        [SerializeField]
        private Vector2 dockOffset = new Vector2(24f, 24f);

        [Header("Windows")]
        [Tooltip("Left empty, the window is found in the scene, and built under this bar's canvas if there is none to find.")]
        [SerializeField]
        private StatisticsWindow statisticsWindow;

        [SerializeField]
        private FairnessWindow fairnessWindow;

        [Tooltip("Build a window that is neither wired up nor already in the scene. Off leaves the button doing nothing but firing its event, for a game that opens its own dialogs.")]
        [SerializeField]
        private bool createWindows = true;

        [Tooltip("Where a window built by this bar is put. Empty means the canvas the bar is on - which is what a window wants, rather than being parented into the bar and moving with it.")]
        [SerializeField]
        private Transform windowParent;

        [Tooltip("A second press on the same button closes the window again. Off leaves it open, for a bar beside a game that closes its own dialogs.")]
        [SerializeField]
        private bool toggleWindows = true;

        [Header("Home")]
        [Tooltip("Used instead of the address the server sends. For trying the button out in the editor, where there is no socket to send one - a build should leave this empty.")]
        [SerializeField]
        private string returnUrl = "";

        [Tooltip("Drop the Home button while there is nowhere to return to. Off draws it anyway, and a press does nothing but fire On Home.")]
        [SerializeField]
        private bool hideHomeWithoutUrl = true;

        [Header("Behaviour")]
        [Tooltip("Watch the socket for the return address arriving, and the windows for being opened and closed.")]
        [SerializeField]
        private bool followState = true;

        [Header("Events")]
        [Tooltip("Any button, before whatever the button itself does.")]
        public UnityEvent<ENavbarButton> OnPressed = new UnityEvent<ENavbarButton>();

        [Tooltip("The Home button, before the page is sent anywhere. Fires whether or not there was an address to send it to.")]
        public UnityEvent OnHome = new UnityEvent();

        private RoundedBox bar;

        private readonly List<NavbarButton> shown = new List<NavbarButton>();
        private readonly List<RoundedBox> boxes = new List<RoundedBox>();
        private readonly List<RectTransform> icons = new List<RectTransform>();
        private readonly List<Image> pictures = new List<Image>();
        private readonly List<TextMeshProUGUI> captions = new List<TextMeshProUGUI>();

        // Which of the built buttons were on screen the last time they were placed. Compared rather than
        // recomputed into, so the return address arriving is a re-layout and every other broadcast is not.
        private readonly List<bool> placed = new List<bool>();

        private bool built;
        private bool listening;

        public RectTransform Rect => (RectTransform)transform;

        /// <summary>Colours, sizes and fonts of the strip, the buttons and the glyphs. Edit and call
        /// <see cref="Layout"/>, or assign a whole new one.</summary>
        public UiNavbarStyle Style
        {
            get => style;
            set
            {
                style = value ?? new UiNavbarStyle();
                Rebuild();
            }
        }

        /// <summary>The slots, in the order they are drawn. Call <see cref="Rebuild"/> after changing it.</summary>
        public List<NavbarButton> Buttons => buttons;

        /// <summary>Which way the bar runs. The bar's own direction - nothing to do with the game being
        /// played in portrait or landscape.</summary>
        public ENavbarFlow Flow
        {
            get => flow;
            set
            {
                flow = value;
                Layout();
            }
        }

        /// <summary>Where the buttons sit along a bar longer than they need.</summary>
        public ENavbarAlign Align
        {
            get => align;
            set
            {
                align = value;
                Layout();
            }
        }

        /// <summary>Size the bar to exactly the buttons showing in it. Off leaves the rect the size it was
        /// given, which is what a bar stretched across the screen wants.</summary>
        public bool FitToButtons
        {
            get => fitToButtons;
            set
            {
                fitToButtons = value;
                Layout();
            }
        }

        /// <summary>Pin the bar to a corner or an edge of what it is drawn in. Off leaves the rect's own
        /// anchors alone - set this before placing a bar by hand, or the next layout pass writes over
        /// it.</summary>
        public bool Docked
        {
            get => dock;
            set
            {
                dock = value;
                Layout();
            }
        }

        public ERectAnchor DockTo
        {
            get => dockTo;
            set
            {
                dockTo = value;
                Layout();
            }
        }

        /// <summary>From that corner, inwards, on both axes.</summary>
        public Vector2 DockOffset
        {
            get => dockOffset;
            set
            {
                dockOffset = value;
                Layout();
            }
        }

        /// <summary>Used instead of the address the server sends. Empty is the normal case.</summary>
        public string ReturnUrl
        {
            get => returnUrl;
            set
            {
                returnUrl = value ?? string.Empty;
                Refresh();
            }
        }

        /// <summary>Where the Home button would take the player: the override if one is set, and what the
        /// server sent otherwise. Null or empty means nowhere, which is what the button hides on.</summary>
        public string Destination => !string.IsNullOrWhiteSpace(returnUrl) ? returnUrl : Navigator.ReturnUrl;

        /// <summary>Whether there is anywhere to return to.</summary>
        public bool CanReturn => Navigator.IsNavigable(Destination);

        /// <summary>The statistics window this bar opens: the one it was given, the one already in the
        /// scene, or one built under the canvas. Null only when there is none of the three - which is what
        /// Create Windows being off leaves.</summary>
        public StatisticsWindow Statistics
        {
            get
            {
                if (statisticsWindow == null)
                    statisticsWindow = Resolve<StatisticsWindow>(parent => StatisticsWindow.Create(parent));

                Watch(statisticsWindow != null ? statisticsWindow.Window : null);
                return statisticsWindow;
            }
        }

        /// <summary>The fairness window this bar opens, found or built the same way.</summary>
        public FairnessWindow Fairness
        {
            get
            {
                if (fairnessWindow == null)
                    fairnessWindow = Resolve<FairnessWindow>(parent => FairnessWindow.Create(parent));

                Watch(fairnessWindow != null ? fairnessWindow.Window : null);
                return fairnessWindow;
            }
        }

        /// <summary>Whether the parts exist yet.</summary>
        public bool IsBuilt => built;

        /// <summary>Builds a bar - strip, buttons and all - under a parent, usually a canvas.</summary>
        // The object is made switched off and turned on at the end, the same as UiWindowBuilder does it: a
        // MonoBehaviour added to an active object runs Awake there and then, and a bar that woke halfway
        // through this would have built itself before it had been told which corner it lives in.
        public static UiNavbar Create(Transform parent, string name = "Navbar", ENavbarFlow flow = ENavbarFlow.Horizontal)
        {
            var created = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RoundedBox));
            created.SetActive(false);

            var rect = (RectTransform)created.transform;

            if (parent != null)
            {
                rect.SetParent(parent, false);
                created.layer = parent.gameObject.layer;
            }

            rect.sizeDelta = new Vector2(220f, 72f);

            var navbar = created.AddComponent<UiNavbar>();
            navbar.flow = flow;

            created.SetActive(true);

            // Awake has done this already in play mode. In the editor nothing else ever will, and a bar
            // built from a context menu that came out empty would be a poor way to find that out.
            navbar.EnsureBuilt();

            return navbar;
        }

        void Awake()
        {
            EnsureBuilt();
        }

        void OnEnable()
        {
            EnsureBuilt();
            Listen(true);

            // The captions are written through Translator.Label, and a caption that is one word in English
            // may be two in German - so a language change is a re-layout rather than a repaint.
            Translator.OnLocaleChanged += Layout;

            Refresh();
        }

        void OnDisable()
        {
            Listen(false);
            Translator.OnLocaleChanged -= Layout;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // Deferred, and by a named method rather than a closure so the same call is not queued twice for
            // one edit. OnValidate is not allowed to make or destroy anything, and a rebuild does both - a
            // slot whose kind was changed in the inspector has a glyph to take away and another to draw. It
            // also runs mid-import and mid-undo, where neither is safe. A frame later is ordinary editor
            // time, where both are.
            UnityEditor.EditorApplication.delayCall -= ValidateDeferred;
            UnityEditor.EditorApplication.delayCall += ValidateDeferred;
        }

        // Nothing is asked about whether the bar has been built yet, and that is the point: `built` is not
        // serialized, so it comes back false after every script reload, every entry into play mode and every
        // time the editor is opened - while the buttons it stands for are still there in the scene. A guard
        // on it here is what made an inspector edit do nothing at all until something else had rebuilt the
        // bar first. Rebuild finds the parts that exist by name and makes only what is missing, so calling
        // it unasked is cheap and always correct.
        private void ValidateDeferred()
        {
            if (this == null)
                return;

            Rebuild();
        }
#endif

        /// <summary>Makes whatever is missing and lays it out. Safe to call as often as you like.</summary>
        public void EnsureBuilt()
        {
            if (built && bar != null && Intact())
            {
                // Outside the build, and deliberately: a listener added with AddListener is not serialized,
                // so the ones put on when the bar was built in the editor are gone by the time the scene is
                // played, while the buttons they were added to survive. Hooking only where the parts are
                // made leaves a row of buttons that do nothing.
                Hook();
                return;
            }

            Rebuild();
        }

        // Whether the buttons this bar thinks it has are still there. A button deleted from the hierarchy by
        // hand leaves a null in the list, and every pass over it from then on would step around a hole.
        private bool Intact()
        {
            for (int i = 0; i < boxes.Count; i++)
            {
                if (boxes[i] == null)
                    return false;
            }

            return true;
        }

        /// <summary>Builds the strip and a button per enabled slot from scratch, then lays them out. Call
        /// after changing the style or the button list from code.</summary>
        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            if (style == null)
                style = new UiNavbarStyle();

            BuildBar();
            BuildButtons();

            built = true;

            Hook();
            Layout();
        }

        /// <summary>Places the strip and everything in it, and repaints. Cheap enough to call whenever
        /// something changes: past the first pass it is a few dozen writes to rects that already exist,
        /// since every part is found by the name it was made under.</summary>
        [ContextMenu("Layout")]
        public void Layout()
        {
            if (!built || style == null)
                return;

            ApplyVisibility();
            ApplyDock();
            Place();
            Paint();
        }

        /// <summary>Reads the state again: whether there is anywhere to return to, and which windows are
        /// open. Re-places the buttons only when that changed what is on screen.</summary>
        public void Refresh()
        {
            if (!built)
                return;

            if (ApplyVisibility())
            {
                ApplyDock();
                Place();
            }

            Paint();
        }

        // ------------------------------------------------------------------ what the buttons do

        /// <summary>Does whatever a slot's kind says, and fires the events either way. Public so a game can
        /// drive the bar from a key or a gamepad rather than from a press.</summary>
        public void Press(ENavbarButton kind)
        {
            OnPressed.Invoke(kind);

            switch (kind)
            {
                case ENavbarButton.Home:
                    GoHome();
                    break;
                case ENavbarButton.Statistics:
                    ShowStatistics();
                    break;
                case ENavbarButton.Fairness:
                    ShowFairness();
                    break;
                default:
                    // Custom. The slot's own event has already gone out.
                    break;
            }
        }

        /// <summary>Leaves the game for the return address, taking the whole page with it rather than the
        /// frame the build is drawn in. False means there was nowhere to go, and nothing happened but the
        /// event.</summary>
        public bool GoHome()
        {
            OnHome.Invoke();

            var url = Destination;
            if (Navigator.LeaveTo(url))
                return true;

            // Only reachable with the button drawn anyway - Hide Home Without Url off - or with an address
            // the browser would not have been sent to. Either is worth a line in the console rather than a
            // press that quietly does nothing.
            Debug.LogWarning(string.IsNullOrWhiteSpace(url)
                ? "UiNavbar: no return address. The server sends it as SystemDto.ReturnUrl."
                : $"UiNavbar: will not navigate to \"{url}\" - only http and https addresses are followed.");

            return false;
        }

        /// <summary>Opens the statistics window, or closes it again. For wiring straight onto a key.</summary>
        public void ShowStatistics() => Toggle(Statistics != null ? Statistics.Window : null);

        /// <summary>Opens the fairness window, or closes it again.</summary>
        public void ShowFairness() => Toggle(Fairness != null ? Fairness.Window : null);

        private void Toggle(UiWindow window)
        {
            if (window == null)
                return;

            if (window.IsOpen && toggleWindows)
            {
                window.Close();
                return;
            }

            window.Open();
        }

        // ------------------------------------------------------------------ building

        private void BuildBar()
        {
            if (bar == null)
                bar = GetComponent<RoundedBox>();
            if (bar == null)
                bar = gameObject.AddComponent<RoundedBox>();
        }

        private void BuildButtons()
        {
            shown.Clear();
            boxes.Clear();
            icons.Clear();
            pictures.Clear();
            captions.Clear();
            placed.Clear();

            int index = 0;
            foreach (var slot in buttons)
            {
                if (slot == null || !slot.Enabled)
                    continue;

                var box = UiWindowParts.Box(transform, ButtonName + index);
                var icon = UiWindowParts.Rect(box.transform, IconName);
                var picture = UiWindowParts.Picture(box.transform, PictureName);
                var caption = UiWindowParts.Label(box.transform, LabelName);

                shown.Add(slot);
                boxes.Add(box);
                icons.Add(icon);
                pictures.Add(picture);
                captions.Add(caption);
                placed.Add(true);

                index++;
            }

            // Buttons built for a longer list last time. Left behind they would draw over the ones that are
            // still wanted, since nothing but this loop knows they are no longer in it.
            //
            // Only the ones this bar made: a game is free to park something of its own under the bar - a
            // badge, a divider, a tooltip - and a sweep of every child would take it away on the next
            // rebuild. The strip itself is this object's own component rather than a child, so it is never
            // in the way here.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (!child.name.StartsWith(ButtonName))
                    continue;

                var box = child.GetComponent<RoundedBox>();
                if (box == null || !boxes.Contains(box))
                    UiWindowParts.Discard(child.gameObject);
            }
        }

        // The press handler carries which slot it belongs to, which a listener taken by name could not, so
        // this is a closure - and a closure cannot be removed by comparing it to another one. Hence
        // RemoveAllListeners, which drops only the ones added from code: anything wired onto a generated
        // button in the inspector is a persistent call and survives it.
        private void Hook()
        {
            for (int i = 0; i < boxes.Count && i < shown.Count; i++)
            {
                var box = boxes[i];
                if (box == null)
                    continue;

                var button = box.GetComponent<Button>();
                if (button == null)
                    button = box.gameObject.AddComponent<Button>();

                button.targetGraphic = box;
                button.transition = Selectable.Transition.None;

                var slot = shown[i];
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    slot.OnPressed?.Invoke();
                    Press(slot.Kind);
                });
            }
        }

        // ------------------------------------------------------------------ placing

        // Which buttons are on screen as things stand. True when that moved, since only then is there any
        // point in placing them again.
        private bool ApplyVisibility()
        {
            bool changed = false;

            for (int i = 0; i < shown.Count && i < boxes.Count; i++)
            {
                bool visible = Visible(shown[i]);

                if (i < placed.Count && placed[i] != visible)
                {
                    placed[i] = visible;
                    changed = true;
                }

                if (boxes[i] != null && boxes[i].gameObject.activeSelf != visible)
                    boxes[i].gameObject.SetActive(visible);
            }

            return changed;
        }

        private bool Visible(NavbarButton slot)
        {
            if (slot == null || !slot.Enabled)
                return false;

            if (slot.Kind != ENavbarButton.Home || !hideHomeWithoutUrl)
                return true;

            if (CanReturn)
                return true;

            // No StateManager anywhere and no game running: a scene being laid out rather than played in,
            // where there is no socket to have sent an address over. The button is drawn so it can be seen,
            // measured and styled - the same reasoning as the statistics window showing sample figures to a
            // scene with nothing feeding it. A build always has a StateManager, so this never hides a real
            // absence from a player.
            return !Application.isPlaying && StateManager.Inst == null;
        }

        private void ApplyDock()
        {
            if (!dock)
                return;

            var corner = Corner(dockTo);
            var parent = Rect.parent as RectTransform;
            var host = DockHost();

            // Both numbers are read as a distance from the edge they are nearest, so one pair of offsets
            // means the same thing in every corner - a bar moved from the top right to the bottom left in
            // the inspector stays the same distance in rather than jumping off the screen.
            var position = new Vector2(Inwards(corner.x, dockOffset.x), Inwards(corner.y, dockOffset.y));

            if (host != null && parent != null && host != parent)
            {
                // The parent has no rect to speak of - an empty holder, or a grid cell the layout never gave
                // an area to - and an anchor is a *fraction* of the parent's rect. In one that is nothing by
                // nothing every fraction lands on the same point, so top right, centre and bottom left all
                // come out in one place and only the pivot appears to move anything. Which reads, from the
                // outside, exactly like the corners being wired up wrong.
                //
                // So the corner is measured on the nearest rect that has a size - the canvas, usually - and
                // carried back into the parent's own space by hand. Dock says pin me to a corner of what I
                // am drawn in, and a collapsed holder is not what anybody means by that. LateUpdate is what
                // keeps it there afterwards.
                var wanted = parent.InverseTransformPoint(host.TransformPoint(Point(host.rect, corner)));
                position += (Vector2)wanted - Point(parent.rect, corner);
            }

            // Written once rather than as they are worked out: this runs every frame while the bar is
            // docking through a borrowed rect, and a rect told its position twice is a layout rebuilt twice.
            Rect.anchorMin = corner;
            Rect.anchorMax = corner;
            Rect.pivot = corner;
            Rect.anchoredPosition = position;
        }

        // A rect measured against the parent's own, so a bar that is docked normally is left exactly as it
        // was - and one hanging off a collapsed holder is followed for as long as it hangs there. A canvas
        // that changed size, a phone that turned, a grid that moved the holder: none of them are watched by
        // anything else, because a borrowed rect is not what the anchors are pointing at.
        void LateUpdate()
        {
            if (!dock || !built)
                return;

            var host = DockHost();
            if (host == null || host == Rect.parent)
                return;

            ApplyDock();
        }

        /// <summary>The rect the dock corner is measured on: the parent, or the nearest thing above it that
        /// has a size when the parent has none.</summary>
        public RectTransform DockHost()
        {
            var rect = Rect.parent as RectTransform;

            while (rect != null)
            {
                // A rect a whole unit across on both sides. Anything smaller is a holder that has not been
                // given a size rather than an area a bar was meant to be pinned inside.
                var area = rect.rect;
                if (area.width > 1f && area.height > 1f)
                    return rect;

                rect = rect.parent as RectTransform;
            }

            return null;
        }

        // Where a corner falls inside a rect, in that rect's own space.
        private static Vector2 Point(Rect area, Vector2 corner) => new Vector2(
            Mathf.Lerp(area.xMin, area.xMax, corner.x),
            Mathf.Lerp(area.yMin, area.yMax, corner.y));

        // On an axis the bar is centred on there is no near edge to measure from, so the offset is left as
        // it was written and reads as a nudge off the middle.
        private static float Inwards(float edge, float offset)
        {
            if (edge > 0.5f)
                return -offset;

            return offset;
        }

        private static Vector2 Corner(ERectAnchor anchor)
        {
            switch (anchor)
            {
                case ERectAnchor.TopLeft:
                    return new Vector2(0f, 1f);
                case ERectAnchor.Top:
                    return new Vector2(0.5f, 1f);
                case ERectAnchor.TopRight:
                    return new Vector2(1f, 1f);
                case ERectAnchor.Left:
                    return new Vector2(0f, 0.5f);
                case ERectAnchor.Right:
                    return new Vector2(1f, 0.5f);
                case ERectAnchor.BottomLeft:
                    return new Vector2(0f, 0f);
                case ERectAnchor.Bottom:
                    return new Vector2(0.5f, 0f);
                case ERectAnchor.BottomRight:
                    return new Vector2(1f, 0f);
                default:
                    return new Vector2(0.5f, 0.5f);
            }
        }

        private void Place()
        {
            bool horizontal = flow == ENavbarFlow.Horizontal;
            var size = new Vector2(Mathf.Max(1f, style.ButtonSize.x), Mathf.Max(1f, style.ButtonSize.y));

            float pad = style.ShowBar ? Mathf.Max(0f, style.BarPadding) : 0f;
            float along = horizontal ? size.x : size.y;
            float across = horizontal ? size.y : size.x;

            int count = Count();
            float spacing = Mathf.Max(0f, style.ButtonSpacing);
            float run = count > 0 ? count * along + (count - 1) * spacing : 0f;

            if (fitToButtons)
            {
                Rect.sizeDelta = horizontal
                    ? new Vector2(run + pad * 2f, across + pad * 2f)
                    : new Vector2(across + pad * 2f, run + pad * 2f);
            }

            // A fitted bar is exactly as long as its buttons, so the room is the run itself rather than
            // something read back off a rect that Unity has not resolved yet.
            float room = fitToButtons
                ? run
                : Mathf.Max(0f, (horizontal ? Rect.rect.width : Rect.rect.height) - pad * 2f);

            float gap = spacing;
            float cursor = 0f;

            switch (align)
            {
                case ENavbarAlign.Center:
                    cursor = (room - run) * 0.5f;
                    break;
                case ENavbarAlign.End:
                    cursor = room - run;
                    break;
                case ENavbarAlign.Spread:
                    if (count > 1)
                        gap = Mathf.Max(spacing, (room - count * along) / (count - 1));
                    else
                        cursor = (room - run) * 0.5f;
                    break;
                default:
                    break;
            }

            for (int i = 0; i < boxes.Count && i < shown.Count; i++)
            {
                if (!placed[i])
                    continue;

                var rect = boxes[i].rectTransform;

                if (horizontal)
                {
                    // Pinned to the left edge and centred across, so the button's own left edge lands at
                    // exactly the cursor whatever the bar ends up being.
                    UiWindowParts.Pin(rect, new Vector2(0f, 0.5f), size, new Vector2(pad + cursor, 0f));
                }
                else
                {
                    UiWindowParts.Pin(rect, new Vector2(0.5f, 1f), size, new Vector2(0f, -(pad + cursor)));
                }

                rect.localRotation = Quaternion.identity;

                PlaceContents(i, size);
                cursor += along + gap;
            }
        }

        private void PlaceContents(int index, Vector2 size)
        {
            bool labelled = style.ShowLabels;
            float band = labelled ? Mathf.Max(0f, style.LabelHeight) + Mathf.Max(0f, style.LabelGap) : 0f;

            // The glyph gets what the caption leaves, and is square in it: an icon that grew taller than
            // the room above the caption would sit behind it.
            float room = Mathf.Max(0f, Mathf.Min(size.x, size.y - band));
            float span = room * Mathf.Clamp01(style.IconScale);

            var icon = icons[index];
            var picture = pictures[index];
            var caption = captions[index];
            var slot = shown[index];

            // Shifted up by half the caption's band, so the glyph is centred in what is left rather than in
            // the whole button.
            var middle = new Vector2(0f, band * 0.5f);

            bool sprite = slot.Icon != null;

            UiWindowParts.Pin(icon, new Vector2(0.5f, 0.5f), new Vector2(span, span), middle);
            icon.localRotation = Quaternion.identity;
            icon.gameObject.SetActive(!sprite);

            if (!sprite)
                UiNavbarIcons.Draw(icon, slot.Kind, span, style.IconColor, style.ButtonFill, style.IconThickness);

            picture.gameObject.SetActive(sprite);
            picture.sprite = slot.Icon;
            picture.color = style.IconColor;
            picture.preserveAspect = true;
            picture.raycastTarget = false;
            UiWindowParts.Pin(picture.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(span, span), middle);

            caption.gameObject.SetActive(labelled);

            if (!labelled)
                return;

            UiWindowParts.Pin(caption.rectTransform, new Vector2(0.5f, 0f), new Vector2(size.x, Mathf.Max(0f, style.LabelHeight)), Vector2.zero);

            // Through the translator rather than straight from the field, the same as every other caption
            // the package draws: "Statistics" is the en_US wording of statistics.title and comes back in the
            // player's language, and a word nothing knows is printed as it was typed. See
            // Translations/README.md.
            caption.text = Translator.Label(slot.Label);
            caption.font = style.LabelFont != null ? style.LabelFont : caption.font;
            caption.fontSize = style.LabelSize;
            caption.color = style.LabelColor;
            caption.fontStyle = style.LabelStyle;
            caption.alignment = TextAlignmentOptions.Center;
            caption.raycastTarget = false;
        }

        private int Count()
        {
            int count = 0;

            for (int i = 0; i < placed.Count; i++)
            {
                if (placed[i])
                    count++;
            }

            return count;
        }

        // ------------------------------------------------------------------ painting

        private void Paint()
        {
            if (bar != null)
            {
                // Nothing showing means no strip either: a bar whose only button was Home, on a game with
                // nowhere to return to, would otherwise be an empty tab of colour in the corner.
                bar.enabled = style.ShowBar && Count() > 0;
                bar.FillGradientMode = EFillGradient.None;
                bar.FillColor = style.BarFill;
                bar.SetBorderSize(0f);
                bar.SetCornerRadius(style.BarCornerRadius);
                bar.EdgeSoftness = 1.25f;

                // A strip that is drawn swallows the clicks that miss its buttons, which is what stops a
                // press on the bar reaching the game behind it. One that is not drawn catches nothing.
                bar.raycastTarget = style.ShowBar;
            }

            for (int i = 0; i < boxes.Count && i < shown.Count; i++)
            {
                var box = boxes[i];
                if (box == null)
                    continue;

                box.FillGradientMode = EFillGradient.None;
                box.FillColor = Active(shown[i].Kind) ? style.ButtonActiveFill : style.ButtonFill;
                box.SetBorderSize(0f);
                box.SetCornerRadius(style.ButtonCornerRadius);
                box.EdgeSoftness = 1.25f;
                box.raycastTarget = true;
            }
        }

        // Whether the window a button opens is on screen. Only the windows this bar already knows about are
        // asked: resolving one here would build a dialog because the bar was repainted.
        private bool Active(ENavbarButton kind)
        {
            switch (kind)
            {
                case ENavbarButton.Statistics:
                    return statisticsWindow != null && statisticsWindow.Window != null && statisticsWindow.Window.IsOpen;
                case ENavbarButton.Fairness:
                    return fairnessWindow != null && fairnessWindow.Window != null && fairnessWindow.Window.IsOpen;
                default:
                    return false;
            }
        }

        // ------------------------------------------------------------------ the state and the windows

        private void Listen(bool on)
        {
            var manager = StateManager.Inst;
            if (manager == null || manager.Events == null || !followState)
                return;

            if (on && !listening)
            {
                manager.Events.OnSystem.AddListener(HandleSystem);
                listening = true;
            }
            else if (!on && listening)
            {
                manager.Events.OnSystem.RemoveListener(HandleSystem);
                listening = false;
            }
        }

        // The return address arrives with the rest of the system state, after the scene has loaded rather
        // than with it, so this is what puts the Home button on screen at all.
        private void HandleSystem(SystemDto value) => Refresh();

        // A window opening or closing is what the active colour is read from, and neither goes through the
        // socket. Added by name rather than as a closure, so removing before adding is enough to keep one
        // listener however often a window is resolved.
        private void Watch(UiWindow window)
        {
            if (window == null || !followState)
                return;

            window.OnOpened.RemoveListener(Refresh);
            window.OnOpened.AddListener(Refresh);
            window.OnClosed.RemoveListener(Refresh);
            window.OnClosed.AddListener(Refresh);
        }

        // The one already in the scene before one is built, so a game that laid its dialogs out by hand
        // keeps them - including a window that is switched off, which every closed one is.
        private T Resolve<T>(System.Func<Transform, T> build) where T : Component
        {
            var found = FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (found != null)
                return found;

            if (!createWindows)
                return null;

            return build(WindowHost());
        }

        // The canvas rather than the bar: a window parented into the bar would move with it, be clipped by
        // it, and sit in the corner the bar was pinned to.
        private Transform WindowHost()
        {
            if (windowParent != null)
                return windowParent;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                return canvas.rootCanvas != null ? canvas.rootCanvas.transform : canvas.transform;

            return transform.parent != null ? transform.parent : transform;
        }
    }
}
