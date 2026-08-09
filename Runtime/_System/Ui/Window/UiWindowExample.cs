using DG.Tweening;
using TMPro;
using UnityEngine;

namespace FlappyTemplate
{
    // Four windows, built at runtime under whatever this sits on: a plain one, a modal one, the statistics
    // window and the bet info window. Drop it on an empty RectTransform inside a canvas and press play, or use
    // Build Now from the component's context menu to see them without leaving the editor.
    //
    // It is here to be read as much as run. Each window below is one chain, and between them they cover
    // most of what UiWindowBuilder can say - a caption, a backdrop, a transition, a drag, a close.
    //
    // The keys are the other half of it: 1 to 4 open the four windows, and Escape closes whatever is open.
    // Opening a window that is already open does nothing, which is why they can be held down.
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

            if (!Input.GetKeyDown(KeyCode.Escape))
                return;

            // Newest first, so Escape peels them off in the order they went on rather than closing the one
            // underneath and leaving the modal sheet over the top of it.
            if (modal != null && modal.IsOpen)
                modal.Close();
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

            // The plain case: a panel, a caption, a close button, and a drag by that caption. Everything
            // else is the style's defaults.
            plain = UiWindowBuilder.Create(transform, "Plain Window")
                .Size(340f, 220f)
                .At(new Vector2(-240f, 120f))
                .Title("Draggable")
                .Draggable()
                .Done();

            // A modal, and a slide rather than a pop. The sheet behind it swallows every click that misses
            // the window, and takes it down when clicked - which is why this one has no reason to be
            // dragged, and says so.
            modal = UiWindowBuilder.Create(transform, "Modal Window")
                .Size(420f, 260f)
                .Title("Modal")
                .Fill(new Color(0.13f, 0.12f, 0.24f))
                .Border(2f, new Color(0.45f, 0.4f, 0.75f))
                .Corners(20f)
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
        }
    }
}
