using UnityEngine;

namespace FlappyTemplate
{
    // Three history strips built at runtime under whatever this sits on: the plain one, one that reads a value
    // out of the game's own outcome, and a scrolling column down the side.
    //
    // It is here to be read as much as run. Between them the three cover everything a game has to decide - which
    // way the strip runs, which end the newest bet lands on, what a chip says, what counts as a win, and what
    // happens when there are more bets than room.
    //
    // The keys are the other half of it: Space adds a bet to all three, Backspace clears them, and 1 pushes a
    // run of bets in one go so the arrival animation can be seen against a strip that is already full.
    [AddComponentMenu("UI/Ui History Example")]
    [RequireComponent(typeof(RectTransform))]
    public class UiHistoryExample : MonoBehaviour
    {
        [SerializeField]
        private bool fillOnStart = true;

        private UiHistory plain;
        private UiHistory multipliers;
        private UiHistory column;

        private int nonce;

        void Start()
        {
            Build();

            if (fillOnStart)
                Push(9);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                Push(1);

            if (Input.GetKeyDown(KeyCode.Alpha1))
                Push(6);

            if (!Input.GetKeyDown(KeyCode.Backspace))
                return;

            plain.Clear();
            multipliers.Clear();
            column.Clear();
        }

        [ContextMenu("Build Now")]
        public void Build()
        {
            Clear();

            // The plain case: a row along the top, newest arriving at the right, as many as fit and the oldest
            // dropped. Nothing is configured, because there is nothing it has to be told - it prints the nonce,
            // it colours a win green and a loss red, and it opens the bet info dialog when a chip is clicked.
            plain = UiHistory.Create(transform, "Plain History", 620f, 64f);
            plain.Rect.anchoredPosition = new Vector2(0f, 180f);
            Detach(plain);

            // The same strip reading the game's own outcome: whatever the server put under "multiplier",
            // truncated to two decimals, zero-padded so a column of numbers lines up, and printed with an x
            // after it. All four of those are inspector fields - this only has to set them from code because it
            // is building the strip from code.
            multipliers = UiHistory.Create(transform, "Multiplier History", 620f, 64f);
            multipliers.Rect.anchoredPosition = new Vector2(0f, 100f);
            Detach(multipliers);
            multipliers.Style.ElementSize = new Vector2(104f, 52f);
            multipliers.TextKey = "multiplier";
            multipliers.TextFormat = "{0}x";
            multipliers.TextDecimals = 2;
            multipliers.TextPad = 2;

            // What a game does when a key in the outcome cannot answer the question: hand over a function. Both
            // of these are the strip's two seams - what a chip says, and which case it is.
            multipliers.Classify = data => Payout(data) >= 2m ? "win" : "loss";

            // A scrolling column, keeping everything rather than dropping the oldest, newest at the top. Drag it
            // or roll the wheel over it; a flick carries.
            column = UiHistory.Create(transform, "Side History", 120f, 320f);
            column.Rect.anchoredPosition = new Vector2(-360f, -40f);
            Detach(column);
            column.Flow = EHistoryFlow.Vertical;
            column.Order = EHistoryOrder.NewestFirst;
            column.Overflow = EHistoryOverflow.Scroll;
            column.Capacity = 0;
            column.Style.ElementSize = new Vector2(0f, 48f);
        }

        // Every strip here is fed by hand, so none of them should be listening for a server or filling itself
        // with samples. A game does the opposite of this: it leaves both alone and the strip looks after itself.
        private static void Detach(UiHistory strip)
        {
            strip.FollowState = false;
            strip.Preview = false;
            strip.Clear();
        }

        [ContextMenu("Clear")]
        public void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.GetComponent<UiHistory>() != null)
                    UiWindowParts.Discard(child.gameObject);
            }

            plain = null;
            multipliers = null;
            column = null;
        }

        // What the server would be sending. A game never writes this - the strips fill themselves in from
        // OnHistory - but an example with no socket running has to come from somewhere.
        private void Push(int count)
        {
            if (plain == null)
                return;

            for (int i = 0; i < count; i++)
            {
                nonce++;

                bool win = nonce % 3 != 0;
                decimal payout = win ? 1.2m + nonce % 7 * 0.43m : 0m;

                var data = new HistoryDto
                {
                    Id = "example-" + nonce.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    GameName = "Example",
                    BetAmount = "1",
                    WinAmount = payout.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Currency = "USD",
                    IPlayerId = nonce % 4 == 0 ? "me" : "someone-else",
                    N = nonce,
                    CreatedAt = 1700000000000L + nonce * 1000L,
                };

                data._Outcome = new GenericDictionary<string, string>
                {
                    { "multiplier", payout.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) },
                };

                // One object per strip. Sharing one would work, but a strip is allowed to hold on to what it was
                // given, and three of them holding the same object is the kind of thing that only breaks later.
                plain.Add(data);
                multipliers.Add(Copy(data));
                column.Add(Copy(data));
            }
        }

        private static HistoryDto Copy(HistoryDto data) =>
            new HistoryDto
            {
                Id = data.Id,
                GameName = data.GameName,
                BetAmount = data.BetAmount,
                WinAmount = data.WinAmount,
                Currency = data.Currency,
                IPlayerId = data.IPlayerId,
                N = data.N,
                _Outcome = data._Outcome,
                CreatedAt = data.CreatedAt,
            };

        private static decimal Payout(HistoryDto data)
        {
            decimal.TryParse(
                UiHistory.Read(data, "multiplier"),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var value
            );

            return value;
        }
    }
}
