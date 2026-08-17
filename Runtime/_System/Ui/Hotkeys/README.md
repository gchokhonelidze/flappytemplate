# Hotkeys

[← All documentation](../../../../)

Keys bound to things the game does, and the dialog that tells the player about them. One line binds a key:

```csharp
Hotkeys.Bind(KeyCode.D, "Min", () => bet.Min());
```

That is the whole of it. There is no manager to put in the scene, no asset to author and **nothing to add to
the window** — the [hotkeys window](../Window/) reads this registry, so a key bound anywhere in the game
appears in its list, named, with its cap lit on the drawn keyboard. Bind and the feature is finished.

*Describes package 1.0.81. Update this file with the code — and **README.html** beside it, which is the same
content laid out for a browser, with the keyboard and the caps drawn rather than described.*

The one thing worth knowing before anything else: **hotkeys are off until the player switches them on.** The
server defaults the `keyboard` setting to `0`, so a key bound as above does nothing at all until the button
along the bottom of the window is pressed. That is deliberate — a bet game that halves a stake because
somebody typed into the wrong panel is a bug report — and it is the reason the window has a switch on it
rather than only a list. See [The setting](#the-setting).

## The three ways in

| | |
| --- | --- |
| `Hotkeys.Bind(key, label, action)` | From code. The ordinary case. |
| `Ui Hotkey` component | From the inspector. Drop it on a Button, pick a key, done. |
| `Hotkeys.Bind(key, label, button)` | From code, pressing a button that already exists. |

All three land in the same registry and read the same in the window. Pick whichever puts the binding where
the thing it presses already lives.

### From code

```csharp
Hotkeys.Bind(KeyCode.Q, "Min",               () => bet.Set(bet.Min));
Hotkeys.Bind(KeyCode.W, "Half the amount",   () => bet.Set(bet.Amount / 2));
Hotkeys.Bind(KeyCode.E, "Double the amount", () => bet.Set(bet.Amount * 2));
Hotkeys.Bind(KeyCode.B, "Cash out",          CashOut);
```

The **label is what the window prints** beside the key cap, and it goes through `Translator.Label` — so
`Cash out` is the en_US wording of a key and comes back in the player's language, while a wording nothing
knows is printed as it was typed. See [Translations](#translations).

`Bind` hands back a `Hotkey`. Keep it if the binding comes and goes, and drop it through that:

```csharp
var cashout = Hotkeys.Bind(KeyCode.B, "Cash out", CashOut);
...
cashout.Dispose();                 // or Hotkeys.Unbind(KeyCode.B)
```

Bindings **outlive the scene** — the registry is static and does not reload with it — so anything bound by an
object that goes away has to go with it. `Dispose` is safe to call twice and safe on a binding that was
already replaced, which is what makes `OnDisable` the right place for it.

### From the inspector

**Add Component → UI → Ui Hotkey**, on the button the key should press:

| | |
| --- | --- |
| **Key** | The key. `None` binds nothing, which is what an unfinished slot should do rather than taking a key it was not given. |
| **Label** | What the window prints. Empty falls back to the key's own name, so a slot somebody forgot to caption reads as `D` rather than as a blank row. |
| **Button** | Clicked on the press. Empty finds the Button on this object — the usual case, and nothing to wire. |
| **Press Button** | Off ignores any button and raises only the events. |
| **On Down** / **On Up** / **On Held** | Like any other `UnityEvent`. |

The binding is made when the component is enabled and dropped when it is disabled, so a control that comes and
goes takes its key with it — and the window's list follows, because the list *is* the registry rather than a
copy of it.

### Pressing a button that already exists

```csharp
Hotkeys.Bind(KeyCode.B, "Cash out", cashoutButton);
```

The press does exactly what a click does, and **nothing while the button is hidden or not interactable** —
asked at the moment of the press rather than at the moment of the bind. A button greyed out between rounds is
a key that does nothing between rounds, which is what the player sees on screen and therefore what they
expect. It is also the one form that cannot drift: there is no second copy of what the button does.

## One key does one thing

Binding a key that is already taken **replaces** the first binding rather than joining it. That is the rule
the web front follows, and it is what lets the window print a flat list instead of having to say which of
three things `S` does.

A rebind of the same control — a panel rebuilt, a scene reloaded — is the usual reason to land there and is
quiet. Two *different* labels over one key logs a warning, because two controls quietly fighting over `S` is
not something a player could ever report usefully:

```
Hotkeys: S was bound to "Double the amount" and is now "Sound". One key does one thing - the first binding is gone.
```

## Down, up, held

`Bind` takes up to three callbacks:

```csharp
Hotkeys.Bind(KeyCode.Space, "Charge", StartCharging, ReleaseCharge, WhileCharging);
```

| | |
| --- | --- |
| `down` | Once when the key goes down — **not** again while it is held. |
| `up` | When it comes back up. For a control that charges while held. |
| `held` | Every frame the key is down, the press frame included. |

The edges are worked out here rather than read from the keyboard, which is why there is only one of each per
press however many frames a key is held for.

A binding **taken away mid-press does not deliver its release**. A cash-out on release would cash out, and
nobody meant that by unbinding it.

## Enabled, and unbound

Two different things, and the difference shows in the window:

```csharp
cashout.Enabled = false;    // still in the list, greyed. Not firing.
cashout.Dispose();          // gone from the list.
```

Use `Enabled` for a control that is only available part of the time — a cash-out key during a round. A player
who has learned `B` should see that the key still exists and is simply not doing anything yet, rather than
watching it vanish and wondering whether they have remembered it wrong.

`Label` and `Enabled` are both writable at any time and **repaint whatever is showing the list**, so a control
that renames itself mid-round does not need to rebind to be read.

## The setting

The gate is the `keyboard` setting, the same one the web front uses, and `Hotkeys.Enabled` reads it:

```csharp
Hotkeys.Enabled = true;     // emits SETTING, so the choice is kept
Hotkeys.Toggle();           // what the window's footer button does
```

Setting it does three things: writes the value into `MainState.Settings` at once, so the window answers the
press rather than the round trip; emits `SETTING` through [`Emitter`](../../Socket/), so the choice follows
the player to their next session **and to the web front**; and repaints.

What `Enabled` answers with, and why:

| | |
| --- | --- |
| A `StateManager` in the scene | The `keyboard` setting, and **off** while there is no answer yet — which is the server's own default. |
| No `StateManager` at all | **On.** A scene being built rather than played in has no socket to have sent a setting over, and keys that cannot be tried out in the editor cannot be laid out either. |

The window follows the setting being changed from somewhere else too — the web front in another tab, a second
device — because `ON_SETTING` comes back over the socket like anything else.

## While the player is typing

A press is held while a text field has focus, so somebody typing an amount does not halve it on the way past
the `A`. The `EventSystem`'s selected object is asked, since a field only takes what is typed while it holds
the selection — that is the same question. `TMP_InputField` and UGUI's `InputField` both count.

```csharp
Hotkeys.SuppressWhileTyping = false;   // a game with no text fields anywhere
```

## Which input backend

The registry reads the **old Input Manager**, because the package has no dependency on the Input System
package and adding one to a template would put it in every game that installs this.

Everything is read through one delegate — *is this key held right now* — and the presses, the releases, the
held keys and the down-state the window paints from are all worked out from it. So a game on another input
path replaces that one delegate and gets the rest for free:

```csharp
// Anywhere before the first bind
Hotkeys.Reader = key => Keyboard.current[Convert(key)].isPressed;
```

A gamepad, a row of on-screen buttons on a phone, or a replay driving the game from a recording all fit the
same shape.

With **Active Input Handling** on *Input System Package (New)* and no reader of your own, the old manager
throws rather than returning false — so it is not asked at all, and one warning is logged instead of a key
that quietly never fires. Set it to **Both**, or give `Hotkeys.Reader` a delegate.

## The badge on a button

**Add Component → UI → Ui Hotkey Mark**, in the corner of a control:

```
┌────────────────────┐
│  Cash out      ┌───┤
│                │ B │
│                └───┤
└────────────────────┘
```

It takes the key from a `Ui Hotkey` above it, so on a button that already has one there is nothing to set. It
**hides itself while hotkeys are switched off**, and while nothing is bound to the key — which is the whole
reason it is a component rather than a label somebody typed: a badge saying `D` on a game where `D` does
nothing is worse than no badge, and the setting behind it is the player's to change at any moment. It lights
while the key is held, so a press is answered on the control itself and not only in the window.

It wants an **empty RectTransform** rather than a Panel — it draws its own cap. Thirty units or so square,
anchored to a corner.

## The window

The dialog is [`HotkeysWindow`](../Window/), and it fills itself in from this registry. **GameObject → UI
(Canvas) → FlappyBet → Hotkeys Window**, or:

```csharp
var keys = HotkeysWindow.Create(canvas);
keys.Window.Open();
```

Or leave it to the [navbar](../Navbar/), whose **Hotkeys** button finds one in the scene or builds one.

Three parts, top to bottom: the **drawn keyboard**, with every bound cap in the accent colour and whichever is
held down lit; the **list** of what each key does, which scrolls past `List Max Height` so the button below
stays on screen; and the **switch**. Its caption says which way things stand rather than what pressing it
would do — `Hotkeys off` while they are off — which is how the web front's button reads, so a player who has
used one recognises the other.

A scene with no template running in it shows a few **sample bindings**, so the window can be laid out and
styled from a menu rather than from a running game. They are four rows of text bound to nothing. A real game
never sees them: the moment anything is bound, or a `StateManager` exists, only the real list is shown.

## Bind the window to a key

The joke the feature earns:

```csharp
Hotkeys.Bind(KeyCode.H, "Hotkeys", navbar.ShowHotkeys);
```

## Translations

Labels go through `Translator.Label`, the same as every other caption the package draws: a key is translated,
the en_US wording of a key is translated, and a wording of your own is printed as it was typed. The dialog's
own furniture is `hotkeys.title`, `hotkeys.on`, `hotkeys.off` and `hotkeys.none`.

The **labels are the game's** — the template has no idea what `D` does in your game — so a game with a
translated caption for a control registers it and passes the key:

```csharp
Translator.Add(ELocale.de_DE, "bet.min", "Min");
Hotkeys.Bind(KeyCode.Q, "bet.min", () => bet.Set(bet.Min));
```

See [Translations](../../Translations/) for adding one.

## Key names

Two different names, read in two different places. The **face** is what is printed on the drawn keyboard —
lower case, `backspace`, `caps`, the way the key is actually engraved. The **name** is what a list row and a
badge say: `D`, `1`, `Enter`, `Shift`, `Num 5`, `Page Up`. The face comes from the layout table; the name is
worked out from the `KeyCode`, so it is right for every key there is rather than only for the hundred on the
picture.

The keyboard is one plain table in `HotkeyCaps.Rows` — five rows of caps, each with a width in cap units. A
game that wants a numpad or an arrow cluster on the picture adds rows to it and the window draws them: nothing
counts on there being five rows or on any particular key being in them.

## What it watches

| | |
| --- | --- |
| `Hotkeys.OnChanged` | A binding added, dropped, renamed or enabled — or the gate flipped. What the window rebuilds its rows on. |
| `Hotkeys.OnPressed` | A bound key went down (`true`) or came back up (`false`). What the window repaints the caps on, and a fair place for a game to play a click. |
| `OnSettings` | The setting arrives over the socket after the scene has loaded rather than with it, and can be changed from another tab. |
| `Translator.OnLocaleChanged` | The captions are written through `Translator.Label`. |

The window's **Follow Bindings** off drops the first two; it then shows whatever it was last told.

## In the editor

Nothing is bound outside play mode — `Ui Hotkey` binds on enable, and enable is a play-mode event — so the
window and the badge both draw themselves from what they were given instead:

- **The window shows sample bindings** while there is no `StateManager`, so it can be styled.
- **The badge draws the key it was given**, so it can be placed and coloured. A badge that can never be seen
  cannot be laid out.

A game never reaches either, because a game is playing.

## From code

| | |
| --- | --- |
| `Hotkeys.Bind(key, label, down, up, held)` | Binds a key. Hands back a `Hotkey`. |
| `Hotkeys.Bind(key, label, button)` | Binds a key to a button that already exists. |
| `Hotkeys.Unbind(key)` / `Unbind(hotkey)` | Drops one. False if there was nothing to drop. |
| `Hotkeys.UnbindAll()` | Drops the lot. For a game changing modes. |
| `Hotkeys.Find(key)` | What is bound to a key, or null. |
| `Hotkeys.IsBound(key)` / `IsDown(key)` | |
| `Hotkeys.Bindings` / `Count` | Every binding, in the order they were bound. |
| `Hotkeys.Enabled` / `Toggle()` | The `keyboard` setting. Setting it emits `SETTING`. |
| `Hotkeys.Reader` | How a key's held state is read. Replace for another input path. |
| `Hotkeys.SuppressWhileTyping` | |
| `Hotkeys.OnChanged` / `OnPressed` | |
| `Hotkey.Label` / `Enabled` | Writable, and repaint the list. |
| `Hotkey.Key` / `IsDown` / `IsBound` | |
| `Hotkey.Dispose()` | Drops it. Safe twice. |
| `HotkeysWindow.Create(parent, name, title)` | Builds the dialog under a parent. |
| `HotkeysWindow.Toggle()` | What the footer button does. |
| `UiHotkey.Key` / `Label` / `Button` / `Binding` | Setting the first three rebinds. |
| `UiHotkey.Rebind()` / `Drop()` | |
| `UiHotkeyMark.Key` / `Refresh()` | |

## Worth knowing

- **Bindings outlive the scene.** The registry is static and its driver survives a scene change, so anything
  bound by an object that goes away has to be dropped by that object. `UiHotkey` does it for you.
- **Nothing polls a key nothing is bound to.** The frame loop runs over the bindings, not over the keyboard,
  so an unbound game pays for one empty `Update`.
- **The driver is made on the first bind**, on a hidden object that is not in the scene file and not in the
  hierarchy. A game that binds nothing never gets one.
- **A press may bind, unbind, or open a window.** The list is copied before the callbacks run, and a binding
  dropped by an earlier press in the same frame does not fire.
- **The keyboard picture catches no clicks.** It is a picture of a keyboard, not a keyboard — a cap that took
  a press would look like a button that does nothing.
- **Statics are cleared at subsystem registration**, so a play session started with the editor set to skip the
  domain reload does not begin holding the last one's bindings.

## Files

| | |
| --- | --- |
| `Hotkeys.cs` | The registry: binding, the frame loop, and the setting. |
| `Hotkey.cs` | One binding, and the handle that drops it. |
| `HotkeyCaps.cs` | What a key is called, and the shape of the drawn keyboard. |
| `HotkeyInput.cs` | Whether a key is held, read from the old Input Manager. |
| `HotkeyDriver.cs` | The hidden object that gives the registry a frame. |
| `UiHotkey.cs` | Binding a key from the inspector. |
| `UiHotkeyMark.cs` | The badge in the corner of a button. |
| `UiHotkeysExample.cs` | Eight bindings covering every form. Read as much as run. |
| `../Window/Hotkeys/HotkeysWindow.cs` | The dialog. |
| `../Window/Hotkeys/HotkeysWindowStyle.cs` | What it looks like. |
| `../Window/Hotkeys/HotkeyKeyboard.cs` | The drawn keyboard inside it. |
| `../Navbar/UiNavbar.cs` | The bar's **Hotkeys** button. |
