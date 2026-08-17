using UnityEngine;

namespace FlappyTemplate
{
    // A worked bet panel driven by keys: a navbar with the hotkeys button in it, a window to look at, and eight
    // bindings covering every way there is to make one. Drop it on an empty RectTransform inside a canvas and
    // press play.
    //
    // It is here to be read as much as run. The bindings below are the whole of the feature's surface:
    //
    //   - Q, W, E, R change the amount. Four one-line binds, which is the ordinary case.
    //   - B cashes out through Emitter, and is switched off between rounds rather than unbound - so the window
    //     still shows the key, greyed, and a player who has learned it is not left wondering.
    //   - Space is bound to whatever the game does on a press, held down: the label follows the key, so the
    //     window is a live readout rather than a list somebody wrote.
    //   - H opens the hotkeys window itself, through the navbar.
    //   - The 1 key is bound twice on purpose, a few seconds apart, to show what a collision looks like in the
    //     console and which of the two wins.
    //
    // **Hotkeys are off until the player switches them on**, so nothing here fires until the button along the
    // bottom of the window is pressed - which is the point of that button and worth seeing rather than reading.
    // In the editor with no template running there is no setting to read and they are simply on.
    [AddComponentMenu("UI/Ui Hotkeys Example")]
    [RequireComponent(typeof(RectTransform))]
    public class UiHotkeysExample : MonoBehaviour
    {
        // A float rather than the decimal a real bet amount would be: Unity does not serialize decimal, and this
        // is a number for the keys to change rather than money.
        [Tooltip("What the amount starts at, so the keys have something to change.")]
        [SerializeField]
        private float amount = 1f;

        [Tooltip("Seconds before the second binding on the 1 key takes it, which is what puts the collision warning in the console.")]
        [Min(0.5f)]
        [SerializeField]
        private float collideAfter = 4f;

        private UiNavbar navbar;
        private Hotkey cashout;
        private Hotkey charge;

        // Kept so they can be dropped again. A binding made in Start and never disposed is fine in a game that
        // lives as long as the scene does - these are here because the example takes one away.
        private Hotkey collision;

        void Start()
        {
            // The bar first, so H has something to open. Its Hotkeys slot finds the window in the scene or builds
            // one; nothing has to be handed to it, because the window reads the registry rather than a list.
            navbar = UiNavbar.Create(transform);

            Bind();
            Invoke(nameof(Collide), collideAfter);
        }

        void OnDestroy()
        {
            // Bindings outlive the scene - the registry is static and does not reload with it - so anything bound
            // by an object that goes away has to go with it. Dispose is safe on a binding that was already
            // replaced, which is exactly what the 1 key's first binding will have been.
            Hotkeys.Unbind(KeyCode.Q);
            Hotkeys.Unbind(KeyCode.W);
            Hotkeys.Unbind(KeyCode.E);
            Hotkeys.Unbind(KeyCode.R);
            Hotkeys.Unbind(KeyCode.H);
            Hotkeys.Unbind(KeyCode.Alpha1);

            if (cashout != null)
                cashout.Dispose();

            if (charge != null)
                charge.Dispose();

            if (collision != null)
                collision.Dispose();
        }

        /// <summary>Every binding the example makes. Runs from Start.</summary>
        public void Bind()
        {
            // The ordinary case: a key, what to call it, and what it does. The label is what the window prints,
            // and it goes through the translator - so an English wording that is also a translation key comes
            // back in the player's language.
            Hotkeys.Bind(KeyCode.Q, "Min", () => Set(1f));
            Hotkeys.Bind(KeyCode.W, "Half the amount", () => Set(amount * 0.5f));
            Hotkeys.Bind(KeyCode.E, "Double the amount", () => Set(amount * 2f));
            Hotkeys.Bind(KeyCode.R, "Max", () => Set(1000f));

            // Held on to, because this one is switched on and off rather than bound and unbound. The difference
            // matters in the window: an unbound key leaves the list, a disabled one stays in it greyed - which is
            // what a player who has learned B needs to see between rounds.
            cashout = Hotkeys.Bind(KeyCode.B, "Cash out", CashOut);
            cashout.Enabled = false;

            // Down and up rather than just down, for a control that charges while it is held. The third callback
            // would run every frame it is down; this one renames itself instead, which is enough to watch.
            charge = Hotkeys.Bind(
                KeyCode.Space,
                "Charge",
                () => charge.Label = "Charging...",
                () => charge.Label = "Charge");

            // The window can open itself. Bound to a method rather than to a closure, which is the shorter form
            // when there is a method to point at.
            Hotkeys.Bind(KeyCode.H, "Hotkeys", navbar.ShowHotkeys);

            collision = Hotkeys.Bind(KeyCode.Alpha1, "First binding", () => Debug.Log("Hotkeys example: the first 1."));
        }

        // A second binding on a key that is already taken. One key does one thing, so this replaces the first
        // rather than joining it - and says so in the console, because two controls quietly fighting over a key
        // is not something a player could report usefully. The window shows the winner.
        private void Collide()
        {
            collision = Hotkeys.Bind(KeyCode.Alpha1, "Second binding", () => Debug.Log("Hotkeys example: the second 1."));
        }

        /// <summary>Lets the cash-out key work, the way a round starting would.</summary>
        [ContextMenu("Start Round")]
        public void StartRound()
        {
            if (cashout != null)
                cashout.Enabled = true;
        }

        /// <summary>Greys it out again, the way a round ending would.</summary>
        [ContextMenu("End Round")]
        public void EndRound()
        {
            if (cashout != null)
                cashout.Enabled = false;
        }

        private void Set(float value)
        {
            amount = Mathf.Max(1f, value);
            Debug.Log("Hotkeys example: amount is now " + amount);
        }

        private void CashOut()
        {
            // Emitter is only there in a game with a socket behind it. In an editor scene built to look at the
            // window there is nothing to send to, and saying so beats a null reference.
            if (Emitter.Inst == null)
            {
                Debug.Log("Hotkeys example: no socket to cash out through.");
                return;
            }

            Emitter.Inst.OnCashout(null);
        }
    }
}
