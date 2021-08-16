using Unity.Entities;

namespace ArcCore.Gameplay.Systems
{
    public class SetupSystemBase : SystemBase
    {
        protected virtual bool DoParticle => false;
        protected override void OnUpdate()
        {
            PlayManager.CreateBuffer();
            if(DoParticle)
            {
                PlayManager.CreateParticleBuffer();
            }
        }
    }
}