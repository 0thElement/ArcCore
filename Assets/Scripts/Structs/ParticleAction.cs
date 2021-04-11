using System;
using ArcCore.Utility;
using Unity.Mathematics;

namespace ArcCore.Structs
{
    public interface IParticleAction
    {
        float3 Position { get; }
        Enum TypeID { get; }
    }

    public readonly struct SkyParticleAction : IParticleAction
    {
        public enum Type
        {
            ARCHELD,
            TAP,

            PURE,
            FAR,
            LOST,

            ___len
        }

        public readonly Type type;
        private readonly float2 position;

        public float3 Position => new float3(position, 0);
        public Enum TypeID => type;
    }

    public readonly struct ComboParticleAction : IParticleAction
    {
        public enum Type
        {
            LATE = SkyParticleAction.Type.___len,
            EARLY,

            ___len
        }

        public readonly Type type;
        private static readonly float3 pos = new float3(0,10f,0); //TEMPORARY

        public float3 Position => pos;
        public Enum TypeID => type;
    }

    public readonly struct TrackParticleAction : IParticleAction
    {
        public enum Type
        {
            TAP = ComboParticleAction.Type.___len,
            HELD,

            ___len
        }

        public readonly Type type;
        public readonly int track;

        public float3 Position => new float3(ArccoreConvert.TrackToX(track),0,0);
        public Enum TypeID => type;
    }
}
