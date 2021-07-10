using Unity.Entities;

namespace ArcCore.Gameplay.Components
{
    public struct ArcColorID : ISharedComponentData
    {
        /// <summary>
        /// The colorId of this arc
        /// </summary>
        public int id;

        public ArcColorID(int id)
        {
            this.id = id;
        }
    }
}
