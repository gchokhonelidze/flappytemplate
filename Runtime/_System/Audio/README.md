# Sound

[← All documentation](../../../)

Playing a sound, with the player's own switches and volumes already applied. One line plays one:

```csharp
Sounds.Play(clickClip);
Sounds.Play("coin");                  // the same, by a name a Sound Bank registered
Sounds.PlayMusic(themeClip);          // the one looping bed under everything
```

That is the whole of it. There is no manager to put in the scene, no mixer to author and no `AudioSource` to
wire up: the first call makes one hidden object that survives scene changes and holds a small pool of voices,
and every call after that borrows one. A game that plays nothing never gets one.

*Describes package 1.0.82. Update this file with the code — and **README.html** beside it, which is the same
content laid out for a browser, with the dialog and its switches drawn rather than described.*

The one thing worth knowing before anything else: **nothing here asks whether sound is on.** The player's four
settings are applied inside `Play`, so a muted channel plays nothing and a channel at 40% plays at 40% — the
caller never checks, and there is no second copy of the rule to fall out of step. The
[sound window](../Ui/Window/) is what moves those settings, and the socket is what remembers them.

## The four settings

| Setting | What it is | Default | Whose |
| --- | --- | --- | --- |
| `sound` | Effects on or off | on | The server's, defaulted there |
| `music` | Music on or off | on | The server's, defaulted there |
| `soundvolume` | Effects level, `0`–`100` | 100 | This package's |
| `musicvolume` | Music level, `0`–`100` | 100 | This package's |

The first two are the same two the web front keeps for the same player, so **a player who muted the music
there arrives here with it muted**. The volumes are new and free-form as far as the server is concerned — it
keeps a session's settings as a flat table of strings and takes whatever it is sent.

Volumes are stored as a **percentage**, not a fraction, for that reason: `"70"` survives a round trip through a
string table, a JSON number and a web front reading it back, while `"0.7"` is one comma-decimal locale away
from being seven. The API is `0`–`1` throughout and the conversion happens in one place, in `Sounds`.

Changing one writes it into `MainState.Settings` **at once** — so a switch answers the press rather than the
round trip — and emits `SETTING`, so the choice follows the player to their next session and to the web front.
A change made anywhere else the same player is signed in arrives back as `ON_SETTING` and is picked up the same
frame.

## Quick start

**Add Component → Audio → Sound Bank**, drop your clips in the list, and play them by name from anywhere:

```csharp
Sounds.Play("coin");
```

A button can play one **without a line of script**: put the bank's `Play` in the button's On Click and type the
name. The dialog the player changes it all from is **GameObject → UI (Canvas) → FlappyBet → Sound Window**, or
the [navbar](../Ui/Navbar/)'s Sound button, which finds one in the scene or builds it.

## Playing

```csharp
Sounds.Play(clip);                          // once, on the sound channel
Sounds.Play(clip, 0.6f);                    // quieter, against the player's own level
Sounds.Play(clip, 1f, Random.Range(.95f, 1.05f));   // with a little pitch spread

var engine = Sounds.Loop(engineClip, 0.5f); // until something stops it
Sounds.Stop(engine);
```

`Play` hands back the voice it borrowed, or **null** — the channel is off, the clip is missing, or the game is
not running. A caller that wants to hold on to a voice has to check; a caller that just wanted a click can
ignore it.

Effects are played on real `AudioSource`s from a pool rather than as one-shots, and that is what makes muting
instant: a one-shot cannot be stopped, cannot be looped and cannot have its volume moved once it is away, so a
player muting the game in the middle of a three-second sound would go on hearing it. `Sounds.Voices` is how
many may sound at once — past that the longest-running one is taken back, which is the right answer for a click
spammed twenty times a second.

**A loop nobody stops holds a voice for the rest of the session.** That is the one way to leak anything here.

## Music

```csharp
Sounds.PlayMusic(theme);              // faded in over whatever was playing
Sounds.PlayMusic(theme, 0.8f, 1.2f);  // quieter, over a longer cross
Sounds.StopMusic();
```

One clip at a time, looped, and **faded rather than cut** when it changes. Asking for the clip that is already
playing does nothing but take the new volume, so this is safe to call from a scene's `Start` without checking —
and the bed carries on across a scene change, because the object it plays on does.

Turning the music setting off **stops** the source rather than turning it down to nothing: a muted bed that
carries on playing is a decoded stream and a voice spent on silence, and on WebGL both cost more than the place
in the track that is being kept. Turning it back on starts the clip again from the beginning.

## WebGL

A browser will not start audio until the player has done something — a click, a key, a touch. Music asked for
before that is **not queued by the browser**; it is played to a muted output and is thirty seconds in by the
time the sound arrives. So music asked for early is held here instead, and started on the first gesture:

```csharp
Sounds.WaitForGesture = false;   // start it immediately anyway
Sounds.Unlock();                 // or: say a gesture has happened, from your own first interaction
```

The gesture is spotted through the old Input Manager, the same backend [Hotkeys](../Ui/Hotkeys/) reads. With
Active Input Handling on **Input System Package (New)** there is nothing to read, so the wait is dropped and a
warning is logged once — a game on that backend should call `Sounds.Unlock()` from its own first interaction.

**There is no `AudioMixer` anywhere in this component**, and that is deliberate: mixer groups and DSP effects
are the part of Unity audio that WebGL supports worst. Volume here is a multiplier on an `AudioSource`, which
every platform does the same way.

Two more things worth setting on the clips themselves, in the import settings rather than here:

