using TMPro;
using UnityEngine;

namespace FlappyTemplate
{
    // Three labels to select from, built at runtime under whatever this sits on: a seed, a line with a tinted
    // caption in front of its value, and a hash long enough to wrap. Drop it on an empty RectTransform inside a
    // canvas with an EventSystem and press play.
    //
    // Drag across any of them or double-click for the lot, then Ctrl+C - or press the copy icon at the right.
    // Each copy is logged. The middle one is the case the bet info sheet's Round ID is: nothing copied has the
    // colour tags in it.
    [AddComponentMenu("UI/Ui Selectable Text Example")]
    [RequireComponent(typeof(RectTransform))]
    public class UiSelectableTextExample : MonoBehaviour
    {
        void Start()
        {
            Build();
        }

        [ContextMenu("Build Now")]
        public void Build()
        {
            Card("Seed", new Vector2(0f, 150f), 60f, "6e43ebe3daf247c9892449b94be2d57a");

            Card("Round Id", new Vector2(0f, 60f), 60f,
                "<color=#ffffff99>Round ID:</color>  d9f77676f0a390e1");

            Card("Hash", new Vector2(0f, -70f), 130f,
                "5cca0e0e2291f73c5999d07c8d336905013026f2fff58fefb2464ef2068c8d4a"
                + "5274784d4f625e6994005efea832d4f1048d14931174378209421f1d8eeaa9c6");
        }

        private void Card(string name, Vector2 position, float height, string text)
        {
            var box = UiWindowParts.Box(transform, name);
            UiWindowParts.Pin(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(560f, height), position);
            box.FillColor = new Color(0.165f, 0.169f, 0.212f);
            box.SetCornerRadius(10f);

            var label = UiWindowParts.Label(box.transform, "Value");
            var rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(16f, 10f);
            rect.offsetMax = new Vector2(-16f, -10f);

            label.text = text;
            label.fontSize = 20f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.Normal;

            // The one line that makes it selectable. It also switches raycasting on, which a label is usually
            // made without.
            var selectable = UiSelectableText.On(label);
            selectable.OnCopied.RemoveAllListeners();
            selectable.OnCopied.AddListener(copied => Debug.Log("Selectable Text example: copied \"" + copied + "\""));

            // And the copy icon at its right: 18 across, 10 clear of the text, which wraps short of it.
            var copy = UiCopyButton.On(label, 18f, 10f);
            copy.OnCopied.RemoveAllListeners();
            copy.OnCopied.AddListener(copied => Debug.Log("Selectable Text example: copy button copied \"" + copied + "\""));
        }
    }
}
