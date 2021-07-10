using UnityEngine;
using ArcCore.Serialization;

namespace ArcCore.Other
{
    public class StartupBehaviour : MonoBehaviour
    {
        public void Awake()
        {
            //Setup files.
            FileManagement.OnAppStart();

            //Delete this gameObject.
            Destroy(gameObject);
        }
    }
}