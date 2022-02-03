using ArcCore.Gameplay.Parsing.Data;
using static Unity.Mathematics.math;
namespace ArcCore.Gameplay.Parsing
{
    public static class Easing
    {
        #region private math
        static float sq(float x) => x * x;
        static float cb(float x) => x * x * x;
        static float qr(float x) => sq(sq(x));
        static float qn(float x) => qr(x) * x;
        #endregion

        #region easings
        public static float SineIn(float x) => 1 - cos(x * PI / 2);
        public static float SineOut(float x) => sin(x * PI / 2);
        public static float SineIO(float x) => (1 - cos(PI * x)) / 2;

        public static float QuadIn(float x) => sq(x);
        public static float QuadOut(float x) => 1 - sq(1 - x);
        public static float QuadIO(float x) => (x < 0.5) ? QuadIn(2*x) / 2 : (1 - QuadOut(-x*2+2) / 2);
        public static float CubicIn(float x) => cb(x);
        public static float CubicOut(float x) => 1 - cb(1 - x);
        public static float CubicIO(float x) => (x < 0.5) ? CubicOut(2*x) / 2 : CubicOut(-x*2+2) / 2;
        public static float QuartIn(float x) => qr(x);
        public static float QuartOut(float x) => 1 - qr(1 - x);
        public static float QuartIO(float x) => (x < 0.5) ? QuartIn(2*x) / 2 : (1 - QuartOut(-x*2+2)) / 2;
        public static float QuintIn(float x) => qn(x);
        public static float QuintOut(float x) => 1 - qn(1 - x);
        public static float QuintIO(float x) => (x < 0.5) ? QuintIn(2*x) / 2 : QuintOut(-x*2+2) / 2;

        public static float ExpoIn(float x) => (x == 0) ? (0) : (pow(2, 10 * x - 10));
        public static float ExpoOut(float x) => (x == 1) ? (1) : (1 - pow(2, -10 * x));
        public static float ExpoIO(float x) => (x < 0.5) ? ExpoIn(x * 2) / 2 : ExpoOut(-x * 2 + 2) / 2;

        public static float CircIn(float x) => 1 - sqrt(1 - sq(x));
        public static float CircOut(float x) => sqrt(1 - sq(1 - x));
        public static float CircIO(float x) => (x < 0.5) ? (CircIn(x*2) / 2) : (CircOut(-x*2+2) / 2);


        public const float bc1 = 1.70158f;
        public const float bc2 = bc1 * 1.525f;
        public const float bc3 = bc1 + 1;
        public const float bc4 = bc2 + 1;
        public static float BackIn(float x) => bc3 * cb(x) - bc1 * sq(x);
        public static float BackOut(float x) => 1 + bc3 * cb(x - 1) + bc1 * sq(x - 1);
        public static float BackIO(float x) => (x < 0.5) ? (pow(2 * x, 2) * (bc4 * 2 * x - bc2)) / 2
                                                         : (pow(2 * x - 2, 2) * (bc4 * (x * 2 - 2) + bc2) + 2) / 2;

        public const float ec1 = 2 * PI / 3;
        public const float ec2 = 2 * PI / 4.5f;
        public static float ElasticIn(float x) => x == 0
                                                  ? 0
                                                  : x == 1
                                                  ? 1
                                                  : -pow(2, 10 * x - 10) * sin((x * 10 - 10.75f) * ec1);
        public static float ElasticOut(float x) => x == 0
                                                  ? 0
                                                  : x == 1
                                                  ? 1
                                                  : pow(2, -10 * x) * sin((x * 10 - 0.75f) * ec1) + 1;
        public static float ElasticIO(float x) => x == 0
                                                  ? 0
                                                  : x == 1
                                                  ? 1
                                                  : x < 0.5
                                                  ? -(pow(2, 20 * x - 10) * sin((20 * x - 11.125f) * ec2)) / 2
                                                  : (pow(2, -20 * x + 10) * sin((20 * x - 11.125f) * ec2)) / 2 + 1;

