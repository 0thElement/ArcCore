using Unity.Entities;

namespace ArcCore.Gameplay.Systems
{
    public class FinalizeSystemBase : SystemBase
    {
        protected virtual bool DoParticle => false;
        protected override void OnUpdate()
        {
            PlayManager.PlaybackBuffer();
            if (DoParticle)
            {
                PlayManager.PlaybackParticleBuffer();
            }
        }
    }
}