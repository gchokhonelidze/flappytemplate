using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlappyTemplate
{
    // Playing a sound, with the player's own switches and volumes already applied.
    //
    //     Sounds.Play(clickClip);                    // a one-shot on the sound channel
    //     Sounds.Play("coin");                       // the same, by the name a SoundBank registered it under
    //     Sounds.PlayMusic(themeClip);               // the one looping bed under everything
    //
    // That is the whole of it. There is no manager to put in the scene, no mixer to author and no AudioSource
    // to wire up: the first call makes one hidden object that survives scene changes and holds a small pool of
    // voices, and every call after that borrows one. A game that plays nothing never gets one.
    //
    // **What the player chose is applied here rather than by the caller.** Four settings back this, and they
    // are the same four the web front keeps for the same player - so a muted game arrives muted:
    //
    // | Setting | What it is | Default |
    // | --- | --- | --- |
    // | `sound` | Effects on or off | on |
    // | `music` | Music on or off | on |
    // | `soundvolume` | Effects level, 0-100 | 100 |
    // | `musicvolume` | Music level, 0-100 | 100 |
    //
    // The first two are the server's own, defaulted there; the volumes are this package's, and a session that
    // has never set one reads as full. Every one of them is stored as a *percentage* rather than a fraction:
    // the server keeps settings as strings, and "70" survives a round trip through anything - the
    // <see cref="Volume(ESoundChannel)"/> API is 0-1 and the conversion happens in one place, here.
    //
    // Changing one writes it into <see cref="MainState.Settings"/> at once, so a switch answers the press
    // rather than the round trip, and sends SETTING so the choice follows the player to their next session and
    // to the web front. A change that arrives from anywhere else - the other tab, a second device - comes back
    // as ON_SETTING and is picked up the same frame.
    //
    // **WebGL.** A browser will not start audio until the player has done something - a click, a key, a touch -
    // and music started before that is not queued, it is played to a muted output and is halfway through by the
    // time the sound comes on. So music asked for before the first gesture is *held* and started when it
    // arrives; <see cref="WaitForGesture"/> turns that off for a build that would rather not. Nothing else in
    // here is WebGL-specific, and deliberately: there is no AudioMixer anywhere in this file, because mixer
    // groups and DSP effects are the part of Unity audio WebGL supports worst. Volume is a multiplier on an
    // AudioSource, which every platform does the same way.
    public static class Sounds
    {
        /// <summary>Whether effects play. The server's own setting, defaulted there to on.</summary>
        public const string SoundSetting = "sound";

        /// <summary>Whether music plays. The server's own setting, defaulted there to on.</summary>
        public const string MusicSetting = "music";

        /// <summary>How loud effects are, 0-100. This package's, and free-form as far as the server is
        /// concerned - it keeps whatever string it is sent.</summary>
        public const string SoundVolumeSetting = "soundvolume";

        /// <summary>How loud music is, 0-100.</summary>
        public const string MusicVolumeSetting = "musicvolume";

        /// <summary>What a volume is stored as: a percentage. See the note above about strings on the
        /// wire.</summary>
        public const float VolumeScale = 100f;

        // What the switches answer with when there is no game to read a setting from - a scene being laid out
        // rather than played in. On, which is what the server defaults both of them to, so a sound tried out
        // in the editor is heard.
        private static bool localSound = true;
        private static bool localMusic = true;
        private static float localSoundVolume = 1f;
        private static float localMusicVolume = 1f;

        private static readonly Dictionary<string, AudioClip> bank =
            new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);

        private static SoundDriver driver;

        /// <summary>A switch or a volume moved, whoever moved it - a control here, or the socket. What a
        /// settings window repaints on.</summary>
        public static event Action OnChanged;

        /// <summary>How many effects may sound at once. Past this the longest-running one is taken back, which
        /// is what a click spammed twenty times a second should do rather than allocate.</summary>
        public static int Voices { get; set; } = 12;

        /// <summary>Hold music until the player has clicked, keyed or touched something. On, and on for a
        /// reason - see the note about browsers above. Off starts it immediately, which is right for a build
        /// that is not going in a browser or one that already knows a gesture has happened.</summary>
        public static bool WaitForGesture { get; set; } = true;

        /// <summary>Whether the browser has let audio start yet. Always true outside WebGL, and outside a
        /// player - the editor has no such rule.</summary>
        public static bool IsUnlocked => unlocked || !WaitForGesture;

#if UNITY_WEBGL && !UNITY_EDITOR
        private static bool unlocked;
#else
        private static bool unlocked = true;
#endif

        /// <summary>Effects on or off. Setting it writes the `sound` setting and emits SETTING.</summary>
        public static bool SoundOn
        {
            get => Read(SoundSetting, ref localSound);
            set => Set(ESoundChannel.Sound, value);
        }

        /// <summary>Music on or off. Setting it writes the `music` setting and emits SETTING. Turning it off
        /// stops what is playing; turning it back on starts it again from the beginning.</summary>
        public static bool MusicOn
        {
            get => Read(MusicSetting, ref localMusic);
            set => Set(ESoundChannel.Music, value);
        }

        /// <summary>How loud effects are, 0-1. Setting it emits SETTING - a slider being dragged wants
        /// <see cref="Preview"/> instead, or it sends one message per frame.</summary>
        public static float SoundVolume
        {
            get => Read(SoundVolumeSetting, ref localSoundVolume);
            set => SetVolume(ESoundChannel.Sound, value);
        }

        /// <summary>How loud music is, 0-1.</summary>
        public static float MusicVolume
        {
            get => Read(MusicVolumeSetting, ref localMusicVolume);
            set => SetVolume(ESoundChannel.Music, value);
        }

        /// <summary>Whether a channel is switched on.</summary>
        public static bool On(ESoundChannel channel) =>
            channel == ESoundChannel.Music ? MusicOn : SoundOn;

        /// <summary>How loud a channel is set to, 0-1, whether or not it is switched on.</summary>
        public static float Volume(ESoundChannel channel) =>
            channel == ESoundChannel.Music ? MusicVolume : SoundVolume;

        /// <summary>What a clip on that channel is actually multiplied by: the volume, or nothing at all while
        /// the channel is off. The one number the driver plays through.</summary>
        public static float Level(ESoundChannel channel) => On(channel) ? Volume(channel) : 0f;

        /// <summary>Switches a channel on or off and remembers the choice.</summary>
        public static void Set(ESoundChannel channel, bool on)
        {
            if (On(channel) == on)
                return;

            if (channel == ESoundChannel.Music)
                localMusic = on;
            else
                localSound = on;

            SettingStore.Set(Key(channel, false), on);
            Changed();
        }

        /// <summary>Flips a channel. What a switch in a settings window does.</summary>
        public static void Toggle(ESoundChannel channel) => Set(channel, !On(channel));

        /// <summary>Sets a channel's volume and remembers it - a write and one SETTING.</summary>
        public static void SetVolume(ESoundChannel channel, float volume)
        {
            Preview(channel, volume);
            Commit(channel);
        }

        /// <summary>Sets a channel's volume without telling the server. What a slider calls while it is being
        /// dragged: the game gets quieter as the finger moves, and the choice is sent once, on
        /// <see cref="Commit"/>, rather than sixty times a second.</summary>
        public static void Preview(ESoundChannel channel, float volume)
        {
            volume = Mathf.Clamp01(volume);

            if (channel == ESoundChannel.Music)
                localMusicVolume = volume;
            else
                localSoundVolume = volume;

            SettingStore.Write(Key(channel, true), Percent(volume));
            Changed();
        }

        /// <summary>Sends a channel's volume as it stands. The other half of <see cref="Preview"/>, and safe
        /// to call when nothing has moved - the server takes the same value twice without complaint.</summary>
        public static void Commit(ESoundChannel channel) =>
            SettingStore.Send(Key(channel, true), Percent(Volume(channel)));

        // ------------------------------------------------------------------ the clips

        /// <summary>Gives a clip a name, so it can be played from anywhere by that name -
        /// <c>Sounds.Play("coin")</c> - rather than by a reference something has to hold. Names are matched
        /// without regard to case. <see cref="SoundBank"/> is the component that fills this in from the
        /// inspector.</summary>
        public static void Register(string name, AudioClip clip)
        {
            if (string.IsNullOrEmpty(name))
                return;

            if (clip == null)
            {
                bank.Remove(name);
                return;
            }

            bank[name] = clip;
        }

        /// <summary>The clip registered under a name, or null.</summary>
        public static AudioClip Clip(string name) =>
            !string.IsNullOrEmpty(name) && bank.TryGetValue(name, out var found) ? found : null;

        /// <summary>Whether anything is registered under that name.</summary>
        public static bool Has(string name) => Clip(name) != null;

        /// <summary>Every name registered, in no particular order.</summary>
        public static IEnumerable<string> Names => bank.Keys;

        /// <summary>Forgets every registered clip. For a game changing to a scene with a bank of its
        /// own.</summary>
        public static void Clear() => bank.Clear();

        // ------------------------------------------------------------------ playing

        /// <summary>Plays a clip once on the sound channel. Null while the channel is off, the clip is
        /// missing, or nothing is playing yet in the editor - so a caller that wants to hold the voice should
        /// check what it is given.</summary>
        /// <param name="clip">What to play.</param>
        /// <param name="volume">How loud, against the player's own level. 1 is that level.</param>
        /// <param name="pitch">Playback speed, which is also the pitch. A little randomness here - 0.95 to
        /// 1.05 - is what keeps a click that fires ten times a second from sounding like a machine.</param>
        public static AudioSource Play(AudioClip clip, float volume = 1f, float pitch = 1f) =>
            Fire(clip, ESoundChannel.Sound, volume, pitch, false);

        /// <summary>The same, for a clip that was registered under a name.</summary>
        public static AudioSource Play(string name, float volume = 1f, float pitch = 1f) =>
            Fire(Named(name), ESoundChannel.Sound, volume, pitch, false);

        /// <summary>Plays a clip on a loop on the sound channel - an engine, a spinning reel - and hands back
        /// the voice it is playing on. Stop it with <see cref="Stop"/>; nothing else will, and a loop nobody
        /// stops holds one of the voices for the rest of the session.</summary>
        public static AudioSource Loop(AudioClip clip, float volume = 1f, float pitch = 1f) =>
            Fire(clip, ESoundChannel.Sound, volume, pitch, true);

        /// <summary>The same, by name.</summary>
        public static AudioSource Loop(string name, float volume = 1f, float pitch = 1f) =>
            Fire(Named(name), ESoundChannel.Sound, volume, pitch, true);

        /// <summary>Stops a voice handed back by <see cref="Play"/> or <see cref="Loop"/> and gives it back to
        /// the pool. Safe on a voice that has already finished, and on null.</summary>
        public static void Stop(AudioSource source)
        {
            if (driver != null)
                driver.Release(source);
        }

        /// <summary>Stops every effect that is playing. Music is left alone - it is the other channel, and a
        /// round ending is not a reason to drop the bed.</summary>
        public static void StopAll()
        {
            if (driver != null)
                driver.ReleaseAll();
        }

        /// <summary>Puts a clip on as the music: one at a time, looped, and faded in over the one that was
        /// playing rather than cut against it. Asking for the clip that is already playing does nothing, so
        /// this is safe to call from a scene's Start without checking.</summary>
        /// <param name="clip">The bed. Null is the same as <see cref="StopMusic"/>.</param>
        /// <param name="volume">How loud, against the player's own music level.</param>
        /// <param name="fade">Seconds to cross over. Zero cuts.</param>
        public static void PlayMusic(AudioClip clip, float volume = 1f, float fade = 0.6f)
        {
            var host = Ensure();
            if (host != null)
                host.Music(clip, volume, fade);
        }

        /// <summary>The same, by name.</summary>
        public static void PlayMusic(string name, float volume = 1f, float fade = 0.6f) =>
            PlayMusic(Named(name), volume, fade);

        /// <summary>Fades the music out and stops it.</summary>
        public static void StopMusic(float fade = 0.6f)
        {
            if (driver != null)
                driver.Music(null, 1f, fade);
        }

        /// <summary>What the music is playing, or null.</summary>
        public static AudioClip Playing => driver != null ? driver.MusicClip : null;

        /// <summary>The source the music plays on, for a game that wants to read its time or hand it to a
        /// spectrum analyser. Null until something has asked for music.</summary>
        public static AudioSource MusicSource => driver != null ? driver.MusicVoice : null;

        /// <summary>Says a gesture has happened, so held music may start. The driver notices a click, a key or
        /// a touch on its own - this is for a game whose first interaction is none of those.</summary>
        public static void Unlock()
        {
            if (unlocked)
                return;

            unlocked = true;

            if (driver != null)
                driver.Unlocked();
        }

        /// <summary>Raises <see cref="OnChanged"/>. Called by the driver when ON_SETTING arrives, so a window
        /// follows a change made in another tab.</summary>
        internal static void Changed() => OnChanged?.Invoke();

        // ------------------------------------------------------------------ the plumbing

        // One hidden object for the whole game, made on the first sound rather than at load. HideAndDontSave
        // keeps it out of the hierarchy and out of the scene file, and DontDestroyOnLoad keeps the music
        // playing across a scene change - which is the whole point of a bed.
        private static SoundDriver Ensure()
        {
            if (driver != null)
                return driver;

            if (!Application.isPlaying)
                return null;

            var host = new GameObject("Sounds") { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.Object.DontDestroyOnLoad(host);
            driver = host.AddComponent<SoundDriver>();

            return driver;
        }

        private static AudioSource Fire(AudioClip clip, ESoundChannel channel, float volume, float pitch, bool loop)
        {
            // Nothing at all while the channel is off, rather than a voice playing silently: a muted game
            // should not be spending its pool on sounds nobody can hear.
            if (clip == null || Level(channel) <= 0f)
                return null;

            var host = Ensure();
            return host != null ? host.Fire(clip, channel, volume, pitch, loop) : null;
        }

        private static AudioClip Named(string name)
        {
            var clip = Clip(name);

            // Said once per name rather than silently doing nothing: a sound that never plays because it was
            // spelled differently in two places is otherwise a very quiet bug.
            if (clip == null && !string.IsNullOrEmpty(name))
                Missing(name);

            return clip;
        }

        private static readonly HashSet<string> complained = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static void Missing(string name)
        {
            if (!complained.Add(name))
                return;

            Debug.LogWarning(
                "Sounds: nothing is registered under \"" + name + "\". Register it with Sounds.Register, or "
                + "drop a Sound Bank component in the scene and name it there - see Audio/README.md.");
        }

        private static string Key(ESoundChannel channel, bool volume)
        {
            if (channel == ESoundChannel.Music)
                return volume ? MusicVolumeSetting : MusicSetting;

            return volume ? SoundVolumeSetting : SoundSetting;
        }

        // The switches: the setting when there is a game to read one from, and the local copy otherwise. A
        // game whose settings have not arrived yet reads the server's own default, which is on for both -
        // silence until the socket answers would be heard as a bug rather than as a setting.
        private static bool Read(string key, ref bool local)
        {
            if (!SettingStore.Available)
                return local;

            return SettingStore.TryFlag(key, out var on) ? on : local;
        }

        private static float Read(string key, ref float local)
        {
            if (!SettingStore.Available)
                return local;

            return SettingStore.TryNumber(key, out var percent) ? Mathf.Clamp01(percent / VolumeScale) : local;
        }

        // Rounded rather than truncated, and to a whole number: a volume is a percentage on the wire, and
        // "69.99999" is not something anybody wants to find in a settings table.
        private static int Percent(float volume) => Mathf.RoundToInt(Mathf.Clamp01(volume) * VolumeScale);

        // Statics outlive a play session when the editor is set to skip the domain reload, so everything is
        // put back by hand rather than left to start the next one holding the last one's driver - which is a
        // destroyed object by then - and the last one's clips.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearAll()
        {
            bank.Clear();
            complained.Clear();

            driver = null;
            OnChanged = null;

            localSound = true;
            localMusic = true;
            localSoundVolume = 1f;
            localMusicVolume = 1f;

            Voices = 12;
            WaitForGesture = true;

#if UNITY_WEBGL && !UNITY_EDITOR
            unlocked = false;
#else
            unlocked = true;
#endif
        }
    }
}
