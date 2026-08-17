using DG.Tweening;
using TMPro;
using UnityEngine;

namespace FlappyTemplate
{
    // Six windows, built at runtime under whatever this sits on: a plain one, a modal one, the statistics
    // window, the bet info window, the game history window and the fairness window. Drop it on an empty
    // RectTransform inside a canvas and press play, or use Build Now from the component's context menu to see
    // them without leaving the editor.
    //
    // It is here to be read as much as run. Each window below is one chain, and between them they cover
    // most of what UiWindowBuilder can say - a caption, a backdrop, a transition, a drag, a close.
    //
    // The keys are the other half of it: 1 to 6 open the six windows, and Escape closes whatever is open.
    //
    // They are placed to overlap on purpose, because that is the other thing to look at here: open several
    // and each one arrives in front of the last, and clicking any of them - the caption, the body, a button
    // or a tab inside it - brings that one forward again. None of the five asks for it; a window does it.
    [AddComponentMenu("UI/Ui Window Example")]
    [RequireComponent(typeof(RectTransform))]
    public class UiWindowExample : MonoBehaviour
    {
        [SerializeField]
        private bool openStatisticsOnStart = true;

        private UiWindow plain;
        private UiWindow modal;
        private StatisticsWindow statistics;
        private BetInfoWindow betInfo;
        private GameHistoryWindow gameHistory;
        private FairnessWindow fairness;

        void Start()
        {
            Build();

            if (openStatisticsOnStart && statistics != null)
                statistics.Window.Open();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) && plain != null)
                plain.Toggle();

            if (Input.GetKeyDown(KeyCode.Alpha2) && modal != null)
                modal.Toggle();

            if (Input.GetKeyDown(KeyCode.Alpha3) && statistics != null)
                statistics.Window.Toggle();

            if (Input.GetKeyDown(KeyCode.Alpha4) && betInfo != null)
                betInfo.Window.Toggle();

            if (Input.GetKeyDown(KeyCode.Alpha5) && gameHistory != null)
                gameHistory.Window.Toggle();

            if (Input.GetKeyDown(KeyCode.Alpha6) && fairness != null)
                fairness.Window.Toggle();

            if (!Input.GetKeyDown(KeyCode.Escape))
                return;

