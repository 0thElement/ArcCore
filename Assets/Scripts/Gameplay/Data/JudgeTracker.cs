using Unity.Mathematics;

namespace ArcCore.Gameplay.Data
{
    public readonly struct Floorpos
    {
        public readonly int position;
        public readonly int jumpLevel;

        public const int Shift = 12;
        public const int One = 1 >> 12;

        public float GetRealPositionFromBase(Floorpos basePos)
        {
            return (position - basePos.position) / (float)One;
        }
    }

    public struct JudgeTracker
    {
        public const float MaxBaseScore = 10_000_000f;

        public int combo;
        public int pureChain;
        public int mpureChain;

        public int longestCombo;
        public int longestPureChain;
        public int longestMpureChain;

        public int maxPureCount;
        public int earlyPureCount;
        public int latePureCount;
        public int earlyFarCount;
        public int lateFarCount;
        public int lostCount; 
        
        public int noteCount;

        public JudgeTracker(int noteCount) : this()
        {
            this.noteCount = noteCount;
        }

        public int PureCount => maxPureCount + earlyPureCount + latePureCount;
        public int FarCount => earlyFarCount + lateFarCount;

        public float Score => ((PureCount + 0.5f * FarCount) / noteCount * MaxBaseScore) + maxPureCount;

        public bool IsMaxPureMem => (maxPureCount == noteCount);
        public bool IsPureMem => (PureCount == noteCount);
        public bool IsFullRec => (lostCount == 0);

        private void IncCombo(int i)
        {
            combo += i;
            longestCombo = math.max(combo, longestCombo);
        }

        private void IncPureChain(int i)
        {
            pureChain += i;
            longestPureChain = math.max(pureChain, longestPureChain);
            IncCombo(i);
        }

        private void IncMpureChain(int i)
        {
            mpureChain += i;
            longestMpureChain = math.max(mpureChain, longestMpureChain);
            IncPureChain(i);
        }

        public void AddJudge(JudgeType jtype, int count = 1)
        {
            switch(jtype)
            {
                case JudgeType.MaxPure:

                    maxPureCount += count;
                    IncMpureChain(count);

                    break;

                case JudgeType.EarlyPure:

                    earlyPureCount += count;
                    mpureChain = 0;
                    IncPureChain(count);

                    break;

                case JudgeType.LatePure:

                    latePureCount += count;
                    mpureChain = 0;
                    IncPureChain(count);

                    break;

                case JudgeType.EarlyFar:

                    earlyFarCount += count;
                    mpureChain = 0;
                    pureChain = 0;
                    IncCombo(count);

                    break;

                case JudgeType.LateFar:

                    lateFarCount += count;
                    mpureChain = 0;
                    pureChain = 0;
                    IncCombo(count);

                    break;

                case JudgeType.Lost:

                    lostCount += count;
                    mpureChain = 0;
                    pureChain = 0;
                    combo = 0;

                    break;
            }
        }
    }
}