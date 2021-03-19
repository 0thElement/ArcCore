using ArcCore;

public static class JudgeManage
{
    public enum JudgeType
    {
        MAX_PURE,
        LATE_PURE,
        EARLY_PURE,
        LATE_FAR,
        EARLY_FAR,
        LOST
    }

    public static JudgeType GetType(int timeDifference)
    {
        if (timeDifference > Constants.FarWindow)
            return JudgeType.LOST;
        else if (timeDifference > Constants.PureWindow)
            return JudgeType.EARLY_FAR;
        else if (timeDifference > Constants.MaxPureWindow)
            return JudgeType.EARLY_PURE;
        else if (timeDifference > -Constants.MaxPureWindow)
            return JudgeType.MAX_PURE;
        else if (timeDifference > -Constants.PureWindow)
            return JudgeType.LATE_PURE;
        else if (timeDifference > -Constants.FarWindow)
            return JudgeType.LATE_FAR;
        else return JudgeType.LOST;
    }
}
