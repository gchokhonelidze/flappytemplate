using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // A window: a rounded panel with a caption, a close button, an area to put things in, and an opening
    // that is animated rather than a SetActive. Everything it is made of is built from code the first time
    // it is needed, so a window is one component on an empty RectTransform - there is no prefab to keep in
    // step with the palette, and no hierarchy to rebuild when a game decides its dialogs have square
    // corners after all.
    //
    //     UiWindowBuilder.Create(canvas, "Settings")
    //         .Size(360f, 480f)
    //         .Title("Settings")
    //         .Draggable()
    //         .Backdrop()
    //         .Open();
    //
    // Parts are found by name before they are made, so all of that survives being saved as a prefab and
    // rebuilt: a second EnsureBuilt reuses the caption that is already there rather than adding another.
    //
    // Whatever goes inside belongs under Content, which is inset from the panel by the style's padding and
    // starts below the caption. StatisticsWindow next door is one worked example of filling it.
    [AddComponentMenu("UI/Ui Window")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class UiWindow : MonoBehaviour
    {
        private const string CaptionName = "Caption";
        private const string TitleName = "Title";
        private const string ViewportName = "Viewport";
        private const string ContentName = "Content";
        private const string CloseName = "Close";
        private const string CrossName = "Cross";
        private const string ScrollbarName = "Scrollbar";
        private const string BackdropName = "Window Backdrop";

        // What the caption and the body answer to in the grid's layout. One word each, because a layout is
        // stored as text.
        private const string CaptionArea = "caption";
        private const string BodyArea = "body";

        [Header("Window")]
        [SerializeField]
        private string title = "Window";

        [SerializeField]
        private UiWindowStyle style = new UiWindowStyle();

        [SerializeField]
        private bool showCaption = true;

        [SerializeField]
        private bool showCloseButton = true;

        [Tooltip("Hide the window at Awake, so it waits for Open. Off leaves it exactly as the scene saved it.")]
        [SerializeField]
        private bool startClosed = true;

        [Tooltip("Destroy the window once it has finished closing, rather than leaving it hidden.")]
        [SerializeField]
        private bool destroyOnClose = false;

        [Header("Size")]
        [Tooltip("Ask the content how tall it wants to be whenever the window opens, and be that tall. Needs something under Content that reports a height - a layout group, a label, a Layout Element. Off, the window is whatever height it was given.")]
        [SerializeField]
        private bool fitContentHeight = false;

        [Tooltip("The tallest the window may be, in its own units. Zero means the parent it is drawn in - the screen, for a full-screen canvas - less Screen Margin.")]
        [Min(0f)]
        [SerializeField]
        private float maxHeight = 0f;

        [Tooltip("Room left above and below a window that has grown as far as it may. Only used while Max Height is zero.")]
        [Min(0f)]
        [SerializeField]
        private float screenMargin = 32f;

        [Tooltip("What happens when the content will not fit the height the window is allowed.")]
        [SerializeField]
        private EWindowScroll scroll = EWindowScroll.WhenTooTall;

        [Header("Dragging")]
        [SerializeField]
        private bool draggable = true;

        [Tooltip("Grab it anywhere rather than by the caption. Note that this catches drags on anything inside it that does not handle its own.")]
        [SerializeField]
        private bool dragAnywhere = false;

        [SerializeField]
        private bool clampToParent = true;

        [Tooltip("How much of a window too big for its parent has to stay inside it. Only comes into play on an axis the window does not fit - one that does is held wholly inside.")]
        [Min(0f)]
        [SerializeField]
        private float keepVisible = 64f;

        [Tooltip("Draw the window over its siblings when it is grabbed or opened.")]
        [SerializeField]
        private bool bringToFront = true;

        [Header("Backdrop")]
        [Tooltip("A full-parent sheet behind the window, which is what makes it modal - it swallows every click that misses.")]
        [SerializeField]
        private bool showBackdrop = false;

        [SerializeField]
        private bool closeOnBackdropClick = true;

        [Header("Sorting")]
        // Bring To Front only settles the window against its own siblings. Anything on another canvas, and
        // anything the scene draws rather than the canvas - sprites, meshes, particles - is sorted long
        // before sibling order is consulted, which is how a window ends up behind the game it belongs to.
        // A canvas of its own with Override Sorting is the answer to that, and a window is exactly the case
        // it exists for.
        [Tooltip("Give the window a canvas of its own and draw it above everything at a lower order. Off leaves it sorted by hierarchy position like any other UI object.")]
        [SerializeField]
        private bool alwaysOnTop = true;

        [Tooltip("Above every sprite and canvas below this number. The backdrop takes one less, so it stays behind its own window and over everything else.")]
        [SerializeField]
        private int sortingOrder = 100;

        [Tooltip("Empty keeps the parent canvas's layer. Only needed when the game draws on a sorting layer above the UI's, since a layer outranks any order within it.")]
        [SerializeField]
        private string sortingLayer = "";

        [Header("Transition")]
        [SerializeField]
        private EWindowTransition transition = EWindowTransition.ScaleFade;

        [Min(0f)]
        [SerializeField]
        private float openDuration = 0.28f;

        [Min(0f)]
        [SerializeField]
        private float closeDuration = 0.2f;

        [SerializeField]
        private Ease openEase = Ease.OutBack;

        [SerializeField]
        private Ease closeEase = Ease.InBack;

        [Tooltip("Run on unscaled time, so a window still opens over a paused game.")]
        [SerializeField]
        private bool unscaledTime = true;

        [Header("Events")]
        public UnityEvent OnOpened = new UnityEvent();

        public UnityEvent OnClosed = new UnityEvent();

        [Tooltip("The close button, before anything happens. Fires whether or not the window then closes.")]
        public UnityEvent OnCloseClicked = new UnityEvent();

        // Kept as serialized fields rather than looked up each time: a window saved as a prefab then keeps
        // its own parts, and a game that replaced one of them by hand is not overruled at the next build.
        [SerializeField, HideInInspector]
        private RoundedBox panel;

        [SerializeField, HideInInspector]
        private RoundedBox caption;

        [SerializeField, HideInInspector]
        private TextMeshProUGUI titleText;

        [SerializeField, HideInInspector]
        private UiGrid grid;

        [SerializeField, HideInInspector]
        private RectTransform viewport;

        [SerializeField, HideInInspector]
        private RectMask2D clip;

        [SerializeField, HideInInspector]
        private ScrollRect scroller;

        [SerializeField, HideInInspector]
        private Scrollbar scrollbar;

        [SerializeField, HideInInspector]
        private RoundedBox scrollTrack;

        [SerializeField, HideInInspector]
        private RoundedBox scrollHandle;

        [SerializeField, HideInInspector]
        private Image grab;

        [SerializeField, HideInInspector]
        private RectTransform content;

        [SerializeField, HideInInspector]
        private RoundedBox closeBox;

        [SerializeField, HideInInspector]
        private Button closeButton;

        [SerializeField, HideInInspector]
        private RectTransform crossIcon;

        [SerializeField, HideInInspector]
        private RoundedBox crossBarA;

        [SerializeField, HideInInspector]
        private RoundedBox crossBarB;

        [SerializeField, HideInInspector]
        private Image spriteIcon;

        [SerializeField, HideInInspector]
        private CanvasGroup group;

        [SerializeField, HideInInspector]
        private Image backdrop;

        // Serialized alongside the parts it stands for. A private bool would come back false after every
        // script reload, and a window in an open scene would then refuse to restyle until it was played.
        [SerializeField, HideInInspector]
        private bool built;

        private Sequence transitionTween;
        private bool finishing;

        // The height the window would be if it were allowed to be, whatever it ended up at. Kept so the clamp
        // can be worked out again when the room changes - a rotated phone, a resized player window - without
        // asking the content to measure itself a second time.
        private float wantedHeight;
        private float lastLimit = -1f;

        // The height the clamp last wrote, so a height set by hand since can be told from one of its own.
        private float appliedHeight = -1f;
        private bool scrolling;

        // The scale the window sits at when it is open, which is not necessarily 1: shrinking a whole window
        // by scaling its rect is a reasonable thing to do, and the panel and the text both stay sharp under
        // it. Every scale in the transition is measured against this rather than against one, or opening a
        // window would be the thing that threw the author's scale away.
        private Vector3 restScale = Vector3.one;

        public RectTransform Rect => (RectTransform)transform;

        /// <summary>Where whatever the window is for goes. Inset from the panel, and starts below the caption.</summary>
        public RectTransform Content
        {
            get
            {
                EnsureBuilt();
                return content;
            }
        }

        /// <summary>The masked cell the content sits in and scrolls inside. The grid gives it whatever the
        /// caption leaves.</summary>
        public RectTransform Viewport
        {
            get
            {
                EnsureBuilt();
                return viewport;
            }
        }

        /// <summary>The scroller, for a game that wants to drive it - jump to the bottom, read the position.
        /// Enabled only while the body is actually scrolling.</summary>
        public ScrollRect Scroller
        {
            get
            {
                EnsureBuilt();
                return scroller;
            }
        }

        /// <summary>The grid that lays the caption and the body out. Rows and layout are the window's own; the
        /// close button and the backdrop are not in it.</summary>
        public UiGrid Grid
        {
            get
            {
                EnsureBuilt();
                return grid;
            }
        }

        /// <summary>Whether the body is scrolling as things stand.</summary>
        public bool IsScrolling => scrolling;

        /// <summary>Ask the content how tall it wants to be on every open.</summary>
        public bool FitContentHeight
        {
            get => fitContentHeight;
            set => fitContentHeight = value;
        }

        /// <summary>The tallest the window may be. Zero means the parent, less <see cref="ScreenMargin"/>.</summary>
        public float MaxHeight
        {
            get => maxHeight;
            set
            {
                maxHeight = Mathf.Max(0f, value);
                Reclamp();
            }
        }

        /// <summary>Room left above and below a window that has grown as far as it may.</summary>
        public float ScreenMargin
        {
            get => screenMargin;
            set
            {
                screenMargin = Mathf.Max(0f, value);
                Reclamp();
            }
        }

        /// <summary>When the body scrolls rather than the window growing.</summary>
        public EWindowScroll Scroll
        {
            get => scroll;
            set
            {
                scroll = value;
                Reclamp();
            }
        }

        /// <summary>The panel itself, for anything the style does not reach.</summary>
        public RoundedBox Panel
        {
            get
            {
                EnsureBuilt();
                return panel;
            }
        }

        /// <summary>The header block. Also the drag handle, unless Drag Anywhere is on.</summary>
        public RoundedBox Caption
        {
            get
            {
                EnsureBuilt();
                return caption;
            }
        }

        public TextMeshProUGUI TitleText
        {
            get
            {
                EnsureBuilt();
                return titleText;
            }
        }

        public Button CloseButton
        {
            get
            {
                EnsureBuilt();
                return closeButton;
            }
        }

        /// <summary>The modal sheet, or null while Show Backdrop is off.</summary>
        public Image Backdrop => backdrop;

        public string Title
        {
            get => title;
            set
            {
                title = value;
                if (titleText != null)
                    titleText.text = value;
            }
        }

        /// <summary>Colours, sizes and fonts. Assigning one applies it; editing the one already there needs
        /// a call to <see cref="ApplyStyle"/> afterwards.</summary>
        public UiWindowStyle Style
        {
            get => style;
            set
            {
                style = value ?? new UiWindowStyle();
                ApplyStyle();
            }
        }

        public bool Draggable
        {
            get => draggable;
            set
            {
                draggable = value;
                ApplyDrag();
            }
        }

        public bool DragAnywhere
        {
            get => dragAnywhere;
            set
            {
                dragAnywhere = value;
                ApplyDrag();
            }
        }

        public bool ShowCloseButton
        {
            get => showCloseButton;
            set
            {
                showCloseButton = value;
                if (closeBox != null)
                    closeBox.gameObject.SetActive(value);
            }
        }

        public bool ShowCaption
        {
            get => showCaption;
            set
            {
                showCaption = value;
                ApplyStyle();
            }
        }

        public EWindowTransition Transition
        {
            get => transition;
            set => transition = value;
        }

        public float OpenDuration
        {
            get => openDuration;
            set => openDuration = Mathf.Max(0f, value);
        }

        public float CloseDuration
        {
            get => closeDuration;
            set => closeDuration = Mathf.Max(0f, value);
        }

        public Ease OpenEase
        {
            get => openEase;
            set => openEase = value;
        }

        public Ease CloseEase
        {
            get => closeEase;
            set => closeEase = value;
        }

        /// <summary>Animate on unscaled time, so a window still opens over a paused game.</summary>
        public bool UnscaledTime
        {
            get => unscaledTime;
            set => unscaledTime = value;
        }

        /// <summary>The sheet behind the window. Turning it on builds it; the colour comes from the style.</summary>
        public bool ShowBackdrop
        {
            get => showBackdrop;
            set
            {
                showBackdrop = value;
                ApplyStyle();
                ShowBackdropSheet(IsOpen);
            }
        }

        public bool CloseOnBackdropClick
        {
            get => closeOnBackdropClick;
            set => closeOnBackdropClick = value;
        }

        /// <summary>Draw on a canvas of its own, above everything sorting below <see cref="SortingOrder"/>.</summary>
        public bool AlwaysOnTop
        {
            get => alwaysOnTop;
            set
            {
                alwaysOnTop = value;
                ApplySorting();
            }
        }

        /// <summary>Where that canvas sorts. The backdrop takes one less.</summary>
        public int SortingOrder
        {
            get => sortingOrder;
            set
            {
                sortingOrder = value;
                ApplySorting();
            }
        }

        /// <summary>Sorting layer for that canvas, or empty for the parent canvas's own.</summary>
        public string SortingLayer
        {
            get => sortingLayer;
            set
            {
                sortingLayer = value;
                ApplySorting();
            }
        }

        public bool ClampToParent
        {
            get => clampToParent;
            set
            {
                clampToParent = value;
                ApplyDrag();
            }
        }

        /// <summary>How much of a window too big for its parent has to stay inside it.</summary>
        public float KeepVisible
        {
            get => keepVisible;
            set
            {
                keepVisible = Mathf.Max(0f, value);
                ApplyDrag();
            }
        }

        public bool BringToFront
        {
            get => bringToFront;
            set
            {
                bringToFront = value;
                ApplyDrag();
            }
        }

        /// <summary>Hide at Awake and wait for Open. Off leaves the window as the scene saved it.</summary>
        public bool StartClosed
        {
            get => startClosed;
            set => startClosed = value;
        }

        public bool DestroyOnClose
        {
            get => destroyOnClose;
            set => destroyOnClose = value;
        }

        /// <summary>The scale the window sits at when it is open. Setting the rect's scale by hand does the
        /// same thing - this is read back from it whenever the window opens - but assigning it here also
        /// takes effect on a window that is already on screen.</summary>
        // Scaling the rect is a fair way to size a whole window down: the panel is generated geometry and the
        // text is SDF, so neither softens the way a sprite would.
        public Vector3 RestScale
        {
            get => restScale;
            set
            {
                restScale = value;

                if (transitionTween == null)
                    Rect.localScale = value;
            }
        }

        /// <summary>The same scale on both axes - the usual case.</summary>
        public void SetScale(float uniform) => RestScale = new Vector3(uniform, uniform, 1f);

        /// <summary>True from the moment Open is called to the moment the closing animation has finished.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>Whether the parts exist yet. False on a window built by the builder and not yet woken.</summary>
        public bool IsBuilt => built;

        void Awake()
        {
            restScale = Rect.localScale;

            EnsureBuilt();
            ApplyStyle();

            // Open is one of the things that can get us here. A window saved switched off has never woken,
            // so the SetActive inside Open is what runs this - and hiding it again on the way through would
            // swallow the call that woke it, leaving a window that appears on the second press and not the
            // first. IsOpen is set before that SetActive precisely so this can tell the two apart.
            if (IsOpen)
                return;

            if (!startClosed)
            {
                IsOpen = true;
                return;
            }

            ResetTransform();
            ShowBackdropSheet(false);
            gameObject.SetActive(false);
        }

        void OnDisable()
        {
            // Not while the closing sequence is delivering its own OnComplete - that is what deactivated the
            // object in the first place, and killing a tween from inside its own callback is asking for it.
            if (finishing)
                return;

            bool interrupted = transitionTween != null && transitionTween.IsActive();
            KillTransition();

            if (!interrupted)
                return;

            // The window was taken off screen part-way through an animation: a parent switched off, a panel
            // hidden by hand, a scene unloaded. The tween has to go, and the transform has to come back with
            // it - a rect left at nine tenths of its scale is exactly what the next Open reads as the window's
            // resting scale, and a dialog that loses a tenth of itself per interrupted animation ends up too
            // small to see. Which looks, from the outside, like a window that stopped opening at all.
            ResetTransform();

            if (group != null)
            {
                group.alpha = IsOpen ? 1f : 0f;
                group.blocksRaycasts = IsOpen;
            }
        }

        // A phone that turned, a player window that was dragged wider: the room the window is allowed changes
        // without anything asking it to, and a window that was clamped to the old screen would be left either
        // scrolling for no reason or hanging off the new one. Only ever a float compare per frame, and only for
        // a window that has been fitted at all.
        void LateUpdate()
        {
            if (!built || wantedHeight <= 0f || transitionTween != null)
                return;

            if (!Mathf.Approximately(Limit(), lastLimit))
                Clamp();
        }

        void OnDestroy()
        {
            KillTransition();

            if (backdrop != null)
                UiWindowParts.Discard(backdrop.gameObject);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (!built || !Application.isPlaying)
            {
                // OnValidate is not allowed to activate or destroy anything, and applying a style does both.
                // Deferring by a frame puts it back on ordinary editor time, where it is.
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this == null)
                        return;

                    if (built)
                        ApplyStyle();
                };

                return;
            }

            ApplyStyle();
        }
