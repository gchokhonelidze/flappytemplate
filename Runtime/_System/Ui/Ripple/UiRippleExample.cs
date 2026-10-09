using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // Three ways of waving, built at runtime under whatever this sits on: a button that waves on every press, a
    // switch that waves when it flips - green going on, grey going off - and a round button with a wide, slow
    // single ring and no dip. Drop it on an empty RectTransform inside a canvas and press play.
    //
    // The first is the whole of the usual case - one line. The second is the one to copy for anything that
    // should answer to what it did rather than to being pressed.
    [AddComponentMenu("UI/Ui Ripple Example")]
    [RequireComponent(typeof(RectTransform))]
    public class UiRippleExample : MonoBehaviour
    {
        private static readonly Color Green = new Color(0.45f, 0.86f, 0.5f);
        private static readonly Color Grey = new Color(1f, 1f, 1f, 0.16f);

        private RoundedBox pill;
        private UiRipple pillWave;
        private bool on = true;

        void Start()
        {
            Build();
        }

        [ContextMenu("Build Now")]
        public void Build()
        {
            // Pressed: a wave in the ripple's own colour, and the button dips.
            var button = Tile("Button", new Vector2(-260f, 0f), new Vector2(180f, 56f), new Color(1f, 1f, 1f, 0.1f), 12f);
            Get<Button>(button).targetGraphic = button;
            Caption(button, "Press");
            UiRipple.On(button.gameObject);

            // Flipped: Play On Press off, and Play called with the colour of where it is going.
            pill = Tile("Switch", new Vector2(0f, 0f), new Vector2(64f, 34f), Green, 17f);
            var flip = Get<Button>(pill);
            flip.targetGraphic = pill;
            flip.onClick.RemoveAllListeners();
            flip.onClick.AddListener(Flip);

            pillWave = UiRipple.On(pill.gameObject);
            pillWave.PlayOnPress = false;

            // One wide, slow ring and a button that stays still.
            var round = Tile("Round", new Vector2(240f, 0f), new Vector2(64f, 64f), new Color(0.98f, 0.72f, 0.2f), 32f);
            Get<Button>(round).targetGraphic = round;

            var wide = UiRipple.On(round.gameObject);
            wide.Spread = 40f;
            wide.Duration = 1.1f;
            wide.Punch = 0f;
            wide.Color = new Color(0.98f, 0.72f, 0.2f, 0.8f);
        }

        private void Flip()
        {
            on = !on;
            pill.FillColor = on ? Green : Grey;
            pillWave.Play(on ? Green : new Color(1f, 1f, 1f, 0.5f));
        }

        private RoundedBox Tile(string name, Vector2 position, Vector2 size, Color fill, float radius)
        {
            var box = UiWindowParts.Box(transform, name);
            UiWindowParts.Pin(box.rectTransform, new Vector2(0.5f, 0.5f), size, position);
            box.FillColor = fill;
            box.SetCornerRadius(radius);
            box.raycastTarget = true;
            return box;
        }

        private static void Caption(RoundedBox on, string text)
        {
            var label = UiWindowParts.Label(on.transform, "Label");
            UiWindowParts.Stretch(label.rectTransform, 0f, 0f, 0f, 0f);
            label.text = text;
            label.fontSize = 20f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
        }

        // Found before it is added, so Build Now run twice does not stack a second Button on each tile.
        private static T Get<T>(Component on) where T : Component
        {
            var found = on.GetComponent<T>();
            return found != null ? found : on.gameObject.AddComponent<T>();
        }
    }
}
