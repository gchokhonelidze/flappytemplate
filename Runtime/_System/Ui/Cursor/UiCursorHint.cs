using UnityEngine;

namespace FlappyTemplate
{
    // Says which cursor this object shows, overriding whatever UiCursor would have worked out for it. For the
    // cases the rule gets wrong: a panel that is clickable through code of its own rather than a Button, a
    // handle that drags, a Button that should not look like one - a window's backdrop is the one the template
    // itself needs.
    //
    // It covers its children too, the nearest one up the hierarchy winning: a hint on a card reaches the labels
    // inside it, and a Button inside that card with a hint of its own keeps its own.
    [AddComponentMenu("UI/Cursor Hint")]
    [DisallowMultipleComponent]
    public class UiCursorHint : MonoBehaviour
    {
        [Tooltip("What the mouse shows over this object and its children.")]
        [SerializeField]
        private ECursor cursor = ECursor.Pointer;

        public ECursor Cursor
        {
            get => cursor;
            set => cursor = value;
        }

        /// <summary>Puts a hint on an object, or changes the one already there.</summary>
        public static UiCursorHint Set(GameObject target, ECursor cursor)
        {
            if (target == null)
                return null;

            var hint = target.GetComponent<UiCursorHint>();
            if (hint == null)
                hint = target.AddComponent<UiCursorHint>();

            hint.cursor = cursor;
            return hint;
        }
    }
}
