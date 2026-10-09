using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace FlappyTemplate
{
    // The clipboard, the same call on every platform. Everywhere but the web this is GUIUtility.systemCopyBuffer.
    // In a browser that buffer is Unity's own and goes nowhere, so the copy is done by the page instead - see
    // JSPlugins/Clipboard.jslib for why it has to happen inside the key press, and why Offer exists at all.
    public static class UiClipboard
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void OfferCopyJS(string text);

        [DllImport("__Internal")]
        private static extern void OfferClickCopyJS(string text);

        [DllImport("__Internal")]
        private static extern int CopyTextJS(string text);
#endif

        /// <summary>Copies the text now. True when it reached the clipboard - or, in a browser, when the copy was
        /// at least handed over: a browser only allows it close to a press or a click, so call this from one.</summary>
        public static bool Copy(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

#if UNITY_WEBGL && !UNITY_EDITOR
            return CopyTextJS(text) != 0;
#else
            GUIUtility.systemCopyBuffer = text;
            return true;
#endif
        }

        /// <summary>Says what the next Ctrl+C or Cmd+C copies, so a browser can copy it inside the key press
        /// itself. Null or empty withdraws it. Does nothing outside a browser, where Copy works at any time.</summary>
        public static void Offer(string text)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            OfferCopyJS(text ?? string.Empty);
#endif
        }

        /// <summary>Says what the release of the press now going on copies, so a browser can copy it inside its own
        /// mouseup or touchend - for a copy button, from its pointer-down. Null or empty withdraws it: the pointer
        /// left the button, or the press became a scroll. Does nothing outside a browser.</summary>
        public static void OfferClick(string text)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            OfferClickCopyJS(text ?? string.Empty);
#endif
        }

        /// <summary>Whether a Ctrl+C is copied by the page rather than by the game - a WebGL build. Where it is,
        /// the game only has to have offered the text.</summary>
        public static bool PageCopies
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }
    }
}
