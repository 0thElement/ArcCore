namespace ArcCore.Gameplay.Utility
{
    public enum JudgeType
    {
        MaxPure,
        LatePure,
        EarlyPure,
        LateFar,
        EarlyFar,
        Lost
    }

    public static class JudgeManage
    {

        public static JudgeType GetType(int timeDifference)
        {
            if (timeDifference > Constants.FarWindow)
                return JudgeType.Lost;
            else if (timeDifference > Constants.PureWindow)
                return JudgeType.EarlyFar;
            else if (timeDifference > Constants.MaxPureWindow)
                return JudgeType.EarlyPure;
            else if (timeDifference > -Constants.MaxPureWindow)
                return JudgeType.MaxPure;
            else if (timeDifference > -Constants.PureWindow)
                return JudgeType.LatePure;
            else if (timeDifference > -Constants.FarWindow)
                return JudgeType.LateFar;
            else return JudgeType.Lost;
        }
    }
}