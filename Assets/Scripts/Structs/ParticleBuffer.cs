using System;
using Unity.Collections;
using Unity.Mathematics;
using ArcCore.Behaviours;
using UnityEngine;

namespace ArcCore.Structs
{
    public struct ParticleBuffer : IDisposable
    {
        private struct TapParticleDesc
        {
            public float2 position;
            public ParticleCreator.JudgeType type;
            public ParticleCreator.JudgeDetail detail;
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

        public void PlayTapParticle(float2 position, ParticleCreator.JudgeType type, ParticleCreator.JudgeDetail detail)
        {
            tapQueue.Enqueue(new TapParticleDesc{position = position, type = type, detail = detail});
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
                ParticleCreator.Instance.TapAt(particleDesc.position, particleDesc.type, particleDesc.detail);
            }

            while (holdQueue.Count > 0)
            {
                HoldParticleDesc particleDesc = holdQueue.Dequeue();
                if (particleDesc.isHoldEnd)
                {
                    ParticleCreator.Instance.DisableLane(particleDesc.lane);
                }
                else
                {
                    ParticleCreator.Instance.HoldAt(particleDesc.lane, particleDesc.isHit);
                }
            }
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