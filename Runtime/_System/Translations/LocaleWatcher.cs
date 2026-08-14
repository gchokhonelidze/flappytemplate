using UnityEngine;

namespace FlappyTemplate
{
    // MainState.Locale is a plain field on a plain object. Nothing fires when it is written - not from the
    // inspector while the game is running, not from a server message, not from a language menu that sets it
    // directly instead of going through Translator.Locale.
    //
    // So one object in the game reads it once a frame and tells Translator when it has moved. One Update for
    // the whole translation system, no matter how many labels are listening: the labels wait on
    // Translator.OnLocaleChanged and do nothing until it is raised.
    //
    // It makes itself when the game starts and hides itself from the hierarchy - there is nothing on it to
    // configure, and a scene should not have to remember to carry one.
    [AddComponentMenu("")]
    internal class LocaleWatcher : MonoBehaviour
    {
        private static LocaleWatcher inst;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Spawn()
        {
            if (inst != null)
                return;

            var host = new GameObject("Locale Watcher") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(host);
            inst = host.AddComponent<LocaleWatcher>();
        }

        private void Update()
        {
            Translator.Poll();
        }
    }
}
