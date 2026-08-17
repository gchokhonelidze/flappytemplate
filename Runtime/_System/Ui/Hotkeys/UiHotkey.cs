using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // Binding a key without writing any code: drop this on the button the key should press, pick the key, and
    // give it a caption for the hotkeys window.
    //
    //     Add Component -> UI -> Ui Hotkey
    //       Key    D
    //       Label  Min
    //
    // On a Button it needs nothing else - the press does exactly what a click does, including doing nothing
    // while the button is greyed out or hidden. On anything else, wire the On Down event like any other
    // UnityEvent. Both work at once if a game wants the click and something beside it.
    //
    // The binding is made when the component is enabled and dropped when it is disabled, so a control that
    // comes and goes takes its key with it - and the hotkeys window's list follows, because the list is the
    // registry rather than a copy of it.
    //
    // Code is the other way round and no worse: Hotkeys.Bind(KeyCode.D, "Min", bet.Min). This is here because a
    // game whose controls are already laid out in a scene should not have to open a script to say which keys
    // press them. See the readme beside this file.
    [AddComponentMenu("UI/Ui Hotkey")]
    [DisallowMultipleComponent]
    public class UiHotkey : MonoBehaviour
    {
        [Tooltip("The key that fires it. None binds nothing, which is what an unfinished slot should do rather than taking a key it was not given.")]
        [SerializeField]
        private KeyCode key = KeyCode.None;

        [Tooltip("What the hotkeys window prints beside the key cap. Goes through the translator, so an English wording that is also a translation key comes back in the player's language.")]
        [SerializeField]
        private string label = "";

        [Header("What it presses")]
        [Tooltip("Clicked when the key goes down. Empty finds a Button on this object, which is the usual case - drop this on the button and there is nothing to wire.")]
        [SerializeField]
        private Button button;

        [Tooltip("Off ignores any button and raises only the events below.")]
        [SerializeField]
        private bool pressButton = true;

        [Header("Events")]
        [Tooltip("The key going down. Fires once per press, not once per frame it is held.")]
        public UnityEvent OnDown = new UnityEvent();

        [Tooltip("The key coming back up. For a control that charges while it is held.")]
        public UnityEvent OnUp = new UnityEvent();

        [Tooltip("Every frame the key is down, the press frame included. Leave it empty unless something really has to run per frame - a repeat is usually better said as a press and a timer.")]
        public UnityEvent OnHeld = new UnityEvent();

        private Hotkey binding;

        /// <summary>The key this binds. Setting it rebinds at once, which is what a game offering the player a
        /// choice of keys needs.</summary>
        public KeyCode Key
        {
            get => key;
            set
            {
                if (key == value)
                    return;

                key = value;
                Rebind();
            }
        }

        /// <summary>What the window prints beside the cap.</summary>
        public string Label
        {
            get => label;
            set
            {
                label = value ?? string.Empty;

                if (binding != null)
                    binding.Label = label;
            }
        }

        /// <summary>The button the key presses, or null for a component that only raises its events. Empty in
        /// the inspector means the Button on this object, if there is one.</summary>
        public Button Button
        {
            get => Target();
            set
            {
                button = value;
                Rebind();
            }
        }

        /// <summary>The binding itself, or null while the component is disabled. For reading its down state, or
        /// switching it off for a round without unbinding it.</summary>
        public Hotkey Binding => binding;

        void OnEnable()
        {
            Rebind();
        }

        void OnDisable()
        {
            Drop();
        }

#if UNITY_EDITOR
        // A key or a caption changed in the inspector while the game is running. Only in play mode: nothing is
        // bound outside it, and OnValidate runs mid-import and mid-undo where binding anything would be a poor
        // idea anyway.
        void OnValidate()
        {
            if (!Application.isPlaying || !isActiveAndEnabled)
                return;

            UnityEditor.EditorApplication.delayCall -= ValidateDeferred;
            UnityEditor.EditorApplication.delayCall += ValidateDeferred;
        }

        private void ValidateDeferred()
        {
            if (this == null || !isActiveAndEnabled)
                return;

            Rebind();
        }
#endif

        /// <summary>Binds the key again - after changing which key, which button, or what it is called from
        /// code. Called on every enable, so a scene does not have to.</summary>
        public void Rebind()
        {
            Drop();

            if (!Application.isPlaying || key == KeyCode.None)
                return;

            binding = Hotkeys.Bind(key, Caption(), Down, Up, OnHeld.Invoke);
        }

        /// <summary>Drops the binding. Called on every disable.</summary>
        public void Drop()
        {
            if (binding == null)
                return;

            binding.Dispose();
            binding = null;
        }

        // Empty falls back to the key's own name, so a slot somebody forgot to caption reads as "D" in the
        // window rather than as a blank row that looks like a bug.
        private string Caption() => !string.IsNullOrWhiteSpace(label) ? label : key.ToString();

        // The button in the field, or the one on this object. Looked up rather than cached: a game is free to
        // add the button after this component, and this is asked once per bind.
        private Button Target()
        {
            if (!pressButton)
                return null;

            return button != null ? button : GetComponent<Button>();
        }

        private void Down()
        {
            var target = Target();

            // Asked at the press rather than at the bind, the same as Hotkeys.Bind(key, label, button) does it:
            // a button greyed out between rounds is a key that does nothing between rounds, which is what the
            // player sees on screen and therefore what they expect.
            if (target != null && target.isActiveAndEnabled && target.interactable)
                target.onClick.Invoke();

            OnDown.Invoke();
        }

        private void Up() => OnUp.Invoke();
    }
}
