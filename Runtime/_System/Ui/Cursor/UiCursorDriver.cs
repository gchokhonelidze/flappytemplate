using UnityEngine;

namespace FlappyTemplate
{
    // The frame UiCursor needs, on a hidden object that survives a scene change - the same arrangement as the
    // hotkeys' driver, and for the same reason: there is nothing to add to a scene and nothing to forget.
    //
    // Everything it does is in UiCursor.Tick. It exists because a static class cannot be given a frame.
    [AddComponentMenu("")]
    internal class UiCursorDriver : MonoBehaviour
    {
        void LateUpdate()
        {
            UiCursor.Tick();
        }

        void OnApplicationFocus(bool focused)
        {
            // A browser tab coming back may have had its cursor reset behind our back; saying it again costs
            // one call.
            if (focused)
                UiCursor.Reapply();
        }
    }
}
