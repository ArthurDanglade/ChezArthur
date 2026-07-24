using UnityEngine;

namespace ChezArthur.UI
{
    /// <summary>
    /// Accès safe area / résolution compatible Device Simulator.
    /// En Editor, UnityEngine.Device.Screen renvoie les valeurs du device simulé ;
    /// UnityEngine.Screen peut rester sur le Game view (bleed = 0 → bande caméra).
    /// </summary>
    public static class ScreenSafeArea
    {
        public static Rect SafeArea
        {
            get
            {
#if UNITY_EDITOR
                return UnityEngine.Device.Screen.safeArea;
#else
                return Screen.safeArea;
#endif
            }
        }

        public static int Width
        {
            get
            {
#if UNITY_EDITOR
                return UnityEngine.Device.Screen.width;
#else
                return Screen.width;
#endif
            }
        }

        public static int Height
        {
            get
            {
#if UNITY_EDITOR
                return UnityEngine.Device.Screen.height;
#else
                return Screen.height;
#endif
            }
        }
    }
}
