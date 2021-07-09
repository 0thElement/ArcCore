using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Chunk;
using ArcCore.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace ArcCore.Utilities.Extensions
{
    public static class ECSExtensions
    {
        #region void ?.DisableEntity(Entity), void ?.EnableEntity(Entity)
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
        #endregion

        #region Entity GameObjectConversionSettings.ConvertTo???(GameObject)
        public static Entity ConvertToEntity(this GameObjectConversionSettings gocSettings, GameObject obj)
            => GameObjectConversionUtility.ConvertGameObjectHierarchy(obj, gocSettings);

        public static Entity ConvertToNote(this GameObjectConversionSettings gocSettings, GameObject obj, EntityManager entityManager)
        {
            Entity en = gocSettings.ConvertToEntity(obj);
            if (entityManager.HasComponent<AppearTime>(en))
            {
                entityManager.AddChunkComponentData<ChunkAppearTime>(en);
            }
            if (entityManager.HasComponent<DisappearTime>(en))
            {
                entityManager.AddChunkComponentData<ChunkDisappearTime>(en);
            }
            entityManager.AddComponent<Disabled>(en);
            return en;
        }
        #endregion

        public static void ExposeLocalToWorld(this EntityManager entityManager, Entity entity)
        {
            entityManager.RemoveComponent<Translation>(entity);
            entityManager.RemoveComponent<Rotation>(entity);
        }
    }
}
