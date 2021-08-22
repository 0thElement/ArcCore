using Unity.Entities;

namespace ArcCore.Gameplay.Systems
{
    [UpdateInGroup(typeof(JudgementSystemGroup)), UpdateAfter(typeof(UnlockedHoldJudgeSystem))]
    public class JudgeFinalizeSystem : FinalizeSystemBase
    {
        protected override bool DoParticle => true;
    }
}