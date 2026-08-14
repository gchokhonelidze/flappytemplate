using System.Collections.Generic;
using UnityEngine;

namespace FlappyTemplate
{
    // Everything the translation system does, on one object. Drop it on an empty RectTransform inside a canvas
    // and press play, or use Build Now from the component's context menu to see it without leaving the editor.
    //
    // It builds a column of labels: a few keys the package ships, a key this example registers itself, and a
    // key nobody has written - which is the interesting one, because that is the label that says
    // "untranslated". Cycle Locale from the context menu and watch the column change language without anything
    // being rebuilt.
    //
    // The registration below is the part worth copying into a game: one dictionary per language, handed to
    // Translator.Add once, before anything is drawn.
    [AddComponentMenu("UI/Translation Example")]
    [RequireComponent(typeof(RectTransform))]
    public class TranslationExample : MonoBehaviour
    {
        [Tooltip("Keys to show. The first four are the package's; game.play is this example's own; game.missing is nobody's, and shows what a hole looks like.")]
        [SerializeField]
        private List<string> keys = new List<string>
        {
            "bet_info.payout",
            "fairness.randomize",
            "statistics.luck",
            "common.na",
            "game.play",
            "game.missing"
        };

        [Tooltip("Height of one line, and the gap between them.")]
        [SerializeField]
        private float lineHeight = 34f;

        [Tooltip("Print the key next to the word \"untranslated\", so a hole says which key it was. What a game should turn on while it is being built.")]
        [SerializeField]
        private bool nameMissingKeys = true;

        void Start()
        {
            Build();
        }

        [ContextMenu("Build Now")]
        public void Build()
        {
            Clear();
            Register();

            if (nameMissingKeys)
                Translator.MissingFormat = "untranslated: {0}";

            for (int i = 0; i < keys.Count; i++)
            {
                var label = UiWindowParts.Label(transform, "Line " + i);
                label.fontSize = lineHeight * 0.55f;
                label.alignment = TMPro.TextAlignmentOptions.MidlineLeft;

                var rect = label.rectTransform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.offsetMin = new Vector2(0f, -lineHeight * (i + 1));
                rect.offsetMax = new Vector2(0f, -lineHeight * i);

                // The one line that matters: the label is told a key, and never told anything again.
                var translated = label.gameObject.AddComponent<UiTranslatedText>();
                translated.Key = keys[i];
            }
        }

        // A game's own strings. Only the languages this example bothered with - every other locale falls back
        // to en_US, which is the point: a language that is not finished still reads.
        [ContextMenu("Register Example Strings")]
        public void Register()
        {
            Translator.Add(ELocale.en_US, new Dictionary<string, string>
            {
                ["game.play"] = "Play"
            });

            Translator.Add(ELocale.ru_RU, new Dictionary<string, string>
            {
                ["game.play"] = "Играть"
            });

            Translator.Add(ELocale.ja_JP, new Dictionary<string, string>
            {
                ["game.play"] = "プレイ"
            });
        }

        [ContextMenu("Next Locale")]
        public void NextLocale()
        {
            var all = (ELocale[])System.Enum.GetValues(typeof(ELocale));
            int at = System.Array.IndexOf(all, Translator.Locale);
            Translator.Locale = all[(at + 1) % all.Length];

            Debug.Log("Locale is now " + Translator.Locale, this);
        }

        // What to run before shipping a language: the keys that have nothing of their own in it and are being
        // read out of en_US instead.
        [ContextMenu("Log Untranslated")]
        public void LogUntranslated()
        {
            var missing = Translator.Untranslated(Translator.Locale);
            if (missing.Count == 0)
            {
                Debug.Log(Translator.Locale + " is complete", this);
                return;
            }

            Debug.Log(Translator.Locale + " falls back to " + Translator.FallbackLocale + " for "
                + missing.Count + " key(s): " + string.Join(", ", missing), this);
        }

        [ContextMenu("Clear")]
        public void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                UiWindowParts.Discard(transform.GetChild(i).gameObject);
        }
    }
}
