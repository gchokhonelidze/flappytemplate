#nullable enable

using System;
using System.Collections.Generic;

namespace FlappyTemplate
{
    // Every word the player reads goes through here.
    //
    //     label.text = Translator.T("bet_info.payout");
    //
    // gives back the text for whatever MainState.Locale currently says. What it does when there is no such text
    // is the whole point of the class:
    //
    //  - The locale has nothing for the key, but en_US does - the en_US text is used. A game shipped before a
    //    locale was finished still reads, in English, rather than showing a hole.
    //  - Neither has it - the word "untranslated" is shown. Not the key: a key is a name for a programmer, and
    //    a player who sees bet_info.payout has been shown a bug dressed as a label. MissingFormat changes this,
    //    and a game being built should set it to "untranslated: {0}" so the key is there to be found.
    //
    // Two tables are searched, in this order, for the locale and then again for en_US:
    //
    //   1. What the game added - Translator.Add(ELocale.de_DE, "shop.title", "Laden")
    //   2. What the package ships - Translations, beside this file
    //
    // so a game may override a package string, add keys of its own, or fill in a locale the package left out,
    // and never edit the package to do it. Nothing is loaded from disk and nothing is parsed: the tables are
    // dictionaries, a lookup is two dictionary hits, and calling T every frame is not a thing to worry about.
    //
    // The locale itself is MainState.Locale and nothing else - this class does not keep a copy. Changing it,
    // from here or from the inspector or from a server message, raises OnLocaleChanged within a frame, which is
    // what UiTranslatedText listens to.
    public static class Translator
    {
        /// <summary>Read whenever the current locale has no text for a key.</summary>
        public const ELocale FallbackLocale = ELocale.en_US;

        /// <summary>Shown in place of a key that neither table knows, in any locale. A <c>{0}</c> anywhere in it
        /// is replaced with the key - <c>Translator.MissingFormat = "untranslated: {0}"</c> while a game is being
        /// built, so a missing string says which one it was.</summary>
        public static string MissingFormat = "untranslated";

        /// <summary>Raised after <see cref="Locale"/> changes, whoever changed it, and by <see cref="Refresh"/>.
        /// Labels redraw from this.</summary>
        public static event Action? OnLocaleChanged;

        // What the game added. Keyed by locale first, because that is the order a lookup asks in.
        private static readonly Dictionary<ELocale, Dictionary<string, string>> added = new();

        // en_US text back to the key that holds it. Null until Label needs it. See English().
        private static Dictionary<string, string>? byEnglish;

        // Where the locale lives while there is no StateManager in the scene - an editor test, a unit test, a
        // tool. With one, MainState is the only copy and this is never read.
        private static ELocale detached = FallbackLocale;

        // The locale OnLocaleChanged was last raised for. LocaleWatcher compares against it once a frame.
        private static ELocale raisedFor = FallbackLocale;

        /// <summary>The locale being read, which is <see cref="MainState.Locale"/> whenever there is a
        /// <see cref="StateManager"/> to hold it. Setting it writes there and raises
        /// <see cref="OnLocaleChanged"/>.</summary>
        public static ELocale Locale
        {
            get
            {
                var state = State;
                return state != null ? state.Locale : detached;
            }
            set
            {
                var state = State;
                if (state != null)
                    state.Locale = value;

                detached = value;
                Poll();
            }
        }

        /// <summary>The text for a key in the current locale.</summary>
        public static string T(string key) => T(Locale, key);

        /// <summary>The text for a key in a locale of your choosing - a name shown next to a flag, say, rather
        /// than in the language the game is being played in.</summary>
        public static string T(ELocale locale, string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            return TryGet(locale, key, out var text) ? text : Missing(key);
        }

        /// <summary>Translates a string that may or may not be a key - a caption typed into the inspector, a
        /// statistics row's title, anything that came out of a scene or out of a game's own data.
        ///
        /// A key either table knows is translated. Failing that the string is matched against the
        /// <see cref="FallbackLocale"/> texts, so a caption a scene saved as "Profit" translates too, without
        /// that scene having to be opened and retyped. Anything neither finds is handed back exactly as it came
        /// in - which is what lets a wording of your own survive.
        ///
        /// This is what the package's own windows call, and why they translate without their inspector fields
        /// changing meaning. Code that knows it is holding a key should call <see cref="T(string)"/>: a key
        /// that has gone missing shows as untranslated there, rather than being printed raw.</summary>
        public static string Label(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            if (TryGet(Locale, value, out var byKey))
                return byKey;

            return English().TryGetValue(value, out var key) && TryGet(Locale, key, out var byText)
                ? byText
                : value;
        }

        /// <summary>The text for a key with <c>{0}</c>, <c>{1}</c> … filled in. The translation carries the
        /// placeholders, so a language that needs the numbers in the other order can have them there.</summary>
        public static string Format(string key, params object[] args)
        {
            var text = T(key);
            if (args == null || args.Length == 0)
                return text;

            try
            {
                return string.Format(text, args);
            }
            catch (FormatException)
            {
                // A translation with a stray brace in it. Better a plain string on screen than an exception out
                // of a label being drawn.
                return text;
            }
        }

        /// <summary>Whether the key has text in the current locale or in en_US - that is, whether
        /// <see cref="T(string)"/> would return something a player can read.</summary>
        public static bool Has(string key) => TryGet(Locale, key, out _);

