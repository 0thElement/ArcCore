using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using System.Reflection;

namespace ArcCore.Gameplay.Utilities
{
    public static class ECSExtensions
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

        public static Entity ConvertToEntity(this GameObjectConversionSettings gocSettings, GameObject obj)
            => GameObjectConversionUtility.ConvertGameObjectHierarchy(obj, gocSettings);

        public static Entity ConvertToNote(this GameObjectConversionSettings gocSettings, GameObject obj, EntityManager entityManager)
        {
            Entity en = gocSettings.ConvertToEntity(obj);
            entityManager.AddComponent<Disabled>(en);
            entityManager.AddSharedComponentData<ChunkAppearTime>(en, new ChunkAppearTime(0));
            return en;
        }

        public static void ExposeLocalToWorld(this EntityManager entityManager, Entity entity)
        {
            entityManager.RemoveComponent<Translation>(entity);
            entityManager.RemoveComponent<Rotation>(entity);
        }

        public static void SetComponents(this EntityManager entityManager, Entity entity, params IComponentData[] components)
        {
            var setComp = typeof(EntityManager).GetMethod("SetComponentData", BindingFlags.Public | BindingFlags.Instance);
            foreach (var component in components)
            {
                setComp.MakeGenericMethod(component.GetType()).Invoke(entityManager, new object[] { entity, component });
            }
        }
    }
}
