using System.Collections.Generic;
using UnityEngine;

namespace FlappyTemplate
{
    // The one hidden object <see cref="Sounds"/> plays through: a small pool of voices for effects, one source
    // for the music, and the three things that have to happen every frame - the volumes following the player's
    // settings, a fade running, and the browser letting audio start at all.
    //
    // Nothing puts this in a scene. It is made on the first sound, marked HideAndDontSave so it is neither in
    // the hierarchy nor in the scene file, and kept across scene changes so the music does not restart every
    // time the game loads a new one.
    //
    // Voices are AudioSources played with Play rather than one-shots on a single source, and that is the whole
    // reason the pool exists: a one-shot cannot be stopped, cannot be looped and cannot have its volume moved
    // once it is away - so muting the game in the middle of a long sound would do nothing at all until it
    // finished. A voice can be reached afterwards, which is what makes the mute in the settings window instant.
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    internal class SoundDriver : MonoBehaviour
    {
        private enum EFade
        {
            None,
            Out,
            In,
        }

        // One playing sound: the source it is on, and how loud the caller asked for it against the player's
        // own level. The two are kept apart so a settings change can be applied without losing the first.
        private class Voice
        {
            public AudioSource Source;
            public ESoundChannel Channel;
            public float Base;
            public float Started;
            public bool Loop;
        }

        private readonly List<Voice> voices = new List<Voice>();

        private AudioSource music;
        private AudioClip wanted;
        private float wantedVolume = 1f;

        private EFade fade;
        private float fadeLeft;
        private float fadeLength;

        // The events object the settings listener is on, rather than a bool: a scene reload makes a new
        // StateManager with a new MainEvents, and a flag would leave this listening to the old one.
        private MainEvents watched;

        private bool dirty = true;
        private bool warnedListener;

        /// <summary>What the music is playing, or is being faded in to play.</summary>
        public AudioClip MusicClip => wanted;

        /// <summary>The source the music plays on. Made on the first call for music, not before.</summary>
        public AudioSource MusicVoice => music;

        void OnEnable()
        {
            Sounds.OnChanged += Follow;
        }

        void OnDisable()
        {
            Sounds.OnChanged -= Follow;
        }

        void OnDestroy()
        {
            if (watched != null)
                watched.OnSettings.RemoveListener(HandleSettings);

            watched = null;
        }

        void Update()
        {
            Listen();
            Gesture();

            if (dirty)
            {
                dirty = false;
                Apply();
            }

            Fade();
            Sweep();
        }

        // ------------------------------------------------------------------ effects

        /// <summary>Plays a clip on a borrowed voice. Null only when there is nothing left to borrow, which
        /// cannot happen - the pool steals from itself rather than refusing.</summary>
        public AudioSource Fire(AudioClip clip, ESoundChannel channel, float volume, float pitch, bool loop)
        {
            if (clip == null)
                return null;

            WarnWithoutListener();

            var voice = Borrow();

            voice.Channel = channel;
            voice.Base = Mathf.Max(0f, volume);
            voice.Started = Time.unscaledTime;
            voice.Loop = loop;

            var source = voice.Source;
            source.clip = clip;
            source.loop = loop;
            source.pitch = pitch;
            source.volume = voice.Base * Sounds.Level(channel);
            source.Play();

            return source;
        }

        /// <summary>Stops one voice and frees it. Nothing at all for a source that is not one of ours - a game
        /// is free to hand this its own AudioSource, and having that silently stop would be worse than having
        /// it ignored.</summary>
        public void Release(AudioSource source)
        {
            if (source == null)
                return;

            for (int i = 0; i < voices.Count; i++)
            {
                if (voices[i].Source != source)
                    continue;

                Quiet(voices[i]);
                return;
            }
        }

        /// <summary>Stops every effect. The music is not one of these.</summary>
        public void ReleaseAll()
        {
            for (int i = 0; i < voices.Count; i++)
                Quiet(voices[i]);
        }

        // A voice that has finished, a new one while the pool is under its limit, or the longest-running one
        // taken back. Which is the right answer to a pool that is full: the oldest sound is the one already
        // half heard, and the alternative - refusing to play - is a press that makes no noise.
        private Voice Borrow()
        {
            for (int i = 0; i < voices.Count; i++)
            {
                if (!voices[i].Source.isPlaying)
                    return voices[i];
            }

            int limit = Mathf.Max(1, Sounds.Voices);

            if (voices.Count < limit)
            {
                var made = new Voice { Source = Source("Voice " + voices.Count) };
                voices.Add(made);
                return made;
            }

            var oldest = voices[0];

            for (int i = 1; i < voices.Count; i++)
            {
                if (voices[i].Started < oldest.Started)
                    oldest = voices[i];
            }

            oldest.Source.Stop();
            return oldest;
        }

        private static void Quiet(Voice voice)
        {
            if (voice.Source == null)
                return;

            voice.Source.Stop();
            voice.Source.clip = null;
            voice.Loop = false;
        }

        // A voice that has run out keeps its clip loaded until it is asked to play another, and a clip that
        // nothing refers to can be unloaded by Unity. Only the reference is dropped here - the source itself
        // stays in the pool, which is what the pool is for.
        private void Sweep()
        {
            for (int i = 0; i < voices.Count; i++)
            {
                var voice = voices[i];

                if (voice.Source != null && !voice.Source.isPlaying && voice.Source.clip != null)
                    voice.Source.clip = null;
            }
        }

        // ------------------------------------------------------------------ music

        /// <summary>Puts a clip on as the music, or takes it off. Held rather than started while the browser
        /// has not let audio through yet - see <see cref="Sounds.WaitForGesture"/>.</summary>
        public void Music(AudioClip clip, float volume, float seconds)
        {
            wantedVolume = Mathf.Max(0f, volume);

            // The same clip again: a scene reloaded, a Start that runs twice. Only the volume is taken, so the
            // bed carries on where it was rather than starting over.
            if (clip != null && clip == wanted)
            {
                dirty = true;
                return;
            }

            wanted = clip;

            // Nothing was ever asked for and nothing is being asked for now: a StopMusic on a game that plays
            // none, which should not be the thing that makes a source for it.
            if (music == null && clip == null)
                return;

            if (music == null)
            {
                music = Source("Music");
                music.loop = true;
            }

            WarnWithoutListener();

            // Nothing playing to fade out of, so there is nothing to cross: start straight into a fade in,
            // which for a fade of zero is simply on.
            if (!music.isPlaying)
            {
                Begin(seconds);
                return;
            }

            if (seconds <= 0f)
            {
                Begin(0f);
                return;
            }

            fade = EFade.Out;
            fadeLength = seconds;
            fadeLeft = seconds;
        }

        /// <summary>Says the browser has let audio through. Music that was held starts now.</summary>
        public void Unlocked()
        {
            if (wanted != null && music != null && !music.isPlaying)
                Begin(0.35f);
        }

        // Swaps the clip in and fades it up. The one place the music source is actually started, so it is also
        // the one place the two rules about starting are asked: the channel has to be on, and the browser has
        // to have let audio through.
        private void Begin(float seconds)
        {
            if (music == null)
                return;

            if (wanted == null)
            {
                music.Stop();
                music.clip = null;
                fade = EFade.None;
                return;
            }

            if (!Sounds.On(ESoundChannel.Music) || !Sounds.IsUnlocked)
            {
                // Held. Turning the channel back on, or the first click of the session, comes back through
                // Apply or Unlocked and lands here again.
                music.Stop();
                return;
            }

            music.clip = wanted;
            music.loop = true;
            music.time = 0f;
            music.volume = seconds > 0f ? 0f : Loudness();
            music.Play();

            if (seconds <= 0f)
            {
                fade = EFade.None;
                return;
            }

            fade = EFade.In;
            fadeLength = seconds;
            fadeLeft = seconds;
        }

        // Unscaled time throughout: a fade is a thing the player hears, and a game that paused itself by
        // setting the time scale to zero should not leave the music halfway down for as long as it is paused.
        private void Fade()
        {
            if (fade == EFade.None || music == null)
                return;

            fadeLeft -= Time.unscaledDeltaTime;

            float travelled = fadeLength > 0f ? Mathf.Clamp01(1f - fadeLeft / fadeLength) : 1f;

            if (fade == EFade.Out)
            {
                music.volume = Loudness() * (1f - travelled);

                if (fadeLeft > 0f)
                    return;

                Begin(fadeLength);
                return;
            }

            music.volume = Loudness() * travelled;

            if (fadeLeft <= 0f)
                fade = EFade.None;
        }

        private float Loudness() => wantedVolume * Sounds.Level(ESoundChannel.Music);

        // ------------------------------------------------------------------ the settings

        // Every playing voice, and the music, told what the player's switches now say. Called on the frame
        // after anything changed rather than on every frame: it walks the pool, and the answer only moves when
        // somebody moves it.
        private void Apply()
        {
            for (int i = 0; i < voices.Count; i++)
            {
                var voice = voices[i];

                if (voice.Source != null && voice.Source.isPlaying)
                    voice.Source.volume = voice.Base * Sounds.Level(voice.Channel);
            }

            if (music == null)
                return;

            bool on = Sounds.On(ESoundChannel.Music) && Sounds.IsUnlocked;

            if (!on)
            {
                // Stopped rather than turned down to nothing. A muted bed that carries on playing is a decoded
                // stream and a mixer voice spent on silence, and on WebGL both are worth more than the place
                // in the track that is being kept.
                if (music.isPlaying)
                    music.Stop();

                fade = EFade.None;
                return;
            }

            if (!music.isPlaying && wanted != null)
            {
                Begin(0.35f);
                return;
            }

            // Mid-fade the volume belongs to the fade, which reads the same level itself on the next frame.
            if (fade == EFade.None)
                music.volume = Loudness();
        }

        private void Follow() => dirty = true;

        // The settings arrive over the socket after the scene has loaded rather than with it, and can be
        // changed from somewhere else the same player is signed in. Compared against the events object rather
        // than guarded by a bool, so a scene reload is picked up rather than left listening to the
        // StateManager that has gone.
        private void Listen()
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

            dirty = true;
        }

