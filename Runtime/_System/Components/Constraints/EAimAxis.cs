namespace FlappyTemplate
{
    // A local axis of the constrained object, used by LookAtConstraint to say which way "at the target"
    // and "up" point on this particular art. A 3D model usually aims down Forward; a sprite drawn facing
    // the camera aims Up or Right instead, because its Forward points out of the screen.
    public enum EAimAxis
    {
        Forward,
        Back,
        Up,
        Down,
        Right,
        Left,
    }
}
