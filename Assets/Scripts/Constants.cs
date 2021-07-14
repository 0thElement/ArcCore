using Unity.Mathematics;

namespace ArcCore
{
    public static class Constants
    {

        public const float InputMaxY =  5.5f;
        public const float InputMinX = -8.5f;
        public const float InputMaxX =  8.5f;
        public const float ArcYZero  =  1f;
        public const float RenderFloorPositionRange = 150f; //TODO: IS THIS THE LEN OF THE FLOOR PRECISELY?

        public const int MaxPureWindow  = 25;
        public const int PureWindow     = 50;
        public const int FarWindow      = 100;
        public const int LostWindow     = 120;
        public const int HoldLostWindow = 250;
        public const int ArcResetTouchWindow = 500;
        public const int ArcRedArcWindow     = 500;

        public const float LaneWidth     = 2.125f;
        public const float LaneFullwidth = LaneWidth * 2;

        public const float LaneFullwidthRecip = 1 / LaneFullwidth;

        public static readonly float2 ArctapBoxExtents = new float2(2.975f, 2.25f);
        public static readonly float2 ArcBoxExtents = new float2(1.955f, 1.8f);

        public static string GetDebugChart() =>
@"
AudioOffset:0
-
timing(0,100.00,4.00);
arc(4200,7200,0.00,0.00,b,1.00,1.00,0,none,true)[arctap(4200)];
hold(7800,10800,4);
arc(9000,9300,0.50,0.50,b,1.00,1.00,0,none,false);
arc(10800,12600,0.00,1.00,si,1.00,1.00,0,none,false);
arc(12600,15000,1.00,0.00,si,0.00,0.00,1,none,false);
arc(15600,15750,0.00,0.00,b,1.00,1.00,0,none,false);
arc(15900,16050,0.00,0.00,b,1.00,1.00,0,none,false);
arc(16200,16350,0.00,0.00,b,1.00,1.00,0,none,false);
arc(16500,16650,0.00,0.00,b,1.00,1.00,0,none,false);
arc(16800,16950,0.00,0.00,b,1.00,1.00,0,none,false);
arc(17100,17250,0.00,0.00,b,1.00,1.00,0,none,false);
arc(17400,17550,0.00,0.00,b,1.00,1.00,0,none,false);
arc(17700,17850,0.00,0.00,b,1.00,1.00,0,none,false);
arc(18000,18150,0.00,0.00,b,1.00,1.00,1,none,false);
arc(18300,18450,0.00,0.00,b,1.00,1.00,1,none,false);
arc(18600,18750,0.00,0.00,b,1.00,1.00,1,none,false);
arc(18900,19050,0.00,0.00,b,1.00,1.00,1,none,false);
arc(19200,19350,0.00,0.00,b,1.00,1.00,1,none,false);
arc(19500,19650,0.00,0.00,b,1.00,1.00,1,none,false);
arc(19800,21000,0.00,1.00,si,1.00,1.00,0,none,false);
arc(21600,22800,1.00,0.00,sisi,1.00,0.00,1,none,false);
arc(23400,24000,-0.25,1.00,sisi,0.00,1.00,0,none,false);
arc(24600,38400,0.00,1.25,sisi,1.00,0.00,0,none,false);
arc(39000,40200,1.00,0.00,sosi,1.00,0.00,1,none,false);
arc(40200,42000,1.00,0.00,b,1.00,0.00,0,none,false);
".Trim();

    }
}
