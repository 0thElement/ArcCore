using UnityEngine;
using ArcCore.UI;
using ArcCore.Storage;

namespace ArcCore
{
    public class StartupBehaviour : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void OnAppStart()
        {
            //Setup files.
            FileManagement.OnAppStart();
        }
    }
}