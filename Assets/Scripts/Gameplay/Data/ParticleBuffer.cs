using System;
using Unity.Collections;
using Unity.Mathematics;
using ArcCore.Gameplay.Behaviours;
using UnityEngine;
using ArcCore.Gameplay.Utility;

namespace ArcCore.Gameplay.Data
{
    public struct ParticleBuffer : IDisposable
    {
        private struct TapParticleDesc
        {
            public float2 position;
            public ParticlePool.JudgeType type;
            public ParticlePool.JudgeDetail detail;
            public float textYOffset;
        }

        private struct HoldParticleDesc
        {
            public int lane;
            public bool isHit;
            public bool isHoldEnd;
        }

        private NativeQueue<TapParticleDesc> tapQueue;
        private NativeQueue<HoldParticleDesc> holdQueue;
        // private NativeQueue<TapParticleDesc> arcQueue;

        public ParticleBuffer(Allocator allocator)
        {
            tapQueue = new NativeQueue<TapParticleDesc>(allocator);
            holdQueue = new NativeQueue<HoldParticleDesc>(allocator);
            // arcQueue = new NativeQueue<TapParticleDesc>(allocator);
        }

        public void PlayTapParticle(float2 position, ParticlePool.JudgeType type, ParticlePool.JudgeDetail detail, float textYOffset = 0)
        {
            tapQueue.Enqueue(new TapParticleDesc{position = position, type = type, detail = detail, textYOffset = textYOffset});
        }

        public void PlayTapParticle(float2 position, JudgeType type, float textYOffset = 0)
        {
            ParticlePool.JudgeType jt = (ParticlePool.JudgeType)(-1);
            switch(type)
            {
                case JudgeType.Lost:
                    jt = ParticlePool.JudgeType.Lost;
                    break;
                case JudgeType.LateFar:
                case JudgeType.EarlyFar:
                    jt = ParticlePool.JudgeType.Far;
                    break;
                case JudgeType.EarlyPure:
                case JudgeType.LatePure:
                    jt = ParticlePool.JudgeType.Pure;
                    break;
                case JudgeType.MaxPure:
                    jt = ParticlePool.JudgeType.MaxPure;
                    break;
            }

            //I WANT TO USE SWITCH EXPRESSIONS BUT FUCKING UNITY IS AWFUL AND I WANT TO DIE FUCK
            ParticlePool.JudgeDetail jd = (ParticlePool.JudgeDetail)(-1);
            switch(type)
            {
                case JudgeType.Lost:
                case JudgeType.MaxPure:
                    jd = ParticlePool.JudgeDetail.None;
                    break;
                case JudgeType.EarlyFar:
                case JudgeType.EarlyPure:
                    jd = ParticlePool.JudgeDetail.Early;
                    break;
                case JudgeType.LateFar:
                case JudgeType.LatePure:
                    jd = ParticlePool.JudgeDetail.Late;
                    break;
            }

            tapQueue.Enqueue(new TapParticleDesc { position = position, type = jt, detail = jd , textYOffset = textYOffset});
        }

        public void PlayHoldParticle(int lane, bool isHit)
        {
            holdQueue.Enqueue(new HoldParticleDesc{lane = lane, isHit = isHit, isHoldEnd = false});
        }
        public void DisableLaneParticle(int lane)
        {
            holdQueue.Enqueue(new HoldParticleDesc{lane = lane, isHit = false, isHoldEnd = true});
        }

        public void Playback()
        {
            while (tapQueue.Count > 0)
            {
                TapParticleDesc particleDesc = tapQueue.Dequeue();
                PlayManager.ParticlePool.TapAt(particleDesc.position, particleDesc.type, particleDesc.detail, particleDesc.textYOffset);
            }

            bool[] toDisable = new bool[] {false, false, false, false};
            while (holdQueue.Count > 0)
            {
                HoldParticleDesc particleDesc = holdQueue.Dequeue();
                if (particleDesc.isHoldEnd)
                {
                    toDisable[particleDesc.lane] = true;
                }
                else
                {
                    PlayManager.ParticlePool.HoldAt(particleDesc.lane, particleDesc.isHit);
                }
            }
            for (int i=0; i<4; i++)
                if (toDisable[i])
                    PlayManager.ParticlePool.DisableLane(i);
        }

        public bool IsCreated => tapQueue.IsCreated;

        public void Dispose()
        {
            tapQueue.Dispose();
            holdQueue.Dispose();
            // arcQueue.Dispose();
        }
    }
}