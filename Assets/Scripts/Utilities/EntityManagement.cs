using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Chunk;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace ArcCore.Utilities
{
    public static class EntityManagement
    {
        private static World world;
        private static GameObjectConversionSettings gocSettings;
        private static EntityManager? enManager;

        public static EntityManager EManager
        {
            get
            {
                if (enManager.HasValue) return enManager.Value;
                if (world == null) world = World.DefaultGameObjectInjectionWorld;

                enManager = world.EntityManager;
                return enManager.Value;
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
        public static Entity GameObjectToNote(GameObject obj)
        {
            Entity en = GameObjectToEntity(obj);
            if(EManager.HasComponent<AppearTime>(en))
            {
                EManager.AddChunkComponentData<ChunkAppearTime>(en);
            }
            if (EManager.HasComponent<DisappearTime>(en))
            {
                EManager.AddChunkComponentData<ChunkDisappearTime>(en);
            }
            EManager.AddComponent<Disabled>(en);
            return en;
        }
        public static void ExposeLocalToWorld(Entity en)
        {
            EManager.RemoveComponent<Translation>(en);
            EManager.RemoveComponent<Rotation>(en);
        }
    }
}