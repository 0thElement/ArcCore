using System;

namespace ArcCore.Storage.Data
{
    [Flags]
    public enum ArccoreInfoType : byte
    {
        Level = 1,
        Pack = 2,
        Partner = 4,

        All = Level | Pack | Partner
    }
}