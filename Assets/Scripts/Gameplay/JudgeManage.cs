namespace ArcCore.Gameplay
{

    public static class JudgeManage
    {
        public static JudgeType GetType(int timeDifference)
        {
            if (timeDifference > Constants.FarWindow)
                return JudgeType.Lost;
            else if (timeDifference > Constants.PureWindow)
                return JudgeType.LateFar;
            else if (timeDifference > Constants.MaxPureWindow)
                return JudgeType.LatePure;
            else if (timeDifference > -Constants.MaxPureWindow)
                return JudgeType.MaxPure;
            else if (timeDifference > -Constants.PureWindow)
                return JudgeType.EarlyPure;
            else if (timeDifference > -Constants.FarWindow)
                return JudgeType.EarlyFar;
            else return JudgeType.Lost;
        }
    }
}