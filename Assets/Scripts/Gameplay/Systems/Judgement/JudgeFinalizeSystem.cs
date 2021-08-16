using Unity.Entities;

namespace ArcCore.Gameplay.Systems
{
    [UpdateInGroup(typeof(JudgementSystemGroup), OrderLast = true)]
    public class JudgeFinalizeSystem : FinalizeSystemBase
    {
        protected override bool DoParticle => true;
    }
}