using UnityEngine;

namespace FlappyTemplate
{
    // Whether a key is held down this frame. The only question the registry ever asks of the keyboard: the
    // presses and the releases are edges Hotkeys works out itself by comparing one frame to the next.
    //
    // One question rather than three is the whole reason Hotkeys.Reader is worth having. A game on another
    // input path - the Input System package, a gamepad, a row of on-screen buttons on a phone - replaces that
    // one delegate and gets presses, releases, held keys, the down-state the window paints from and the
    // suppression while an input field has focus, without writing any of it again.
    internal static class HotkeyInput
    {
        /// <summary>Whether the key is held, read from the old Input Manager.</summary>
        // The Input Manager rather than the Input System package, because the package has no dependency on the
        // latter and adding one to a template would put it in every game that installs this. A game that has
        // moved to the new backend replaces Hotkeys.Reader instead - see the readme beside this file.
        public static bool Read(KeyCode key)
        {
            if (key == KeyCode.None)
                return false;

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(key);
#else
            Warn();
            return false;
#endif
        }

#if !ENABLE_LEGACY_INPUT_MANAGER
        private static bool warned;

        // Player Settings has Active Input Handling on Input System Package (New), which switches the old
        // Input Manager off - and asking it anything from there throws rather than returning false. So it is
        // not asked at all, and this is said once instead of a key that quietly never fires.
        private static void Warn()
        {
            if (warned)
                return;

            warned = true;

            Debug.LogWarning(
                "Hotkeys: Active Input Handling is set to Input System Package (New), which the template does "
                + "not read. Set it to Both in Player Settings, or give Hotkeys.Reader a delegate of your own - "
                + "see Ui/Hotkeys/README.md.");
        }
#endif
    }
}
