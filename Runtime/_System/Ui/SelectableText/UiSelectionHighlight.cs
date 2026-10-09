using UnityEngine;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // The tint behind selected text - in front of it, strictly, being a child of the label, which is why it is
    // drawn translucent. A graphic of its own rather than a vertex colour on the label's glyphs, so selecting
    // text never touches the mesh TextMeshPro builds, and a label restyled mid-selection keeps its look.
    //
    // Made by UiSelectableText the first time something is selected, never saved, and laid exactly over the
    // label so the two share one coordinate space: the rectangles UiSelectableText hands it are in the
    // label's units and land where the glyphs are.
    [AddComponentMenu("")]
    [RequireComponent(typeof(CanvasRenderer))]
    internal class UiSelectionHighlight : MaskableGraphic
    {
        public UiSelectableText Owner;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (Owner != null)
                Owner.FillHighlight(vh, color);
        }
    }
}
