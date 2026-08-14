using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // A window: a rounded panel with a caption, a close button, an area to put things in, and an opening
    // that is animated rather than a SetActive. Everything it is made of is built from code the first time
    // it is needed, so a window is one component on an empty RectTransform - there is no prefab to keep in
    // step, and no hierarchy to rebuild when a game decides its dialogs look different after all.
    //
    //     UiWindowBuilder.Create(canvas, "Settings")
    //         .Size(360f, 480f)
    //         .Title("Settings")
    //         .Draggable()
    //         .Backdrop()
    //         .Open();
    //
    // What this class owns is where things go, not what they look like: the caption's height, the inset of
    // the content, the width of the scrollbar, and the rules about growing, scrolling, dragging and sorting.
    // Colours, corners, borders and fonts belong to the parts themselves - select Panel (this object),
    // Caption, Title, Close or Scrollbar in the hierarchy and style each as you would any other RoundedBox
    // or TextMeshPro label. Nothing here paints over them afterwards.
    //
    // A window whose parts are made for the first time is seeded with a plain dark dialog, so one created
    // from the menu arrives looking like a window rather than like a white square. That happens once, at the
    // moment each part is made, and never again - so a window styled by hand stays styled.
    //
    // Parts are found by name before they are made, so all of that survives being saved as a prefab and
    // rebuilt: a second EnsureBuilt reuses the caption that is already there rather than adding another.
    //
    // Whatever goes inside belongs under Content, which is inset from the panel by the padding below and
    // starts under the caption. StatisticsWindow next door is one worked example of filling it.
    [AddComponentMenu("UI/Ui Window")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class UiWindow : MonoBehaviour, IPointerDownHandler
    {
        private const string CaptionName = "Caption";
        private const string TitleName = "Title";
        private const string ViewportName = "Viewport";
        private const string ClipName = "Clip";
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
        private bool showCaption = true;

        [SerializeField]
        private bool showCloseButton = true;

        [Tooltip("Hide the window at Awake, so it waits for Open. Off leaves it exactly as the scene saved it.")]
        [SerializeField]
        private bool startClosed = true;

        [Tooltip("Destroy the window once it has finished closing, rather than leaving it hidden.")]
        [SerializeField]
        private bool destroyOnClose = false;

        [Header("Layout")]
        [Tooltip("The whole header block: the drag strip, the close button and the title. Content starts under it. What it looks like is the Caption object's own business.")]
        [Min(0f)]
        [SerializeField]
        private float captionHeight = 104f;

        // Four floats rather than a RectOffset, which looks like the obvious type for this and is the wrong
        // one: a RectOffset is a handle onto a native object, so building one in a field initialiser throws
        // before the component exists.
        [Tooltip("Inset of the content area from the left of the panel.")]
        [SerializeField]
        private float contentPaddingLeft = 20f;

        [Tooltip("Inset of the content area below the caption. Measured from the bottom of the caption, not the top of the panel.")]
        [SerializeField]
        private float contentPaddingTop = 0f;

        [SerializeField]
        private float contentPaddingRight = 20f;

        [SerializeField]
        private float contentPaddingBottom = 20f;

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

        [Header("Scrolling")]
        [Tooltip("Draw a bar down the right of the body while it is scrolling. Off leaves the wheel and the drag, and no hint that there is more below. Its colours are the Scrollbar and Handle objects' own.")]
        [SerializeField]
        private bool showScrollbar = true;

        [Min(1f)]
        [SerializeField]
        private float scrollbarWidth = 8f;

        [Tooltip("From the right edge of the body, inwards. The content is moved over by the bar's width and this together, so the two never overlap.")]
        [SerializeField]
        private float scrollbarInset = 4f;

        [Tooltip("Canvas units per notch of the wheel.")]
        [Min(1f)]
        [SerializeField]
        private float scrollSensitivity = 28f;

        [Tooltip("Let a flick carry on after the finger has left. What a touch screen expects, and WebGL on a phone is a touch screen.")]
        [SerializeField]
        private bool scrollInertia = true;

        [Tooltip("How quickly a flick runs out. Lower stops sooner; 0.135 is UGUI's own.")]
        [Range(0.01f, 0.99f)]
        [SerializeField]
        private float scrollDeceleration = 0.135f;

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

        [Tooltip("Bring the window in front of every other open window when it is opened, grabbed, or clicked anywhere.")]
        [SerializeField]
        private bool bringToFront = true;

        [Header("Backdrop")]
        [Tooltip("A full-parent sheet behind the window, which is what makes it modal - it swallows every click that misses. Its colour is the sheet's own; the window only fades it in and out.")]
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

        [Tooltip("Above every sprite and canvas below this number. The floor the open windows are stacked from rather than the number each one ends up at - see the stack below. The backdrop takes one less than its own window.")]
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

        [Tooltip("Scale a window starts at when it grows in, and shrinks back to when it leaves.")]
        [Min(0f)]
        [SerializeField]
        private float openScale = 0.86f;

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

        // The padded rect inside the viewport, and what the scroller is told its viewport is. It is the whole
        // reason the padding survives scrolling: a ScrollRect holds the content it moves to the rect it is
        // given, so a viewport that stops at the padding is a scroll that stops there too, rather than one
        // that pulls the last row flat against the bottom of the panel.
        [SerializeField, HideInInspector]
        private RectTransform clipArea;

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
        private CanvasGroup group;

        [SerializeField, HideInInspector]
        private Image backdrop;

        // The sheet's fade goes through a group rather than through its colour, which leaves the colour
        // entirely to whoever styled it - a sheet tinted by hand is not reset to black on the next open.
        [SerializeField, HideInInspector]
        private CanvasGroup backdropGroup;

        // Serialized alongside the parts it stands for. A private bool would come back false after every
        // script reload, and a window in an open scene would then refuse to lay out until it was played.
        [SerializeField, HideInInspector]
        private bool built;

        // Every window that is currently open, bottom of the pile first. One list for the whole game, because
        // the question it answers - which window is in front - is not one a window can answer about itself:
        // sibling order settles nothing between two windows on canvases of their own, and nothing at all
        // between two windows under different parents.
        private static readonly List<UiWindow> stack = new List<UiWindow>();

        // What the selection was the last time the stack looked, and the frame it looked on. See WatchFocus.
        private static GameObject watchedSelection;
        private static int watchedFrame = -1;

        // Where this window sits in the pile, or -1 for a window that has never been stacked - in the editor,
        // or before the first open. Not serialized: a window's place in the pile is a fact about the session
        // it is open in, and a scene that saved one would come back claiming a place it has not earned.
        private int activeOrder = -1;

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

        /// <summary>The cell the body sits in, which the grid gives whatever the caption leaves. The scroller
        /// and the scrollbar live on it; the content is a step further in, inside <see cref="ClipArea"/>.</summary>
        public RectTransform Viewport
        {
            get
            {
                EnsureBuilt();
                return viewport;
            }
        }

        /// <summary>The viewport less the content padding: the masked rect the content scrolls inside, and
        /// what the scroller measures its travel against. The padding is here rather than on the content
        /// precisely so that scrolling cannot take it away.</summary>
        public RectTransform ClipArea
        {
            get
            {
                EnsureBuilt();
                return clipArea;
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

        /// <summary>The panel itself. Its colour, corners and border are set on the RoundedBox, by hand or
        /// from code; the window only reads the border back to know how far in the caption and body sit.</summary>
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

        /// <summary>The close button's own box, for styling it.</summary>
        public RoundedBox CloseBox
        {
            get
            {
                EnsureBuilt();
                return closeBox;
            }
        }

        /// <summary>The track down the right of the body, for styling it. The handle is the box under it,
        /// and <see cref="Scroller"/> is the thing that moves.</summary>
        public RoundedBox ScrollTrack
        {
            get
            {
                EnsureBuilt();
                return scrollTrack;
            }
        }

        public RoundedBox ScrollHandle
        {
            get
            {
                EnsureBuilt();
                return scrollHandle;
            }
        }

        /// <summary>The modal sheet, or null until Show Backdrop has built one. Its colour is its own.</summary>
        public Image Backdrop => backdrop;

        public string Title
        {
            get => title;
            set
            {
                title = value;
                if (titleText != null)
                    titleText.text = Translator.Label(value);
            }
        }

        /// <summary>Height of the header block. Content starts under it.</summary>
        public float CaptionHeight
        {
            get => captionHeight;
            set
            {
                captionHeight = Mathf.Max(0f, value);
                ApplyLayout();
            }
        }

        public float ContentPaddingLeft
        {
            get => contentPaddingLeft;
            set
            {
                contentPaddingLeft = value;
                ApplyLayout();
            }
        }

        public float ContentPaddingTop
        {
            get => contentPaddingTop;
            set
            {
                contentPaddingTop = value;
                ApplyLayout();
            }
        }

        public float ContentPaddingRight
        {
            get => contentPaddingRight;
            set
            {
                contentPaddingRight = value;
                ApplyLayout();
            }
        }

        public float ContentPaddingBottom
        {
            get => contentPaddingBottom;
            set
            {
                contentPaddingBottom = value;
                ApplyLayout();
            }
        }

        /// <summary>The same inset on all four sides.</summary>
        public void SetContentPadding(float left, float top, float right, float bottom)
        {
            contentPaddingLeft = left;
            contentPaddingTop = top;
            contentPaddingRight = right;
            contentPaddingBottom = bottom;
            ApplyLayout();
        }

        /// <summary>Everything the content does not get: the caption's row and the panel's border above and
        /// below it. What a window laying itself out by hand has to add to its own height.</summary>
        public float ChromeHeight => (showCaption ? captionHeight : 0f) + BorderTop + BorderBottom;

        public bool ShowScrollbar
        {
            get => showScrollbar;
            set
            {
                showScrollbar = value;
                ApplyLayout();
            }
        }

        public float ScrollbarWidth
        {
            get => scrollbarWidth;
            set
            {
                scrollbarWidth = Mathf.Max(1f, value);
                ApplyLayout();
            }
        }

        /// <summary>From the right edge of the body, inwards.</summary>
        public float ScrollbarInset
        {
            get => scrollbarInset;
            set
            {
                scrollbarInset = value;
                ApplyLayout();
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
                ApplyLayout();
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

        /// <summary>The scale a window grows in from, and shrinks back to when it leaves.</summary>
        public float OpenScale
        {
            get => openScale;
            set => openScale = Mathf.Max(0f, value);
        }

        /// <summary>Animate on unscaled time, so a window still opens over a paused game.</summary>
        public bool UnscaledTime
        {
            get => unscaledTime;
            set => unscaledTime = value;
        }

        /// <summary>The sheet behind the window. Turning it on builds it; what colour it is, is its own.</summary>
        public bool ShowBackdrop
        {
            get => showBackdrop;
            set
            {
                showBackdrop = value;
                EnsureBackdrop();
                ApplySorting();
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

        /// <summary>The floor the open windows are stacked from, not the number this one ends up at - see
        /// <see cref="Raise"/>. The backdrop takes one less than its own window.</summary>
        public int SortingOrder
        {
            get => sortingOrder;
            set
            {
                sortingOrder = value;

                // Both, because the floor is the highest of them: raising one window's order lifts the pile
                // it is in, and a window that is not in one has only itself to answer to.
                Restack();
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
            ApplyLayout();

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

        // A window is switched off while it is closed, so this is also where a window that was away while the
        // language changed catches up - the title is written again on the way back in, before anything is on
        // screen to be seen changing.
        void OnEnable()
        {
            Translator.OnLocaleChanged += WriteTitle;
            WriteTitle();

            // A window is switched off while it is closed, so being enabled is the same event as being
            // opened - including the window a scene saved switched on, which never goes through Open at all.
            // Newest on top: a dialog that arrives behind the one it was opened from has, from where the
            // player is sitting, not opened.
            Enlist();
        }

        void OnDisable()
        {
            Translator.OnLocaleChanged -= WriteTitle;
            Delist();

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
            // Once a frame between all of them, whichever window gets here first. Only open windows run at
            // all, so a game with no dialog on screen pays nothing for it.
            WatchFocus();

            if (!built || wantedHeight <= 0f || transitionTween != null)
                return;

            if (!Mathf.Approximately(Limit(), lastLimit))
                Clamp();
        }

        void OnDestroy()
        {
            Delist();
            KillTransition();

            if (backdrop != null)
                UiWindowParts.Discard(backdrop.gameObject);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (!built || !Application.isPlaying)
            {
                // OnValidate is not allowed to activate or destroy anything, and laying out does both.
                // Deferring by a frame puts it back on ordinary editor time, where it is.
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this == null)
                        return;

                    if (built)
                        ApplyLayout();
                };

                return;
            }

            ApplyLayout();
        }
#endif

        /// <summary>Makes whatever the window is missing. Safe to call at any point and as often as you
        /// like - parts that exist are found by name rather than made again.</summary>
        public void EnsureBuilt()
        {
            // The viewport and the grid are in that list because a window saved by a version that had neither
            // comes back saying it is built. Missing parts are what "built" is really asking about, so the flag
            // alone would leave that window with a null grid and a layout pass that throws.
            if (!built || panel == null || content == null || viewport == null || clipArea == null || grid == null)
                BuildParts();

            // Outside that check, and deliberately. A listener added with AddListener is not serialized, so
            // the ones put on when the window was built in the editor are gone by the time the scene is
            // played - while the parts they were added to, and the flag saying the window is built, both
            // survive. Hooking only where the parts are made leaves a close button that does nothing.
            HookEvents();
        }

        private void BuildParts()
        {
            // A window nobody has built before, as opposed to one coming back from a scene file missing a
            // part. Only the first kind is given a look to start from; the second keeps whatever it was
            // styled with, and only the part that had gone missing is seeded.
            bool fresh = !built && UiWindowParts.Find<RoundedBox>(transform, CaptionName) == null;

            bool madePanel = panel == null && GetComponent<RoundedBox>() == null;

            if (panel == null)
                panel = GetComponent<RoundedBox>();
            if (panel == null)
                panel = gameObject.AddComponent<RoundedBox>();

            if (fresh || madePanel)
                UiWindowSeed.Panel(panel);

            if (group == null)
                group = GetComponent<CanvasGroup>();
            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();

            if (grid == null)
                grid = UiWindowParts.Grid(Rect);

            bool madeCaption = UiWindowParts.Find<RoundedBox>(transform, CaptionName) == null;
            caption = UiWindowParts.Box(transform, CaptionName);

            bool madeTitle = UiWindowParts.Find<TextMeshProUGUI>(caption.transform, TitleName) == null;
            titleText = UiWindowParts.Label(caption.transform, TitleName);

            if (madeCaption)
                UiWindowSeed.Caption(caption);

            if (madeTitle)
                UiWindowSeed.Title(titleText);

            viewport = UiWindowParts.Rect(transform, ViewportName);
            clipArea = UiWindowParts.Rect(viewport, ClipName);

            // A window built before the viewport - or before the clip - has its Content further up, with
            // whatever the game put in it. Moved rather than replaced: finding parts by name would otherwise
            // make a second, empty Content inside the clip and leave the full one orphaned beside it.
            var stray = UiWindowParts.Find<RectTransform>(transform, ContentName);
            if (stray == null)
                stray = UiWindowParts.Find<RectTransform>(viewport, ContentName);

            if (stray != null)
                stray.SetParent(clipArea, false);

            content = UiWindowParts.Rect(clipArea, ContentName);

            // The mask is what makes scrolling look like scrolling rather than like content sliding over the
            // caption. On the clip rather than on the viewport, so it cuts at the padding: content scrolling
            // past is gone by the time it reaches the panel, instead of running into the margin the window
            // was given. RectMask2D rather than Mask: it costs no stencil pass, and both RoundedBox and TMP
            // clip against it - a rounded box is generated geometry on the default UI material, which reads
            // the clip rect like any other graphic.
            var stale = viewport.GetComponent<RectMask2D>();
            if (stale != null)
                UiWindowParts.Discard(stale);

            clip = clipArea.GetComponent<RectMask2D>();
            if (clip == null)
                clip = clipArea.gameObject.AddComponent<RectMask2D>();

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
            BuildClose();

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
        // the handle's anchors as it moves, so nothing here sets its position - only its size and its place in
        // the body, both of which are the window's business rather than the bar's colour.
        private void BuildScrollbar()
        {
            bool madeTrack = UiWindowParts.Find<RoundedBox>(viewport, ScrollbarName) == null;
            scrollTrack = UiWindowParts.Box(viewport, ScrollbarName);

            if (scrollbar == null)
                scrollbar = scrollTrack.GetComponent<Scrollbar>();
            if (scrollbar == null)
                scrollbar = scrollTrack.gameObject.AddComponent<Scrollbar>();

            var slide = UiWindowParts.Rect(scrollTrack.transform, "Sliding Area");
            UiWindowParts.Stretch(slide, 0f, 0f, 0f, 0f);

            bool madeHandle = UiWindowParts.Find<RoundedBox>(slide, "Handle") == null;
            scrollHandle = UiWindowParts.Box(slide, "Handle");

            if (madeTrack)
                UiWindowSeed.ScrollTrack(scrollTrack);

            if (madeHandle)
                UiWindowSeed.ScrollHandle(scrollHandle);

            scrollTrack.raycastTarget = true;
            scrollHandle.raycastTarget = true;

            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = scrollHandle.rectTransform;
            scrollbar.targetGraphic = scrollHandle;
            scrollbar.transition = Selectable.Transition.None;

            // Over the clip, not under it: a bar drawn before its own body is a bar the content slides over,
            // and on a window rebuilt from an older layout the clip is the newer child of the two.
            scrollTrack.rectTransform.SetAsLastSibling();
        }

        // The button, and the cross drawn inside it out of two rotated boxes rather than fetched from an
        // atlas - which is what keeps it sharp at any size. Both are placed and coloured once, when they are
        // made: a game that wants a different cross moves, resizes, recolours or replaces them, and nothing
        // here writes over that afterwards. An Image dropped on Close with a sprite in it does the same job.
        private void BuildClose()
        {
            bool madeClose = UiWindowParts.Find<RoundedBox>(transform, CloseName) == null;
            closeBox = UiWindowParts.Box(transform, CloseName);

            closeButton = closeBox.GetComponent<Button>();
            if (closeButton == null)
                closeButton = closeBox.gameObject.AddComponent<Button>();

            closeButton.targetGraphic = closeBox;
            closeBox.raycastTarget = true;

            bool madeCross = UiWindowParts.Find<RectTransform>(closeBox.transform, CrossName) == null;
            var cross = UiWindowParts.Rect(closeBox.transform, CrossName);

            bool madeBarA = UiWindowParts.Find<RoundedBox>(cross, "Bar A") == null;
            var barA = UiWindowParts.Box(cross, "Bar A");

            bool madeBarB = UiWindowParts.Find<RoundedBox>(cross, "Bar B") == null;
            var barB = UiWindowParts.Box(cross, "Bar B");

            if (madeClose)
                UiWindowSeed.Close(closeBox);

            if (madeCross)
                UiWindowSeed.Cross(cross, closeBox.rectTransform.sizeDelta);

            if (madeBarA)
                UiWindowSeed.Bar(barA, cross.sizeDelta.x, 45f);

            if (madeBarB)
                UiWindowSeed.Bar(barB, cross.sizeDelta.x, -45f);

            closeBox.gameObject.SetActive(showCloseButton);
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

        /// <summary>Puts every part where it belongs: the caption's row, the body under it, the content
        /// inset, the scrollbar down the side. Colours and fonts are not its business - it is safe to call on
        /// a window somebody has styled, and cheap enough to call whenever a size changes.</summary>
        private void WriteTitle()
        {
            if (titleText != null)
                titleText.text = Translator.Label(title);
        }

        [ContextMenu("Apply Layout")]
        public void ApplyLayout()
        {
            if (!built || panel == null || caption == null || titleText == null || content == null
                || closeBox == null || viewport == null || clipArea == null || grid == null)
                return;

            // Clicks rather than looks, so these are held rather than left to the inspector: a panel that
            // does not catch a click lets it through to the backdrop, which closes the window the player was
            // aiming at, and a title that does catch one swallows the drag that starts on it.
            panel.raycastTarget = true;
            caption.raycastTarget = true;
            titleText.raycastTarget = false;

            // Through the translator rather than straight from the field: a title of "Fairness" is a key's
            // en_US text and comes back in the player's language, and a title nothing knows is printed as it
            // was typed. See Translations/README.md.
            titleText.text = Translator.Label(title);

            // The caption and the body are two rows of one column, inset by the panel's own border so neither
            // sits over the outline. Said as a layout rather than by switching the caption on and off: a grid
            // takes its layout as the whole truth about which of its children are showing, and re-asserts it
            // every time it is enabled - so a caption hidden with SetActive would come back.
            grid.padding = new RectOffset(
                Mathf.RoundToInt(BorderLeft),
                Mathf.RoundToInt(BorderRight),
                Mathf.RoundToInt(BorderTop),
                Mathf.RoundToInt(BorderBottom));

            grid.RowGap = 0f;
            grid.ColumnGap = 0f;

            var arrangement = UiGridLayout.Build().Columns(GridTrack.Flexible());

            if (showCaption)
            {
                arrangement.Rows(GridTrack.Fixed(captionHeight), GridTrack.Flexible())
                    .Row(CaptionArea)
                    .Row(BodyArea);
            }
            else
            {
                arrangement.Rows(GridTrack.Flexible()).Row(BodyArea);
            }

            grid.SetLayout(arrangement.Done());

            closeBox.gameObject.SetActive(showCloseButton);

            ApplyScrollbarPlacement();
            ApplyScrollState();
            EnsureBackdrop();
            ApplySorting();
        }

        // ------------------------------------------------------------------ which window is in front

        // Statics outlive a play session when the editor is set to skip the domain reload, so the pile is
        // emptied by hand rather than left to start the next one holding the last one's windows.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearStack()
        {
            stack.Clear();
            watchedSelection = null;
            watchedFrame = -1;
        }

        /// <summary>Puts the window in front of every other one that is open. What opening it does, and what
        /// touching it does afterwards - so this is only needed for a front that some other thing decides.</summary>
        // Two things at once, because a window is sorted by two different rules depending on Always On Top:
        // the sibling move settles a window drawn in hierarchy order, and the restack settles one drawn on a
        // canvas of its own. Doing both means neither has to be asked about.
        public void Raise()
        {
            int at = stack.IndexOf(this);

            // Already the front one and already told so. Worth the check: this runs on every press, and the
            // work below writes to every open window's canvas.
            if (at >= 0 && at == stack.Count - 1 && activeOrder >= 0)
                return;

            if (at >= 0)
                stack.RemoveAt(at);

            stack.Add(this);

            if (transform.parent != null)
                transform.SetAsLastSibling();

            // The sheet follows the window it belongs to rather than being left under whatever was raised
            // past it - the same reasoning as in ShowBackdropSheet, applied again now the window has moved.
            if (backdrop != null && backdrop.gameObject.activeSelf)
                backdrop.transform.SetSiblingIndex(transform.GetSiblingIndex());

            Restack();
        }

        private void Enlist()
        {
            if (!stack.Contains(this))
                stack.Add(this);

            // Not through Raise: a window with Bring To Front off still takes its place in the pile - it just
            // does not climb it afterwards.
            if (bringToFront)
                Raise();
            else
                Restack();
        }

        private void Delist()
        {
            if (!stack.Remove(this))
                return;

            // Back to the plain serialized order, so a window that opens again is sorted by the stack it
            // joins rather than by the place it held in a pile it has left.
            activeOrder = -1;
            Restack();
        }

        // Where each open window's canvas ends up. Two apart rather than one, because every window may want a
        // backdrop immediately under itself and over the window below - which is the slot the odd numbers are.
        //
        // Handed out by position in the pile rather than counted upwards from the last one, so the numbers
        // stay inside a known band however many times windows are raised past each other. The floor is the
        // highest Sorting Order among the windows that are open: a window deliberately set above the rest
        // lifts the whole pile with it rather than being buried by it.
        private static void Restack()
        {
            // Destroyed windows first. OnDestroy takes them out itself, but a static list outlives a play
            // session where the editor is set to skip the domain reload, and a hole left in the pile would
            // push every window above it a slot higher on each restack.
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                if (stack[i] == null)
                    stack.RemoveAt(i);
            }

            if (stack.Count == 0)
                return;

            int floor = int.MinValue;

            for (int i = 0; i < stack.Count; i++)
                floor = Mathf.Max(floor, stack[i].sortingOrder);

            for (int i = 0; i < stack.Count; i++)
            {
                stack[i].activeOrder = floor + i * 2;
                stack[i].ApplySorting();
            }
        }

        /// <summary>A press anywhere on the window that nothing inside it took for itself.</summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (bringToFront)
                Raise();
        }

        // The other half of clicking a window to the front, and the reason the press above is not enough on
        // its own: a pointer-down is delivered to the first handler found walking up from what was hit and
        // stops there, so a press that lands on a Button, a Toggle or an input field inside the window never
        // reaches the window. Those are all Selectables, and pressing one is exactly what moves the event
        // system's selection - so a selection that has changed into a window is a press the window missed.
        //
        // Read rather than subscribed to because the event system offers nothing to subscribe to. One
        // reference compare per frame while any window is open, and nothing at all while none is.
        private static void WatchFocus()
        {
            if (watchedFrame == Time.frameCount)
                return;

            watchedFrame = Time.frameCount;

            var events = EventSystem.current;
            var selected = events != null ? events.currentSelectedGameObject : null;

            if (selected == watchedSelection)
                return;

            watchedSelection = selected;

            if (selected == null)
                return;

            var window = selected.GetComponentInParent<UiWindow>();

            if (window != null && window.bringToFront)
                window.Raise();
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
            Sort(gameObject, alwaysOnTop, DrawOrder);

            // One below the window, so the sheet is over the game and under the dialog it belongs to - and,
            // once the pile is two apart per window, over every window below this one as well.
            if (backdrop != null)
                Sort(backdrop.gameObject, alwaysOnTop && showBackdrop, DrawOrder - 1);
        }

        // The place the stack gave it, or the serialized order for a window that is not in a stack: one in a
        // scene being edited, or the only window there is.
        private int DrawOrder => activeOrder >= 0 ? activeOrder : sortingOrder;

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
        // an error per assignment - and this runs on every layout pass.
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

        // How far in the panel's outline reaches on each side. Read off the box rather than held here, so a
        // border thickened by hand moves the caption and the body in with it.
        private float BorderLeft => panel != null ? panel.BorderLeft : 0f;

        private float BorderRight => panel != null ? panel.BorderRight : 0f;

        private float BorderTop => panel != null ? panel.BorderTop : 0f;

        private float BorderBottom => panel != null ? panel.BorderBottom : 0f;

        // ------------------------------------------------------------------ how tall it may be

        /// <summary>Asks the content how tall it wants to be and makes the window that tall - as far as it is
        /// allowed, after which the body scrolls instead.</summary>
        // The content has to be able to answer: something under Content that reports a height, which a layout
        // group, a label or a Layout Element all do and a plain panel does not. A window whose content is
        // arranged by hand should work out its own number and call FitTo instead.
        public void Fit()
        {
            EnsureBuilt();
            FitTo(ChromeHeight + Body());
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

        // What the content says it needs, plus the padding around it. Measured immediately rather than waited
        // for: a window is fitted at the moment it opens, and a layout that has not run yet reports the height
        // it had last time.
        private float Body()
        {
            if (content == null)
                return 0f;

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            float wanted = Mathf.Max(0f, LayoutUtility.GetPreferredHeight(content));
            return wanted + contentPaddingTop + contentPaddingBottom;
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

        // Whatever scrolling currently is, written onto the parts. Called by the layout pass as well, so a
        // window that has never been fitted has its mask and its scroller off rather than however Unity made
        // them.
        private void ApplyScrollState()
        {
            if (scroller != null)
            {
                // The clip, not the viewport: the scroller holds its content to the rect it is given at both
                // ends, so this is what keeps the padding under the last row rather than scrolling it away.
                scroller.viewport = clipArea;
                scroller.content = content;
                scroller.horizontal = false;
                scroller.vertical = true;
                scroller.movementType = ScrollRect.MovementType.Clamped;
                scroller.scrollSensitivity = scrollSensitivity;
                scroller.verticalScrollbar = showScrollbar ? scrollbar : null;

                // A flick that carries on after the finger has left is what a touch screen expects, and WebGL on
                // a phone is a touch screen.
                scroller.inertia = scrollInertia;
                scroller.decelerationRate = scrollDeceleration;

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
                scrollTrack.gameObject.SetActive(scrolling && showScrollbar);

            ApplyBody();
        }

        // The padding is the clip's, and the content sits square inside it. Which is the whole trick: a
        // ScrollRect measures its travel against the rect it was handed as a viewport, so padding written onto
        // the content as an offset is padding the scroller takes straight back off - the first drag pulls the
        // top margin away, and the end of the travel leaves the last row flat against the bottom of the panel.
        // Padding the clip instead makes the margin part of the frame the content moves inside, and it is
        // there at rest, mid-scroll and at the end alike.
        private void ApplyBody()
        {
            if (content == null || viewport == null || clipArea == null)
                return;

            float left = contentPaddingLeft;
            float top = contentPaddingTop;
            float bottom = contentPaddingBottom;
            float right = contentPaddingRight;

            if (scrolling && showScrollbar)
                right += scrollbarWidth + Mathf.Max(0f, scrollbarInset);

            UiWindowParts.Stretch(clipArea, left, top, right, bottom);

            // Not scrolling, the content is simply the clip: same rect, no travel, nothing to hold.
            if (!scrolling)
            {
                UiWindowParts.Stretch(content, 0f, 0f, 0f, 0f);
                return;
            }

            // Scrolling, it is as tall as it asked to be and anchored to the top of the clip, which is the
            // shape a ScrollRect moves. The height is the content's own - the padding is not in it, because
            // the padding is the clip's.
            float height = Mathf.Max(0f, wantedHeight - ChromeHeight - top - bottom);

            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, height);
            content.anchoredPosition = Vector2.zero;
        }

        // Where the bar sits, and nothing about what it looks like: down the right of the viewport, inset by
        // the same padding as the content so the two line up.
        private void ApplyScrollbarPlacement()
        {
            if (scrollTrack == null || scrollHandle == null)
                return;

            var bar = scrollTrack.rectTransform;

            bar.anchorMin = new Vector2(1f, 0f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(1f, 0.5f);
            bar.sizeDelta = new Vector2(scrollbarWidth, -(contentPaddingTop + contentPaddingBottom));
            bar.anchoredPosition = new Vector2(
                -Mathf.Max(0f, scrollbarInset),
                (contentPaddingBottom - contentPaddingTop) * 0.5f);

            // UGUI's Scrollbar moves its handle by writing the anchors and nothing else, so the handle's size
            // and position have to be nothing at all for it to sit exactly in the band it is given. Left at the
            // 100 by 100 a new rect comes with, it draws a hundred units past the track on every side - which
            // reads as a scrollbar that is enormous and a Scrollbar Width that does nothing.
            var handle = scrollHandle.rectTransform;
            handle.sizeDelta = Vector2.zero;
            handle.anchoredPosition = Vector2.zero;
        }

        /// <summary>Opens with the transition. Already open, it is left alone.</summary>
        public void Open() => Open(true);

        /// <summary>Straight on, no tween. For restoring a window that was open when the game was saved.</summary>
        public void OpenInstant() => Open(false);

        public void Open(bool animated)
        {
            EnsureBuilt();
            ApplyLayout();

            if (IsOpen && gameObject.activeSelf && transitionTween == null)
            {
                // Already open and settled - but said to the transform rather than taken on trust. This is the
                // way out of a window that is open as far as this class knows and invisible on screen: whatever
                // left it part-scaled or transparent, the press that asks for it again puts it right instead of
                // returning to a dialog nobody can see.
                ResetTransform();

                // And to the front with it. Asking for a window that is already open is nearly always asking
                // to look at it, which a dialog left behind three others does not answer.
                if (bringToFront)
                    Raise();

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

            // After the SetActive rather than instead of the one it already ran through OnEnable: a window
            // that was on screen and is being opened again - a second press, a reopen from code - never left
            // the pile, so being enabled is not what puts it back on top of it.
            if (bringToFront)
                Raise();

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
        /// a state the layout can no longer describe. Parts that are still there keep how they look.</summary>
        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            built = false;
            EnsureBuilt();
            ApplyLayout();
        }

        private void HandleCloseClicked()
        {
            OnCloseClicked.Invoke();
            Close();
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

        // The sheet is made the first time it is asked for and left alone after that: its colour is whatever
        // the Image says, which is the one seeded when it was made until somebody changes it. The fade goes
        // through the group beside it, so opening and closing never touch that colour.
        private void EnsureBackdrop()
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

                bool made = UiWindowParts.Find<Image>(parent, BackdropName) == null;
                backdrop = UiWindowParts.Picture(parent, BackdropName);

                if (made)
                    UiWindowSeed.Backdrop(backdrop);
            }

            backdrop.raycastTarget = true;

            if (backdropGroup == null)
                backdropGroup = backdrop.GetComponent<CanvasGroup>();
            if (backdropGroup == null)
                backdropGroup = backdrop.gameObject.AddComponent<CanvasGroup>();

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

                if (backdropGroup != null)
                    backdropGroup.alpha = 1f;

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
            float from = Mathf.Max(0.0001f, openScale);

            if (opening)
            {
                if (slides)
                    rect.anchoredPosition = rest + offset;
                if (scales)
                    rect.localScale = restScale * from;
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
                var to = opening ? restScale : restScale * from;
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

            if (backdropGroup != null && showBackdrop)
            {
                var sheet = backdropGroup;

                if (opening)
                    sheet.alpha = 0f;

                sequence.Join(DOTween
                    .To(() => sheet.alpha, a => sheet.alpha = a, opening ? 1f : 0f, duration)
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

            if (backdropGroup != null)
                backdropGroup.alpha = 1f;
        }

        private void KillTransition()
        {
            if (transitionTween != null && transitionTween.IsActive())
                transitionTween.Kill();

            transitionTween = null;
        }
    }
}