| | |
| --- | --- |
| **Load Type** | *Decompress On Load* for short effects — a compressed clip decoded on the first press is a click that arrives late. |
| **Preload Audio Data** | On for anything that has to be instant. WebGL has no streaming worth the name. |

## Sound Bank

The component that fills the registry from the inspector, and the shortest way into sound with no code at all.

| | |
| --- | --- |
| **Clips** | Name and clip per row. An empty name uses the clip's own asset name. |
| **Volume**, **Pitch** | What this bank's own `Play` uses. The pitch is picked between the two numbers on every press. |
| **Music**, **Play Music** | The bed to put on when the object wakes, and whether to. |
| **Clear On Disable** | Off — a bank in the first scene covers the whole game. On takes its names away with it. |

Names are matched **without regard to case**, and the registry is static: it outlives the scene, so one bank at
the start of a game covers all of it, and a bank in a scene loaded later adds to it rather than replacing it.

A name nothing is registered under is logged **once** rather than silently doing nothing — a sound that never
plays because it was spelled two ways is otherwise a very quiet bug.

## The window

`Sound Window` is the dialog: one card per channel, a switch on each and a slider under it. It reads and writes
`Sounds`, which is the same thing the game plays through, so a slider moved there is heard on the next click.
It is documented with the other dialogs in [Window](../Ui/Window/).

**A slider sends once, not per frame.** Dragging one moves the volume immediately — that is the point of a
volume slider — but the `SETTING` that keeps the choice is held until the drag has been still for `Send Delay`,
and flushed when the window closes. Sixty messages a second up a socket for one finger movement is the thing
that exists to avoid.

## In the editor

With no template running there are no settings to read, so both channels are **on** and both volumes **full** —
the same defaults the server would send. A sound tried out in the editor is heard, and the dialog can be laid
out and styled from a menu rather than from a running game.

Nothing is audible without an `AudioListener` in the scene, which a canvas-only game built from an empty scene
can easily be missing. The first sound checks and says so once.

## From code

| | |
| --- | --- |
| `Sounds.Play(clip, volume, pitch)` | One-shot on the sound channel. Hands back the voice, or null. |
| `Sounds.Play(name, volume, pitch)` | The same, by registered name. |
| `Sounds.Loop(clip, volume, pitch)` | Loops until stopped. |
| `Sounds.Stop(source)` | Stops one voice and frees it. Safe on null. |
| `Sounds.StopAll()` | Every effect. Music is left alone. |
| `Sounds.PlayMusic(clip, volume, fade)` | The bed, faded in. Null clip is the same as stopping. |
| `Sounds.StopMusic(fade)` | Fades it out. |
| `Sounds.Playing`, `Sounds.MusicSource` | What is playing, and what it is playing on. |
| `Sounds.Register(name, clip)` | Names a clip. `Clip(name)`, `Has(name)`, `Names`, `Clear()` beside it. |
| `Sounds.SoundOn`, `Sounds.MusicOn` | The switches. Setting one emits `SETTING`. |
| `Sounds.SoundVolume`, `Sounds.MusicVolume` | `0`–`1`. Setting one emits `SETTING`. |
| `Sounds.On(channel)`, `Volume(channel)`, `Level(channel)` | The same by channel. `Level` is the volume, or nothing while the channel is off. |
| `Sounds.Set(channel, on)`, `Toggle(channel)` | Switch a channel. |
| `Sounds.Preview(channel, volume)` | Applies a volume **without** sending it. What a slider being dragged calls. |
| `Sounds.Commit(channel)` | Sends whatever the volume now is. The other half of `Preview`. |
| `Sounds.Voices` | How many effects may sound at once. 12. |
| `Sounds.WaitForGesture`, `IsUnlocked`, `Unlock()` | The browser's rule about starting audio. |
| `Sounds.OnChanged` | A switch or a volume moved, whoever moved it. What a settings window repaints on. |

## Worth knowing

- **`Play` returns null when the channel is off.** Not a failure — the player is allowed to have muted the
  game — but code that keeps the voice has to expect it.
- **Volumes are percentages on the wire and fractions in the API.** A setting written by hand as `0.7` reads as
  0.7% here, not 70%.
- **`Preview` does not send.** A slider that only ever calls `Preview` moves the volume for this session and is
  forgotten by the next one. Pair it with `Commit`.
- **Music restarts rather than resumes** when the channel is switched off and on again. The stop is deliberate;
  see [Music](#music).
- **The driver is hidden.** `HideAndDontSave` keeps it out of the hierarchy and out of the scene file, so there
  is nothing to find in the Inspector and nothing to accidentally save into a scene.
- **Settings arrive after the scene loads.** Anything reading `Sounds.SoundOn` in `Awake` is reading the
  default, not the player's choice. Listen to `Sounds.OnChanged` rather than asking once.

## Files

| | |
| --- | --- |
| `Sounds.cs` | The whole public surface: the four settings, the clip registry and every way to play something. |
| `SoundDriver.cs` | The hidden object it plays through: the voice pool, the music source, the fades, and the browser's gesture. |
| `SoundBank.cs` | The component that names clips from the inspector and starts the bed. |
| `ESoundChannel.cs` | Effects or music — the two things the player has switches for. |
| `SoundsExample.cs` | A worked scene: a bank, a bed, keys that play things and the dialog that mutes them. |
| `../States/SettingStore.cs` | How a setting is read and written. Shared with [Hotkeys](../Ui/Hotkeys/). |
| `../Ui/Window/Sound/SoundWindow.cs` | The dialog. Documented in [Window](../Ui/Window/). |
