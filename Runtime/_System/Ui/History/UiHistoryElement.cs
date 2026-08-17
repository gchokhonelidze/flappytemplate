using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // One bet in a history strip, and the seam a game builds its own through.
    //
    // The strip draws nothing of its own: an element is a prefab of the game's, and everything about how a round
    // looks is decided here rather than in a setting on the strip. Which means two things are worth knowing
    // before writing one:
    //
    //  - Derive from it and override Write. Data is the whole HistoryDto and Outcome(key) reaches the game's own
    //    payload, so a chip that says what happened - a multiplier, a colour, an icon, a coin - is a serialized
    //    field of yours and two lines in Write. The base does nothing: there is no value the strip could have
    //    worked out that the game would not rather decide for itself.
    //
    //  - Override Appear for a prefab that wants to arrive its own way.
    //
    // The size of an element is the prefab's too - its own Layout Element, or the rect it was drawn at. The strip
    // measures the prefab and makes a track that size.
    //
    // Clicking is wired up here rather than in the prefab: a Button is added if there is none, and it opens the
    // bet info window on this bet. A prefab that brings its own Button keeps it - only the listener is added.
    [AddComponentMenu("UI/Ui History Element")]
    [RequireComponent(typeof(RectTransform))]
    public class UiHistoryElement : MonoBehaviour
    {
        [Tooltip("The background, and what the pointer lands on. Left empty: a child called Plate, else a graphic on this object, else the first one inside that is not text.")]
        [SerializeField]
        private Graphic plate;

        // Which prefab this was made from. The strip throws away anything left over from another one rather than
        // trying to reuse it: a prefab is not a chip with different colours.
        [HideInInspector]
        [SerializeField]
        private UiHistoryElement source;

        private UiHistory owner;
        private HistoryDto data;
        private GameHistoryDto round;
        private bool found;
        private bool wired;

        private CanvasGroup group;
        private Button button;
        private Tweener grow;
        private Tweener fade;

        /// <summary>The bet this element is showing, or null while it is spare.</summary>
        public HistoryDto Data => data;

        /// <summary>The shared round this element is showing, or null on a strip of the player's own bets.
        ///
        /// A round is drawn through <see cref="Data"/> like everything else - the strip maps it onto a bet, so
        /// an element written for the player's history draws a shared one without being changed - and this is
        /// the rest of it: the number of bets on the round and what they came to, which a bet has no room for.
        /// See <see cref="UiHistory.Feed"/>.</summary>
        public GameHistoryDto Round => round;

        /// <summary>The strip this belongs to. Null on an element that was never handed to one.</summary>
        public UiHistory History => owner;

        public RectTransform Rect => (RectTransform)transform;

        public Graphic Plate
        {
            get => plate;
            set => plate = value;
        }

        internal UiHistoryElement Source
        {
            get => source;
            set => source = value;
        }

        /// <summary>A value out of the game's own payload for this bet, or empty.</summary>
        public string Outcome(string key) => UiHistory.Read(data, key);

        /// <summary>Opens the bet info window on this bet, the same as clicking it. Wired to the element's
        /// Button, and public so a prefab can call it from its own control instead.</summary>
        public void Pick()
        {
            // A clicked Button stays selected, and a selected Selectable goes on drawing its highlighted colour
            // - which on a strip of chips reads as some other bet being hovered. Let it go as the click lands.
            var events = EventSystem.current;
            if (events != null && events.currentSelectedGameObject == gameObject)
                events.SetSelectedGameObject(null);

            if (owner != null)
                owner.Pick(this);
        }

        /// <summary>Puts the bet in the element. Override it - this is what a prefab is for.</summary>
        // Empty on purpose. The strip knows a bet arrived and nothing about what it means, so anything written
        // here would be a guess at a game it cannot see.
        public virtual void Write(HistoryDto value) { }

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

        internal void Bind(HistoryDto value, GameHistoryDto source)
        {
            data = value;
            round = source;

            Parts();
            Write(value);
        }

        internal void Release()
        {
            Kill();
            Rest();

            data = null;
            round = null;
        }

        // Only ever looked for once, and only for what was not filled in on the prefab. GetComponentInChildren
        // over a strip of forty elements every time one arrived would be the one expensive thing in here.
        private void Parts()
        {
            if (found)
                return;

            found = true;

            if (plate == null)
                plate = FindPlate();
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

            // Text last, and only if there is nothing else. A label is routinely laid out larger than the element
            // it sits in, and a hit area that overhangs its neighbours means pressing one chip opens the bet
            // beside it.
            var all = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && !(all[i] is TMP_Text))
                    return all[i];
            }

            return null;
        }

        private void Wire()
        {
            if (wired)
                return;

            button = GetComponent<Button>();
            bool made = button == null;

            if (made)
            {
                button = gameObject.AddComponent<Button>();

                // Nothing to navigate between: fifteen chips in a row would swallow the arrow keys and hand the
                // player a selection they never asked for.
                var navigation = button.navigation;
                navigation.mode = Navigation.Mode.None;
                button.navigation = navigation;
            }

            // The plate is what the pointer lands on, so it has to be a raycast target - a chip made of graphics
            // that all opt out of raycasting is a chip that cannot be clicked. The button tints it on hover
            // through the canvas renderer, which leaves the plate's own colour alone.
            if (plate != null)
            {
                plate.raycastTarget = true;

                if (button.targetGraphic == null)
                    button.targetGraphic = plate;
            }

            // Text is never the hit area. A label is routinely laid out larger than the element it sits in - a
            // 200 wide caption in a 60 wide chip is an ordinary thing to build - and a raycast target that
            // overhangs its neighbours means the pointer over one chip lands on the one beside it. The plate is
            // what gets pressed, and the plate is exactly the size of the element.
            var texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i] != plate)
                    texts[i].raycastTarget = false;
            }

            button.onClick.AddListener(Pick);
            wired = true;
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