        /// <summary>The text for a key in the current locale, falling back to en_US. False means neither had
        /// it, and nothing was written to <paramref name="text"/> worth showing.</summary>
        public static bool TryGet(string key, out string text) => TryGet(Locale, key, out text);

        /// <summary>The text for a key in a given locale, falling back to en_US.</summary>
        public static bool TryGet(ELocale locale, string key, out string text)
        {
            if (!string.IsNullOrEmpty(key))
            {
                if (Find(locale, key, out text))
                    return true;

                if (locale != FallbackLocale && Find(FallbackLocale, key, out text))
                    return true;
            }

            text = string.Empty;
            return false;
        }

        /// <summary>Adds or replaces one string. A key the package already ships is overridden by this, in that
        /// locale only.</summary>
        public static void Add(ELocale locale, string key, string text)
        {
            if (string.IsNullOrEmpty(key))
                return;

            if (!added.TryGetValue(locale, out var table))
                added[locale] = table = new Dictionary<string, string>();

            table[key] = text ?? string.Empty;
            Refresh();
        }

        /// <summary>Adds a whole locale at once - the usual way a game registers its own strings, one call per
        /// language from a static dictionary.</summary>
        public static void Add(ELocale locale, IEnumerable<KeyValuePair<string, string>> entries)
        {
            if (entries == null)
                return;

            if (!added.TryGetValue(locale, out var table))
                added[locale] = table = new Dictionary<string, string>();

            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.Key))
                    table[entry.Key] = entry.Value ?? string.Empty;
            }

            Refresh();
        }

        /// <summary>Drops one string the game added. What the package ships is untouched, so the key goes back
        /// to the package's text rather than going missing.</summary>
        public static void Remove(ELocale locale, string key)
        {
            if (added.TryGetValue(locale, out var table) && table.Remove(key))
                Refresh();
        }

        /// <summary>Drops everything the game added, in every locale. The package's own strings stay.</summary>
        public static void Clear()
        {
            if (added.Count == 0)
                return;

            added.Clear();
            Refresh();
        }

        /// <summary>Every key either table knows, in no particular order. For a tool that lists them, and for
        /// <see cref="Untranslated"/>.</summary>
        public static IEnumerable<string> Keys
        {
            get
            {
                var seen = new HashSet<string>();

                foreach (var key in Translations.Keys)
                {
                    if (seen.Add(key))
                        yield return key;
                }

                foreach (var table in added.Values)
                {
                    foreach (var key in table.Keys)
                    {
                        if (seen.Add(key))
                            yield return key;
                    }
                }
            }
        }

        /// <summary>The keys that have no text of their own in a locale - the ones falling through to en_US, or
        /// missing altogether. What to print before shipping a language.</summary>
        public static List<string> Untranslated(ELocale locale)
        {
            var missing = new List<string>();

            foreach (var key in Keys)
            {
                if (!Find(locale, key, out _))
                    missing.Add(key);
            }

            missing.Sort(StringComparer.Ordinal);
            return missing;
        }

        /// <summary>Redraws every listening label without the locale having changed - after strings have been
        /// added, or after <see cref="MissingFormat"/> has.</summary>
        public static void Refresh()
        {
            // Every method that writes to the added table comes through here, so this is where the en_US index
            // Label reads is dropped. Rebuilt the next time one is asked for, and not before.
            byEnglish = null;

            raisedFor = Locale;
            OnLocaleChanged?.Invoke();
        }

        // Once a frame, from LocaleWatcher, and straight after the Locale setter. Whoever wrote to
        // MainState.Locale - the inspector, a server message, a menu - is found out here.
        internal static void Poll()
        {
            var now = Locale;
            if (now == raisedFor)
                return;

            raisedFor = now;
            OnLocaleChanged?.Invoke();
        }

        // The added table first, then the package's. No fallback here: the caller does that, so it can say
        // which of the two locales an answer came from.
        private static bool Find(ELocale locale, string key, out string text)
        {
            if (added.TryGetValue(locale, out var table) && table.TryGetValue(key, out var mine))
            {
                text = mine ?? string.Empty;
                return true;
            }

            return Translations.Find(locale, key, out text);
        }

        // en_US text back to the key that holds it, for Label. Built on demand and thrown away whenever the
        // added table moves, since a game may register an en_US string of its own at any point.
        //
        // The package's keys go in first and the earliest wins, so two keys that happen to share an English
        // wording resolve to the one written first rather than to whichever was enumerated last. A game's own
        // en_US strings go in afterwards and do win, since they are the more specific answer.
        private static Dictionary<string, string> English()
        {
            if (byEnglish != null)
                return byEnglish;

            var map = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var key in Translations.Keys)
            {
                if (Translations.Find(FallbackLocale, key, out var text) && !string.IsNullOrEmpty(text)
                    && !map.ContainsKey(text))
                    map[text] = key;
            }

            if (added.TryGetValue(FallbackLocale, out var mine))
            {
                foreach (var entry in mine)
                {
                    if (!string.IsNullOrEmpty(entry.Value))
                        map[entry.Value] = entry.Key;
                }
            }

            byEnglish = map;
            return map;
        }

        private static string Missing(string key)
        {
            var format = MissingFormat;
            if (string.IsNullOrEmpty(format))
                return string.Empty;

            return format.Contains("{0}") ? string.Format(format, key) : format;
        }

        private static MainState? State
        {
            get
            {
                var manager = StateManager.Inst;
                return manager != null ? manager.MainState : null;
            }
        }
    }
}
