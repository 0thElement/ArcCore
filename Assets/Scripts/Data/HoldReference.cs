using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

namespace ArcCore.Data
{
    public struct HoldReference : IComponentData
    {
        public Entity Value;
    }
}
