using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // Keys bound to things the game does, and the list of them the hotkeys window prints.
    //
    //     Hotkeys.Bind(KeyCode.D, "Min", () => bet.Min());
    //     Hotkeys.Bind(KeyCode.A, "Half the amount", () => bet.Half());
    //     Hotkeys.Bind(KeyCode.B, "Cash out", () => Emitter.Inst.OnCashout(null));
    //
    // That is the whole of binding a key. There is no manager to put in the scene, no asset to author and
    // nothing to add to the window - the window reads this list, so a key bound anywhere in the game appears
    // in it, named, with its cap lit on the drawn keyboard. A binding made from code that comes and goes is
    // dropped again through the handle Bind hands back, or through Unbind:
    //
    //     var cashout = Hotkeys.Bind(KeyCode.B, "Cash out", CashOut);
    //     cashout.Dispose();                 // or Hotkeys.Unbind(KeyCode.B)
    //
    // A key already bound is rebound rather than doubled up: one key does one thing, which is the rule the web
    // front follows and the reason the window can print a flat list. Replacing a binding whose label differs
    // says so in the console, since two controls quietly fighting over S is not something a player could
    // report usefully.
    //
    // **The player has to have asked for this.** Hotkeys are off until the `keyboard` setting says otherwise -
    // the server defaults it to 0 - so <see cref="Enabled"/> gates every binding, and the window's footer
    // button is what flips it. Flipping it emits SETTING through <see cref="Emitter"/>, so the choice follows
    // the player to their next session and to the web front. A scene with no template running at all - one
    // being built rather than played in - has no setting to read and treats them as on, so keys can be tried
    // out in the editor.
    //
    // Nothing here polls a key nothing is bound to: the loop runs over the bindings, not over the keyboard. An
    // unbound game pays for one empty Update.
    public static class Hotkeys
    {
        /// <summary>The setting the gate is stored in, which is the same name the web front uses.</summary>
        public const string Setting = "keyboard";

        // Bind order rather than key order, so the window's list reads in the order the game bound its
        // controls - which is the order the game thinks about them, and nothing a player would guess from an
        // alphabetical KeyCode.
        private static readonly List<Hotkey> bound = new List<Hotkey>();
        private static readonly Dictionary<KeyCode, Hotkey> byKey = new Dictionary<KeyCode, Hotkey>();

        // The list being walked, copied out of `bound` before the callbacks run. A press is free to bind,
        // unbind or open a window, and all three would otherwise be a list changed mid-loop.
        private static readonly List<Hotkey> pumping = new List<Hotkey>();

        private static HotkeyDriver driver;

        // The events object the settings listener is on, rather than a bool: a scene reload makes a new
        // StateManager with a new MainEvents, and a flag would leave this listening to the old one.
        private static MainEvents watched;

        // What Enabled answers with when there is no template to read the setting from.
        private static bool local = true;

        /// <summary>How a key's held state is read. Replace it to drive the bindings from something other than
        /// the old Input Manager - the Input System package, a gamepad, a row of buttons on a phone - and the
        /// presses, the releases and the down-state the window paints from all follow from it.</summary>
        public static Func<KeyCode, bool> Reader = HotkeyInput.Read;

        /// <summary>Hold the bindings while a text field has focus, so a player typing an amount does not halve
        /// it on the way past the A. On by default, and the reason it is a field is that a game with no text
        /// fields anywhere saves a lookup per frame by turning it off.</summary>
        public static bool SuppressWhileTyping = true;

        /// <summary>The list changed, or the gate flipped: a binding added, dropped, renamed or enabled. What
        /// the window rebuilds its rows on.</summary>
        public static event Action OnChanged;

        /// <summary>A bound key went down (true) or came back up (false). What the window repaints the caps
        /// on - and a fair place for a game to play a click.</summary>
        public static event Action<Hotkey, bool> OnPressed;

        /// <summary>Every binding, in the order they were bound.</summary>
        public static IReadOnlyList<Hotkey> Bindings => bound;

        /// <summary>How many keys are bound.</summary>
        public static int Count => bound.Count;

        /// <summary>Whether the player has hotkeys switched on. Reads the `keyboard` setting; setting it emits
        /// SETTING so the choice is kept, and writes it into the local state at once so the window answers the
        /// press rather than the round trip.</summary>
        // A scene with no StateManager is one being built rather than played in, and there is no socket to have
        // sent a setting over - so the local default stands and keys work. A scene that has one is a game, and
        // a game with no answer yet is a game the player has not said yes to: off, which is the server's own
        // default.
        public static bool Enabled
        {
            get
            {
                var settings = Settings();
                if (settings == null)
                    return local;

                return settings.TryGetValue(Setting, out var token) && Truthy(token);
            }
            set
            {
                if (Enabled == value)
                    return;

                local = value;
                Write(value);

                if (Emitter.Inst != null)
                    Emitter.Inst.OnSettingSet(new SettingDto { Name = Setting, Value = value ? 1 : 0 });

                Changed();
            }
        }

        /// <summary>Switches them on if they are off, and off if they are on. What the window's footer does.</summary>
        public static void Toggle() => Enabled = !Enabled;

        // ------------------------------------------------------------------ binding

        /// <summary>Binds a key to something the game does. The label is what the window prints beside the cap,
        /// and goes through <see cref="Translator.Label"/>. Hand back the result to drop the binding, or leave
        /// it - a binding lives until something takes it away.</summary>
        /// <param name="key">The key that fires it. <see cref="KeyCode.None"/> binds nothing.</param>
        /// <param name="label">Readable text, or a translation key. Shown in the hotkeys window.</param>
        /// <param name="down">Called once when the key goes down - not again while it is held.</param>
        /// <param name="up">Called when it comes back up. For a control that charges while held.</param>
        /// <param name="held">Called every frame the key is down, the press frame included.</param>
        public static Hotkey Bind(KeyCode key, string label, Action down, Action up = null, Action held = null)
        {
            if (key == KeyCode.None)
            {
                Debug.LogWarning("Hotkeys: KeyCode.None cannot be bound - \"" + label + "\" was not registered.");
                return null;
            }

            var made = new Hotkey(key, label, down, up, held);

            // One binding per key. A rebind of the same control - a window rebuilt, a scene reloaded - is the
            // usual reason to land here and is nothing to say anything about; two different controls over one
            // key is a collision the game wants to know about, and a player never could report.
            if (byKey.TryGetValue(key, out var taken) && taken != null)
            {
                if (!string.IsNullOrEmpty(taken.Label) && taken.Label != made.Label)
                {
                    Debug.LogWarning(
                        "Hotkeys: " + key + " was bound to \"" + taken.Label + "\" and is now \""
                        + made.Label + "\". One key does one thing - the first binding is gone.");
                }

                Drop(taken);
            }

            byKey[key] = made;
            bound.Add(made);

            EnsureDriver();
            Changed();

            return made;
        }

        /// <summary>Binds a key to a button that is already on screen: the press does exactly what a click on it
        /// does, and nothing at all while the button is hidden or not interactable.</summary>
        // The shortest correct way to add a key to a game that has its controls wired up already, and the one
        // that cannot drift: there is no second copy of what the button does to keep in step with the first.
        public static Hotkey Bind(KeyCode key, string label, Button button)
        {
            if (button == null)
            {
                Debug.LogWarning("Hotkeys: no button to bind " + key + " to.");
                return null;
            }

            return Bind(key, label, () =>
            {
                // Asked at the moment of the press rather than at the moment of the bind. A button greyed out
                // between rounds is a key that does nothing between rounds, which is what the player sees on
                // screen and therefore what they expect.
                if (button != null && button.isActiveAndEnabled && button.interactable)
                    button.onClick.Invoke();
            });
        }

        /// <summary>Drops whatever is bound to a key. False if nothing was.</summary>
        public static bool Unbind(KeyCode key)
        {
            if (!byKey.TryGetValue(key, out var found) || found == null)
                return false;

            Drop(found);
            Changed();
            return true;
        }

        /// <summary>Drops one binding. False if it had already gone - which is what makes this safe to call
        /// from OnDisable without asking whether anything happened since.</summary>
        public static bool Unbind(Hotkey hotkey)
        {
            if (hotkey == null || !hotkey.IsBound)
                return false;

            Drop(hotkey);
            Changed();
            return true;
        }

        /// <summary>Drops the lot. For a game changing modes rather than for tidying up after one scene.</summary>
        public static void UnbindAll()
        {
            if (bound.Count == 0)
                return;

            for (int i = bound.Count - 1; i >= 0; i--)
                Retire(bound[i]);

            bound.Clear();
            byKey.Clear();
            Changed();
        }

        /// <summary>What is bound to a key, or null.</summary>
        public static Hotkey Find(KeyCode key) => byKey.TryGetValue(key, out var found) ? found : null;

        /// <summary>Whether anything is bound to a key.</summary>
        public static bool IsBound(KeyCode key) => byKey.ContainsKey(key);

        /// <summary>Whether a bound key is held down as things stand. False for a key nothing is bound to,
        /// because nothing here watches those.</summary>
        public static bool IsDown(KeyCode key)
        {
            var found = Find(key);
            return found != null && found.IsDown;
        }

        // Out of the list and the table, and marked so a callback holding it cannot fire it again. The down
        // state is cleared without raising Up: a binding taken away mid-press should not deliver a release
        // nobody is listening for any more - a cash-out on release would cash out.
        private static void Drop(Hotkey hotkey)
        {
            bound.Remove(hotkey);

            if (byKey.TryGetValue(hotkey.Key, out var held) && held == hotkey)
                byKey.Remove(hotkey.Key);

            Retire(hotkey);
        }

        private static void Retire(Hotkey hotkey)
        {
            if (hotkey == null)
                return;

            hotkey.IsBound = false;
            hotkey.IsDown = false;
            hotkey.Down = null;
            hotkey.Up = null;
            hotkey.Held = null;
        }

        /// <summary>Raises <see cref="OnChanged"/>. Called by <see cref="Hotkey"/> when a label or an enabled
        /// flag is written, so a list on screen follows it.</summary>
        internal static void Changed() => OnChanged?.Invoke();

        // ------------------------------------------------------------------ the frame

        // Statics outlive a play session when the editor is set to skip the domain reload, so everything is
        // emptied by hand rather than left to start the next one holding the last one's bindings - and the last
        // one's driver, which is a destroyed object by then.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Clear()
        {
            for (int i = 0; i < bound.Count; i++)
                Retire(bound[i]);

            bound.Clear();
            byKey.Clear();
            pumping.Clear();

            driver = null;
            watched = null;
            local = true;

            OnChanged = null;
            OnPressed = null;

            Reader = HotkeyInput.Read;
        }

        // One hidden object for the whole game, made on the first bind rather than at load: a game that binds
        // nothing never gets one. HideAndDontSave keeps it out of the hierarchy and out of the scene file, and
        // DontDestroyOnLoad keeps the bindings alive across a scene change - which they have to be, since the
        // registry they belong to is static and does not reload with the scene.
        private static void EnsureDriver()
        {
            if (driver != null || !Application.isPlaying)
                return;

            var host = new GameObject("Hotkeys") { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.Object.DontDestroyOnLoad(host);
            driver = host.AddComponent<HotkeyDriver>();
        }

        /// <summary>One frame: catch up with the state, then read the bound keys and fire what changed.</summary>
        internal static void Tick()
        {
            Follow();
            Poll();
        }

        // The setting arrives over the socket after the scene has loaded rather than with it, and a window that
        // is on screen has to follow it being changed from somewhere else - the web front in another tab, a
        // second device. Compared against the events object rather than guarded by a bool, so a scene reload is
        // picked up rather than left listening to the StateManager that has gone.
        private static void Follow()
        {
            var manager = StateManager.Inst;
            var events = manager != null ? manager.Events : null;

            if (events == watched)
                return;

            if (watched != null)
                watched.OnSettings.RemoveListener(HandleSettings);

            watched = events;

            if (watched != null)
                watched.OnSettings.AddListener(HandleSettings);

            Changed();
        }

        private static void HandleSettings(Dictionary<string, JToken> data) => Changed();

        private static void Poll()
        {
            if (bound.Count == 0)
                return;

            // Asked once for the frame rather than once per binding: both are the same answer, and the typing
            // check walks up from the selected object.
            bool live = Enabled && !Typing();
            var read = Reader ?? HotkeyInput.Read;

            // Copied out before anything is invoked. A press is free to bind, unbind, open a window or reload a
            // scene, and every one of those would otherwise be the list changing under the loop.
            pumping.Clear();
            pumping.AddRange(bound);

            for (int i = 0; i < pumping.Count; i++)
            {
                var hotkey = pumping[i];

                // Dropped by something a press earlier in this same frame did. Its state was cleared as it
                // went, so there is nothing left to settle here.
                if (hotkey == null || !hotkey.IsBound)
                    continue;

                bool down = live && hotkey.Enabled && read(hotkey.Key);

                if (down == hotkey.IsDown)
                {
                    if (down && hotkey.Held != null)
                        hotkey.Held.Invoke();

                    continue;
                }

                hotkey.IsDown = down;

                // Before the callback rather than after: a press that opens a window should find the cap
                // already lit, and a game playing a click wants it now rather than a frame late.
                var pressed = OnPressed;
                if (pressed != null)
                    pressed.Invoke(hotkey, down);

                if (down)
                {
                    // The press before the frame, so a control that starts something in Down and adds to it in
                    // Held is not asked to add to a thing that has not started yet.
                    if (hotkey.Down != null)
                        hotkey.Down.Invoke();

                    if (hotkey.Held != null)
                        hotkey.Held.Invoke();
                }
                else if (hotkey.Up != null)
                {
                    hotkey.Up.Invoke();
                }
            }
        }

        // Whether the player is typing into something, in which case the keys belong to the field rather than
        // to the game. The selected object is asked rather than every field in the scene: a field only takes
        // what is typed while it holds the selection, so that is the same question.
        private static bool Typing()
        {
            if (!SuppressWhileTyping)
                return false;

            var events = EventSystem.current;
            var selected = events != null ? events.currentSelectedGameObject : null;

            if (selected == null)
                return false;

            if (selected.GetComponent<TMP_InputField>() != null)
                return true;

            return selected.GetComponent<InputField>() != null;
        }

        // ------------------------------------------------------------------ the setting

        private static Dictionary<string, JToken> Settings()
        {
            var manager = StateManager.Inst;
            if (manager == null || manager.MainState == null)
                return null;

            return manager.MainState.Settings;
        }

        // Written into the state as well as sent, so everything reading the setting agrees before the server
        // has answered. The socket's own ON_SETTING merges over this a moment later and says the same thing.
        private static void Write(bool value)
        {
            var manager = StateManager.Inst;
            if (manager == null || manager.MainState == null)
                return;

            manager.MainState.Settings[Setting] = new JValue(value ? 1 : 0);
            manager.MainState._Settings[Setting] = value ? "1" : "0";
        }

        // The server sends settings as strings - "1" and "0" - the web front writes numbers, and a hand-written
        // one may well be a bool. All three mean the same thing here, and a setting that arrives in the shape
        // nobody expected should read as off rather than throw.
        private static bool Truthy(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return false;

            switch (token.Type)
            {
                case JTokenType.Boolean:
                    return token.Value<bool>();
                case JTokenType.Integer:
                case JTokenType.Float:
                    return token.Value<double>() != 0d;
                case JTokenType.String:
                    var text = token.Value<string>();
                    return !string.IsNullOrEmpty(text) && text != "0"
                        && !text.Equals("false", StringComparison.OrdinalIgnoreCase);
                default:
                    return false;
            }
        }
    }
}
