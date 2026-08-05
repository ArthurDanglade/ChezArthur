using UnityEngine;

namespace ChezArthur.Gameplay.Feedback
{
    /// <summary>
    /// Haptique combat (D6) — Android d'abord, no-op éditeur / iOS.
    /// Toggle Prefs <c>haptics_enabled</c> (défaut ON) ; pas d'UI dans ce lot (zone MT).
    /// </summary>
    public static class HapticManager
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        public const string PrefsKeyEnabled = "haptics_enabled";

        private const long LightMs = 15L;
        private const int LightAmplitude = 80;
        private const long MediumMs = 30L;
        private const int MediumAmplitude = 140;
        private const long HeavyMs = 60L;
        private const int HeavyAmplitude = 255;

        // ═══════════════════════════════════════════
        // CACHE ANDROID
        // ═══════════════════════════════════════════
        private static bool _vibratorResolved;
        private static AndroidJavaObject _vibrator;
        private static int _apiLevel = -1;

        // ═══════════════════════════════════════════
        // API
        // ═══════════════════════════════════════════

        /// <summary>
        /// Joue un pulse haptique. No-op si None, Prefs off, ou hors Android device.
        /// </summary>
        public static void Play(FeedbackBundle.HapticLevel level)
        {
            if (level == FeedbackBundle.HapticLevel.None)
                return;

#if UNITY_EDITOR || !UNITY_ANDROID
            return;
#else
            if (PlayerPrefs.GetInt(PrefsKeyEnabled, 1) == 0)
                return;

            long ms;
            int amplitude;
            switch (level)
            {
                case FeedbackBundle.HapticLevel.Light:
                    ms = LightMs;
                    amplitude = LightAmplitude;
                    break;
                case FeedbackBundle.HapticLevel.Medium:
                    ms = MediumMs;
                    amplitude = MediumAmplitude;
                    break;
                case FeedbackBundle.HapticLevel.Heavy:
                    ms = HeavyMs;
                    amplitude = HeavyAmplitude;
                    break;
                default:
                    return;
            }

            try
            {
                AndroidJavaObject vibrator = GetVibrator();
                if (vibrator == null)
                    return;

                if (GetApiLevel() >= 26)
                {
                    using (AndroidJavaClass effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (AndroidJavaObject effect = effectClass.CallStatic<AndroidJavaObject>(
                        "createOneShot", ms, amplitude))
                    {
                        vibrator.Call("vibrate", effect);
                    }
                }
                else
                {
                    vibrator.Call("vibrate", ms);
                }
            }
            catch (System.Exception)
            {
                // Jamais de crash haptique.
            }
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject GetVibrator()
        {
            if (_vibratorResolved)
                return _vibrator;

            _vibratorResolved = true;
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
            }
            catch (System.Exception)
            {
                _vibrator = null;
            }

            return _vibrator;
        }

        private static int GetApiLevel()
        {
            if (_apiLevel >= 0)
                return _apiLevel;

            try
            {
                using (AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION"))
                    _apiLevel = version.GetStatic<int>("SDK_INT");
            }
            catch (System.Exception)
            {
                _apiLevel = 0;
            }

            return _apiLevel;
        }
#endif
    }
}
