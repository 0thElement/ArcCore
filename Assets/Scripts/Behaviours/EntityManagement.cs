using Unity.Entities;
using UnityEngine;

namespace ArcCore.Behaviours
{
    public static class EntityManagement
    {
        private static World world;
        private static GameObjectConversionSettings gocSettings;
        private static EntityManager? entityManager;

        public static EntityManager EntityManager
        {
            get
            {
                if (entityManager.HasValue) return entityManager.Value;
                if (world == null) world = World.DefaultGameObjectInjectionWorld;

                entityManager = world.EntityManager;
                return entityManager.Value;
            }
        }

        public static GameObjectConversionSettings GOCSettings
        {
            get
            {
                if (gocSettings != null) return gocSettings;
                if (world == null) world = World.DefaultGameObjectInjectionWorld;
                gocSettings = GameObjectConversionSettings.FromWorld(world, null);
                return gocSettings;
            }
        }

        public static Entity GameObjectToEntity(GameObject obj)
            => GameObjectConversionUtility.ConvertGameObjectHierarchy(obj, GOCSettings);
    }
}