#endif

        /// <summary>Makes whatever the window is missing. Safe to call at any point and as often as you
        /// like - parts that exist are found by name rather than made again.</summary>
        public void EnsureBuilt()
        {
            // The viewport and the grid are in that list because a window saved by a version that had neither
            // comes back saying it is built. Missing parts are what "built" is really asking about, so the flag
            // alone would leave that window with a null grid and an ApplyStyle that throws.
            if (!built || panel == null || content == null || viewport == null || grid == null)
                BuildParts();

            // Outside that check, and deliberately. A listener added with AddListener is not serialized, so
            // the ones put on when the window was built in the editor are gone by the time the scene is
            // played - while the parts they were added to, and the flag saying the window is built, both
            // survive. Hooking only where the parts are made leaves a close button that does nothing.
            HookEvents();
        }

        private void BuildParts()
        {
            if (panel == null)
                panel = GetComponent<RoundedBox>();
            if (panel == null)
                panel = gameObject.AddComponent<RoundedBox>();

            if (group == null)
                group = GetComponent<CanvasGroup>();
            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();

            if (grid == null)
                grid = UiWindowParts.Grid(Rect);

            caption = UiWindowParts.Box(transform, CaptionName);
            titleText = UiWindowParts.Label(caption.transform, TitleName);

            viewport = UiWindowParts.Rect(transform, ViewportName);

            // A window built before the viewport existed has its Content at the root, with whatever the game
            // put in it. Moved rather than replaced: finding parts by name would otherwise make a second,
            // empty Content inside the viewport and leave the full one orphaned beside it.
            var stray = UiWindowParts.Find<RectTransform>(transform, ContentName);
            if (stray != null)
                stray.SetParent(viewport, false);

            content = UiWindowParts.Rect(viewport, ContentName);

            // The mask is what makes scrolling look like scrolling rather than like content sliding over the
            // caption. RectMask2D rather than Mask: it costs no stencil pass, and both RoundedBox and TMP
            // clip against it - a rounded box is generated geometry on the default UI material, which reads
            // the clip rect like any other graphic.
            if (clip == null)
                clip = viewport.GetComponent<RectMask2D>();
            if (clip == null)
                clip = viewport.gameObject.AddComponent<RectMask2D>();

            if (scroller == null)
                scroller = viewport.GetComponent<ScrollRect>();
            if (scroller == null)
                scroller = viewport.gameObject.AddComponent<ScrollRect>();

            // Something for a finger to take hold of. A ScrollRect is only offered a drag - or a wheel - if the
            // pointer lands on a raycast target that is the scroller itself or something under it, and every
            // graphic a window puts in its body is deliberately not one: labels, cards and rules should not
            // swallow clicks. So the scroller wears an invisible one of its own, over the whole body and behind
            // everything in it, which is what makes touch scrolling work at all. Switched off along with the
            // scrolling, so a window that is not scrolling neither draws it nor catches anything with it.
            if (grab == null)
                grab = viewport.GetComponent<Image>();
            if (grab == null)
                grab = viewport.gameObject.AddComponent<Image>();

            grab.color = new Color(0f, 0f, 0f, 0f);
            grab.raycastTarget = true;

            BuildScrollbar();

            closeBox = UiWindowParts.Box(transform, CloseName);
            closeButton = closeBox.GetComponent<Button>();
            if (closeButton == null)
                closeButton = closeBox.gameObject.AddComponent<Button>();

            closeButton.targetGraphic = closeBox;

            crossIcon = UiWindowParts.Rect(closeBox.transform, CrossName);
            crossBarA = UiWindowParts.Box(crossIcon, "Bar A");
            crossBarB = UiWindowParts.Box(crossIcon, "Bar B");
            spriteIcon = UiWindowParts.Picture(closeBox.transform, "Icon");

            // The body last, so it draws over the caption if the two ever overlap - a window whose caption is
            // taller than the grid's row allows for should be covered by its content, not cover it. The close
            // button goes after that, since it is over everything by definition.
            viewport.SetAsLastSibling();
            closeBox.rectTransform.SetAsLastSibling();

            // The two cells the grid arranges, and the one overlay it must keep its hands off. An ignored child
            // is not placed, not sized, and not shown or hidden by the layout either, which is what lets Show
            // Close Button stay an ordinary SetActive.
            UiWindowParts.Name(caption.rectTransform, CaptionArea);
            UiWindowParts.Name(viewport, BodyArea);
            UiWindowParts.Ignore(closeBox.rectTransform, true);

            built = true;
            ApplyDrag();
        }

        // A track down the right of the body with a handle in it, in the shape UGUI's Scrollbar expects: the
        // graphic on the bar itself, a sliding area inside it, and the handle inside that. The Scrollbar writes
        // the handle's anchors as it moves, so nothing here sets its position - only what it looks like.
        private void BuildScrollbar()
        {
            scrollTrack = UiWindowParts.Box(viewport, ScrollbarName);

            if (scrollbar == null)
                scrollbar = scrollTrack.GetComponent<Scrollbar>();
            if (scrollbar == null)
                scrollbar = scrollTrack.gameObject.AddComponent<Scrollbar>();

            var slide = UiWindowParts.Rect(scrollTrack.transform, "Sliding Area");
            UiWindowParts.Stretch(slide, 0f, 0f, 0f, 0f);

            scrollHandle = UiWindowParts.Box(slide, "Handle");

            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = scrollHandle.rectTransform;
            scrollbar.targetGraphic = scrollHandle;
            scrollbar.transition = Selectable.Transition.None;
        }

        /// <summary>Puts the window's own listeners back on its buttons. Called on every build and every
        /// load, because AddListener does not survive being saved and reloaded.</summary>
        // Removed before it is added, every time: UnityEvent compares a listener by its target and method
        // rather than by the delegate object, so this is what keeps a second call from firing Close twice.
        private void HookEvents()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HandleCloseClicked);
                closeButton.onClick.AddListener(HandleCloseClicked);
            }

            if (backdrop == null)
                return;

            var sheet = backdrop.GetComponent<Button>();
            if (sheet == null)
                return;

            sheet.onClick.RemoveListener(HandleBackdropClicked);
            sheet.onClick.AddListener(HandleBackdropClicked);
        }

        /// <summary>Pushes every colour, size and font from the style onto the parts. Cheap enough to call
        /// whenever a theme changes, including on a window that is already open.</summary>
        [ContextMenu("Apply Style")]
        public void ApplyStyle()
        {
            if (style == null)
                style = new UiWindowStyle();

            if (!built || panel == null || caption == null || titleText == null || content == null
                || closeBox == null || viewport == null || grid == null)
                return;

            float border = Mathf.Max(0f, style.BorderSize);

            panel.FillGradientMode = EFillGradient.None;
            panel.FillColor = style.Fill;
            panel.SetCornerRadius(style.CornerRadius);
            panel.SetBorderSize(border);
            panel.SetBorderColor(style.BorderColor);
            panel.EdgeSoftness = style.EdgeSoftness;
            panel.raycastTarget = true;

            // The caption and the body are two rows of one column, inset by the border so the caption wash sits
            // inside the outline rather than over it. Said as a layout rather than by switching the caption on
            // and off: a grid takes its layout as the whole truth about which of its children are showing, and
            // re-asserts it every time it is enabled - so a caption hidden with SetActive would come back.
            int inset = Mathf.RoundToInt(border);
            grid.padding = new RectOffset(inset, inset, inset, inset);
            grid.RowGap = 0f;
            grid.ColumnGap = 0f;

            var arrangement = UiGridLayout.Build().Columns(GridTrack.Flexible());

            if (showCaption)
            {
                arrangement.Rows(GridTrack.Fixed(style.CaptionHeight), GridTrack.Flexible())
                    .Row(CaptionArea)
                    .Row(BodyArea);
            }
            else
            {
                arrangement.Rows(GridTrack.Flexible()).Row(BodyArea);
            }

            grid.SetLayout(arrangement.Done());

            caption.FillGradientMode = EFillGradient.None;
            caption.FillColor = style.CaptionFill;
            caption.SetBorderSize(0f);

            float captionRadius = Mathf.Max(0f, style.CornerRadius - border);
            caption.RadiusTopLeft = captionRadius;
            caption.RadiusTopRight = captionRadius;
            caption.RadiusBottomRight = 0f;
            caption.RadiusBottomLeft = 0f;
            caption.EdgeSoftness = style.EdgeSoftness;
            caption.raycastTarget = true;

            UiWindowParts.Stretch(titleText.rectTransform, 12f, style.TitleTopInset, 12f, 6f);
            titleText.text = title;
            titleText.font = style.TitleFont != null ? style.TitleFont : titleText.font;
            titleText.fontSize = style.TitleSize;
            titleText.color = style.TitleColor;
            titleText.fontStyle = style.TitleStyle;
            titleText.alignment = style.TitleAlignment;
            titleText.raycastTarget = false;

            ApplyScrollStyle();
            ApplyScrollState();
            ApplyCloseStyle();
            ApplyBackdropStyle();
            ApplySorting();
        }

        // A nested canvas takes its graphics out of the parent canvas's draw call and sorts them on its own
        // sortingLayer and sortingOrder, which is what lifts a window clear of everything else on screen -
        // other canvases included, and sprites, which sort against a canvas by exactly these two numbers.
        //
        // It takes the graphics out of the parent's raycast list at the same time, so the GraphicRaycaster
        // is not optional: without one the window would draw on top and still let every click through to
        // whatever is behind it.
        private void ApplySorting()
        {
            Sort(gameObject, alwaysOnTop, sortingOrder);

            // One below the window, so the sheet is over the game and under the dialog it belongs to.
            if (backdrop != null)
                Sort(backdrop.gameObject, alwaysOnTop && showBackdrop, sortingOrder - 1);
        }

        private void Sort(GameObject target, bool on, int order)
        {
            var canvas = target.GetComponent<Canvas>();

            if (!on)
            {
                // The component is left where it is rather than pulled off: it may have been put there by
                // hand for its own reasons, and removing it would take the raycaster's registration with it.
                if (canvas != null)
                    canvas.overrideSorting = false;

                return;
            }

            if (canvas == null)
                canvas = target.AddComponent<Canvas>();

            canvas.overrideSorting = true;
            canvas.sortingOrder = order;

            if (!string.IsNullOrEmpty(sortingLayer))
                ApplySortingLayer(canvas);

            if (target.GetComponent<GraphicRaycaster>() == null)
                target.AddComponent<GraphicRaycaster>();
        }

        // Checked against the project's layers first, because assigning a name that is not one of them logs
        // an error per assignment - and this runs on every style pass.
        private void ApplySortingLayer(Canvas canvas)
        {
            foreach (var layer in UnityEngine.SortingLayer.layers)
            {
                if (layer.name != sortingLayer)
                    continue;

                canvas.sortingLayerID = layer.id;
                return;
            }
        }

        // ------------------------------------------------------------------ how tall it may be

        /// <summary>Asks the content how tall it wants to be and makes the window that tall - as far as it is
        /// allowed, after which the body scrolls instead.</summary>
        // The content has to be able to answer: something under Content that reports a height, which a layout
        // group, a label or a Layout Element all do and a plain panel does not. A window whose content is
        // arranged by hand should work out its own number and call FitTo instead.
        public void Fit()
        {
            EnsureBuilt();
            FitTo(Chrome + Body());
        }

        /// <summary>Makes the window a height the caller worked out, clamped to what it is allowed - after
        /// which the body scrolls rather than the window growing.</summary>
        public void FitTo(float height)
        {
            EnsureBuilt();

            wantedHeight = Mathf.Max(0f, height);
            Clamp();
        }

        /// <summary>Back to the top of the body. Where a dialog shown again should start.</summary>
        public void ScrollToTop()
        {
            if (scroller != null && scroller.content != null)
                scroller.verticalNormalizedPosition = 1f;
        }

        // The part of the height that is not content: the caption's row and the border above and below it. The
        // content's own padding is inside the viewport and counted with the content.
        private float Chrome => (showCaption ? style.CaptionHeight : 0f) + Mathf.Max(0f, style.BorderSize) * 2f;

        // What the content says it needs, plus the padding around it. Measured immediately rather than waited
        // for: a window is fitted at the moment it opens, and a layout that has not run yet reports the height
        // it had last time.
        private float Body()
        {
            if (content == null)
                return 0f;

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            float wanted = Mathf.Max(0f, LayoutUtility.GetPreferredHeight(content));
            return wanted + style.ContentPaddingTop + style.ContentPaddingBottom;
        }

        // Height, in the window's own units, that the parent has room for. Divided by the scale because the
        // window is measured in its own space and drawn in its parent's - a window at half scale fits twice as
        // much of itself on screen, which is the same reasoning the drag clamp uses.
        private float Limit()
        {
            if (maxHeight > 0f)
                return maxHeight;

            var parent = Rect.parent as RectTransform;
            float room = parent != null ? parent.rect.height : Screen.height;
            room -= Mathf.Max(0f, screenMargin) * 2f;

            float scale = Mathf.Max(0.0001f, Mathf.Abs(restScale.y));
            return Mathf.Max(0f, room / scale);
        }

        private void Clamp()
        {
            lastLimit = Limit();

            float limit = lastLimit > 0f ? lastLimit : float.MaxValue;
            float height = Mathf.Min(wantedHeight, limit);

            var rect = Rect;
            if (height > 0f)
            {
                if (!Mathf.Approximately(rect.sizeDelta.y, height))
                    rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);

                appliedHeight = height;
            }

            ApplyScroll(wantedHeight > height + 0.5f);
        }

        // The height on the rect is either what this last wrote or what somebody set by hand, and the two mean
        // very different things: one is a window already held to the screen, the other is a fresh answer to how
        // tall the window wants to be. Without telling them apart, a window clamped once would take the clamped
        // height for its wish the next time it opened and stop scrolling - quietly clipping the rest.
        private void Adopt()
        {
            float current = Rect.sizeDelta.y;

            if (appliedHeight < 0f || !Mathf.Approximately(current, appliedHeight))
                wantedHeight = current;
        }

        // Only worth doing when the window already knows how tall it wants to be - before the first fit there
        // is nothing to clamp, and clamping to nothing would collapse a window that was sized by hand.
        private void Reclamp()
        {
            if (built && wantedHeight > 0f)
                Clamp();
        }

        private void ApplyScroll(bool needed)
        {
            bool was = scrolling;
            scrolling = scroll == EWindowScroll.Always || (scroll == EWindowScroll.WhenTooTall && needed);

            ApplyScrollState();

            // A body that has just started scrolling starts at the top. Without this it would keep whatever
            // position the last bet, or the last tab, left it at.
            if (scrolling && !was)
                ScrollToTop();
        }

        // Whatever scrolling currently is, written onto the parts. Called by the style pass as well, so a window
        // that has never been fitted has its mask and its scroller off rather than however Unity made them.
        private void ApplyScrollState()
        {
            if (scroller != null)
            {
                scroller.viewport = viewport;
                scroller.content = content;
                scroller.horizontal = false;
                scroller.vertical = true;
                scroller.movementType = ScrollRect.MovementType.Clamped;
                scroller.scrollSensitivity = style.ScrollSensitivity;
                scroller.verticalScrollbar = style.ShowScrollbar ? scrollbar : null;

                // A flick that carries on after the finger has left is what a touch screen expects, and WebGL on
                // a phone is a touch screen.
                scroller.inertia = style.ScrollInertia;
                scroller.decelerationRate = style.ScrollDeceleration;

                // Permanent, and the bar switched on and off here instead: the automatic modes resize the
                // viewport to make room, and the viewport is a cell of the grid - two things writing one rect.
                scroller.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
                scroller.enabled = scrolling;
            }

            // Nothing to clip while nothing moves, and a mask that is off is a mask that costs nothing.
            if (clip != null)
                clip.enabled = scrolling;

            // Only in the way while there is something to scroll. Off, a drag that started in the body reaches
            // whatever is behind it - the window's own handle, for a window that is dragged from anywhere.
            if (grab != null)
                grab.enabled = scrolling;

            if (scrollTrack != null)
                scrollTrack.gameObject.SetActive(scrolling && style.ShowScrollbar);

            ApplyBody();
        }

        // Where the content sits inside the viewport. Not scrolling, it fills it, inset by the padding, exactly
        // as it did when the window laid itself out by hand. Scrolling, it is as tall as it asked to be and
        // anchored to the top, which is the shape a ScrollRect moves.
        private void ApplyBody()
        {
            if (content == null || viewport == null)
                return;

            float left = style.ContentPaddingLeft;
            float top = style.ContentPaddingTop;
            float bottom = style.ContentPaddingBottom;
            float right = style.ContentPaddingRight;

            if (scrolling && style.ShowScrollbar)
                right += style.ScrollbarWidth + Mathf.Max(0f, style.ScrollbarInset);

            if (!scrolling)
            {
                UiWindowParts.Stretch(content, left, top, right, bottom);
                return;
            }

            float height = Mathf.Max(0f, wantedHeight - Chrome - top - bottom);

            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(-(left + right), height);
            content.anchoredPosition = new Vector2((left - right) * 0.5f, -top);
        }

        private void ApplyScrollStyle()
        {
            if (scrollTrack == null || scrollHandle == null)
                return;

            var bar = scrollTrack.rectTransform;

            // Down the right of the viewport, inset by the same padding as the content so the two line up.
            bar.anchorMin = new Vector2(1f, 0f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(1f, 0.5f);
            bar.sizeDelta = new Vector2(style.ScrollbarWidth, -(style.ContentPaddingTop + style.ContentPaddingBottom));
            bar.anchoredPosition = new Vector2(
                -Mathf.Max(0f, style.ScrollbarInset),
                (style.ContentPaddingBottom - style.ContentPaddingTop) * 0.5f);

            float radius = style.ScrollbarCornerRadius < 0f ? 100000f : style.ScrollbarCornerRadius;

            Paint(scrollTrack, style.ScrollbarTrackColor, radius);
            scrollTrack.raycastTarget = true;

            // UGUI's Scrollbar moves its handle by writing the anchors and nothing else, so the handle's size
            // and position have to be nothing at all for it to sit exactly in the band it is given. Left at the
            // 100 by 100 a new rect comes with, it draws a hundred units past the track on every side - which
            // reads as a scrollbar that is enormous and a Scrollbar Width that does nothing.
            var handle = scrollHandle.rectTransform;
            handle.sizeDelta = Vector2.zero;
            handle.anchoredPosition = Vector2.zero;

            Paint(scrollHandle, style.ScrollbarHandleColor, radius);
            scrollHandle.raycastTarget = true;
        }

        private void Paint(RoundedBox box, Color fill, float radius)
        {
            box.FillGradientMode = EFillGradient.None;
            box.FillColor = fill;
            box.SetBorderSize(0f);
            box.SetCornerRadius(radius);
            box.EdgeSoftness = style.EdgeSoftness;
        }

        /// <summary>Opens with the transition. Already open, it is left alone.</summary>
        public void Open() => Open(true);

        /// <summary>Straight on, no tween. For restoring a window that was open when the game was saved.</summary>
        public void OpenInstant() => Open(false);

        public void Open(bool animated)
        {
            EnsureBuilt();
            ApplyStyle();

            if (IsOpen && gameObject.activeSelf && transitionTween == null)
            {
                // Already open and settled - but said to the transform rather than taken on trust. This is the
                // way out of a window that is open as far as this class knows and invisible on screen: whatever
                // left it part-scaled or transparent, the press that asks for it again puts it right instead of
                // returning to a dialog nobody can see.
                ResetTransform();
                return;
            }

            // Between transitions the rect is at its resting scale, so whatever is on it now is what the
            // author last set - including a scale changed since the window was built. Mid-transition it is a
            // frame of the animation and says nothing, which is why this is not read unconditionally.
            if (transitionTween == null)
                restScale = Rect.localScale;

            KillTransition();

            // Before the SetActive, not after: on a window that has never woken, that call runs Awake here
            // and now, and Awake reads this to know it is not the one deciding whether the window is open.
            IsOpen = true;
            gameObject.SetActive(true);

            // After the SetActive, because measuring content that has never been enabled measures nothing, and
            // before the transition, because the transition animates the size the window is about to have.
            //
            // Both ways round, a window is held to the screen on the way in: one that was sized by hand and is
            // taller than the room it has scrolls too, without anybody having to ask for it.
            if (fitContentHeight)
            {
                Fit();
            }
            else
            {
                Adopt();
                Clamp();
            }

            if (bringToFront)
                transform.SetAsLastSibling();

            ShowBackdropSheet(true);

            PlayTransition(true, animated, () => OnOpened.Invoke());
        }

        /// <summary>Closes with the transition. The window is hidden - or destroyed, with Destroy On Close -
        /// once it has finished, and OnClosed fires then rather than now.</summary>
        public void Close() => Close(true);

        public void CloseInstant() => Close(false);

        public void Close(bool animated)
        {
            if (!IsOpen && !gameObject.activeSelf)
                return;

            EnsureBuilt();
            KillTransition();
            IsOpen = false;

            PlayTransition(false, animated, () =>
            {
                finishing = true;
                ShowBackdropSheet(false);
                ResetTransform();

                if (destroyOnClose)
                {
                    OnClosed.Invoke();
                    finishing = false;
                    UiWindowParts.Discard(gameObject);
                    return;
                }

                gameObject.SetActive(false);
                finishing = false;
                OnClosed.Invoke();
            });
        }

        public void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        /// <summary>Back to the middle of the parent, after a drag has left it somewhere awkward.</summary>
        public void Center()
        {
            Rect.anchoredPosition = Vector2.zero;
        }

        /// <summary>Rebuilds the parts from scratch. The way out of a window whose hierarchy was edited into
        /// a state the style can no longer describe.</summary>
        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            built = false;
            EnsureBuilt();
            ApplyStyle();
        }

        private void HandleCloseClicked()
        {
            OnCloseClicked.Invoke();
            Close();
        }

        private void ApplyCloseStyle()
        {
            closeBox.gameObject.SetActive(showCloseButton);
            UiWindowParts.Pin(closeBox.rectTransform, new Vector2(1f, 1f), style.CloseSize, style.CloseOffset);

            closeBox.FillGradientMode = EFillGradient.None;
            closeBox.FillColor = style.CloseFill;
            closeBox.SetBorderSize(style.CloseBorderSize);
            closeBox.SetBorderColor(style.CloseBorderColor);
            closeBox.EdgeSoftness = style.EdgeSoftness;
            closeBox.raycastTarget = true;

            // A radius larger than the box is held to it, so any negative value means "as round as it goes"
            // and stays a circle whatever the button is resized to.
            closeBox.SetCornerRadius(style.CloseCornerRadius < 0f ? 100000f : style.CloseCornerRadius);

            float span = Mathf.Min(style.CloseSize.x, style.CloseSize.y) * Mathf.Clamp01(style.CloseIconScale);
            bool useSprite = style.CloseIcon != null;

            if (spriteIcon != null)
            {
                spriteIcon.gameObject.SetActive(useSprite);
                spriteIcon.sprite = style.CloseIcon;
                spriteIcon.color = style.CloseIconColor;
                spriteIcon.preserveAspect = true;
                spriteIcon.raycastTarget = false;
                UiWindowParts.Pin(spriteIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(span, span), Vector2.zero);
            }

            if (crossIcon == null || crossBarA == null || crossBarB == null)
                return;

            crossIcon.gameObject.SetActive(!useSprite);
            UiWindowParts.Pin(crossIcon, new Vector2(0.5f, 0.5f), new Vector2(span, span), Vector2.zero);

            // Two bars across the same middle, turned against each other. Drawn rather than fetched from an
            // atlas: a cross is two rectangles, and this way it is the right thickness at any button size.
            Bar(crossBarA, span, 45f);
            Bar(crossBarB, span, -45f);
        }

        private void Bar(RoundedBox bar, float span, float angle)
        {
            UiWindowParts.Pin(bar.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(span, style.CloseIconThickness), Vector2.zero);
            bar.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            bar.FillGradientMode = EFillGradient.None;
            bar.FillColor = style.CloseIconColor;
            bar.SetBorderSize(0f);
            bar.SetCornerRadius(100000f);
            bar.EdgeSoftness = style.EdgeSoftness;
            bar.raycastTarget = false;
        }

        private void ApplyDrag()
        {
            if (caption == null)
                return;

            var host = dragAnywhere ? gameObject : caption.gameObject;
            var other = dragAnywhere ? caption.gameObject : gameObject;

            var stale = other.GetComponent<UiWindowDragHandle>();
            if (stale != null)
                UiWindowParts.Discard(stale);

            var handle = host.GetComponent<UiWindowDragHandle>();

            if (!draggable)
            {
                if (handle != null)
                    UiWindowParts.Discard(handle);

                return;
            }

            if (handle == null)
                handle = host.AddComponent<UiWindowDragHandle>();

            handle.Target = Rect;
            handle.ClampToParent = clampToParent;
            handle.KeepVisible = keepVisible;
            handle.BringToFront = bringToFront;
            handle.Interactable = true;
        }

        private void ApplyBackdropStyle()
        {
            if (!showBackdrop)
            {
                if (backdrop != null)
                    backdrop.gameObject.SetActive(false);

                return;
            }

            if (backdrop == null)
            {
                var parent = transform.parent;
                if (parent == null)
                    return;

                backdrop = UiWindowParts.Picture(parent, BackdropName);
                backdrop.raycastTarget = true;
            }

            var button = backdrop.GetComponent<Button>();
            if (button == null)
                button = backdrop.gameObject.AddComponent<Button>();

            button.targetGraphic = backdrop;
            button.transition = Selectable.Transition.None;

            // Outside the creation branch: a sheet that was made in the editor comes back from the scene
            // file with its Button and without the listener, since AddListener is not something that is
            // saved. HookEvents covers the same ground on load; this covers a backdrop switched on later.
            button.onClick.RemoveListener(HandleBackdropClicked);
            button.onClick.AddListener(HandleBackdropClicked);

            UiWindowParts.Stretch(backdrop.rectTransform, 0f, 0f, 0f, 0f);
            backdrop.color = style.BackdropColor;
        }

        private void HandleBackdropClicked()
        {
            if (closeOnBackdropClick)
                Close();
        }

        private void ShowBackdropSheet(bool visible)
        {
            if (backdrop == null)
                return;

            backdrop.gameObject.SetActive(visible && showBackdrop);

            if (!visible)
                return;

            // Taking the window's own place pushes the window one along, which leaves the sheet immediately
            // behind it however many other windows are open.
            backdrop.transform.SetSiblingIndex(transform.GetSiblingIndex());
        }

        private void PlayTransition(bool opening, bool animated, Action done)
        {
            var rect = Rect;
            float duration = opening ? openDuration : closeDuration;

            if (!animated || transition == EWindowTransition.None || duration <= 0f || !gameObject.activeInHierarchy)
            {
                ResetTransform();
                group.alpha = opening ? 1f : 0f;
                group.blocksRaycasts = opening;
                done?.Invoke();
                return;
            }

            bool fades = transition == EWindowTransition.Fade || transition == EWindowTransition.ScaleFade;
            bool scales = transition == EWindowTransition.Scale || transition == EWindowTransition.ScaleFade;
            Vector2 offset = SlideOffset();
            bool slides = offset != Vector2.zero;

            // Read where the window rests now rather than remembering it from the last time: a window that
            // has been dragged rests where the player left it, and must come back to there.
            Vector2 rest = rect.anchoredPosition;
            float openScale = Mathf.Max(0.0001f, style.OpenScale);

            if (opening)
            {
                if (slides)
                    rect.anchoredPosition = rest + offset;
                if (scales)
                    rect.localScale = restScale * openScale;
                if (fades)
                    group.alpha = 0f;
            }

            // Off for the whole animation either way: a window on its way in is not something to click yet,
            // and one on its way out has already been dismissed.
            group.blocksRaycasts = false;

            var ease = opening ? openEase : closeEase;
            var sequence = DOTween.Sequence().SetUpdate(unscaledTime);

            if (slides)
            {
                var to = opening ? rest : rest + offset;
                sequence.Join(DOTween
                    .To(() => rect.anchoredPosition, v => rect.anchoredPosition = v, to, duration)
                    .SetEase(ease)
                    .SetUpdate(unscaledTime));
            }

            if (scales)
            {
                var to = opening ? restScale : restScale * openScale;
                sequence.Join(DOTween
                    .To(() => rect.localScale, v => rect.localScale = v, to, duration)
                    .SetEase(ease)
                    .SetUpdate(unscaledTime));
            }

            if (fades)
            {
                // Deliberately not the window's own ease: OutBack on an alpha overshoots past opaque and
                // back, which is a flicker rather than a bounce.
                sequence.Join(DOTween
                    .To(() => group.alpha, a => group.alpha = a, opening ? 1f : 0f, duration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(unscaledTime));
            }

            if (backdrop != null && showBackdrop)
            {
                var sheet = backdrop;
                var target = style.BackdropColor;
                var from = target;
                from.a = 0f;

                if (opening)
                    sheet.color = from;

                sequence.Join(DOTween
                    .To(() => sheet.color, c => sheet.color = c, opening ? target : from, duration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(unscaledTime));
            }

            transitionTween = sequence;
            sequence.OnComplete(() =>
            {
                transitionTween = null;

                // Both ways: a window that slid out has to be put back where it rests before it is hidden,
                // or the next Open would take the off-screen position for its resting place and slide in
                // from somewhere else entirely.
                rect.anchoredPosition = rest;

                if (opening)
                {
                    rect.localScale = restScale;
                    group.alpha = 1f;
                    group.blocksRaycasts = true;
                }

                done?.Invoke();
            });
        }

        // Slides are measured against the parent, so a window leaves the screen rather than moving its own
        // width and stopping where it can still be seen. Named for the way the window travels as it opens.
        private Vector2 SlideOffset()
        {
            var rect = Rect;
            var parent = rect.parent as RectTransform;
            float width = parent != null ? parent.rect.width : Screen.width;
            float height = parent != null ? parent.rect.height : Screen.height;

            // The window's own size taken into the parent's space, for the same reason the drag clamp does
            // it: the travel is measured against the parent, and a scaled window covers less of it than its
            // rect says.
            float ownWidth = rect.rect.width * Mathf.Abs(restScale.x);
            float ownHeight = rect.rect.height * Mathf.Abs(restScale.y);

            switch (transition)
            {
                case EWindowTransition.SlideUp:
                    return new Vector2(0f, -(height * 0.5f + ownHeight));
                case EWindowTransition.SlideDown:
                    return new Vector2(0f, height * 0.5f + ownHeight);
                case EWindowTransition.SlideLeft:
                    return new Vector2(width * 0.5f + ownWidth, 0f);
                case EWindowTransition.SlideRight:
                    return new Vector2(-(width * 0.5f + ownWidth), 0f);
                default:
                    return Vector2.zero;
            }
        }

        private void ResetTransform()
        {
            var rect = Rect;
            rect.localScale = restScale;

            if (group != null)
            {
                group.alpha = 1f;
                group.blocksRaycasts = true;
            }
        }

        private void KillTransition()
        {
            if (transitionTween != null && transitionTween.IsActive())
                transitionTween.Kill();

            transitionTween = null;
        }
    }
}
