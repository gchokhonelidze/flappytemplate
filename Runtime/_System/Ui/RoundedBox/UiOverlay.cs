using UnityEngine;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // Something drawn over the box it is parented to - a border repeated above the content, a fire around
    // the frame - is not one of that box's children in any layout sense. It has to say so, because a box
    // with a layout group on it will otherwise treat it as one more thing to arrange.
    //
    // Two things go wrong when it does not. A layout flows it into a cell of its own and stretches it to
    // that instead of to the box, which is how an effect ends up beside or below what it belongs to. And a
    // UiGrid takes its layout as the whole truth about which children are showing, so a child it has no
    // name for is switched off the next time the grid is enabled.
    //
    // A LayoutElement with Ignore Layout on is the answer to both, and is what a UiWindow's own close button
    // wears for exactly this reason.
    internal static class UiOverlay
    {
        /// <summary>Takes an object out of its parent's layout, if the parent has one at all.</summary>
        public static void KeepOutOfLayout(GameObject target)
        {
            if (target == null)
                return;

            var parent = target.transform.parent;
            if (parent == null || parent.GetComponent<LayoutGroup>() == null)
                return;

            var element = target.GetComponent<LayoutElement>();
            if (element == null)
                element = target.AddComponent<LayoutElement>();

            if (!element.ignoreLayout)
                element.ignoreLayout = true;
        }
    }
}
