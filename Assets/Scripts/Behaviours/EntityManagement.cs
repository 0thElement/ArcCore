using Unity.Entities;
using UnityEngine;

namespace ArcCore.Behaviours
{
    public class EntityManagement : MonoBehaviour
    {
        private static World world;
        private static GameObjectConversionSettings gocSettings;

        public static EntityManager EntityManager { get; private set; }

        private void Awake()
        {
            world = World.DefaultGameObjectInjectionWorld;
            EntityManager = world.EntityManager;
            gocSettings = GameObjectConversionSettings.FromWorld(world, null);
        }

        public static Entity GameObjectToEntity(GameObject obj)
            => GameObjectConversionUtility.ConvertGameObjectHierarchy(obj, gocSettings);
    }
}