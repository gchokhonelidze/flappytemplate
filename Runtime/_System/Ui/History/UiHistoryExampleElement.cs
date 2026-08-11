using System.Globalization;
using TMPro;
using UnityEngine;

namespace FlappyTemplate
{
    // The element the example strips are made of, and roughly the shape every game's own ends up: a label, a
    // colour, and a Write that turns one bet into both.
    //
    // Two of these are built in UiHistoryExample - one printing the nonce, one printing the multiplier the
    // server sent under a key of the game's own, green when the round paid and red when it did not. That is the
    // whole of what the strip used to do with settings, and it is four lines here because the game knows what
    // its own payload means and the strip never could.
    //
    // A real game draws this as a prefab rather than building it from code. The class is the same either way.
    [AddComponentMenu("UI/Ui History Example Element")]
    public class UiHistoryExampleElement : UiHistoryElement
    {
        [Tooltip("Where the value is printed.")]
        [SerializeField]
        private TextMeshProUGUI label;

        [Tooltip("The key in the bet's outcome payload to print. Empty prints the nonce instead - what a game with no value worth showing would do.")]
        [SerializeField]
        private string key = string.Empty;

        [SerializeField]
        private Color won = new Color(0.388f, 1f, 0.580f);

        [SerializeField]
        private Color lost = new Color(1f, 0.388f, 0.388f);

        public TextMeshProUGUI Label
        {
            get => label;
            set => label = value;
        }

        public string Key
        {
            get => key;
            set => key = value;
        }

        public override void Write(HistoryDto value)
        {
            if (label == null || value == null)
                return;

            if (string.IsNullOrEmpty(key))
            {
                label.text = value.N.HasValue ? value.N.Value.ToString(CultureInfo.InvariantCulture) : value.Id;
                label.color = Color.white;
                return;
            }

            // Truncated rather than rounded, the same way the rest of this package prints money: a multiplier the
            // server called 2.839 was 2.83, and rounding it up to 2.84 shows the player a number that never
            // happened.
            decimal.TryParse(Outcome(key), NumberStyles.Any, CultureInfo.InvariantCulture, out var number);
            decimal cut = decimal.Truncate(number * 100m) / 100m;

            label.text = cut.ToString("0.00", CultureInfo.InvariantCulture) + "x";
            label.color = number >= 1m ? won : lost;
        }
    }
}
