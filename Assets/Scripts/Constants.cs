namespace ArcCore
{
    public static class Constants
    {

        public const float InputMaxY =  5.5f;
        public const float InputMinX = -8.5f;
        public const float InputMaxX =  8.5f;
        public const float ArcYZero  =  1f;
        public const float RenderFloorPositionRange = 150f; //CHECK: IS THIS THE LEN OF THE FLOOR PRECISELY?

        public const int MaxPureWindow = 25;
        public const int PureWindow    = 50;
        public const int FarWindow     = 100;
        public const int LostWindow    = 120;

        public const float LaneWidth     = 2.125f;
        public const float LaneFullwidth = LaneWidth * 2;

        public const float LaneFullwidthRecip = 1 / LaneFullwidth;

        public const float ArcColliderRadius = 1.5f;
    }
}
