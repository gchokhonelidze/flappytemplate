using TMPro;
using UnityEngine;

namespace FlappyTemplate
{
    // The little key cap in the corner of a button, saying which key presses it.
    //
    //     Add Component -> UI -> Ui Hotkey Mark
    //
    // Dropped on a button that already has a UiHotkey it needs nothing at all - it takes the key from that. On
    // anything else, name the key yourself.
    //
    // It **hides itself while hotkeys are switched off**, and while nothing is bound to the key, which is the whole
    // reason it is a component rather than a label somebody typed: a badge saying "D" on a game where D does
    // nothing is worse than no badge, and the setting behind it is the player's to change at any moment. It lights
    // while the key is held, so a press is answered on the control itself and not only in the hotkeys window.
    //
    // Put it in the corner of the button - a rect anchored to the top right, thirty units or so square - and leave
    // the rest to the style fields. It draws its own cap, so it wants an empty RectTransform rather than a Panel.
    [AddComponentMenu("UI/Ui Hotkey Mark")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class UiHotkeyMark : MonoBehaviour
    {
        private const string PlateName = "Plate";
        private const string FaceName = "Face";

        [Tooltip("The key to show. None takes it from a Ui Hotkey on this object or its parents, which is the usual case.")]
        [SerializeField]
        private KeyCode key = KeyCode.None;

        [Header("Look")]
        public Color Fill = new Color(0.91f, 0.77f, 0.36f);

        [Tooltip("While the key is held down.")]
        public Color DownFill = new Color(0.99f, 0.89f, 0.53f);

        public Color TextColor = new Color(0.20f, 0.15f, 0.32f);

        [Min(0f)]
        public float CornerRadius = 6f;

        [Min(1f)]
        public float TextSize = 16f;

        public TMP_FontAsset Font;

        public FontStyles TextStyle = FontStyles.Bold;

        [Header("When to show")]
        [Tooltip("Hide while nothing is bound to the key. On is nearly always right - a badge for a key that does nothing is a promise the game does not keep.")]
        [SerializeField]
        private bool hideWhenUnbound = true;

        [Tooltip("Hide while the player has hotkeys switched off. On for the same reason.")]
        [SerializeField]
        private bool hideWhenOff = true;

        private RoundedBox plate;
        private TextMeshProUGUI face;
        private bool built;
        private bool listening;

        /// <summary>The key this shows. None takes it from a <see cref="UiHotkey"/> above it.</summary>
        public KeyCode Key
        {
            get => Bound();
            set
            {
                key = value;
                Refresh();
            }
        }

        void OnEnable()
        {
            EnsureBuilt();
            Listen(true);
            Refresh();
        }

        void OnDisable()
        {
            Listen(false);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // Deferred, and by a named method rather than a closure so the same call is not queued twice for one
            // edit. OnValidate is not allowed to make or destroy anything, and building the cap does both.
            UnityEditor.EditorApplication.delayCall -= ValidateDeferred;
            UnityEditor.EditorApplication.delayCall += ValidateDeferred;
        }

        private void ValidateDeferred()
        {
            if (this == null)
                return;

            EnsureBuilt();
            Refresh();
        }
#endif

        /// <summary>Makes the cap and the label, or picks up the ones already there.</summary>
        public void EnsureBuilt()
        {
            if (built && plate != null && face != null)
                return;

            plate = UiWindowParts.Box(transform, PlateName);
            face = UiWindowParts.Label(plate.transform, FaceName);

            UiWindowParts.Stretch(plate.rectTransform, 0f, 0f, 0f, 0f);
            UiWindowParts.Stretch(face.rectTransform, 1f, 0f, 1f, 0f);

            built = true;
        }

        /// <summary>Reads the binding again: whether to show at all, what to print, and whether the key is down.</summary>
        public void Refresh()
        {
            if (!built || plate == null || face == null)
                return;

            var wanted = Bound();
            var binding = Hotkeys.Find(wanted);

            bool on = !hideWhenOff || Hotkeys.Enabled;
            bool has = !hideWhenUnbound || (binding != null && binding.Enabled);

            // In the editor with nothing running there is no registry to have bound anything, and a badge that can
            // never be seen cannot be placed or styled. So it is drawn on the key it was given - a game never gets
            // here, because a game is playing.
            bool preview = !Application.isPlaying && wanted != KeyCode.None;

            bool visible = wanted != KeyCode.None && (preview || (on && has));

            if (plate.gameObject.activeSelf != visible)
                plate.gameObject.SetActive(visible);

            if (!visible)
                return;

            bool held = binding != null && binding.IsDown;

            plate.FillGradientMode = EFillGradient.None;
            plate.FillColor = held ? DownFill : Fill;
            plate.SetBorderSize(0f);
            plate.SetCornerRadius(Mathf.Max(0f, CornerRadius));
            plate.EdgeSoftness = 1.25f;

            // The badge says which key, it is not a second way to press it: the button underneath has to keep the
            // click, and a cap that took one would leave a dead spot in the corner of the control.
            plate.raycastTarget = false;

            face.text = HotkeyCaps.Name(wanted);
            face.font = Font != null ? Font : face.font;
            face.fontSize = TextSize;
            face.color = TextColor;
            face.fontStyle = TextStyle;
            face.alignment = TextAlignmentOptions.Center;
            face.raycastTarget = false;

            // A key called Page Up in the corner of a button shrinks rather than spilling out of the cap.
            face.enableAutoSizing = true;
            face.fontSizeMin = Mathf.Max(6f, TextSize * 0.5f);
            face.fontSizeMax = TextSize;
            face.textWrappingMode = TextWrappingModes.NoWrap;
        }

        // The key in the field, or the one the UiHotkey beside it binds. Looked up rather than cached: a game is
        // free to change which key a control answers to, and this is asked only when something changed.
        private KeyCode Bound()
        {
            if (key != KeyCode.None)
                return key;

            var owner = GetComponentInParent<UiHotkey>();
            return owner != null ? owner.Key : KeyCode.None;
        }

        private void Listen(bool on)
        {
            if (on && !listening)
            {
                Hotkeys.OnChanged += Refresh;
                Hotkeys.OnPressed += HandlePressed;
                listening = true;
            }
            else if (!on && listening)
            {
                Hotkeys.OnChanged -= Refresh;
                Hotkeys.OnPressed -= HandlePressed;
                listening = false;
            }
        }

        // Only the key this badge is for. Every mark in the game hears every press, so the ones that are not
        // interested say so cheaply rather than repainting.
        private void HandlePressed(Hotkey hotkey, bool down)
        {
            if (hotkey != null && hotkey.Key == Bound())
                Refresh();
        }
    }
}
