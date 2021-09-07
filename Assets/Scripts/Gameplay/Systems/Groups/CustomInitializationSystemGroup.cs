using System.Collections.Generic;
using Unity.Entities;

namespace ArcCore.Gameplay.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class CustomInitializationSystemGroup : ComponentSystemGroup
    {
    }
}