using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // A message that slides in over the top right corner of the game and goes away again: a pill with a sign
    // on its left, the message beside it, and a close cross when one is asked for. Four kinds, each in its own
    // two colours - a dark pill and a bright ink.
    //
    //     UiManager.Inst.OnShowToast.Invoke(new ToastOptions { Type = EToastType.Success, Message = "Bet placed" });
    //
    // Built from code the way the windows are: every part is a named child found before it is made, so the
    // prefab carries nothing but this component on a RectTransform, and Build Now can be run again on a toast
    // that already exists without doubling its parts. Everything is a RoundedBox, a triangle or a label, drawn
    // in the colours of the style - no sprite, no atlas entry, and a palette changed in the inspector reaches
    // the sign as well as the pill.
    //
    // The pill is sized to its text: as wide as the message wants up to the Max Width of the style, and taller
    // than its Min Height only when the text wraps or a title is set over the message.
    [AddComponentMenu("UI/Toast")]
    [RequireComponent(typeof(RectTransform))]
    public class Toast : MonoBehaviour
    {
        private const string PanelName = "Panel";
        private const string IconName = "Icon";
        private const string TitleName = "Title";
        private const string MessageName = "Message";
        private const string CloseName = "Close";
        private const string CrossName = "Cross";
        private const string BarAName = "Bar A";
        private const string BarBName = "Bar B";

        [SerializeField]
        private ToastStyle style = new ToastStyle();

        private RoundedBox panel;
        private RectTransform icon;
        private TextMeshProUGUI title;
        private TextMeshProUGUI message;
        private RoundedBox close;
        private RoundedBox barA;
        private RoundedBox barB;

        private bool built;
        private ToastOptions current;
        private Tween slide;
        private Tween wait;

        /// <summary>The look. Assigning a style repaints a built toast with it; a field changed in place takes
        /// effect on the next Show, or on Build Now.</summary>
        public ToastStyle Style
        {
            get => style;
            set
            {
                style = value ?? new ToastStyle();
                if (built)
                    Apply();
            }
        }

        /// <summary>On screen, or on its way there.</summary>
        public bool IsShowing { get; private set; }

        /// <summary>What is being shown, or was last. Null before the first Show.</summary>
        public ToastOptions Current => current;

        public RectTransform RectTransform => (RectTransform)transform;

        /// <summary>The pill.</summary>
        public RoundedBox Panel
        {
            get
            {
                Build();
                return panel;
            }
        }

        public TextMeshProUGUI TitleText
        {
            get
            {
                Build();
                return title;
            }
        }

        public TextMeshProUGUI MessageText
        {
            get
            {
                Build();
                return message;
            }
        }

        /// <summary>The disc the cross is on. A ripple is put on it when the style asks for one.</summary>
        public RoundedBox CloseBox
        {
            get
            {
                Build();
                return close;
            }
        }

        void Awake()
        {
            Build();
        }

        void OnDisable()
        {
            // Switched off from outside mid-slide: the tweens would otherwise keep writing to a rect nobody
            // can see, and the next enable would start from wherever they had got to.
            Kill();
            IsShowing = false;

            if (built)
                RectTransform.anchoredPosition = Hidden();
        }

        /// <summary>Makes every part that is missing and paints them all. Safe to run again.</summary>
        [ContextMenu("Build Now")]
        public void Build()
        {
            if (built && panel != null)
                return;

            var root = RectTransform;

            // Pinned to the top right corner of whatever it is under, which is where it slides in from. The
            // rect is sized by Layout, so the size the prefab carries is of no account.
            root.anchorMin = Vector2.one;
            root.anchorMax = Vector2.one;
            root.pivot = Vector2.one;

            // In the order they are drawn: the pill first, so everything else is over it.
            panel = UiWindowParts.Box(root, PanelName);
            icon = UiWindowParts.Rect(root, IconName);
            title = UiWindowParts.Label(root, TitleName);
            message = UiWindowParts.Label(root, MessageName);
            close = UiWindowParts.Box(root, CloseName);

            panel.transform.SetAsFirstSibling();
            close.transform.SetAsLastSibling();

            var cross = UiWindowParts.Rect(close.transform, CrossName);
            barA = UiWindowParts.Box(cross, BarAName);
            barB = UiWindowParts.Box(cross, BarBName);

            Hook();

            built = true;
            Apply();

            if (!IsShowing)
                root.anchoredPosition = Hidden();
        }

        /// <summary>Slides a message in. A toast already showing is replaced where it stands.</summary>
        public void Show(ToastOptions options)
        {
            if (options == null)
                options = new ToastOptions();

            Build();
            Kill();

            current = options;
            gameObject.SetActive(true);

            title.text = options.Title ?? string.Empty;
            message.text = options.Message ?? string.Empty;
            close.gameObject.SetActive(options.ShowCloseButton);

            Apply();

            var root = RectTransform;
            float duration = Seconds(options.AnimationDuration);

            // From off the edge every time, even over a toast that was already up: the new message arriving
            // is what tells the player it is a new message.
            root.anchoredPosition = Hidden();
            IsShowing = true;

            // Built with DOTween.To rather than DOAnchorPos: the shortcuts live in the UI module of DOTween,
            // which is compiled into the project assembly and cannot be reached from a package. Unscaled,
            // like the windows - a message over a paused game should not be waiting on time that is not
            // running.
            slide = DOTween
                .To(() => root.anchoredPosition, v => root.anchoredPosition = v, Shown(), duration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    slide = null;

                    if (options.AutoHide && options.HideAfter > TimeSpan.Zero)
                        wait = DOVirtual.DelayedCall(Seconds(options.HideAfter), Hide, true);
                });
        }

        /// <summary>Slides the toast out and switches it off. Nothing happens if it is not up.</summary>
        public void Hide()
        {
            if (!built || !gameObject.activeSelf)
                return;

            Kill();

            var root = RectTransform;
            float duration = current != null ? Seconds(current.AnimationDuration) : 0.5f;

            slide = DOTween
                .To(() => root.anchoredPosition, v => root.anchoredPosition = v, Hidden(), duration)
                .SetEase(Ease.InBack)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    slide = null;
                    IsShowing = false;
                    gameObject.SetActive(false);
                });
        }

        /// <summary>What the close cross calls. Kept for a button wired to it in the inspector.</summary>
        public void OnCloseClick() => Hide();

        /// <summary>Puts a sample of a kind on screen without animating it or taking it down again - for styling
        /// in the editor, where nothing is running to send one.</summary>
        public void Preview(EToastType kind, string text = "Placeholder")
        {
            Build();
            Kill();

            current = new ToastOptions { Type = kind, Message = text, AutoHide = false };
            gameObject.SetActive(true);

            title.text = string.Empty;
            message.text = text;
            close.gameObject.SetActive(false);

            Apply();
            RectTransform.anchoredPosition = Shown();
            IsShowing = true;
        }

        [ContextMenu("Preview Success")]
        private void PreviewSuccess() => Preview(EToastType.Success);

        [ContextMenu("Preview Warning")]
        private void PreviewWarning() => Preview(EToastType.Warning);

        [ContextMenu("Preview Error")]
        private void PreviewError() => Preview(EToastType.Error);

        [ContextMenu("Preview Default")]
        private void PreviewDefault() => Preview(EToastType.Default);

        // ------------------------------------------------------------------ the look

        // Everything about the current kind, from the palette down: the pill, the sign, the text and the
        // cross, then where each of them goes.
        private void Apply()
        {
            var kind = current != null ? current.Type : EToastType.Default;
            var palette = style.Palette(kind);

            UiWindowParts.Stretch(panel.rectTransform, 0f, 0f, 0f, 0f);
            panel.FillGradientMode = EFillGradient.None;
            panel.FillColor = palette.Fill;
            panel.SetBorderSize(0f);
            panel.SetCornerRadius(style.CornerRadius);
            panel.EdgeSoftness = 1.25f;

            // The pill takes the click, so a press on a toast does not land on the game under it.
            panel.raycastTarget = true;

            ToastIcons.Draw(icon, kind, style.IconSize, palette.Ink, palette.Fill);

            Look(title, style.TitleSize, style.TitleStyle, palette.Ink);
            Look(message, style.MessageSize, style.MessageStyle, palette.Ink);

            var disc = palette.Ink;
            disc.a *= style.CloseDisc;
            close.FillGradientMode = EFillGradient.None;
            close.FillColor = disc;
            close.SetBorderSize(0f);
            close.SetCornerRadius(100000f);
            close.EdgeSoftness = 1.25f;
            close.raycastTarget = true;

            // The cross: two pills across the middle of a square two fifths of the button, turned either way -
            // the same cross a window wears on its close button, in the ink of the toast.
            float span = style.CloseSize * 0.4f;
            var cross = (RectTransform)barA.transform.parent;
            UiWindowParts.Pin(cross, new Vector2(0.5f, 0.5f), new Vector2(span, span), Vector2.zero);
            Bar(barA, span, 45f, palette.Ink);
            Bar(barB, span, -45f, palette.Ink);

            Layout();
        }

        private void Look(TextMeshProUGUI label, float size, FontStyles fontStyle, Color ink)
        {
            if (style.Font != null)
                label.font = style.Font;

            label.fontSize = size;
            label.fontStyle = fontStyle;
            label.color = ink;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
        }

        private void Bar(RoundedBox bar, float span, float angle, Color ink)
        {
            UiWindowParts.Pin(bar.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(span, style.CloseThickness), Vector2.zero);
            bar.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            bar.FillGradientMode = EFillGradient.None;
            bar.FillColor = ink;
            bar.SetBorderSize(0f);
            bar.SetCornerRadius(100000f);
            bar.EdgeSoftness = 1.25f;
            bar.raycastTarget = false;
        }

        // The pill around its text. Width first: what the text wants on one line, held to what the style
        // allows; then the heights at that width, which is where a long message wraps and the pill grows.
        private void Layout()
        {
            bool hasTitle = !string.IsNullOrEmpty(title.text);
            bool hasClose = close.gameObject.activeSelf;

            title.gameObject.SetActive(hasTitle);

            float fixedWidth = style.PaddingX * 2f + style.IconSize + style.IconGap;
            if (hasClose)
                fixedWidth += style.CloseGap + style.CloseSize;

            float room = Mathf.Max(40f, style.MaxWidth - fixedWidth);
            float want = message.preferredWidth;
            if (hasTitle)
                want = Mathf.Max(want, title.preferredWidth);

            // A pixel of slack, so a width rounded down never wraps the last word onto a line of its own.
            float textWidth = Mathf.Ceil(Mathf.Min(room, want)) + 1f;

            float textX = style.PaddingX + style.IconSize + style.IconGap;
            var left = new Vector2(0f, 0.5f);

            // Widths before heights: the preferred height of a label is measured at the width its rect has.
            float titleHeight = 0f;
            if (hasTitle)
            {
                UiWindowParts.Pin(title.rectTransform, left, new Vector2(textWidth, 0f), new Vector2(textX, 0f));
                titleHeight = title.preferredHeight;
            }

            UiWindowParts.Pin(message.rectTransform, left, new Vector2(textWidth, 0f), new Vector2(textX, 0f));
            float messageHeight = message.preferredHeight;

            float textHeight = messageHeight;
            if (hasTitle)
                textHeight += titleHeight + style.TitleGap;

            float height = Mathf.Max(style.MinHeight, textHeight + style.PaddingY * 2f);
            RectTransform.sizeDelta = new Vector2(fixedWidth + textWidth, height);

            UiWindowParts.Pin(icon, left, new Vector2(style.IconSize, style.IconSize), new Vector2(style.PaddingX, 0f));

            // The text block centred on the pill, title over message.
            float top = textHeight * 0.5f;
            if (hasTitle)
            {
                UiWindowParts.Pin(title.rectTransform, left, new Vector2(textWidth, titleHeight), new Vector2(textX, top - titleHeight * 0.5f));
                top -= titleHeight + style.TitleGap;
            }

            UiWindowParts.Pin(message.rectTransform, left, new Vector2(textWidth, messageHeight), new Vector2(textX, top - messageHeight * 0.5f));

            UiWindowParts.Pin(close.rectTransform, new Vector2(1f, 0.5f), new Vector2(style.CloseSize, style.CloseSize), new Vector2(-style.PaddingX, 0f));
        }

        private void Hook()
        {
            var button = close.GetComponent<Button>();
            if (button == null)
                button = close.gameObject.AddComponent<Button>();

            button.targetGraphic = close;
            button.transition = Selectable.Transition.None;

            // Removed before it is added, every time: AddListener is not serialized, so this runs on every
            // build, and a listener added twice would hide twice.
            button.onClick.RemoveListener(Hide);
            button.onClick.AddListener(Hide);

            if (style.CloseWave)
                UiRipple.On(close.gameObject);
        }

        // ------------------------------------------------------------------ where it goes

        /// <summary>Where the pill rests: inset from the corner it is pinned to.</summary>
        private Vector2 Shown() => new Vector2(-style.Inset.x, -style.Inset.y);

        /// <summary>Off the right edge by its own width and a little more, at the same height.</summary>
        private Vector2 Hidden() => new Vector2(RectTransform.rect.width + style.Slack, -style.Inset.y);

        private void Kill()
        {
            if (slide != null && slide.IsActive())
                slide.Kill();

            if (wait != null && wait.IsActive())
                wait.Kill();

            slide = null;
            wait = null;
        }

        private static float Seconds(TimeSpan span) => Mathf.Max(0f, (float)span.TotalSeconds);
    }
}
