using System;
using Unity.Collections;
using Unity.Mathematics;
using ArcCore.Behaviours;

namespace ArcCore.Structs
{
    public struct ParticleBuffer : IDisposable
    {
        private struct ParticleDesc
        {
            public float2 particlePos;
            public ParticleCreator.ParticleType type;

            public ParticleDesc(float2 particlePos, ParticleCreator.ParticleType type)
            {
                this.particlePos = particlePos;
                this.type = type;
            }

            public override bool Equals(object obj)
            {
                return obj is ParticleDesc other &&
                       particlePos.Equals(other.particlePos) &&
                       type == other.type;
            }

            public override int GetHashCode()
            {
                int hashCode = -797515996;
                hashCode = hashCode * -1521134295 + particlePos.GetHashCode();
                hashCode = hashCode * -1521134295 + type.GetHashCode();
                return hashCode;
            }

            public void Deconstruct(out float2 particlePos, out ParticleCreator.ParticleType type)
            {
                particlePos = this.particlePos;
                type = this.type;
            }

            public static implicit operator (float2 particlePos, ParticleCreator.ParticleType type)(ParticleDesc value)
            {
                return (value.particlePos, value.type);
            }

            public static implicit operator ParticleDesc((float2 particlePos, ParticleCreator.ParticleType type) value)
            {
                return new ParticleDesc(value.particlePos, value.type);
            }
        }
        private NativeList<ParticleDesc> values;
        public ParticleBuffer(Allocator allocator)
        {
            values = new NativeList<ParticleDesc>(4, allocator);
        }

        public void CreateParticle(float2 position, ParticleCreator.ParticleType type)
        {
            values.Add(new ParticleDesc(position, type));
        }

        public void Playback()
        {
            for(int i = 1; i < values.Length; i++)
            {
                ParticleCreator.Instance.PlayParticleAt(values[i].particlePos, values[i].type);
            }
        }

        public bool IsCreated => values.IsCreated;

        public void Dispose()
        {
            values.Dispose();
        }
    }
}