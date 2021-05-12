using Unity.Mathematics;

namespace ArcCore.Structs
{
    public struct ArcCompleteState
    {
        public ArcState state;
        public bool isRed;

        public float redRoll;
        public float cutoff;

        public const float maxRedRoll = 1;
        public const float minRedRoll = 0;
        private const float redRange = maxRedRoll - minRedRoll;

        public ArcCompleteState(ArcState state)
        {
            this.state = state;
            isRed = false;
            redRoll = 0f;
            cutoff = 0f;
        }

        public ArcCompleteState(ArcCompleteState from, ArcState? withState = null, bool? withRed = null)
        {
            state = withState ?? from.state;
            isRed = withRed ?? from.isRed;
            redRoll = from.redRoll;
            cutoff = from.cutoff;
        }

        public ArcCompleteState Copy(ArcState? withState = null, bool? withRed = null)
            => new ArcCompleteState(this, withState, withRed);

        public void Update(float currentBpm, float deltaTime = 0.02f)
        {
            const float dfac = 10;
            float timef = deltaTime * dfac;
            redRoll = math.min(redRoll + (isRed ? -1 : 1) * timef * redRange, maxRedRoll);
        }
    }
}