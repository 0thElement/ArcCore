using System;
using Unity.Collections;
using Unity.Mathematics;
using ArcCore.Behaviours;
using UnityEngine;
using ArcCore.Utility;

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

        public void PlayTapParticle(float2 position, JudgeType type)
        {
            ParticleCreator.JudgeType jt = (ParticleCreator.JudgeType)(-1);
            switch(type)
            {
                case JudgeType.Lost:
                    jt = ParticleCreator.JudgeType.Lost;
                    break;
                case JudgeType.LateFar:
                case JudgeType.EarlyFar:
                    jt = ParticleCreator.JudgeType.Far;
                    break;
                case JudgeType.EarlyPure:
                case JudgeType.LatePure:
                case JudgeType.MaxPure:
                    jt = ParticleCreator.JudgeType.Pure;
                    break;
            }

            //I WANT TO USE SWITCH EXPRESSIONS BUT FUCKING UNITY IS AWFUL AND I WANT TO DIE FUCK
            ParticleCreator.JudgeDetail jd = (ParticleCreator.JudgeDetail)(-1);
            switch(type)
            {
                case JudgeType.Lost:
                case JudgeType.MaxPure:
                    jd = ParticleCreator.JudgeDetail.None;
                    break;
                case JudgeType.EarlyFar:
                case JudgeType.EarlyPure:
                    jd = ParticleCreator.JudgeDetail.Early;
                    break;
                case JudgeType.LateFar:
                case JudgeType.LatePure:
                    jd = ParticleCreator.JudgeDetail.Late;
                    break;
            }

            tapQueue.Enqueue(new TapParticleDesc { position = position, type = jt, detail = jd });
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