        public static float BounceIn(float x) => 1 - BounceOut(1 - x);
        public static float BounceOut(float x)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (x < 1 / d1)
            {
                return n1 * x * x;
            }
            else if (x < 2 / d1)
            {
                return n1 * (x -= 1.5f / d1) * x + 0.75f;
            }
            else if (x < 2.5 / d1)
            {
                return n1 * (x -= 2.25f / d1) * x + 0.9375f;
            }
            else
            {
                return n1 * (x -= 2.625f / d1) * x + 0.984375f;
            }
        }
        public static float BounceIO(float x) => x < 0.5
                                                  ? (1 - BounceOut(1 - 2 * x)) / 2
                                                  : (1 + BounceOut(2 * x - 1)) / 2;

        public static float InstantIn(float x) => (x == 0) ? 0 : 1;
        public static float InstantOut(float x) => (x == 1) ? 1 : 0;
        #endregion

        public static EasingType FromCameraEase(CameraEasing cameraEasing)
        {
            switch(cameraEasing)
            {
                case CameraEasing.l:
                case CameraEasing.s:
                    return EasingType.Linear;
                case CameraEasing.qi:
                    return EasingType.CubicIn;
                case CameraEasing.qo:
                    return EasingType.CircleOut;
                default:
                    return 0;
            }
        }

