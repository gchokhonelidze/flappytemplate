using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // A label the player can select and copy from, and not type into. Goes on any TextMeshPro label:
    //
    //     UiSelectableText.On(seedLabel);
    //
    // and the label keeps everything it had - its text, its style, its place in a grid. A read-only
    // TMP_InputField was the other way to get this, and the wrong one here: it needs a viewport and a caret
    // object beside the text, which a UiGrid cell hides; it writes its own copy of the text over the label's
    // whenever it takes focus; and it takes every drag, so a finger meant to scroll a dialog selects instead.
    //
    // With a mouse: drag to select, double-click for the whole value. Shift-click extends. Ctrl+C or Cmd+C
    // copies, Ctrl+A selects the lot.
    // With a finger, a drag still scrolls whatever is behind the label, and holding still for half a second
    // selects the whole value and copies it on release, since a phone has no Ctrl+C to press.
    //
    // A click anywhere else drops the selection. That rides on the EventSystem's selected object, the same
    // thing that says which input field the keyboard goes to, so it is also what tells Hotkeys to stand back
    // while a value is selected.
    [AddComponentMenu("UI/Selectable Text")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class UiSelectableText : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler,
        ISelectHandler, IDeselectHandler
    {
        [Tooltip("The tint laid over selected text. Drawn over the glyphs rather than behind them, so keep it see-through.")]
        [SerializeField]
        private Color highlightColor = new Color(0.33f, 0.6f, 1f, 0.42f);

        [Tooltip("On a touch screen, how long a finger has to stay still on the label to select all of it - and copy it, when the finger lifts. Zero turns that off.")]
        [Min(0f)]
        [SerializeField]
        private float longPressSeconds = 0.5f;

        [Tooltip("Something was copied, with what. A fair place for a \"Copied\" toast.")]
        [SerializeField]
        private UnityEvent<string> onCopied = new UnityEvent<string>();

        // Whoever last told the page what Ctrl+C copies, so a label losing its selection only withdraws its own
        // offer and not one another label has made since.
        private static UiSelectableText offering;

        private TextMeshProUGUI label;
        private UiSelectionHighlight highlight;

        // Character positions, in TextMeshPro's characterInfo - the glyphs as laid out, rich text tags already
        // gone. A position is the gap before that character, so 0 is the start and characterCount the end.
        private int anchor;
        private int caret;

        // The text the selection was made in. Anything else written to the label makes the positions meaningless,
        // and the selection goes with it.
        private string selectedIn;

        // Two presses this close in time and place are a double-click. Counted here rather than read from
        // PointerEventData.clickCount, which the Input System's UI module only raises when the button comes back
        // up - so on the second press of a double-click it still says 1.
        private const float DoubleClickSeconds = 0.4f;
        private const float DoubleClickDistance = 8f;

        private float lastPressTime = float.NegativeInfinity;
        private Vector2 lastPressPosition;

        // The press that selected everything: a mouse that wobbles on the second click of a double-click would
        // otherwise drag the selection back down to a few characters.
        private bool pressSelectedAll;

        private bool touchDown;
        private bool touchMoved;
        private bool touchSelected;
        private float touchStart;

        /// <summary>The label this selects from.</summary>
        public TextMeshProUGUI Label => label != null ? label : label = GetComponent<TextMeshProUGUI>();

        public Color HighlightColor
        {
            get => highlightColor;
            set
            {
                highlightColor = value;
                if (highlight != null)
                    highlight.color = value;
            }
        }

        public float LongPressSeconds
        {
            get => longPressSeconds;
            set => longPressSeconds = Mathf.Max(0f, value);
        }

        /// <summary>Something was copied, with what.</summary>
        public UnityEvent<string> OnCopied => onCopied;

        /// <summary>Whether any of the text is selected.</summary>
        public bool HasSelection => anchor != caret;

        /// <summary>What is selected, as the player reads it: rich text tags left out, a wrapped line joined
        /// back up. Empty with nothing selected.</summary>
        public string SelectedText
        {
            get
            {
                if (!HasSelection || Label == null)
                    return string.Empty;

                var info = Label.textInfo;
                int start = Mathf.Clamp(Mathf.Min(anchor, caret), 0, info.characterCount);
                int end = Mathf.Clamp(Mathf.Max(anchor, caret), 0, info.characterCount);

                var text = new StringBuilder(end - start);
                for (int i = start; i < end; i++)
                    text.Append(info.characterInfo[i].character);

                return text.ToString();
            }
        }

        /// <summary>Puts the component on a label, or finds the one already there.</summary>
        public static UiSelectableText On(TextMeshProUGUI target)
        {
            if (target == null)
                return null;

            var selectable = target.GetComponent<UiSelectableText>();
            if (selectable == null)
                selectable = target.gameObject.AddComponent<UiSelectableText>();

            target.raycastTarget = true;
            return selectable;
        }

        /// <summary>Selects between two character positions, either way round.</summary>
        public void Select(int from, int to)
        {
            int count = Count();
            anchor = Mathf.Clamp(from, 0, count);
            caret = Mathf.Clamp(to, 0, count);
            Changed();
        }

        /// <summary>Selects every character.</summary>
        public void SelectAll() => Select(0, Count());

        /// <summary>Drops the selection.</summary>
        public void ClearSelection()
        {
            anchor = caret = 0;
            selectedIn = null;

            if (highlight != null)
                highlight.SetVerticesDirty();

            if (offering == this)
            {
                offering = null;
                UiClipboard.Offer(null);
            }
        }

        /// <summary>Copies the selection - all of the text when nothing is selected. A browser only allows it
        /// close to a press or a click, so call it from one.</summary>
        public bool Copy()
        {
            string text = HasSelection ? SelectedText : Parsed();
            if (!UiClipboard.Copy(text))
                return false;

            onCopied.Invoke(text);
            return true;
        }

        // ------------------------------------------------------------------ unity

        void OnEnable()
        {
            if (Label != null)
                Label.raycastTarget = true;
        }

        void OnDisable()
        {
            ClearSelection();
            touchDown = false;
        }

        void OnDestroy()
        {
            if (highlight != null)
                UiWindowParts.Discard(highlight.gameObject);
        }

        void Update()
        {
            // Rewritten under the selection - the dialog pointed at another bet. Positions into the old text
            // would select some arbitrary stretch of the new one.
            if (HasSelection && Label != null && Label.text != selectedIn)
                ClearSelection();

            if (touchDown && !touchMoved && !touchSelected && longPressSeconds > 0f
                && Time.unscaledTime - touchStart >= longPressSeconds)
            {
                touchSelected = true;
                Focus(null);
                SelectAll();
            }

            if (HasSelection)
            {
                ReadKeys();

                // The label can move under the selection - wrap differently on a resize, or be restyled. A
                // handful of quads, so redrawn every frame it is showing rather than tracked.
                if (highlight != null)
                    highlight.SetVerticesDirty();
            }
        }

        // ------------------------------------------------------------------ the pointer

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || Label == null)
                return;

            if (IsTouch(eventData))
            {
                touchDown = true;
                touchMoved = false;
                touchSelected = false;
                touchStart = Time.unscaledTime;
                return;
            }

            Focus(eventData);

            int at = IndexAt(eventData);

            float now = Time.unscaledTime;
            bool repeat = now - lastPressTime <= DoubleClickSeconds
                && (eventData.position - lastPressPosition).sqrMagnitude <= DoubleClickDistance * DoubleClickDistance;

            lastPressTime = now;
            lastPressPosition = eventData.position;
            pressSelectedAll = repeat;

            // Double-click takes the whole value, not a word: what a player selects here is a seed or an id to
            // paste somewhere, and the caption in front of the id is the one space-separated part they never want
            // - so a word would be the same as all of it, except where it is wrong. A third click is a repeat of
            // the second and selects it all again.
            if (repeat)
                SelectAll();
            else if (Shift() && HasSelection)
                Select(anchor, at);
            else
                Select(at, at);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!touchDown)
                return;

            touchDown = false;

            // Copied on the lift rather than when the selection appeared: a browser counts the lift as the
            // player doing something, and only lets a page write the clipboard close to that.
            if (touchSelected && !touchMoved)
                Copy();
        }

        // Every drag is offered to whatever is behind the label first, which on a scrolling body is the
        // ScrollRect: this is what stops a flick that is already running when the finger lands.
        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            var behind = Behind<IInitializePotentialDragHandler>();
            if (behind != null)
                ExecuteEvents.Execute(behind, eventData, ExecuteEvents.initializePotentialDrag);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!IsTouch(eventData))
                return;

            // A finger dragging is scrolling, not selecting. The drag is handed to whatever is behind the label
            // and stays there: the input module sends the rest of it to pointerDrag.
            touchMoved = true;

            var behind = Behind<IBeginDragHandler>();
            if (behind == null)
                return;

            eventData.pointerDrag = behind;
            ExecuteEvents.Execute(behind, eventData, ExecuteEvents.beginDragHandler);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (IsTouch(eventData) || eventData.button != PointerEventData.InputButton.Left || pressSelectedAll)
                return;

            Select(anchor, IndexAt(eventData));
        }

        public void OnEndDrag(PointerEventData eventData)
        {
        }

        // Nothing to do on being selected - but answering it is what keeps a second click on the same label
        // from deselecting it first. The input module drops the selection on a press over anything that does
        // not take selection itself, which would clear the text a shift-click is about to extend.
        public void OnSelect(BaseEventData eventData)
        {
        }

        public void OnDeselect(BaseEventData eventData) => ClearSelection();

        // ------------------------------------------------------------------ the keyboard

        private void ReadKeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !Focused())
                return;

            bool command = keyboard.ctrlKey.isPressed || keyboard.leftMetaKey.isPressed || keyboard.rightMetaKey.isPressed;
            if (!command)
                return;

            if (keyboard.aKey.wasPressedThisFrame)
                SelectAll();

            if (!keyboard.cKey.wasPressedThisFrame)
                return;

            // In a browser the page has already copied it, inside the key press, from what Changed offered -
            // all that is left here is to say so.
            if (UiClipboard.PageCopies)
                onCopied.Invoke(SelectedText);
            else
                Copy();
        }

        private static bool Shift()
        {
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.shiftKey.isPressed;
        }

        // ------------------------------------------------------------------ the highlight

        // One rectangle a line, from the left of the first selected character on it to the right of the last,
        // as tall as the line. Called by the highlight while the canvas rebuilds it.
        internal void FillHighlight(VertexHelper vh, Color32 color)
        {
            if (!HasSelection || Label == null)
                return;

            var info = Label.textInfo;
            int start = Mathf.Clamp(Mathf.Min(anchor, caret), 0, info.characterCount);
            int end = Mathf.Clamp(Mathf.Max(anchor, caret), 0, info.characterCount);

            int line = -1;
            float left = 0f;
            float right = 0f;

            for (int i = start; i < end; i++)
            {
                var character = info.characterInfo[i];

                // origin and xAdvance rather than the glyph's box: a space has no box, and a selection that
                // skipped the gaps between words would read as a row of separate words.
                float from = character.origin;
                float to = character.xAdvance;

                if (character.lineNumber != line)
                {
                    if (line >= 0)
                        Quad(vh, info, line, left, right, color);

                    line = character.lineNumber;
                    left = Mathf.Min(from, to);
                    right = Mathf.Max(from, to);
                }
                else
                {
                    left = Mathf.Min(left, Mathf.Min(from, to));
                    right = Mathf.Max(right, Mathf.Max(from, to));
                }
            }

            if (line >= 0)
                Quad(vh, info, line, left, right, color);
        }

        private static void Quad(VertexHelper vh, TMP_TextInfo info, int line, float left, float right, Color32 color)
        {
            if (line >= info.lineCount || right <= left)
                return;

            var lineInfo = info.lineInfo[line];
            float top = lineInfo.ascender;
            float bottom = lineInfo.descender;

            int first = vh.currentVertCount;
            vh.AddVert(new Vector3(left, bottom), color, Vector4.zero);
            vh.AddVert(new Vector3(left, top), color, Vector4.zero);
            vh.AddVert(new Vector3(right, top), color, Vector4.zero);
            vh.AddVert(new Vector3(right, bottom), color, Vector4.zero);
            vh.AddTriangle(first, first + 1, first + 2);
            vh.AddTriangle(first + 2, first + 3, first);
        }

        // Laid exactly over the label, pivot and all, so a point in the label's units is the same point in the
        // highlight's. Never saved: it is made again the next time anything is selected.
        private void EnsureHighlight()
        {
            if (highlight != null)
                return;

            var made = new GameObject("Selection", typeof(RectTransform), typeof(CanvasRenderer), typeof(UiSelectionHighlight));
            made.hideFlags = HideFlags.DontSave;

            var rect = (RectTransform)made.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = Label.rectTransform.pivot;

            highlight = made.GetComponent<UiSelectionHighlight>();
            highlight.Owner = this;
            highlight.raycastTarget = false;
            highlight.color = highlightColor;
        }

        // ------------------------------------------------------------------ small change

        private void Changed()
        {
            selectedIn = Label.text;

            EnsureHighlight();
            highlight.rectTransform.pivot = Label.rectTransform.pivot;
            highlight.SetVerticesDirty();

            if (!Focused())
                return;

            // Said to the page now, so the copy can happen inside the key press when it comes.
            if (HasSelection)
            {
                offering = this;
                UiClipboard.Offer(SelectedText);
            }
            else if (offering == this)
            {
                offering = null;
                UiClipboard.Offer(null);
            }
        }

        // The gap nearest the pointer: before the character it is over, or after it when it is over that
        // character's right half. Worked out here rather than with TMP_TextUtilities.GetCursorIndexFromPosition,
        // whose search along a line stops one short of the line's last character - so the last character of a
        // value could never be reached by a drag.
        private int IndexAt(PointerEventData eventData)
        {
            var info = Label != null ? Label.textInfo : null;
            int count = info != null ? info.characterCount : 0;
            if (count == 0 || info.lineCount == 0)
                return 0;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    Label.rectTransform, eventData.position, eventData.pressEventCamera, out var point))
                return caret;

            // The line the pointer is on, or the nearest one above or below it: a drag that runs off the top of
            // the label is on the first line, and off the bottom on the last.
            int line = 0;
            float nearest = float.MaxValue;

            for (int i = 0; i < info.lineCount; i++)
            {
                var lineInfo = info.lineInfo[i];
                if (lineInfo.characterCount == 0)
                    continue;

                float away = point.y > lineInfo.ascender ? point.y - lineInfo.ascender
                    : point.y < lineInfo.descender ? lineInfo.descender - point.y
                    : 0f;

                if (away < nearest)
                {
                    nearest = away;
                    line = i;
                }
            }

            var on = info.lineInfo[line];
            int first = Mathf.Clamp(on.firstCharacterIndex, 0, count - 1);
            int last = Mathf.Clamp(on.lastCharacterIndex, first, count - 1);

            for (int i = first; i <= last; i++)
            {
                var character = info.characterInfo[i];
                if (point.x < (character.origin + character.xAdvance) * 0.5f)
                    return i;
            }

            // Past the end of the line: after its last character - but before a line break, so a drag to the end
            // of one line does not take the start of the next with it.
            return info.characterInfo[last].character == '\n' ? last : Mathf.Min(last + 1, count);
        }

        private int Count() => Label != null && Label.textInfo != null ? Label.textInfo.characterCount : 0;

        private string Parsed() => Label != null ? Label.GetParsedText() : string.Empty;

        private void Focus(PointerEventData eventData)
        {
            var events = EventSystem.current;
            if (events != null && events.currentSelectedGameObject != gameObject)
                events.SetSelectedGameObject(gameObject, eventData);
        }

        private bool Focused()
        {
            var events = EventSystem.current;
            return events != null && events.currentSelectedGameObject == gameObject;
        }

        private GameObject Behind<T>() where T : IEventSystemHandler
        {
            return transform.parent != null ? ExecuteEvents.GetEventHandler<T>(transform.parent.gameObject) : null;
        }

        private static bool IsTouch(PointerEventData eventData)
        {
            return eventData is ExtendedPointerEventData extended && extended.pointerType == UIPointerType.Touch;
        }
    }
}
