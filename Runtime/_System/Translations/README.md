# Translations

[← All documentation](../../../README.md)

Every word the player reads, in the language `MainState.Locale` names. One call:

```csharp
label.text = Translator.T("bet_info.payout");
```

There is no file to load, no addressable to wait on and no initialisation step. The strings are dictionaries
compiled into the package; a lookup is two dictionary hits, so calling `T` from `Update` is not a thing to
worry about.

The package's own windows — **Statistics**, **Bet info**, **Fairness** and any `UiWindow` title — already go
through this, so they follow the locale with nothing wired up. See [Strings from a scene](#scene) for how, and
why an existing scene translates without being opened.

*Describes package 1.0.68. Update this file with the code — and **README.html** beside it, which is the same
content laid out for a browser.*

---

## Quick start

1. Put **Ui Translated Text** on a TextMeshPro or a uGUI Text and type a key — `statistics.luck`.
2. Change `StateManager.Inst.MainState.Locale`, from code or from the inspector while the game runs.
3. The label rewrites itself. So does every other one.

From code, without the component:

```csharp
title.text  = Translator.T("fairness.title");
Translator.Locale = ELocale.de_DE;                  // writes MainState.Locale
Translator.OnLocaleChanged += Redraw;               // your turn to redraw
```

---

## What happens when a string is missing

This is the whole of it, and it is worth reading once:

| The key is in… | You get |
|---|---|
| The current locale | That text |
| Only `en_US` | The **en_US** text — a language that is not finished still reads |
| Neither, in either table | **`untranslated`** |

A key is never shown. `bet_info.payout` on screen is a bug wearing a label's clothes, and a player cannot tell
it from a feature. While a game is being built, name the holes instead:

```csharp
Translator.MissingFormat = "untranslated: {0}";
```

`{0}` is the key. Without it the format is used as it stands, so the default prints the bare word.

---

## The two tables

A lookup asks two tables, in this order, for the current locale — and then asks both again for `en_US`:

1. **What the game added** — `Translator.Add(…)`
2. **What the package ships** — [`Translations.cs`](Translations.cs)

So a game may override a package string, add keys of its own, or fill in a locale the package left out, and
never edit the package to do it — an update walks over `Translations.cs`, and would walk over anything you had
written into it.

```csharp
Translator.Add(ELocale.de_DE, new Dictionary<string, string>
{
    ["shop.title"]     = "Laden",
    ["bet_info.payout"] = "Gewinn",     // overrides the package, in de_DE only
});
```

| Call | Does |
|---|---|
| `Add(locale, key, text)` | One string |
| `Add(locale, entries)` | A whole language at once — the usual way |
| `Remove(locale, key)` | Drops yours; the package's text comes back |
| `Clear()` | Drops everything you added, in every locale |

Adding redraws every listening label, so strings registered after a scene has loaded still land.

---

## Strings from a scene

`T` is for code that knows it is holding a key. A caption typed into an inspector, a `StatisticsRow` title, a
window's `Title` — none of those are known to be keys, and half of them are English text a scene saved months
ago. `Label` is for those:

```csharp
titleText.text = Translator.Label(title);
```

| The string is… | You get |
|---|---|
| A key either table knows — `bet_info.payout` | The translation |
| A **known en_US text** — `"Payout"` | The translation of the key that holds it |
| Neither — `"Winnings"` | The string, exactly as it came in |

The middle row is the one that matters: it is why the package's windows translate without a single scene or
prefab being touched, and why a wording of your own is left alone rather than being replaced or shown as
`untranslated`. Nothing is ever printed as a bare key by `Label` — the worst case is the string you gave it.

The reverse index it reads is built the first time it is needed and dropped whenever strings are added, so a
game that registers its own `en_US` text gets that matched too. Two keys that happen to share an English
wording resolve to whichever was written first; a game's own `en_US` string wins over the package's.

> Use `T` where you control the key and want a missing one to be loud. Use `Label` for anything that came out
> of a scene, a prefab or a game's own data.

---

## Keys

Lower case, `area.thing`. The package ships these:

| Area | Keys |
|---|---|
| `common` | `na`, `hidden`, `nonce`, `client_seed`, `server_seed`, `block_hash`, `server_sha512` |
| `bet_info` | `title`, `profit`, `bet`, `payout`, `player`, `bet_id`, `game`, `time`, `details`, `verify` |
| `fairness` | `title`, `new_client_seed`, `randomize`, `current_pair`, `previous_pair`, `server_sha512`, `bets_made` |
| `statistics` | `title`, `current`, `overall`, `total_wagered`, `counts`, `revenue`, `total_profit`, `luck` |

A word that reads the same in two windows is one key under `common`, so a better wording for it is a one-line
change rather than a hunt.

### Adding one to the package

`Translations.cs` is a list of `Row` calls, one per key, **one named argument per language**:

```csharp
Row("shop.buy",
    en_US: "Buy", ru_RU: "Купить", fr_FR: "Acheter", bn_BD: "কিনুন",
    …
    ka_GE: "ყიდვა", hy_AM: "Գնել");
```

Every argument is required, so a key added without a Georgian string **does not compile**. That is the rule the
file exists to enforce: a translation is added for every locale at once, or it is not added. They are named
rather than positional because a row is 22 strings long, and a language shifted by one is a bug nobody would
see until a player did.

---

## Numbers and names in a string

Put the placeholders in the translation, so a language that wants them in the other order can have them there:

```csharp
// "won.line" is "You won {0} on {1}" in en_US
label.text = Translator.Format("won.line", amount, game);
```

A translation with a stray brace in it prints as it stands rather than throwing under a label being drawn.

---

## How a label knows

`MainState.Locale` is a plain field: nothing fires when it is written, whether by the inspector, a language
menu or a server message. So one hidden object — `LocaleWatcher`, made for you when the game starts — reads it
once a frame and raises `Translator.OnLocaleChanged` when it has moved.

One `Update` for the whole system, however many labels are listening: a `UiTranslatedText` does nothing until
that event is raised.

| Member | Means |
|---|---|
| `Translator.Locale` | Get and set `MainState.Locale`. Setting it raises the event at once rather than next frame |
| `Translator.OnLocaleChanged` | Redraw whatever you drew by hand |
| `Translator.Refresh()` | Raise it without the locale having changed — after adding strings, or changing `MissingFormat` |

### Ui Translated Text

| Field | Means |
|---|---|
| `Key` | What to look up |
| `Prefix`, `Suffix` | Written around the translation, and never translated themselves |
| `Label` | Which label to write into. Empty: this object's, else the first one inside it |

Nothing else on the label is touched — font, size, colour and alignment stay the label's own.

---

## Before shipping a language

```csharp
Debug.Log(string.Join(", ", Translator.Untranslated(ELocale.th_TH)));
```

The keys that have nothing of their own in that locale — the ones being read out of `en_US` instead. Empty
means the language is whole. `Translator.Keys` lists every key both tables know.

---

## Locales

The 22 `ELocale` names, all of them filled in for every key the package ships:

`en_US` `ru_RU` `fr_FR` `bn_BD` `de_DE` `es_ES` `id_ID` `pt_PT` `tr_TR` `vi_VN` `ar_AE` `hi_IN` `th_TH` `ja_JP`
`ko_KR` `zh_CN` `fil_PH` `ur_PK` `fa_IR` `ms_MY` `ka_GE` `hy_AM`

> `ar_AE`, `ur_PK` and `fa_IR` are read right to left, and `bn_BD`, `hi_IN`, `th_TH`, `ka_GE`, `hy_AM` and the
> CJK languages are outside a Latin font's character set. The translator hands back the text; making it legible
> is the font asset's job — a TMP font with those ranges in it, and `isRightToLeftText` on for the three above.

---

## Example

`TranslationExample` builds a column of labels — some of the package's keys, one it registers itself, and one
nobody has written so the hole can be seen. **Next Locale** from its context menu cycles the language;
**Log Untranslated** prints what the current one is missing.
