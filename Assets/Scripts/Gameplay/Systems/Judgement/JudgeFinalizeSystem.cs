using Unity.Entities;


namespace ArcCore.Gameplay.Systems
{
    [UpdateInGroup(typeof(JudgementSystemGroup)), UpdateAfter(typeof(ArcIncrementSystem))]
    public class JudgeFinalizeSystem : FinalizeSystemBase
    {
        protected override bool DoParticle => true;
    }
}