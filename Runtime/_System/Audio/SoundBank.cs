using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlappyTemplate
{
    // The clips a game plays, named, in one place - and the shortest way to get sound into a scene without
    // writing any code at all.
    //
    // Drop it on anything, fill the list in, and every clip in it is playable from anywhere by its name:
    //
    //     Sounds.Play("coin");
    //
    // A button can play one without a line of script: put `Play` in its On Click and type the name. The music
    // field starts the bed when the scene loads - held, on WebGL, until the player's first click, which is the
    // rule browsers impose and <see cref="Sounds"/> works around.
    //
    // Names are matched without regard to case and an entry left unnamed answers to its clip's own asset name,
    // so a list of dropped clips works as it is. The registry behind it is static and outlives the scene, so
    // one bank at the start of a game covers all of it - and a bank in a scene loaded later adds to it rather
    // than replacing it. `Clear On Disable` is there for the other case: a scene whose clips should go with it.
    [AddComponentMenu("Audio/Sound Bank")]
    [DisallowMultipleComponent]
    public class SoundBank : MonoBehaviour
    {
        /// <summary>One named clip. An empty name means the clip's own.</summary>
        [Serializable]
        public class Entry
        {
            [Tooltip("What Sounds.Play asks for. Empty uses the clip's asset name.")]
            public string Name;

            public AudioClip Clip;
        }

        [Header("Effects")]
        [Tooltip("Every clip this bank registers. Play them with Sounds.Play(\"name\"), or with this component's own Play from a button.")]
        [SerializeField]
        private List<Entry> clips = new List<Entry>();

        [Tooltip("How loud this bank's own Play is, against whatever the player set the effects level to.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float volume = 1f;

        [Tooltip("Playback speed, picked between the two on every press. Leave both at 1 for none. A little spread - 0.95 to 1.05 - is what keeps a click that fires ten times a second from sounding like a machine.")]
        [SerializeField]
        private Vector2 pitch = Vector2.one;

        [Header("Music")]
        [Tooltip("The bed to put on when this object wakes. Empty leaves whatever is playing alone.")]
        [SerializeField]
        private AudioClip music;

        [Tooltip("Start it as soon as the scene loads. On WebGL it is held until the player's first click, which is the browser's rule rather than this component's.")]
        [SerializeField]
        private bool playMusic = true;

        [Range(0f, 1f)]
        [SerializeField]
        private float musicVolume = 1f;

        [Tooltip("Seconds to fade in over, and to cross out of whatever was playing before.")]
        [Min(0f)]
        [SerializeField]
        private float musicFade = 0.6f;

        [Header("Behaviour")]
        [Tooltip("Forget this bank's names when the object goes away. Off - the usual case - leaves them registered, so a bank in the first scene covers the whole game.")]
        [SerializeField]
        private bool clearOnDisable = false;

        /// <summary>The list, for a game that fills it from code. Call <see cref="Register"/> afterwards.</summary>
        public List<Entry> Clips => clips;

        /// <summary>The bed this bank starts. Assigning it puts the new one on straight away if the bank is
        /// set to play music at all.</summary>
        public AudioClip Music
        {
            get => music;
            set
            {
                music = value;

                if (playMusic && isActiveAndEnabled)
                    StartMusic();
            }
        }

        void OnEnable()
        {
            Register();

            if (playMusic)
                StartMusic();
        }

        void OnDisable()
        {
            if (!clearOnDisable)
                return;

            for (int i = 0; i < clips.Count; i++)
            {
                var entry = clips[i];
                if (entry != null && entry.Clip != null)
                    Sounds.Register(Name(entry), null);
            }
        }

        /// <summary>Puts every clip in the list into the registry. Called on the way in; call it again after
        /// changing the list from code.</summary>
        public void Register()
        {
            for (int i = 0; i < clips.Count; i++)
            {
                var entry = clips[i];
                if (entry == null || entry.Clip == null)
                    continue;

                Sounds.Register(Name(entry), entry.Clip);
            }
        }

        /// <summary>Plays one of this bank's clips by name, at the bank's own volume and with its pitch
        /// spread. Wire it straight onto a button's On Click - it takes the name as its one argument, which is
        /// what makes it usable from the inspector.</summary>
        public void Play(string name)
        {
            var clip = Sounds.Clip(name);
            if (clip == null)
            {
                // Through Sounds rather than silently: it says the name once and only once, which is what a
                // misspelling in an inspector field needs.
                Sounds.Play(name, volume);
                return;
            }

            Sounds.Play(clip, volume, Pitch());
        }

        /// <summary>Plays a clip that is not in the list, at this bank's volume and pitch spread.</summary>
        public void Play(AudioClip clip) => Sounds.Play(clip, volume, Pitch());

        /// <summary>Puts this bank's music on. What Play Music does at load, for a game that would rather
        /// choose the moment.</summary>
        public void StartMusic()
        {
            if (music != null)
                Sounds.PlayMusic(music, musicVolume, musicFade);
        }

        /// <summary>Fades the music out. The same as <see cref="Sounds.StopMusic"/>, here so it can be wired
        /// onto a button.</summary>
        public void StopMusic() => Sounds.StopMusic(musicFade);

        private float Pitch()
        {
            float low = Mathf.Min(pitch.x, pitch.y);
            float high = Mathf.Max(pitch.x, pitch.y);

            return Mathf.Approximately(low, high) ? low : UnityEngine.Random.Range(low, high);
        }

        private static string Name(Entry entry) =>
            string.IsNullOrWhiteSpace(entry.Name) ? entry.Clip.name : entry.Name.Trim();
    }
}