        public static bool TryParse(string value, out EasingType result)
        {
            switch (value.ToLower())
            {
                case "linear":
                    result = EasingType.Linear;
                    return true;
                case "sine_in":
                case "sinein":
                    result = EasingType.SineIn;
                    return true;
                case "sine_out":
                case "sineout":
                    result = EasingType.SineOut;
                    return true;
                case "sine_in_out":
                case "sine_inout":
                case "sineinout":
                case "sine_io":
                case "sineio":
                    result = EasingType.SineInOut;
                    return true;
                case "quad_in":
                case "quadin":
                    result = EasingType.QuadIn;
                    return true;
                case "quad_out":
                case "quadout":
                    result = EasingType.QuadOut;
                    return true;
                case "quad_in_out":
                case "quad_inout":
                case "quadinout":
                case "quad_io":
                case "quadio":
                    result = EasingType.QuadInOut;
                    return true;
                case "cubic_in":
                case "cubicin":
                    result = EasingType.CubicIn;
                    return true;
                case "cubic_out":
                case "cubicout":
                    result = EasingType.CubicOut;
                    return true;
                case "cubic_in_out":
                case "cubic_inout":
                case "cubicinout":
                case "cubic_io":
                case "cubicio":
                    result = EasingType.CubicInOut;
                    return true;
                case "quartic_in":
                case "quarticin":
                    result = EasingType.QuarticIn;
                    return true;
                case "quartic_out":
                case "quartiout":
                    result = EasingType.QuarticOut;
                    return true;
                case "quartic_in_out":
                case "quartic_inout":
                case "quarticinout":
                case "quartic_io":
                case "quarticio":
                    result = EasingType.QuarticInOut;
                    return true;
                case "quintic_in":
                case "quinticin":
                    result = EasingType.QuinticIn;
                    return true;
                case "quintic_out":
                case "quinticout":
                    result = EasingType.QuinticOut;
                    return true;
                case "quintic_in_out":
                case "quintic_inout":
                case "quinticinout":
                case "quintic_io":
                case "quinticio":
                    result = EasingType.QuinticInOut;
                    return true;
                case "exponential_in":
                case "exponentialin":
                case "expo_in":
                case "expoin":
                    result = EasingType.ExponentialIn;
                    return true;
                case "exponential_out":
                case "exponentialout":
                case "expo_out":
                case "expoout":
                    result = EasingType.ExponentialOut;
                    return true;
                case "exponential_in_out":
                case "exponential_inout":
                case "exponentialinout":
                case "exponential_io":
                case "exponentialio":
                case "expo_in_out":
                case "expo_inout":
                case "expoinout":
                case "expo_io":
                case "expoio":
                    result = EasingType.ExponentialInOut;
                    return true;
                case "circle_in":
                case "circlein":
                    result = EasingType.CircleIn;
                    return true;
                case "circle_out":
                case "circleout":
                    result = EasingType.CircleOut;
                    return true;
                case "circle_in_out":
                case "circle_inout":
                case "circleinout":
                case "circle_io":
                case "circleio":
                    result = EasingType.CircleInOut;
                    return true;
                case "back_in":
                case "backin":
                    result = EasingType.BackIn;
                    return true;
                case "back_out":
                case "backout":
                    result = EasingType.BackOut;
                    return true;
                case "back_in_out":
                case "back_inout":
                case "backinout":
                case "back_io":
                case "backio":
                    result = EasingType.BackInOut;
                    return true;
                case "elastic_in":
                case "elasticin":
                    result = EasingType.ElasticIn;
                    return true;
                case "elastic_out":
                case "elasticout":
                    result = EasingType.ElasticOut;
                    return true;
                case "elastic_in_out":
                case "elastic_inout":
                case "elasticinout":
                case "elastic_io":
                case "elasticio":
                    result = EasingType.ElasticInOut;
                    return true;
                case "bounce_in":
                case "bouncein":
                    result = EasingType.BounceIn;
                    return true;
                case "bounce_out":
                case "bounceout":
                    result = EasingType.BounceOut;
                    return true;
                case "bounce_in_out":
                case "bounce_inout":
                case "bounceinout":
                case "bounce_io":
                case "bounceio":
                    result = EasingType.BounceInOut;
                    return true;
                case "instant_in":
                case "instantin":
                    result = EasingType.InstantIn;
                    return true;
                case "instant_out":
                case "instantout":
                    result = EasingType.InstantOut;
                    return true;
                default:
                    result = EasingType.Linear;
                    return false;
            }
        }
        public static float Do(float x, EasingType easingType)
        {
            switch(easingType)
            {
                case EasingType.SineIn:
                    return SineIn(x);
                case EasingType.SineOut:
                    return SineOut(x);
                case EasingType.SineInOut:
                    return SineIO(x);
                case EasingType.QuadIn:
                    return QuadIn(x);
                case EasingType.QuadOut:
                    return QuadOut(x);
                case EasingType.QuadInOut:
                    return QuadIO(x);
                case EasingType.CubicIn:
                    return CubicIn(x);
                case EasingType.CubicOut:
                    return CubicOut(x);
                case EasingType.CubicInOut:
                    return CubicIO(x);
                case EasingType.QuarticIn:
                    return QuartIn(x);
                case EasingType.QuarticOut:
                    return QuartOut(x);
                case EasingType.QuarticInOut:
                    return QuartIO(x);
                case EasingType.QuinticIn:
                    return QuintIn(x);
                case EasingType.QuinticOut:
                    return QuintOut(x);
                case EasingType.QuinticInOut:
                    return QuintIO(x);
                case EasingType.ExponentialIn:
                    return ExpoIn(x);
                case EasingType.ExponentialOut:
                    return ExpoOut(x);
                case EasingType.ExponentialInOut:
                    return ExpoIO(x);
                case EasingType.CircleIn:
                    return CircIn(x);
                case EasingType.CircleOut:
                    return CircOut(x);
                case EasingType.CircleInOut:
                    return CircIO(x);
                case EasingType.BackIn:
                    return BackIn(x);
                case EasingType.BackOut:
                    return BackOut(x);
                case EasingType.BackInOut:
                    return BackIO(x);
                case EasingType.ElasticIn:
                    return ElasticIn(x);
                case EasingType.ElasticOut:
                    return ElasticOut(x);
                case EasingType.ElasticInOut:
                    return ElasticIO(x);
                case EasingType.BounceIn:
                    return BounceIn(x);
                case EasingType.BounceOut:
                    return BounceOut(x);
                case EasingType.BounceInOut:
                    return BounceIO(x);
                case EasingType.InstantIn:
                    return InstantIn(x);
                case EasingType.InstantOut:
                    return InstantOut(x);
                default:
                    return x;
            }
        }
    }
}