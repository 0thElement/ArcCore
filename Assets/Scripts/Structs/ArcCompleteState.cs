using Unity.Mathematics;

namespace ArcCore.Structs
{
    public struct ArcCompleteState
    {
        public ArcState state;
        public float redRoll;
        public float cutoff;

        public const float maxRedRoll = 1;
        public const float minRedRoll = 0;
        private const float redRange = maxRedRoll - minRedRoll;

        public ArcCompleteState(ArcState state)
        {
            this.state = state;
            redRoll = 0f;
            cutoff = 0f;
        }

        public ArcCompleteState(ArcCompleteState from, ArcState withState)
        {
            state = withState;
            redRoll = from.redRoll;
            cutoff = from.cutoff;
        }

        public void Update(float currentBpm, float deltaTime = 0.02f)
        {
            const float dfac = 10;
            float timef = deltaTime * dfac;
            switch (state)
            {
                case ArcState.Normal:
                case ArcState.Unheld:
                    redRoll = math.min(redRoll - timef * redRange, minRedRoll);
                    return;

                case ArcState.Red:
                    redRoll = math.min(redRoll + timef * redRange, maxRedRoll);
                    return;
            }
        }
    }
}