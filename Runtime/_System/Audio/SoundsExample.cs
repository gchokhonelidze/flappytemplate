using UnityEngine;

namespace FlappyTemplate
{
    // A worked example of the whole audio surface: a bank of named clips, a bed of music, a few keys that play
    // things, and the dialog the player changes it all from. Drop it on an empty RectTransform inside a canvas,
    // put two or three clips in the fields, and press play.
    //
    // It is here to be read as much as run. What it shows, in order:
    //
    //   - **A bank registers names.** SoundBank is filled from the inspector and every clip in it becomes
    //     playable from anywhere as `Sounds.Play("name")` - no reference passed around, no manager found.
    //   - **Playing is one line**, and the player's own switch and volume are already in it. Nothing here asks
    //     whether sound is on; a muted channel plays nothing, and that is the whole contract.
    //   - **Music is one clip at a time**, faded rather than cut, and it survives a scene change because the
    //     object it plays on does.
    //   - **The dialog changes settings, not this object.** Open it with N and move a slider: the next press of
    //     a key is quieter, because both read the same four settings.
    //
    // On WebGL the music does not start until the first click or key - the browser's rule, not the package's -
    // so the first press of a key here is also the moment the bed comes in.
    [AddComponentMenu("Audio/Sounds Example")]
    [RequireComponent(typeof(RectTransform))]
    public class SoundsExample : MonoBehaviour
    {
        [Tooltip("Played on Z, as a one-shot. Registered as \"click\".")]
        [SerializeField]
        private AudioClip click;

        [Tooltip("Played on X. Registered as \"win\".")]
        [SerializeField]
        private AudioClip win;

        [Tooltip("Looped on C and stopped again on V - a spinning reel, an engine. Registered as \"loop\".")]
        [SerializeField]
        private AudioClip engine;

        [Tooltip("The bed. Put on at Start, faded in.")]
        [SerializeField]
        private AudioClip music;

        private UiNavbar navbar;
        private SoundBank bank;
        private AudioSource running;

        void Start()
        {
            // The bar first, so the sound button has something to open. Its Sound slot finds the window in the
            // scene or builds one; nothing has to be handed to it, because the window reads the settings.
            navbar = UiNavbar.Create(transform);

            // The bank could just as well be a component dropped on an object in the scene with its list filled
            // in - that is the usual way to use it, and this is the same thing from code.
            bank = gameObject.AddComponent<SoundBank>();
            bank.Clips.Add(new SoundBank.Entry { Name = "click", Clip = click });
            bank.Clips.Add(new SoundBank.Entry { Name = "win", Clip = win });
            bank.Clips.Add(new SoundBank.Entry { Name = "loop", Clip = engine });
            bank.Register();

            // One line, and the player's music switch and level are already applied. Held until the first
            // gesture on WebGL, which is what makes music that starts silently halfway through impossible.
            Sounds.PlayMusic(music, 1f, 1.2f);

            Bind();
        }

        void OnDestroy()
        {
            // Bindings outlive the scene - the registry is static and does not reload with it - so anything
            // bound by an object that goes away has to go with it.
            Hotkeys.Unbind(KeyCode.Z);
            Hotkeys.Unbind(KeyCode.X);
            Hotkeys.Unbind(KeyCode.C);
            Hotkeys.Unbind(KeyCode.V);
            Hotkeys.Unbind(KeyCode.N);
            Hotkeys.Unbind(KeyCode.M);

            // A loop nobody stops holds one of the pool's voices for the rest of the session, which is the one
            // way to leak anything here.
            Sounds.Stop(running);
        }

        /// <summary>Every binding the example makes. Hotkeys are off until the player switches them on, so the
        /// buttons below are the way in if nothing happens.</summary>
        public void Bind()
        {
            // By name, through the bank. A pitch spread of a few per cent is what keeps a click that fires ten
            // times a second from sounding like a machine.
            Hotkeys.Bind(KeyCode.Z, "Click", () => Sounds.Play("click", 1f, Random.Range(0.95f, 1.05f)));
            Hotkeys.Bind(KeyCode.X, "Win", () => Sounds.Play(win));

            Hotkeys.Bind(KeyCode.C, "Start the loop", StartLoop);
            Hotkeys.Bind(KeyCode.V, "Stop the loop", StopLoop);

            Hotkeys.Bind(KeyCode.N, "Sound settings", navbar.ShowSound);
            Hotkeys.Bind(KeyCode.M, "Mute everything", MuteAll);
        }

        /// <summary>A looping effect, held so it can be stopped. Nothing else will stop it.</summary>
        [ContextMenu("Start Loop")]
        public void StartLoop()
        {
            if (running != null)
                return;

            // Null when the effects channel is off, which is worth handling rather than asserting: the player
            // is allowed to have muted the game.
            running = Sounds.Loop(engine, 0.6f);
        }

        [ContextMenu("Stop Loop")]
        public void StopLoop()
        {
            Sounds.Stop(running);
            running = null;
        }

        /// <summary>Both channels off, the way a mute button in the corner of a game would do it. This is a
        /// setting like any other: it goes up the socket and the player finds it still muted tomorrow.</summary>
        [ContextMenu("Mute Everything")]
        public void MuteAll()
        {
            Sounds.SoundOn = false;
            Sounds.MusicOn = false;
        }

        /// <summary>Both back on, at half volume, in one place - so the difference between a switch and a level
        /// is there to be heard.</summary>
        [ContextMenu("Half Volume")]
        public void HalfVolume()
        {
            Sounds.SoundOn = true;
            Sounds.MusicOn = true;
            Sounds.SoundVolume = 0.5f;
            Sounds.MusicVolume = 0.5f;
        }
    }
}
