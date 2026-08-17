using TMPro;
using UnityEngine;

namespace FlappyTemplate
{
    // Four history strips built at runtime under whatever this sits on: a plain row, a row of coloured
    // multipliers, a scrolling column down the side, and a row of a shared game's rounds.
    //
    // It is here to be read as much as run. The first thing it does is the first thing a game does: draw an
    // element. The strip has no chip of its own and no settings for one - a bet's size, its colours and what it
    // says are all the element's - so an example that skipped that step would build four empty rects.
    //
    // Between them they cover everything that is left to decide: which way the strip runs, which end the newest
    // bet lands on, what happens when there are more bets than room, and which history is being shown at all.
    // The fourth is the last of those: the same element as the second, fed rounds instead of bets.
    //
    // The keys are the other half of it: Space adds a bet to all four, Backspace clears them, and 1 pushes a
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
        private UiHistory shared;

        private UiHistoryExampleElement nonceChip;
        private UiHistoryExampleElement valueChip;

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
            shared.Clear();
        }

        [ContextMenu("Build Now")]
        public void Build()
        {
            Clear();

            // The elements first, because without one a strip has nothing to draw a bet with. A game draws these
            // as prefabs and drags them into Element Prefab; they are built here because everything in this file
            // is, and a template kept switched off in the scene works exactly as well as an asset.
            nonceChip = Chip("Nonce Chip", string.Empty, new Vector2(96f, 52f));
            valueChip = Chip("Value Chip", "multiplier", new Vector2(104f, 52f));

            // The plain case: a row along the top, newest arriving at the right, as many as fit and the oldest
            // dropped. Nothing is configured beyond the element - the strip opens the bet info dialog when a chip
            // is clicked and looks after the rest itself.
            plain = UiHistory.Create(transform, "Plain History", 620f, 64f);
            plain.Rect.anchoredPosition = new Vector2(0f, 180f);
            plain.ElementPrefab = nonceChip;
            plain.Align = EHistoryAlign.End;
            Detach(plain);

            // The same strip with a different element. Everything that changed between the two - the multiplier
            // out of the game's own payload, two decimals, an x after it, green or red by how the round went - is
            // in UiHistoryExampleElement.Write, which is where a game says what a win looks like.
            multipliers = UiHistory.Create(transform, "Multiplier History", 620f, 64f);
            multipliers.Rect.anchoredPosition = new Vector2(0f, 100f);
            multipliers.ElementPrefab = valueChip;
            Detach(multipliers);

            // A scrolling column, keeping everything rather than dropping the oldest, newest at the top. Drag it
            // or roll the wheel over it; a flick carries.
            column = UiHistory.Create(transform, "Side History", 120f, 320f);
            column.Rect.anchoredPosition = new Vector2(-360f, -40f);
            column.ElementPrefab = valueChip;
            Detach(column);
            column.Flow = EHistoryFlow.Vertical;
            column.Order = EHistoryOrder.NewestFirst;
            column.Overflow = EHistoryOverflow.Scroll;
            column.Capacity = 0;

            // The shared feed, on the same element as the second strip. That is the whole point of it: a round
            // is drawn as a bet, so a chip written for the player's own history draws one without being told.
            // What is different is behind it - the strip listens for ON_GAME_HISTORY instead of ON_HISTORY, and
            // a press opens the game history window instead of the bet info one.
            //
            // Said outright here because there is no server running to say it. A game leaves Feed on Auto and
            // the server decides.
            shared = UiHistory.Create(transform, "Round History", 620f, 64f);
            shared.Rect.anchoredPosition = new Vector2(0f, 20f);
            shared.ElementPrefab = valueChip;
            Detach(shared);
            shared.Feed = EHistoryFeed.Shared;
        }

        // What a game draws in the editor instead. A plate, a label in the middle of it, and the component that
        // turns a bet into what the label says - kept switched off, because it is a template rather than a chip
        // on screen. The strip switches its copies back on as it makes them.
        private UiHistoryExampleElement Chip(string name, string key, Vector2 size)
        {
            var created = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RoundedBox));
            var rect = (RectTransform)created.transform;
            rect.SetParent(transform, false);
            rect.localScale = Vector3.one;
            rect.sizeDelta = size;
            created.layer = gameObject.layer;

            var box = created.GetComponent<RoundedBox>();
            box.FillColor = new Color(0.149f, 0.137f, 0.263f);
            box.SetBorderColor(new Color(1f, 1f, 1f, 0.08f));
            box.SetBorderSize(1.5f);
            box.SetCornerRadius(8f);
            box.EdgeSoftness = 1.25f;

            var label = UiWindowParts.Label(rect, "Value");
            label.fontSize = 26f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            UiWindowParts.Stretch(label.rectTransform, 6f, 6f, 6f, 6f);

            var element = created.AddComponent<UiHistoryExampleElement>();
            element.Label = label;
            element.Key = key;

            created.SetActive(false);
            return element;
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
                if (child.GetComponent<UiHistory>() != null || child.GetComponent<UiHistoryElement>() != null)
                    UiWindowParts.Discard(child.gameObject);
            }

            plain = null;
            multipliers = null;
            column = null;
            shared = null;
            nonceChip = null;
            valueChip = null;
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

                // The shared strip is fed rounds rather than bets: the totals of everybody who played, and the
                // outcome the room got. It reads the same key out of the payload, which is why the same chip
                // draws it.
                var round = new GameHistoryDto
                {
                    Id = data.Id,
                    TotalBetCount = 1 + nonce % 6,
                    TotalBetAmountUsd = (4m + nonce % 9).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    TotalWinAmountUsd = ((4m + nonce % 9) * payout).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    _Outcome = data._Outcome,
                };

                shared.Add(round);
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
    }
}
