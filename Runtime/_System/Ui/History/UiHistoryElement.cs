using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // One bet in a history strip, and the seam a game builds its own through.
    //
    // Three things are worth knowing before writing a prefab against this:
    //
    //  - It is often all a prefab needs. Put this component on the root, leave the fields empty, and the base
    //    behaviour finds a label and a background inside and fills those in. So the cheapest possible custom
    //    element is a prefab with a TextMeshPro label in it and this on the root - no script of your own.
    //
    //  - Derive from it and override Write to fill in more than a label - an icon, a multiplier, a coin, a
    //    little chart. Data is the whole HistoryDto, Text is what the strip decided this element should say,
    //    and Outcome(key) reaches the game's own payload.
    //
    //  - Override Paint to colour it yourself, or turn Paint Elements off on the strip and paint nothing: a
    //    prefab that arrives already looking right does not want its colours written over.
    //
    // Clicking is wired up here rather than in the prefab: a Button is added if there is none, and it opens the
    // bet info window on this bet. A prefab that brings its own Button keeps it - only the listener is added.
    [AddComponentMenu("UI/Ui History Element")]
    [RequireComponent(typeof(RectTransform))]
    public class UiHistoryElement : MonoBehaviour
    {
        [Tooltip("Where the value is printed. Left empty, the first TextMeshPro label anywhere inside is used.")]
        [SerializeField]
        private TextMeshProUGUI label;

        [Tooltip("The background the scenario colours. Left empty: a child called Plate, else a graphic on this object, else the first one inside that is not the label.")]
        [SerializeField]
        private Graphic plate;

        [Tooltip("The bar drawn on a marked element - the player's own bets, by default. Left empty, a child called Accent is used, and an element with no such child simply has no accent.")]
        [SerializeField]
        private Graphic accent;

        // Set on the elements the strip builds itself, and the reason it exists is a prefab's dignity: shape and
        // text metrics are written onto a chip this package drew, and left alone on one a game drew.
        [HideInInspector]
        [SerializeField]
        private bool native;

        private UiHistory owner;
        private HistoryDto data;
        private string text = string.Empty;
        private string scenario = string.Empty;
        private bool marked;
        private bool found;
        private bool wired;

        private CanvasGroup group;
        private Button button;
        private Tweener grow;
        private Tweener fade;

        /// <summary>The bet this element is showing, or null while it is spare.</summary>
        public HistoryDto Data => data;

        /// <summary>What the strip worked out this element should say - the outcome value, the nonce, or the
        /// tail of the id. Write puts it in the label; a game that wants something else has it here.</summary>
        public string Text => text;

        /// <summary>The name of the case the strip put this bet in: "win", "loss", or whatever the game's own
        /// classifier returned.</summary>
        public string Scenario => scenario;

        /// <summary>Whether the strip picked this bet out - the player's own, by default. What the accent bar
        /// is drawn on.</summary>
        public bool Marked => marked;

        /// <summary>The strip this belongs to. Null on an element that was never handed to one.</summary>
        public UiHistory History => owner;

        public RectTransform Rect => (RectTransform)transform;

        public TextMeshProUGUI Label
        {
            get => label;
            set => label = value;
        }

        public Graphic Plate
        {
            get => plate;
            set => plate = value;
        }

        public Graphic Accent
        {
            get => accent;
            set => accent = value;
        }

        internal bool Native
        {
            get => native;
            set => native = value;
        }

        /// <summary>A value out of the game's own payload for this bet, or empty.</summary>
        public string Outcome(string key) => UiHistory.Read(data, key);

        /// <summary>Opens the bet info window on this bet, the same as clicking it. Wired to the element's
        /// Button, and public so a prefab can call it from its own control instead.</summary>
        public void Pick()
        {
            if (owner != null)
                owner.Pick(this);
        }

        /// <summary>Puts the bet in the element. Override to fill in more than a label.</summary>
        // Base behaviour on purpose: whatever label was found gets Text. It is what makes a prefab with no
        // script of its own work, and what an override calls through to when it only wants to add to it.
        public virtual void Write(HistoryDto value)
        {
            if (label != null)
                label.text = text;
        }

        /// <summary>Colours the element for the case it turned out to be. Override, or turn the strip's Paint
        /// Elements off, to keep a prefab's own colours.</summary>
        public virtual void Paint(UiHistoryScenario look)
        {
            if (look == null)
                return;

            var style = owner != null ? owner.Style : null;

            if (plate is RoundedBox box)
            {
                box.FillColor = look.Fill;
                box.SetBorderColor(look.BorderColor);

                // A scenario that names no border size wants the strip's, and a prefab that was never asked
                // about it wants neither: writing a border onto a game's own art is not this component's
                // business.
                float border = look.BorderSize >= 0f
                    ? look.BorderSize
                    : (native && style != null ? style.BorderSize : -1f);

                if (border >= 0f)
                    box.SetBorderSize(border);
            }
            else if (plate != null)
            {
                plate.color = look.Fill;
            }

            if (label != null)
                label.color = look.TextColor;

            PaintAccent(look, style);
        }

        /// <summary>The arrival animation. Override for a prefab that wants to arrive its own way.</summary>
        // Built with DOTween.To rather than DOScale or DOFade: the shortcuts live in DOTween's UI module, which
        // is compiled into the project's own assembly and cannot be reached from a package.
        public virtual void Appear()
        {
            var style = owner != null ? owner.Style : null;
            if (style == null)
                return;

            Kill();

            if (style.AppearDuration <= 0f)
            {
                Rest();
                return;
            }

            bool unscaled = style.UnscaledTime;

            if (style.AppearScale > 0f && !Mathf.Approximately(style.AppearScale, 1f))
            {
                var target = transform;
                target.localScale = new Vector3(style.AppearScale, style.AppearScale, 1f);

                grow = DOTween
                    .To(
                        () => target.localScale.x,
                        value => target.localScale = new Vector3(value, value, 1f),
                        1f,
                        style.AppearDuration
                    )
                    .SetEase(style.AppearEase)
                    .SetUpdate(unscaled);
            }

            if (!style.AppearFade)
                return;

            var canvas = Group();
            canvas.alpha = 0f;

            // Shorter than the growing, and on a plain ease: a chip that is still translucent while it settles
            // reads as a chip that has not finished loading.
            fade = DOTween
                .To(() => canvas.alpha, value => canvas.alpha = value, 1f, style.AppearDuration * 0.7f)
                .SetEase(Ease.OutQuad)
                .SetUpdate(unscaled);
        }

        /// <summary>Puts it back where an animation would have left it: full size, fully there.</summary>
        public void Rest()
        {
            transform.localScale = Vector3.one;

            if (group != null)
                group.alpha = 1f;
        }

        // ------------------------------------------------------------------ the strip's side

        internal void Adopt(UiHistory history)
        {
            owner = history;
            Parts();
            Wire();
        }

        internal void Bind(HistoryDto value, string valueText, string scenarioName, bool markedNow)
        {
            data = value;
            text = valueText ?? string.Empty;
            scenario = scenarioName ?? string.Empty;
            marked = markedNow;

            Parts();
            Write(value);
        }

        internal void Release()
        {
            Kill();
            Rest();

            data = null;
            text = string.Empty;
            scenario = string.Empty;
            marked = false;
        }

        // Only ever looked for once, and only for what was not filled in on the prefab. GetComponentInChildren
        // over a strip of forty elements every time one arrived would be the one expensive thing in here.
        private void Parts()
        {
            if (found)
                return;

            found = true;

            if (label == null)
                label = GetComponentInChildren<TextMeshProUGUI>(true);

            if (plate == null)
                plate = FindPlate();

            if (accent != null)
                return;

            var bar = transform.Find("Accent");
            if (bar != null)
                accent = bar.GetComponent<Graphic>();
        }

        private Graphic FindPlate()
        {
            var named = transform.Find("Plate");
            if (named != null)
            {
                var graphic = named.GetComponent<Graphic>();
                if (graphic != null)
                    return graphic;
            }

            var own = GetComponent<Graphic>();
            if (own != null)
                return own;

            var all = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != label && all[i] != accent)
                    return all[i];
            }

            return null;
        }

        private void Wire()
        {
            if (wired)
                return;

            button = GetComponent<Button>();
            if (button == null)
                button = gameObject.AddComponent<Button>();

            // The plate is what the pointer lands on, so it has to be a raycast target - a chip made of
            // graphics that all opt out of raycasting is a chip that cannot be clicked. The button tints it on
            // hover through the canvas renderer, which leaves the scenario's own fill colour alone.
            if (plate != null)
            {
                plate.raycastTarget = true;

                if (button.targetGraphic == null)
                    button.targetGraphic = plate;
            }

            button.onClick.AddListener(Pick);
            wired = true;
        }

        private void PaintAccent(UiHistoryScenario look, UiHistoryStyle style)
        {
            if (accent == null)
                return;

            bool on = marked && look.AccentSize > 0f;

            // SetActive is safe here, unlike inside a grid: the accent is a child of the element, and no layout
            // has an opinion about whether it is showing.
            if (accent.gameObject.activeSelf != on)
                accent.gameObject.SetActive(on);

            if (!on)
                return;

            accent.color = look.AccentColor;

            if (!native || style == null)
                return;

            var rect = accent.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(style.AccentInset, style.AccentOffset);
            rect.offsetMax = new Vector2(-style.AccentInset, style.AccentOffset + look.AccentSize);

            if (accent is RoundedBox bar)
            {
                bar.SetCornerRadius(look.AccentSize * 0.5f);
                bar.SetBorderSize(0f);
                bar.EdgeSoftness = style.EdgeSoftness;
            }
        }

        private CanvasGroup Group()
        {
            if (group == null)
                group = GetComponent<CanvasGroup>();

            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();

            return group;
        }

        private void Kill()
        {
            if (grow != null && grow.IsActive())
                grow.Kill();

            if (fade != null && fade.IsActive())
                fade.Kill();

            grow = null;
            fade = null;
        }

        void OnDisable()
        {
            // A tween left running on an element that has been switched off would come back to a chip the strip
            // has since handed to another bet, and finish the animation on that one instead.
            Kill();
            Rest();
        }

        void OnDestroy()
        {
            Kill();
        }
    }
}
