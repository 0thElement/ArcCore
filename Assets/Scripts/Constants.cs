#define DebugChart

namespace ArcCore
{
    public static class Constants
    {

        public const float InputMaxY =  5.5f;
        public const float InputMinX = -8.5f;
        public const float InputMaxX =  8.5f;
        public const float ArcYZero  =  1f;
        public const float RenderFloorPositionRange = 150f; //TODO: IS THIS THE LEN OF THE FLOOR PRECISELY?

        public const int MaxPureWindow = 25;
        public const int PureWindow    = 50;
        public const int FarWindow     = 100;
        public const int LostWindow    = 120;

        public const float LaneWidth     = 2.125f;
        public const float LaneFullwidth = LaneWidth * 2;

        public const float LaneFullwidthRecip = 1 / LaneFullwidth;

        public const float ArcColliderRadius = 1.5f;

        public const int HoldLenienceTime = 6; //0.100 seconds
        public const int ArcRedMaxTime = 20;   //0.333 seconds

#if DebugChart
        public static string GetDebugChart() =>
@"
AudioOffset:150
-
timing(0,1962.00,4.00);
(244,2);
(366,3);
(489,2);
(733,1);
(978,4);
(1223,1);
hold(1712,2201,4);
(1712,2);
(1957,3);
(2201,4);
hold(2691,4036,1);
(4159,3);
(4281,2);
(4403,3);
(4648,4);
(4892,1);
(5137,4);
arc(5626,5993,0.00,0.00,b,1.00,1.00,0,none,false);
(5871,2);
arc(6116,6605,1.00,1.00,b,1.00,1.00,1,none,false);
arc(6605,7828,1.00,0.00,si,1.00,1.00,1,none,false);
(6605,3);
arc(7828,8318,0.00,1.00,si,1.00,1.00,1,none,false);
arc(7828,8318,0.00,0.00,b,1.00,1.00,0,none,false);
arc(8318,9296,0.00,0.50,si,1.00,0.00,0,none,true)[arctap(8807),arctap(9296)];
arc(8318,9296,1.00,0.50,si,1.00,0.00,0,none,true);
hold(8318,8685,3);
hold(8318,8685,2);
arc(9296,9785,0.50,1.25,si,0.00,0.50,0,none,true)[arctap(9785)];
arc(9785,10275,1.25,-0.25,si,0.50,0.50,0,none,true)[arctap(10275)];
arc(10275,10764,-0.25,0.00,sisi,0.50,1.00,0,none,true)[arctap(10764)];
arc(10764,11253,0.00,0.50,si,1.00,1.50,0,none,true)[arctap(11253)];
arc(11253,11743,0.50,0.50,b,1.50,-0.50,0,none,true)[arctap(11743)];
(11987,2);
(11987,2);
(12110,3);
(12110,3);
(12232,4);
(12232,4);
arc(12477,12478,0.88,0.88,b,0.00,0.00,1,none,true)[arctap(12477)];
(12477,4);
arc(12721,12722,0.13,0.13,b,0.00,0.00,0,none,true)[arctap(12721)];
(12721,1);
hold(12966,13211,3);
arc(12966,12967,1.00,1.00,b,0.00,0.00,0,none,true)[arctap(12966)];
(12966,4);
arc(13455,13456,0.00,0.00,b,0.00,0.00,1,none,true)[arctap(13455)];
(13455,1);
hold(13455,13700,2);
(13700,1);
hold(13944,14311,4);
hold(13944,14311,1);
arc(13944,14311,-0.25,-0.25,b,0.00,0.00,0,none,false);
arc(13944,14311,1.25,1.25,b,0.00,0.00,1,none,false);
arc(16146,19082,0.00,0.00,b,1.00,1.00,0,none,false);
arc(19082,21039,1.00,1.00,b,1.00,1.00,0,none,false);
arc(21039,22996,0.00,0.50,si,1.00,1.00,0,none,false);
arc(22018,22996,1.00,0.50,si,1.00,1.00,1,none,false);
arc(22996,22996,0.50,-0.38,b,1.00,0.00,0,none,false);
arc(22996,23363,-0.38,-0.38,b,0.00,0.00,0,none,false);
arc(22996,23363,1.38,1.38,b,0.00,0.00,1,none,false);
arc(22996,22996,0.50,1.38,b,1.00,0.00,1,none,false);
".Trim();
#endif

    }
}
