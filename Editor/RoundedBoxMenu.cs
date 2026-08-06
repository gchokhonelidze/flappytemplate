using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FlappyTemplate.Editor
{
    // Puts Rounded Box in GameObject > UI next to Image and Panel, and makes it behave like them: a
    // canvas and an EventSystem appear if the scene has none, the object lands under whatever was
    // right-clicked, and one undo takes the whole lot back out.
    //
    // The steps below are the ones UGUI's own menu runs, rewritten here because its MenuOptions class is
    // internal. Anything that skips them - a plain new GameObject with the component added - leaves the
    // object unparented, at the wrong layer, and outside the undo group.
    public static class RoundedBoxMenu
    {
        // Image and Raw Image sit at 2001 and 2002, Panel at 2020. Landing in the gap puts this in their
        // group rather than off in a section of its own.
        private const int MenuPriority = 2003;

        private const string UiLayerName = "UI";

        // Wide enough to read as a box rather than a swatch, and rounded on arrival so what the component
        // is for is obvious without touching a field. No border: an unwanted one is a nuisance to clear,
        // and Image sets the same precedent by starting as a plain white rect.
        private static readonly Vector2 DefaultSize = new Vector2(200f, 120f);
        private const float DefaultRadius = 16f;

        [MenuItem("GameObject/UI (Canvas)/Rounded Box", false, MenuPriority)]
        private static void CreateRoundedBox(MenuCommand command)
        {
            var box = CreateBox(command);

            // Collapses the canvas, the EventSystem and the box itself into one entry, so a single undo
            // leaves the scene as it was found.
            Undo.SetCurrentGroupName("Create Rounded Box");
            Selection.activeGameObject = box;
        }

        [MenuItem("GameObject/UI (Canvas)/Rounded Box with Image", false, MenuPriority + 1)]
        private static void CreateRoundedBoxWithImage(MenuCommand command)
        {
            var box = CreateBox(command);
            AddMaskedImage(box.GetComponent<RoundedBox>());

            Undo.SetCurrentGroupName("Create Rounded Box with Image");
            Selection.activeGameObject = box;
        }

        private static GameObject CreateBox(MenuCommand command)
        {
            var parent = ResolveParent(command.context as GameObject);

            // ObjectFactory rather than new GameObject: it registers the creation for undo and applies
            // whatever component defaults the project has set up.
            // CanvasRenderer named outright rather than left to RequireComponent: without one the graphic
            // has nothing to draw through, and it cannot be put back from the inspector afterwards.
            var box = ObjectFactory.CreateGameObject("Rounded Box", typeof(RectTransform), typeof(CanvasRenderer), typeof(RoundedBox));
            box.GetComponent<RoundedBox>().SetCornerRadius(DefaultRadius);

            var rectTransform = (RectTransform)box.transform;
            rectTransform.sizeDelta = DefaultSize;

            GameObjectUtility.EnsureUniqueNameForSibling(box);
            Parent(box, parent);
            return box;
        }

        /// <summary>Builds the rig that clips a picture to a box's rounded corners, and returns the image.</summary>
        // Three parts have to agree for this to look right, which is why it is a button rather than a note
        // in the documentation: the Mask that does the clipping, the image beneath it, and the overlay that
        // puts the border back on top of the image the mask just let through.
        public static GameObject AddMaskedImage(RoundedBox box)
        {
            if (box == null)
                return null;

            var mask = box.GetComponent<Mask>();
            if (mask == null)
                mask = Undo.AddComponent<Mask>(box.gameObject);

            // The box has to keep drawing itself: its fill is what the picture sits on, and with this off
            // the mask writes a stencil and no colour at all.
            if (!mask.showMaskGraphic)
            {
                Undo.RecordObject(mask, "Show Mask Graphic");
                mask.showMaskGraphic = true;
            }

            var image = ObjectFactory.CreateGameObject("Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.SetTransformParent(image.transform, box.transform, string.Empty);
            Stretch((RectTransform)image.transform);
            image.layer = box.gameObject.layer;
            GameObjectUtility.EnsureUniqueNameForSibling(image);

            EnsureBorderOverlay(box);

            Undo.SetCurrentGroupName("Add Masked Image");
            return image;
        }

        /// <summary>Adds the child that draws this box's border above its content, or returns the existing one.</summary>
        // One overlay per box is enough - it draws the whole border - so a second call finds the first
        // rather than stacking another frame on top of it.
        public static GameObject EnsureBorderOverlay(RoundedBox box)
        {
            foreach (var existing in box.GetComponentsInChildren<RoundedBoxBorderOverlay>(true))
            {
                if (existing.Source == box)
                    return existing.gameObject;
            }

            var overlay = ObjectFactory.CreateGameObject("Border", typeof(RectTransform), typeof(CanvasRenderer), typeof(RoundedBox), typeof(RoundedBoxBorderOverlay));
            Undo.SetTransformParent(overlay.transform, box.transform, string.Empty);
            Stretch((RectTransform)overlay.transform);
            overlay.layer = box.gameObject.layer;
            overlay.transform.SetAsLastSibling();

            // Everything on it is copied from the box; this is only so the hierarchy shows the border
            // before the first frame gets a chance to run.
            overlay.GetComponent<RoundedBoxBorderOverlay>().Sync();
            return overlay;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        // Right-clicking anything already under a canvas nests the new box inside it, which is how the
        // rest of the UI menu behaves. Anything else - no selection, or a selection out in the 3D scene -
        // sends it to the canvas instead, since a Graphic outside one draws nothing at all.
        private static GameObject ResolveParent(GameObject context)
        {
            if (context != null && context.GetComponentInParent<Canvas>(true) != null)
                return context;

            return GetOrCreateCanvas();
        }

        private static GameObject GetOrCreateCanvas()
        {
            // Asking the stage rather than the scene is what keeps this working inside prefab mode: the
            // canvas of the prefab being edited is the one that should be found, not one left open in the
            // scene behind it.
            var stage = StageUtility.GetCurrentStageHandle();

            var existing = stage.FindComponentOfType<Canvas>();
            if (existing != null && existing.gameObject.activeInHierarchy)
                return existing.gameObject;

            var canvasObject = ObjectFactory.CreateGameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.layer = LayerMask.NameToLayer(UiLayerName);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            StageUtility.PlaceGameObjectInCurrentStage(canvasObject);

            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                // A prefab stage has no scene root to drop it in, so the canvas goes under the prefab
                // itself - and it gets no EventSystem, which belongs to the scene rather than the asset.
                Undo.SetTransformParent(canvasObject.transform, prefabStage.prefabContentsRoot.transform, string.Empty);
            }
            else
            {
                EnsureEventSystem(stage);
            }

            return canvasObject;
        }

        private static void EnsureEventSystem(StageHandle stage)
        {
            if (stage.FindComponentOfType<EventSystem>() != null)
                return;

            var eventSystem = ObjectFactory.CreateGameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            StageUtility.PlaceGameObjectInCurrentStage(eventSystem);
        }

        // Undo.SetTransformParent rather than a plain parent assignment, so reparenting joins the undo
        // group with everything else; the rect is then zeroed because a RectTransform dropped into a new
        // parent keeps its old anchored position and can land off screen.
        private static void Parent(GameObject child, GameObject parent)
        {
            if (parent == null)
                return;

            Undo.SetTransformParent(child.transform, parent.transform, string.Empty);

            var rectTransform = child.transform as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.localRotation = Quaternion.identity;
                rectTransform.localScale = Vector3.one;
            }

            SetLayerRecursively(child, parent.layer);
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            foreach (Transform child in target.transform)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}
