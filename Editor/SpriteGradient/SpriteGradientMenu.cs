using UnityEditor;
using UnityEngine;

namespace FlappyTemplate.Editor
{
    // Puts Sprite Gradient in GameObject > UI (Canvas) > FlappyBet, beside the Rounded Box it is the other
    // half of: one draws a shape and colours it, the other takes a shape somebody drew and colours that.
    //
    // The steps that make a new object behave like Image's own menu entry - a canvas and an EventSystem if
    // the scene has none, the right parent, the right layer, one undo for the lot - live in RoundedBoxMenu
    // and are called from here rather than copied.
    public static class SpriteGradientMenu
    {
        // Two past the box's own entries, so it lands in the same group rather than behind a separator.
        private const int MenuPriority = FlappyBetMenu.Priority + 2;

        // Square, since most of what goes through this is a badge, a glyph or a token rather than a panel.
        private static readonly Vector2 DefaultSize = new Vector2(120f, 120f);

        [MenuItem(FlappyBetMenu.Group + "Sprite Gradient", false, MenuPriority)]
        private static void CreateSpriteGradient(MenuCommand command)
        {
            var parent = RoundedBoxMenu.ResolveParent(command.context as GameObject);

            // CanvasRenderer named outright rather than left to RequireComponent: without one the graphic
            // has nothing to draw through, and it cannot be put back from the inspector afterwards.
            var created = ObjectFactory.CreateGameObject(
                "Sprite Gradient",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(SpriteGradient));

            ((RectTransform)created.transform).sizeDelta = DefaultSize;

            GameObjectUtility.EnsureUniqueNameForSibling(created);
            RoundedBoxMenu.Parent(created, parent);

            // Collapses the canvas, the EventSystem and the graphic itself into one entry, so a single undo
            // leaves the scene as it was found.
            Undo.SetCurrentGroupName("Create Sprite Gradient");
            Selection.activeGameObject = created;
        }
    }
}
