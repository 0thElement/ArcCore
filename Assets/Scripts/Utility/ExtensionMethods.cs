// #define DEBUG

using Unity.Entities;

namespace ArcCore.Utility
{
    public static class ExtensionMethods
    {
        public static void DisableEntity(this EntityManager em, Entity entity)
        {
            em.AddComponent<Disabled>(entity);
        }

        public static void EnableEntity(this EntityManager em, Entity entity)
        {
            em.RemoveComponent<Disabled>(entity);
        }

        public static void DisableEntity(this EntityCommandBuffer ec, Entity entity)
        {
            ec.AddComponent<Disabled>(entity);
        }
        public static void EnableEntity(this EntityCommandBuffer ec, Entity entity)
        {
            ec.RemoveComponent<Disabled>(entity);
        }
    }
}