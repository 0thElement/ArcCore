using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public struct EntityReference : IComponentData
    {
        public Entity Value;
    }
}
