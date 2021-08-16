using UnityEngine;
using ArcCore.Serialization;

namespace ArcCore.Other
{
    public class StartupBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void OnAppStart()
        {
            Debug.Log("a");
            //Setup files.
            FileManagement.OnAppStart();
        }
    }
}