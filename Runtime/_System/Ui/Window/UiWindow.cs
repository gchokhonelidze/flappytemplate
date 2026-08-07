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
        private const string ContentName = "Content";
        private const string CloseName = "Close";
        private const string CrossName = "Cross";
        private const string BackdropName = "Window Backdrop";

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

        [Header("Dragging")]
        [SerializeField]
        private bool draggable = true;

        [Tooltip("Grab it anywhere rather than by the caption. Note that this catches drags on anything inside it that does not handle its own.")]
        [SerializeField]
        private bool dragAnywhere = false;

        [SerializeField]
        private bool clampToParent = true;

        [Tooltip("Draw the window over its siblings when it is grabbed or opened.")]
        [SerializeField]
        private bool bringToFront = true;

        [Header("Backdrop")]
        [Tooltip("A full-parent sheet behind the window, which is what makes it modal - it swallows every click that misses.")]
        [SerializeField]
        private bool showBackdrop = false;

        [SerializeField]
        private bool closeOnBackdropClick = true;

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

        public bool ClampToParent
        {
            get => clampToParent;
            set
            {
                clampToParent = value;
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

        /// <summary>True from the moment Open is called to the moment the closing animation has finished.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>Whether the parts exist yet. False on a window built by the builder and not yet woken.</summary>
        public bool IsBuilt => built;

        void Awake()
        {
            EnsureBuilt();
            ApplyStyle();

            IsOpen = gameObject.activeSelf;

            if (startClosed)
            {
                IsOpen = false;
                ResetTransform();
                ShowBackdropSheet(false);
                gameObject.SetActive(false);
            }
        }

        void OnDisable()
        {
            // Not while the closing sequence is delivering its own OnComplete - that is what deactivated the
            // object in the first place, and killing a tween from inside its own callback is asking for it.
            if (!finishing)
                KillTransition();
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
            if (built && panel != null && content != null)
                return;

            if (panel == null)
                panel = GetComponent<RoundedBox>();
            if (panel == null)
                panel = gameObject.AddComponent<RoundedBox>();

            if (group == null)
                group = GetComponent<CanvasGroup>();
            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();

            caption = UiWindowParts.Box(transform, CaptionName);
            titleText = UiWindowParts.Label(caption.transform, TitleName);
            content = UiWindowParts.Rect(transform, ContentName);

            closeBox = UiWindowParts.Box(transform, CloseName);
            closeButton = closeBox.GetComponent<Button>();
            if (closeButton == null)
                closeButton = closeBox.gameObject.AddComponent<Button>();

            closeButton.targetGraphic = closeBox;
            closeButton.onClick.RemoveListener(HandleCloseClicked);
            closeButton.onClick.AddListener(HandleCloseClicked);

            crossIcon = UiWindowParts.Rect(closeBox.transform, CrossName);
            crossBarA = UiWindowParts.Box(crossIcon, "Bar A");
            crossBarB = UiWindowParts.Box(crossIcon, "Bar B");
            spriteIcon = UiWindowParts.Picture(closeBox.transform, "Icon");

            // Content last, so it draws over the caption if the two ever overlap - a window whose caption is
            // taller than the padding allows for should cover the header, not be covered by it.
            content.SetAsLastSibling();

            built = true;
            ApplyDrag();
        }

        /// <summary>Pushes every colour, size and font from the style onto the parts. Cheap enough to call
        /// whenever a theme changes, including on a window that is already open.</summary>
        [ContextMenu("Apply Style")]
        public void ApplyStyle()
        {
            if (style == null)
                style = new UiWindowStyle();

            if (!built || panel == null || caption == null || titleText == null || content == null || closeBox == null)
                return;

            float border = Mathf.Max(0f, style.BorderSize);

            panel.FillGradientMode = EFillGradient.None;
            panel.FillColor = style.Fill;
            panel.SetCornerRadius(style.CornerRadius);
            panel.SetBorderSize(border);
            panel.SetBorderColor(style.BorderColor);
            panel.EdgeSoftness = style.EdgeSoftness;
            panel.raycastTarget = true;

            // Inset by the border on three sides so the caption wash sits inside the outline rather than
            // over it, with its top corners following what is left of the panel's radius.
            UiWindowParts.TopStrip(caption.rectTransform, style.CaptionHeight, border, border);
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
            caption.gameObject.SetActive(showCaption);

            UiWindowParts.Stretch(titleText.rectTransform, 12f, style.TitleTopInset, 12f, 6f);
            titleText.text = title;
            titleText.font = style.TitleFont != null ? style.TitleFont : titleText.font;
            titleText.fontSize = style.TitleSize;
            titleText.color = style.TitleColor;
            titleText.fontStyle = style.TitleStyle;
            titleText.alignment = style.TitleAlignment;
            titleText.raycastTarget = false;

            var padding = style.ContentPadding ?? new RectOffset();
            float top = (showCaption ? style.CaptionHeight : border) + padding.top;
            UiWindowParts.Stretch(content, padding.left + border, top, padding.right + border, padding.bottom + border);

            ApplyCloseStyle();
            ApplyBackdropStyle();
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
                return;

            KillTransition();
            gameObject.SetActive(true);

            if (bringToFront)
                transform.SetAsLastSibling();

            IsOpen = true;
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

                var button = backdrop.GetComponent<Button>();
                if (button == null)
                    button = backdrop.gameObject.AddComponent<Button>();

                button.targetGraphic = backdrop;
                button.transition = Selectable.Transition.None;
                button.onClick.RemoveListener(HandleBackdropClicked);
                button.onClick.AddListener(HandleBackdropClicked);
            }

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
                    rect.localScale = Vector3.one * openScale;
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
                var to = opening ? Vector3.one : Vector3.one * openScale;
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
                    rect.localScale = Vector3.one;
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

            switch (transition)
            {
                case EWindowTransition.SlideUp:
                    return new Vector2(0f, -(height * 0.5f + rect.rect.height));
                case EWindowTransition.SlideDown:
                    return new Vector2(0f, height * 0.5f + rect.rect.height);
                case EWindowTransition.SlideLeft:
                    return new Vector2(width * 0.5f + rect.rect.width, 0f);
                case EWindowTransition.SlideRight:
                    return new Vector2(-(width * 0.5f + rect.rect.width), 0f);
                default:
                    return Vector2.zero;
            }
        }

        private void ResetTransform()
        {
            var rect = Rect;
            rect.localScale = Vector3.one;

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
