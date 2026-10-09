using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace FlappyTemplate
{
    // A small copy icon that copies a label's text when clicked, and turns into a tick for a moment to say so.
    //
    //     UiCopyButton.On(seedLabel, 18f, 10f);   // an icon 18 across, 10 clear of the text
    //
    // It sits inside the label, at its right edge, and the label's right margin is widened by the icon and the
    // gap so wrapped text never runs underneath it. Inside the label rather than beside it because a label is
    // usually a cell of a grid, and a sibling would be a cell the grid has not been told about - and hidden.
    //
    // Copying from a click in a browser has to happen inside the browser's own release event, which Unity only
    // hears about a frame later. So the text is offered to the page when the button is pressed, and the page
    // copies it in the mouseup or touchend - see JSPlugins/Clipboard.jslib. A press that wanders off the
    // button, or turns into a scroll, withdraws the offer before the release comes.
    [AddComponentMenu("UI/Copy Button")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class UiCopyButton : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Tooltip("The label whose text is copied - as the player reads it, rich text tags left out.")]
        [SerializeField]
        private TMP_Text source;

        [Tooltip("Copied instead when there is no Source.")]
        [SerializeField]
        private string text = string.Empty;

        [SerializeField]
        private Color color = new Color(1f, 1f, 1f, 0.5f);

        [SerializeField]
        private Color hoverColor = new Color(1f, 1f, 1f, 0.9f);

        [Tooltip("The tick's colour, after a copy.")]
        [SerializeField]
        private Color copiedColor = new Color(0.451f, 0.859f, 0.502f, 1f);

        [Tooltip("How long the tick stays before the icon comes back.")]
        [Min(0f)]
        [SerializeField]
        private float copiedSeconds = 1.2f;

        [Tooltip("Line thickness of the drawn icon and tick.")]
        [Min(0.5f)]
        [SerializeField]
        private float thickness = 1.6f;

        [Tooltip("Something was copied, with what. A fair place for a \"Copied\" toast.")]
        [SerializeField]
        private UnityEvent<string> onCopied = new UnityEvent<string>();

        private RoundedBox hit;
        private RoundedBox[] back;
        private RoundedBox front;
        private RoundedBox tickShort;
        private RoundedBox tickLong;

        private bool hovered;
        private PointerEventData press;
        private float copiedUntil = -1f;

        public TMP_Text Source
        {
            get => source;
            set => source = value;
        }

        /// <summary>Copied when there is no <see cref="Source"/>.</summary>
        public string Text
        {
            get => text;
            set => text = value ?? string.Empty;
        }

        public Color Color
        {
            get => color;
            set
            {
                color = value;
                Paint();
            }
        }

        public Color HoverColor
        {
            get => hoverColor;
            set
            {
                hoverColor = value;
                Paint();
            }
        }

        public Color CopiedColor
        {
            get => copiedColor;
            set
            {
                copiedColor = value;
                Paint();
            }
        }

        public float CopiedSeconds
        {
            get => copiedSeconds;
            set => copiedSeconds = Mathf.Max(0f, value);
        }

        /// <summary>Something was copied, with what.</summary>
        public UnityEvent<string> OnCopied => onCopied;

        /// <summary>Whether the tick is showing.</summary>
        public bool ShowingCopied => Time.unscaledTime < copiedUntil;

        /// <summary>What a click copies right now.</summary>
        public string Copyable => source != null ? source.GetParsedText() : text;

        public RectTransform Rect => (RectTransform)transform;

        /// <summary>Puts a copy button at the right edge of a label, or finds the one already there, and keeps
        /// the label's text clear of it. Call again to resize it.</summary>
        public static UiCopyButton On(TMP_Text label, float size, float gap)
        {
            if (label == null)
                return null;

            var found = label.transform.Find("Copy");
            var button = found != null ? found.GetComponent<UiCopyButton>() : null;

            if (button == null)
            {
                var made = new GameObject("Copy", typeof(RectTransform), typeof(UiCopyButton));
                made.transform.SetParent(label.transform, false);
                button = made.GetComponent<UiCopyButton>();
            }

            button.source = label;
            button.Place(size);

            var margin = label.margin;
            margin.z = size + Mathf.Max(0f, gap);
            label.margin = margin;

            return button;
        }

        /// <summary>Copies now. From code it only reaches a browser's clipboard close to a press or a click.</summary>
        public bool Copy()
        {
            string value = Copyable;
            if (!UiClipboard.Copy(value))
                return false;

            Copied(value);
            return true;
        }

        // ------------------------------------------------------------------ unity

        void Awake()
        {
            Build();
            Paint();
        }

        void OnDisable()
        {
            Withdraw();
            hovered = false;
            copiedUntil = -1f;
            Paint();
        }

        void Update()
        {
            // The press became a scroll: the drag went to whatever is behind the button, and its release must not
            // copy anything.
            if (press != null && press.dragging)
                Withdraw();

            if (copiedUntil > 0f && !ShowingCopied)
            {
                copiedUntil = -1f;
                Paint();
            }
        }

        // ------------------------------------------------------------------ the pointer

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            press = eventData;

            // Ahead of the release, so the page can copy inside it.
            UiClipboard.OfferClick(Copyable);
        }

        public void OnPointerUp(PointerEventData eventData) => Withdraw();

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            Paint();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            Paint();

            if (press != null && eventData.pointerId == press.pointerId)
                Withdraw();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            // In a browser the page has copied it already, in the release. Copying again from here as well is
            // for a click quicker than a frame, which can be over before the offer was made - harmless when the
            // first copy took, and on most browsers still close enough to the click to work.
            string value = Copyable;
            UiClipboard.Copy(value);
            Copied(value);
        }

        // ------------------------------------------------------------------ drawing

        // Built from Rounded Boxes found by name before they are made, like the template's windows: safe to run
        // again on a button that was saved in a scene.
        private void Build()
        {
            hit = Part("Hit");
            back = new[] { Part("Back Top"), Part("Back Left"), Part("Back Right"), Part("Back Bottom") };
            front = Part("Front");
            tickShort = Part("Tick Short");
            tickLong = Part("Tick Long");

            // Bigger than the icon, so it is not a target only a mouse could hit; invisible.
            hit.raycastTarget = true;
            hit.FillColor = Color.clear;

            Place(Rect.rect.width > 0f ? Rect.rect.width : 18f);
        }

        private RoundedBox Part(string name)
        {
            var part = UiWindowParts.Box(transform, name);
            part.raycastTarget = false;
            return part;
        }

        // Two pages, the usual copy mark: the front one outlined, and the back one as the four stretches of its
        // outline the front one does not cover. Drawn as stretches rather than as a second outline underneath,
        // because the icon is see-through and an outline under another would show through it. Then a tick in the
        // same square, for after.
        private void Place(float size)
        {
            if (hit == null)
                Build();

            var rect = Rect;
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = Vector2.zero;

            float page = size * 0.68f;
            float t = Mathf.Min(thickness, size * 0.15f);
            float gap = t * 1.5f;

            Square(hit.rectTransform, new Vector2(size * 0.5f, 0f), size + 12f);

            // From the button's bottom-left: the back page is the top-left square, the front one the bottom-right.
            Bar(back[0].rectTransform, 0f, size - t, page, size);
            Bar(back[1].rectTransform, 0f, size - page, t, size - t);
            Bar(back[2].rectTransform, page - t, page + gap, page, size - t);
            Bar(back[3].rectTransform, t, size - page, size - page - gap, size - page + t);

            foreach (var bar in back)
                bar.SetCornerRadius(0f);

            Square(front.rectTransform, new Vector2(size - page * 0.5f, page * 0.5f - size * 0.5f), page);
            front.SetBorderSize(t);
            front.SetCornerRadius(size * 0.14f);

            Stroke(tickShort.rectTransform, new Vector2(0.16f, 0.50f) * size, new Vector2(0.40f, 0.26f) * size, t * 1.2f);
            Stroke(tickLong.rectTransform, new Vector2(0.40f, 0.26f) * size, new Vector2(0.86f, 0.74f) * size, t * 1.2f);

            Paint();
        }

        // A rectangle between two corners, measured from the button's bottom-left.
        private static void Bar(RectTransform part, float left, float bottom, float right, float top)
        {
            part.anchorMin = Vector2.zero;
            part.anchorMax = Vector2.zero;
            part.pivot = Vector2.zero;
            part.sizeDelta = new Vector2(Mathf.Max(0f, right - left), Mathf.Max(0f, top - bottom));
            part.anchoredPosition = new Vector2(left, bottom);
            part.localRotation = Quaternion.identity;
        }

        // A square of the given side, centred on a point measured from the button's bottom-left - y from its
        // middle, which is where the anchors put the origin vertically.
        private static void Square(RectTransform part, Vector2 centre, float side)
        {
            part.anchorMin = new Vector2(0f, 0.5f);
            part.anchorMax = new Vector2(0f, 0.5f);
            part.pivot = new Vector2(0.5f, 0.5f);
            part.sizeDelta = new Vector2(side, side);
            part.anchoredPosition = centre;
            part.localRotation = Quaternion.identity;
        }

        // A bar from one point to another, both measured from the button's bottom-left.
        private static void Stroke(RectTransform part, Vector2 from, Vector2 to, float width)
        {
            var along = to - from;
            float length = along.magnitude + width;

            part.anchorMin = new Vector2(0f, 0f);
            part.anchorMax = new Vector2(0f, 0f);
            part.pivot = new Vector2(0.5f, 0.5f);
            part.sizeDelta = new Vector2(length, width);
            part.anchoredPosition = (from + to) * 0.5f;
            part.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg);
        }

        private void Paint()
        {
            if (hit == null)
                return;

            bool copied = ShowingCopied;
            var ink = hovered ? hoverColor : color;

            foreach (var bar in back)
            {
                bar.gameObject.SetActive(!copied);
                bar.FillColor = ink;
            }

            front.gameObject.SetActive(!copied);
            tickShort.gameObject.SetActive(copied);
            tickLong.gameObject.SetActive(copied);

            front.FillColor = Color.clear;
            front.SetBorderColor(ink);

            tickShort.FillColor = copiedColor;
            tickLong.FillColor = copiedColor;
            tickShort.SetCornerRadius(tickShort.rectTransform.sizeDelta.y * 0.5f);
            tickLong.SetCornerRadius(tickLong.rectTransform.sizeDelta.y * 0.5f);
        }

        // ------------------------------------------------------------------ small change

        private void Copied(string value)
        {
            copiedUntil = Time.unscaledTime + copiedSeconds;
            Paint();
            onCopied.Invoke(value);
        }

        private void Withdraw()
        {
            if (press == null)
                return;

            press = null;
            UiClipboard.OfferClick(null);
        }
    }
}
