using System.Collections;
using Unity.Entities;
using UnityEngine;

namespace ArcCore.Utilities.Extensions
{
    public class ECSMonoBehaviour : MonoBehaviour
    {
        /// <summary>
        /// The <see cref="Unity.Entities.EntityManager"/> for this mono-behaviour.
        /// </summary>
        public static EntityManager EntityManager { get; private set; } = World.DefaultGameObjectInjectionWorld.EntityManager;

        /// <summary>
        /// The <see cref="Unity.Entities.GameObjectConversionSettings"/> for this mono-behaviour;
        /// </summary>
        public static GameObjectConversionSettings GameObjectConversionSettings { get; private set; } = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, null);
    }
}