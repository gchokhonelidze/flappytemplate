namespace FlappyTemplate
{
    // How the depth value handed to RectTransformPoint3D is read.
    public enum EAnchorDepth
    {
        // An absolute world z plane. The point keeps its z, so a camera that moves in z changes how
        // far away - and how big - the object ends up.
        WorldZ,

        // World units in front of the camera. The point rides along with the camera, so it holds its
        // apparent size no matter where the camera goes.
        DistanceFromCamera,
    }
}
