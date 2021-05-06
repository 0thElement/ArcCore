using System.Collections;
using UnityEngine;
using ArcCore.Structs;

namespace ArcCore.Behaviours
{
    public class ParticleManager : MonoBehaviour
    {
        public static ParticleManager Instance { get; private set; }

        void Start()
        {
            Instance = this;
        }

        void Update()
        {

        }

        public void ParseParticle(IParticleAction particleAction)
        {
            //TODO
        }
    }
}