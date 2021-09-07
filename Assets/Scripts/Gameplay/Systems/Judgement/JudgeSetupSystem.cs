using Unity.Entities;

namespace ArcCore.Gameplay.Systems
{
    [UpdateInGroup(typeof(JudgementSystemGroup), OrderFirst = true)]
    public class JudgeSetupSystem : SetupSystemBase
    {
        protected override bool DoParticle => true;
    }
}