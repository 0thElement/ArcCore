using System;
using ArcCore.Utility;
using Unity.Mathematics;

namespace ArcCore.Structs
{

    public readonly struct TrackParticleAction
    {
        public enum Type
        {
            TAP = ComboParticleAction.Type.___len,
            HELD,

            ___len
        }

        public readonly Type type;
        public readonly int track;
    }
}
