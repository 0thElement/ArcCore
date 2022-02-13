﻿using Unity.Entities;

namespace ArcCore.Gameplay.Systems
{
    public class SetupSystemBase : SystemBase
    {
        protected virtual bool DoParticle => false;
        protected sealed override void OnUpdate()
        {
            if (!PlayManager.IsUpdatingAndActive) return;

            PlayManager.CreateBuffer();
            if(DoParticle)
            {
                PlayManager.CreateParticleBuffer();
            }
        }
    }
}