            // Newest first, so Escape peels them off in the order they went on rather than closing the one
            // underneath and leaving the modal sheet over the top of it.
            if (modal != null && modal.IsOpen)
                modal.Close();
            else if (fairness != null && fairness.Window.IsOpen)
                fairness.Window.Close();
            else if (gameHistory != null && gameHistory.Window.IsOpen)
                gameHistory.Window.Close();
            else if (betInfo != null && betInfo.Window.IsOpen)
                betInfo.Window.Close();
            else if (statistics != null && statistics.Window.IsOpen)
                statistics.Window.Close();
            else if (plain != null && plain.IsOpen)
                plain.Close();
        }

        [ContextMenu("Build Now")]
        public void Build()
        {
            Clear();

            // The plain case: a panel, a caption, a close button, and a drag by that caption. Nothing is said
            // about how it looks, so it arrives in the plain dark dialog every new window starts as - select
            // any of its parts in the hierarchy and style them from there.
            plain = UiWindowBuilder.Create(transform, "Plain Window")
                .Size(340f, 220f)
                .At(new Vector2(-240f, 120f))
                .Title("Draggable")
                .Draggable()
                .Done();

            // A modal, and a slide rather than a pop. The sheet behind it swallows every click that misses
            // the window, and takes it down when clicked - which is why this one has no reason to be
            // dragged, and says so.
            //
            // It is also the styled one. Panel, Caption and Title hand back the component itself, so a
            // window built from code is coloured the same way one built in the editor is: by telling the
            // RoundedBox and the label what to be, once, after which nothing writes over them.
            modal = UiWindowBuilder.Create(transform, "Modal Window")
                .Size(420f, 260f)
                .Title("Modal")
                .Panel(box =>
                {
                    box.FillColor = new Color(0.13f, 0.12f, 0.24f);
                    box.SetBorderSize(2f);
                    box.SetBorderColor(new Color(0.45f, 0.4f, 0.75f));
                    box.SetCornerRadius(20f);
                })
                .Caption(box =>
                {
                    box.FillColor = new Color(1f, 1f, 1f, 0.06f);

                    // The caption sits inside the border, so its top corners are the panel's radius less
                    // the border to follow the outline rather than cut across it.
                    box.RadiusTopLeft = 18f;
                    box.RadiusTopRight = 18f;
                })
                .Title(label => label.color = new Color(0.85f, 0.83f, 1f))
                .Backdrop(new Color(0f, 0f, 0f, 0.65f))
                .Transition(EWindowTransition.SlideUp, 0.34f, 0.24f)
                .Easing(Ease.OutCubic, Ease.InCubic)
                .Fixed()
                .Done();

            // The statistics window builds its own contents, so the chain only has to say where it goes and
            // how it behaves. Its height comes out of the rows it ends up showing.
            statistics = StatisticsWindow.Create(transform);
            statistics.Window.Draggable = true;
            statistics.Window.Rect.anchoredPosition = new Vector2(260f, 0f);

            // The bet info window is the same idea one step further: it fills itself in from the server, and
            // leaves one block empty for the game. Show(id) would ask for a real bet; with no socket running
            // it shows a sample so there is something to look at.
            betInfo = BetInfoWindow.Create(transform);
            betInfo.Window.Rect.anchoredPosition = new Vector2(-220f, -40f);
            betInfo.OnTransaction.AddListener(ShowRoll);
            ShowRoll(betInfo.Transaction);

            // The game history window is the shared-game half of the bet info one: a round rather than a bet,
            // everybody who played it rather than one player, and a press on any of them opening the bet info
            // dialog on that bet - which is why it is handed the one built above rather than finding its own.
            gameHistory = GameHistoryWindow.Create(transform);
            gameHistory.Window.Rect.anchoredPosition = new Vector2(0f, -80f);
            gameHistory.BetInfo = betInfo;
            gameHistory.OnRound.AddListener(ShowRound);
            ShowRound(gameHistory.Round);

            // The fairness window is the one with something to send rather than only something to show: the
            // two buttons emit RANDOMIZE and RANDOMIZE_CLIENTSALT_ONLY, and it fills itself in from the pair
            // that comes back. With no socket running the buttons roll the sample over instead, so both can
            // be pressed here.
            fairness = FairnessWindow.Create(transform);
            fairness.Window.Rect.anchoredPosition = new Vector2(240f, -60f);
            fairness.OnSeeds.AddListener(seeds => Debug.Log("New seed pair, nonce " + seeds.Nonce));
        }

        // What no template can write for a game: the block under the fields that says what actually happened.
        // A dice game draws the roll here, a crash game the multiplier it stopped at, this one prints whatever
        // the server called the outcome.
        //
        // A label rather than a panel with a label in it, and that is the one thing to know: the row it lands
        // in is as tall as what is in it says it needs to be, and a label answers that question while a plain
        // panel does not. Wrap it in something and either give that a Layout Element or set Outcome Height.
        private void ShowRoll(TransactionPublic data)
        {
            if (betInfo == null || data == null)
                return;

            var label = UiWindowParts.Label(betInfo.Outcome, "Roll");
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 22f;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.98f, 0.8f, 0.08f);
            label.text = data.Outcome != null && data.Outcome.Count > 0
                ? "Outcome: " + string.Join(", ", data.Outcome.Keys)
                : "This game draws its own outcome here";

            betInfo.Refresh();
        }

        // The same thing again for a whole round rather than one bet. A shared game draws what the room got
        // here - the multiplier it stopped at, the number that came up - and the window grows around it.
        private void ShowRound(GameHistoryByIdDto data)
        {
            if (gameHistory == null || data == null)
                return;

            var label = UiWindowParts.Label(gameHistory.Outcome, "Roll");
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 34f;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.98f, 0.8f, 0.08f);
            label.text = data.Outcome != null && data.Outcome.Count > 0
                ? "Outcome: " + string.Join(", ", data.Outcome.Keys)
                : "This game draws its own round here";

            gameHistory.Refresh();
        }

        [ContextMenu("Clear")]
        public void Clear()
        {
            // Backwards, because destroying a child moves every one after it down a place. Backdrops are
            // children of this object too - a window puts its sheet beside itself, not inside itself - so
            // they go the same way.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;

                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }

            plain = null;
            modal = null;
            statistics = null;
            betInfo = null;
            gameHistory = null;
            fairness = null;
        }
    }
}
