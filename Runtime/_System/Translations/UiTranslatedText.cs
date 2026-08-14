using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // A label that writes itself. Put it on a TextMeshPro or a uGUI Text, give it a key, and the text is
    // whatever that key says in the current locale - now, and again the moment the locale changes.
    //
    // It finds the label on its own object, and failing that on the first child that has one, so the usual case
    // is: add the component, type the key, done. Nothing else on the label is touched - font, size, colour,
    // alignment and every other setting are the label's own business.
    //
    // Code that builds its labels rather than laying them out in a scene does not need this at all and can call
    // Translator.T directly - but then it is the one that has to redraw when the locale changes. This exists so
    // that the common case does not have to think about that.
    [AddComponentMenu("UI/Ui Translated Text")]
    [ExecuteAlways]
    public class UiTranslatedText : MonoBehaviour
    {
        [Tooltip("The key to look up - area.thing, as written in Translations or registered with Translator.Add. An unknown key shows Translator.MissingFormat, which is the word \"untranslated\".")]
        [SerializeField]
        private string key = "";

        [Tooltip("Written before the translation, untranslated itself. For a colon or a bullet that is the same in every language.")]
        [SerializeField]
        private string prefix = "";

        [Tooltip("Written after the translation, untranslated itself.")]
        [SerializeField]
        private string suffix = "";

        [Tooltip("The label to write into. Left empty: a TextMeshPro or Text on this object, else the first one inside it.")]
        [SerializeField]
        private Graphic label;

        private TMP_Text tmp;
        private Text ugui;

        /// <summary>The key being shown. Setting it rewrites the label at once.</summary>
        public string Key
        {
            get => key;
            set
            {
                key = value;
                Apply();
            }
        }

        /// <summary>Written before and after the translation, and never translated themselves.</summary>
        public string Prefix
        {
            get => prefix;
            set
            {
                prefix = value;
                Apply();
            }
        }

        /// <inheritdoc cref="Prefix"/>
        public string Suffix
        {
            get => suffix;
            set
            {
                suffix = value;
                Apply();
            }
        }

        /// <summary>The text this is showing right now, prefix and suffix included.</summary>
        public string Text => prefix + Translator.T(key) + suffix;

        private void OnEnable()
        {
            Translator.OnLocaleChanged += Apply;
            Apply();
        }

        private void OnDisable()
        {
            Translator.OnLocaleChanged -= Apply;
        }

        private void OnValidate()
        {
            if (isActiveAndEnabled)
                Apply();
        }

        /// <summary>Writes the translation into the label. Called for you when the locale changes; call it after
        /// registering strings later than a scene loaded.</summary>
        [ContextMenu("Apply")]
        public void Apply()
        {
            if (!Bind())
                return;

            var text = Text;

            if (tmp != null && tmp.text != text)
                tmp.text = text;
            else if (ugui != null && ugui.text != text)
                ugui.text = text;
        }

        // Held on to between calls, since Apply runs on every locale change and a GetComponentInChildren per
        // label per change is a search nobody asked for.
        private bool Bind()
        {
            if (tmp != null || ugui != null)
                return true;

            var found = label != null ? label : GetComponent<Graphic>();

            tmp = found as TMP_Text;
            ugui = found as Text;

            if (tmp == null && ugui == null)
            {
                tmp = GetComponentInChildren<TMP_Text>(true);
                if (tmp == null)
                    ugui = GetComponentInChildren<Text>(true);
            }

            return tmp != null || ugui != null;
        }
    }
}
