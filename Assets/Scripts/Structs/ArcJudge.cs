namespace ArcCore.Structs
{
    public readonly struct ArcJudge
    {
        public readonly int time;
        public readonly int rawArcIdx;
        public readonly bool isStrict;

        public ArcJudge(int time, int rawArcIdx, bool isStrict)
        {
            this.time = time;
            this.rawArcIdx = rawArcIdx;
            this.isStrict = isStrict;
        }

        public ArcJudge(ArcJudge judge, bool strict)
            : this(judge.time, judge.rawArcIdx, strict) {; }
    }
}