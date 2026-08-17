using UnityEngine;

namespace FlappyTemplate
{
    // The one thing in the game with an Update on it that hotkeys need. Made by Hotkeys on the first bind, on a
    // hidden object that survives a scene change - there is nothing to add to a scene and nothing to remember
    // to keep there, which is the point: a key bound from a script somewhere in the game works because it was
    // bound, not because something else was also set up.
    //
    // Everything it does is in Hotkeys.Tick. It exists because a static class cannot be given a frame.
    [AddComponentMenu("")]
    internal class HotkeyDriver : MonoBehaviour
    {
        void Update()
        {
            Hotkeys.Tick();
        }
    }
}
