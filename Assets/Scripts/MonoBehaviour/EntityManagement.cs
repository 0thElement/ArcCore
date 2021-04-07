using ArcCore.Data;
using System.Collections;
using Unity.Entities;
using UnityEngine;

namespace ArcCore.MonoBehaviours
{
    public class EntityManagement : MonoBehaviour
    {
        private static World world;
        private static GameObjectConversionSettings gocSettings;

        public static EntityManager entityManager { get; private set; }

        public EntityArchetype arcJudgeArchetype;

        private void Awake()
        {
            world = World.DefaultGameObjectInjectionWorld;
            entityManager = world.EntityManager;
            gocSettings = GameObjectConversionSettings.FromWorld(world, null);

            arcJudgeArchetype = entityManager.CreateArchetype(
                ComponentType.ReadOnly<ChartTimeSpan>(),
                ComponentType.ReadOnly<ColorID>(),
                ComponentType.ReadOnly<ArcFunnelPtr>(),
                typeof(AppearTime),
                typeof(Disabled),
                ComponentType.ChunkComponent<ChunkAppearTime>(),
                ComponentType.ReadOnly<StrictArcJudge>()
                );
        }

        public static Entity GameObjectToEntity(GameObject obj)
            => GameObjectConversionUtility.ConvertGameObjectHierarchy(obj, gocSettings);
    }
}