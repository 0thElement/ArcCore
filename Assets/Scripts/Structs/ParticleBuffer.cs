using System;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using ArcCore.Behaviours;

namespace ArcCore.Structs
{
    [BurstCompile]
    public struct ParticleBuffer : IDisposable
    {
        private struct ParticleDesc
        {
            public float2 position;
            public ParticleCreator.TextParticleType type;

            public ParticleDesc(float2 position, ParticleCreator.TextParticleType type)
            {
                this.position = position;
                this.type = type;
            }

            public override bool Equals(object obj)
            {
                return obj is ParticleDesc other &&
                       position.Equals(other.position) &&
                       type == other.type;
            }

            public override int GetHashCode()
            {
                int hashCode = -797515996;
                hashCode = hashCode * -1521134295 + position.GetHashCode();
                hashCode = hashCode * -1521134295 + type.GetHashCode();
                return hashCode;
            }

            public void Deconstruct(out float2 position, out ParticleCreator.TextParticleType type)
            {
                position = this.position;
                type = this.type;
            }

            public static implicit operator (float2 position, ParticleCreator.TextParticleType type)(ParticleDesc value)
            {
                return (value.position, value.type);
            }

            public static implicit operator ParticleDesc((float2 position, ParticleCreator.TextParticleType type) value)
            {
                return new ParticleDesc(value.position, value.type);
            }
        }
        private NativeQueue<ParticleDesc> queue;
        public ParticleBuffer(Allocator allocator)
        {
            queue = new NativeQueue<ParticleDesc>(allocator);
        }

        public void CreateParticle(float2 position, ParticleCreator.TextParticleType type)
        {
            queue.Enqueue(new ParticleDesc(position, type));
        }

        public void Playback()
        {
            while (queue.Count > 0)
            {
                ParticleDesc particleDesc = queue.Dequeue();
                ParticleCreator.Instance.PlayParticleAt(particleDesc.position, particleDesc.type);
            }
        }

        public bool IsCreated => queue.IsCreated;

        public void Dispose()
        {
            queue.Dispose();
        }
    }
}