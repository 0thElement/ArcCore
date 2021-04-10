﻿using Unity.Entities;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public struct HoldIsHeld : IComponentData
    {
        public bool value;
        public HoldIsHeld(bool v) 
            => value = v;
    }
}
