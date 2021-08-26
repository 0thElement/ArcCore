using Unity.Entities;
using UnityEngine;

namespace ArcCore.Utilities.Extensions
{
    public class ECSMonoBehaviour : MonoBehaviour
    {
        private static EntityManager? eman;
        private static GameObjectConversionSettings gocsett;

        /// <summary>
        /// The <see cref="Unity.Entities.EntityManager"/> for this mono-behaviour.
        /// </summary>
        public static EntityManager EntityManager
        {
            get
            {
                if(eman is null)
                {
                    eman = World.DefaultGameObjectInjectionWorld.EntityManager;
                }
                return eman.Value;
            }
        }

        /// <summary>
        /// The <see cref="Unity.Entities.GameObjectConversionSettings"/> for this mono-behaviour;
        /// </summary>
        public static GameObjectConversionSettings GameObjectConversionSettings
        {
            get
            {
                if(gocsett is null)
                {
                    gocsett = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, null);
                }
                return gocsett;
            }
        }
    }
}