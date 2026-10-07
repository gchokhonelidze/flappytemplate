using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // What the cursor does over each kind of thing, built at runtime under whatever this sits on: a button, the
    // same button switched off, a card made clickable with a handler of its own, and a handle hinted as
    // draggable. Drop it on an empty RectTransform inside a canvas and press play - then make a WebGL build,
    // since the editor has no hand to show.
    //
    // Nothing here sets the cursor. That is the point: UiCursor works it out from what each one is, and the
    // hint on the last one is the only line that says anything about cursors at all.
    [AddComponentMenu("UI/Ui Cursor Example")]
    [RequireComponent(typeof(RectTransform))]
    public class UiCursorExample : MonoBehaviour
    {
        void Start()
        {
            Build();
        }

        [ContextMenu("Build Now")]
        public void Build()
        {
            // A Button: the hand, because it can be pressed.
            var on = Tile("Button", new Vector2(-330f, 0f), new Color(0.565f, 0.894f, 0.604f));
            Get<Button>(on).targetGraphic = on;

            // The same Button, not interactable: the arrow, because pressing it would do nothing.
            var off = Tile("Disabled Button", new Vector2(-110f, 0f), new Color(0.6f, 0.6f, 0.6f));
            var dead = Get<Button>(off);
            dead.targetGraphic = off;
            dead.interactable = false;

            // Clickable through a handler rather than a Button - an EventTrigger here, a game's own
            // IPointerClickHandler as often: still the hand.
            var card = Tile("Clickable Card", new Vector2(110f, 0f), new Color(0.98f, 0.8f, 0.08f));
            var trigger = card.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = card.gameObject.AddComponent<EventTrigger>();

            var click = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            click.callback.AddListener(_ => Debug.Log("Clickable Card clicked"));
            trigger.triggers.Clear();
            trigger.triggers.Add(click);

            // Nothing clickable on it, so the rule alone would give the arrow. The hint says what it is.
            var handle = Tile("Drag Handle", new Vector2(330f, 0f), new Color(0.69f, 0.35f, 0.95f));
            UiCursorHint.Set(handle.gameObject, ECursor.Grab);
        }

        private RoundedBox Tile(string name, Vector2 position, Color fill)
        {
            var box = UiWindowParts.Box(transform, name);
            UiWindowParts.Pin(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(190f, 110f), position);
            box.FillColor = fill;
            box.SetCornerRadius(14f);
            box.raycastTarget = true;

            var label = UiWindowParts.Label(box.transform, "Label");
            UiWindowParts.Stretch(label.rectTransform, 8f, 8f, 8f, 8f);
            label.text = name;
            label.fontSize = 22f;
            label.color = new Color(0.11f, 0.1f, 0.29f);
            label.alignment = TMPro.TextAlignmentOptions.Center;
            return box;
        }

        // Found before it is added, so Build Now run twice does not stack a second Button on each tile.
        private static T Get<T>(Component on) where T : Component
        {
            var found = on.GetComponent<T>();
            return found != null ? found : on.gameObject.AddComponent<T>();
        }
    }
}
