using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace FlappyTemplate
{
    // The hand over anything clickable. UGUI has no idea of a cursor - a Button tints when it is hovered and the
    // arrow stays an arrow - so this looks at what is under the mouse and says which cursor it wants:
    //
    //   - a Button, Toggle, Slider, Dropdown or anything else Selectable that can be used: the hand;
    //   - a text field: the I-beam;
    //   - a Selectable that is switched off, or nothing that answers a click: the arrow;
    //   - anything with a UiCursorHint on it or above it: whatever that says.
    //
    // It runs by itself. Nothing is added to a scene: the first scene that loads starts a hidden driver, and
    // UiCursor.Enabled = false is how a game that draws its own cursor turns it off.
    //
    // In a WebGL build the cursor is the browser's own, set as CSS on the canvas - which is the only cursor a page
    // can show, and the reason this exists at all. Anywhere else Unity has no hand of its own to show, so nothing
    // changes unless a texture has been given for it with SetTexture. That includes the editor: the hand is
    // something to check in a build, not in play mode.
    public static class UiCursor
    {
        // How often the cursor is worked out again while the mouse is still. Something can arrive under a mouse
        // that has not moved - a window opening, a list scrolling - and a tenth of a second is soon enough for
        // that without raycasting the whole canvas every frame for nothing.
        private const float StillInterval = 0.1f;

        private static readonly Dictionary<ECursor, CursorTexture> textures = new Dictionary<ECursor, CursorTexture>();
        private static readonly List<RaycastResult> hits = new List<RaycastResult>();

        private static UiCursorDriver driver;
        private static PointerEventData pointer;
        private static EventSystem pointerSystem;
        private static Vector2 lastPosition = new Vector2(float.NaN, float.NaN);
        private static float nextCheck;
        private static bool enabled = true;
        private static bool applied;

        private struct CursorTexture
        {
            public Texture2D Texture;
            public Vector2 Hotspot;
        }

        /// <summary>Whether the cursor follows what is under the mouse. Off puts the arrow back and leaves it -
        /// for a game that draws a cursor of its own.</summary>
        public static bool Enabled
        {
            get => enabled;
            set
            {
                if (enabled == value)
                    return;

                enabled = value;

                if (!enabled)
                    Show(ECursor.Default);
                else
                    lastPosition = new Vector2(float.NaN, float.NaN);
            }
        }

        /// <summary>The cursor showing now.</summary>
        public static ECursor Current { get; private set; } = ECursor.Default;

        /// <summary>A cursor image for every platform but the web, where the browser draws its own. The hotspot
        /// is the pixel that does the pointing, from the top left - the tip of the finger, for a hand. Null
        /// takes it away again.</summary>
        public static void SetTexture(ECursor cursor, Texture2D texture, Vector2 hotspot)
        {
            if (texture == null)
                textures.Remove(cursor);
            else
                textures[cursor] = new CursorTexture { Texture = texture, Hotspot = hotspot };

            Reapply();
        }

        /// <summary>Which cursor an object asks for: its own hint or the nearest one above it, else what the
        /// first Selectable or click handler up the hierarchy says. Public so a game can ask about an object
        /// of its own.</summary>
        // Walked one level at a time, the nearest answer winning, so a disabled Button inside a clickable card
        // is an arrow and a label inside a Button is a hand.
        public static ECursor Resolve(GameObject target)
        {
            for (var level = target != null ? target.transform : null; level != null; level = level.parent)
            {
                var hint = level.GetComponent<UiCursorHint>();
                if (hint != null && hint.isActiveAndEnabled)
                    return hint.Cursor;

                // Text that can be selected and copied: the I-beam, as over a field, though nothing can be typed.
                var text = level.GetComponent<UiSelectableText>();
                if (text != null && text.isActiveAndEnabled)
                    return ECursor.Text;

                var selectable = level.GetComponent<Selectable>();
                if (selectable != null && selectable.isActiveAndEnabled)
                {
                    if (!selectable.IsInteractable())
                        return ECursor.Default;

                    return selectable is TMP_InputField || selectable is InputField ? ECursor.Text : ECursor.Pointer;
                }

                // Clickable without being a Selectable - a game's own handler on a plain image. Held to the same
                // rule as the two above: a handler that is switched off answers nothing.
                if (level.GetComponent<IPointerClickHandler>() is Behaviour handler && handler.isActiveAndEnabled)
                    return ECursor.Pointer;
            }

            return ECursor.Default;
        }

        /// <summary>Says the current cursor again - after a texture changed, or the page took it back.</summary>
        public static void Reapply()
        {
            applied = false;
            Show(Current);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // With domain reload off, statics survive from one play to the next, and a driver from the last one
            // is a destroyed object that still compares as set.
            driver = null;
            pointer = null;
            pointerSystem = null;
            enabled = true;
            applied = false;
            Current = ECursor.Default;
            lastPosition = new Vector2(float.NaN, float.NaN);
            hits.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Start()
        {
            if (driver != null)
                return;

            // DontDestroyOnLoad keeps it across a scene change; hidden because it is not something anybody needs
            // to see in the hierarchy or could usefully change there.
            var host = new GameObject("UiCursor") { hideFlags = HideFlags.HideAndDontSave };
            Object.DontDestroyOnLoad(host);
            driver = host.AddComponent<UiCursorDriver>();
        }

        internal static void Tick()
        {
            if (!enabled)
                return;

            var system = EventSystem.current;
            if (system == null)
            {
                Show(ECursor.Default);
                return;
            }

            // No mouse is a touch screen, where there is no cursor to change.
            var mouse = Mouse.current;
            if (mouse == null)
                return;

            Vector2 position = mouse.position.ReadValue();
            float now = Time.unscaledTime;

            if (position == lastPosition && now < nextCheck)
                return;

            lastPosition = position;
            nextCheck = now + StillInterval;

            // Held down, the cursor stays what it was when the press began: a drag that leaves the slider it
            // started on is still dragging that slider, and the hand flicking to an arrow mid-drag reads as the
            // drag having let go.
            if (mouse.leftButton.isPressed)
                return;

            if (pointer == null || pointerSystem != system)
            {
                pointer = new PointerEventData(system);
                pointerSystem = system;
            }

            pointer.Reset();
            pointer.position = position;

            hits.Clear();
            system.RaycastAll(pointer, hits);

            // The first hit is the one a click would go to, so it is the only one asked.
            Show(hits.Count > 0 ? Resolve(hits[0].gameObject) : ECursor.Default);
            hits.Clear();
        }

        private static void Show(ECursor cursor)
        {
            if (applied && cursor == Current)
                return;

            Current = cursor;
            applied = true;

#if UNITY_WEBGL && !UNITY_EDITOR
            SetCursorJS(Css(cursor));
#else
            // Nothing given, nothing done: the arrow is already the system's, and setting it back to null would
            // throw away a cursor a game had set with Cursor.SetCursor itself.
            if (textures.Count == 0)
                return;

            if (textures.TryGetValue(cursor, out var image))
                UnityEngine.Cursor.SetCursor(image.Texture, image.Hotspot, CursorMode.Auto);
            else
                UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
#endif
        }

        /// <summary>The CSS name the browser knows a cursor by.</summary>
        public static string Css(ECursor cursor)
        {
            switch (cursor)
            {
                case ECursor.Pointer: return "pointer";
                case ECursor.Text: return "text";
                case ECursor.Grab: return "grab";
                case ECursor.Grabbing: return "grabbing";
                case ECursor.NotAllowed: return "not-allowed";
                default: return "default";
            }
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void SetCursorJS(string css);
#endif
    }
}
