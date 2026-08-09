using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // The small, dull half of building a window from code: make a child, give it a rect, find the one that
    // is already there. Both windows in this folder are assembled out of these, which is the only reason
    // they are in a file of their own rather than repeated twice.
    //
    // Every maker looks for an existing child of that name first. Building is therefore something that can
    // be run again - on a window that was saved as a prefab, on one whose style changed, on one being
    // rebuilt in the editor - without doubling the objects each time.
    internal static class UiWindowParts
    {
        /// <summary>An existing child of that name, or null. Only the immediate children are looked at.</summary>
        public static T Find<T>(Transform parent, string name) where T : Component
        {
            if (parent == null)
                return null;

            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name != name)
                    continue;

                var found = child.GetComponent<T>();
                if (found != null)
                    return found;
            }

            return null;
        }

        /// <summary>A plain rect, for grouping and for positioning things that draw nothing themselves.</summary>
        public static RectTransform Rect(Transform parent, string name)
        {
            var found = Find<RectTransform>(parent, name);
            if (found != null)
                return found;

            var created = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)created.transform;
            Attach(rect, parent);
            return rect;
        }

        /// <summary>A rounded box. CanvasRenderer is named up front - a Graphic without one draws nothing,
        /// and it cannot be added afterwards from the inspector.</summary>
        public static RoundedBox Box(Transform parent, string name)
        {
            var found = Find<RoundedBox>(parent, name);
            if (found != null)
                return found;

            var created = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RoundedBox));
            Attach((RectTransform)created.transform, parent);
            return created.GetComponent<RoundedBox>();
        }

        public static TextMeshProUGUI Label(Transform parent, string name)
        {
            var found = Find<TextMeshProUGUI>(parent, name);
            if (found != null)
                return found;

            var created = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            Attach((RectTransform)created.transform, parent);

            var label = created.GetComponent<TextMeshProUGUI>();
            label.raycastTarget = false;
            return label;
        }

        public static Image Picture(Transform parent, string name)
        {
            var found = Find<Image>(parent, name);
            if (found != null)
                return found;

            var created = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Attach((RectTransform)created.transform, parent);

            var image = created.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Fills the parent, inset per side. Positive insets always move inwards, on all four.</summary>
        public static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Pins a corner or an edge: anchor, pivot and all. (0,1) is the top left, (1,1) the top
        /// right, (.5,.5) the middle.</summary>
        public static void Pin(RectTransform rect, Vector2 corner, Vector2 size, Vector2 offset)
        {
            rect.anchorMin = corner;
            rect.anchorMax = corner;
            rect.pivot = corner;
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;
        }

        /// <summary>A strip across the top of the parent, measured downwards from it.</summary>
        public static void TopStrip(RectTransform rect, float height, float inset, float top)
        {
            // Anchored to the parent's top edge on both sides, so sizeDelta.x is a width relative to the
            // parent rather than an absolute one: -inset*2 is "the parent, less that much off each side".
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(-inset * 2f, height);
            rect.anchoredPosition = new Vector2(0f, -top);
        }

        /// <summary>The grid on a rect, made if it is not there yet.</summary>
        public static UiGrid Grid(RectTransform rect)
        {
            var grid = rect.GetComponent<UiGrid>();
            if (grid == null)
                grid = rect.gameObject.AddComponent<UiGrid>();

            return grid;
        }

        public static UiGridItem Item(RectTransform rect)
        {
            var item = rect.GetComponent<UiGridItem>();
            if (item == null)
                item = rect.gameObject.AddComponent<UiGridItem>();

            return item;
        }

        /// <summary>Gives a cell the name its grid's layout knows it by, stretched to fill what it is given.</summary>
        // Naming rather than switching on and off, and that is not a preference: a grid takes its layout as the
        // whole truth about which children are showing and re-asserts it whenever it is enabled - so a cell
        // hidden with SetActive comes back on the next time the window opens.
        public static void Name(RectTransform rect, string area)
        {
            var item = Item(rect);
            item.Area = area;
            item.OverrideAlign = false;
        }

        /// <summary>The same, for a cell that keeps its own size and sits in the middle of what it is given.</summary>
        public static void Name(RectTransform rect, string area, Vector2 size)
        {
            var item = Item(rect);
            item.Area = area;
            item.OverrideAlign = true;
            item.HorizontalAlign = EGridAlign.Center;
            item.VerticalAlign = EGridAlign.Center;
            Measured(rect, size);
        }

        /// <summary>Takes something out of the grid's hands: not placed, not sized, and not shown or hidden by
        /// the layout either. For an overlay - a close button, a badge over a corner.</summary>
        public static void Ignore(RectTransform rect, bool ignored)
        {
            var element = rect.GetComponent<LayoutElement>();

            if (element == null)
            {
                if (!ignored)
                    return;

                element = rect.gameObject.AddComponent<LayoutElement>();
            }

            element.ignoreLayout = ignored;
        }

        /// <summary>Sets a rect's size and says so to whatever is measuring it.</summary>
        // An auto track is as big as the items in it say they need to be, and a plain rect says nothing at all,
        // so anything square in a cell - a coin, a tick, a button - would measure as nothing and hang out of
        // its row. A LayoutElement is how a rect answers that question.
        public static void Measured(RectTransform rect, Vector2 size)
        {
            rect.sizeDelta = size;

            var element = rect.GetComponent<LayoutElement>();
            if (element == null)
                element = rect.gameObject.AddComponent<LayoutElement>();

            element.ignoreLayout = false;
            element.minWidth = size.x;
            element.minHeight = size.y;
            element.preferredWidth = size.x;
            element.preferredHeight = size.y;
        }

        /// <summary>Works in edit mode as well, where Destroy would leave the object behind until play.</summary>
        public static void Discard(Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(target);
            else
                Object.DestroyImmediate(target);
        }

        private static void Attach(RectTransform rect, Transform parent)
        {
            if (parent == null)
                return;

            // WorldPositionStays off, or a rect dropped into a scaled canvas keeps its world size and comes
            // out the wrong shape.
            rect.SetParent(parent, false);
            rect.gameObject.layer = parent.gameObject.layer;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }
    }
}