        private void HandleSettings(Dictionary<string, Newtonsoft.Json.Linq.JToken> data) => Sounds.Changed();

        // ------------------------------------------------------------------ the browser

        // A browser will not start audio until the player has done something. Unity resumes its own audio
        // context on that gesture; what this watches for is the same moment, so music that was held can be
        // started with it rather than a scene later.
        private void Gesture()
        {
            if (Sounds.IsUnlocked)
                return;

#if ENABLE_LEGACY_INPUT_MANAGER
            if (!Input.anyKeyDown && !Input.GetMouseButtonDown(0) && Input.touchCount == 0)
                return;

            Sounds.Unlock();
#else
            // Active Input Handling is on Input System Package (New), which the template does not read - see
            // Ui/Hotkeys/README.md for why it takes no dependency on it. Waiting for a gesture we cannot see
            // would be music that never plays, so the wait is dropped and said once instead.
            if (!warnedInput)
            {
                warnedInput = true;

                Debug.LogWarning(
                    "Sounds: the old Input Manager is switched off, so the first click cannot be seen. Music "
                    + "will start straight away and a browser may mute it - call Sounds.Unlock() from your own "
                    + "first interaction, or set Active Input Handling to Both.");
            }

            Sounds.Unlock();
#endif
        }

#if !ENABLE_LEGACY_INPUT_MANAGER
        private bool warnedInput;
#endif

        // ------------------------------------------------------------------ small change

        private AudioSource Source(string name)
        {
            var host = new GameObject(name);
            host.transform.SetParent(transform, false);

            var source = host.AddComponent<AudioSource>();

            source.playOnAwake = false;
            source.loop = false;

            // Flat 2D: a canvas game has no listener position worth panning against, and a UI click that came
            // out of the left speaker because the button was on the left is not a feature.
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;

            // Kept out of the time scale, so a sound still plays at the right pitch over a paused game.
            source.ignoreListenerPause = true;

            return source;
        }

        // Nothing is audible without a listener anywhere in the scene, and a canvas-only game built from an
        // empty scene is exactly the case that ends up without one. Said once, when a sound is first asked
        // for - which is the moment somebody is wondering why they cannot hear anything.
        private void WarnWithoutListener()
        {
            if (warnedListener)
                return;

            warnedListener = true;

            if (FindFirstObjectByType<AudioListener>(FindObjectsInactive.Include) != null)
                return;

            Debug.LogWarning(
                "Sounds: there is no AudioListener in the scene, so nothing will be heard. Add one to the "
                + "camera - a new camera has one by default.");
        }
    }
}
