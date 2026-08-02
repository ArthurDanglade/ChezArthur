using UnityEngine;
using UnityEngine.Audio;

namespace ChezArthur.Audio
{
    /// <summary>
    /// Point d'accès unique au MainMixer (Music / Ambiance / SFX + snapshots de duck).
    /// Null-safe : sans mixer, le jeu reste en mode legacy (volumes par source).
    /// </summary>
    public static class AudioBuses
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string MixerResourceName = "MainMixer";
        private const string ParamMusicVolume = "MusicVolume";
        private const string ParamSfxVolume = "SfxVolume";
        private const string SnapshotNormal = "Normal";
        private const string SnapshotAimFocus = "AimFocus";
        private const float MinDb = -80f;

        // ═══════════════════════════════════════════
        // CACHE STATIQUE
        // ═══════════════════════════════════════════
        private static bool _triedLoad;
        private static bool _warnedMissing;
        private static AudioMixer _mixer;
        private static AudioMixerGroup _musicGroup;
        private static AudioMixerGroup _ambianceGroup;
        private static AudioMixerGroup _sfxGroup;
        private static AudioMixerSnapshot _normalSnapshot;
        private static AudioMixerSnapshot _aimFocusSnapshot;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary> True si le MainMixer a été chargé depuis Resources. </summary>
        public static bool IsAvailable
        {
            get
            {
                EnsureLoaded();
                return _mixer != null;
            }
        }

        public static AudioMixerGroup MusicGroup
        {
            get
            {
                EnsureLoaded();
                return _musicGroup;
            }
        }

        public static AudioMixerGroup AmbianceGroup
        {
            get
            {
                EnsureLoaded();
                return _ambianceGroup;
            }
        }

        public static AudioMixerGroup SfxGroup
        {
            get
            {
                EnsureLoaded();
                return _sfxGroup;
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Applique le volume musique (0–1) sur le paramètre exposé MusicVolume.
        /// </summary>
        public static void SetMusicVolume01(float v)
        {
            EnsureLoaded();
            if (_mixer == null)
                return;

            _mixer.SetFloat(ParamMusicVolume, LinearToDb(v));
        }

        /// <summary>
        /// Applique le volume SFX (0–1) sur le paramètre exposé SfxVolume.
        /// </summary>
        public static void SetSfxVolume01(float v)
        {
            EnsureLoaded();
            if (_mixer == null)
                return;

            _mixer.SetFloat(ParamSfxVolume, LinearToDb(v));
        }

        /// <summary>
        /// Transition vers le snapshot AimFocus (duck musique pendant la visée).
        /// </summary>
        public static void TransitionToAim(float seconds)
        {
            EnsureLoaded();
            if (_aimFocusSnapshot == null)
                return;

            _aimFocusSnapshot.TransitionTo(Mathf.Max(0f, seconds));
        }

        /// <summary>
        /// Transition vers le snapshot Normal (restauration après visée).
        /// </summary>
        public static void TransitionToNormal(float seconds)
        {
            EnsureLoaded();
            if (_normalSnapshot == null)
                return;

            _normalSnapshot.TransitionTo(Mathf.Max(0f, seconds));
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private static void EnsureLoaded()
        {
            if (_triedLoad)
                return;

            _triedLoad = true;
            _mixer = Resources.Load<AudioMixer>(MixerResourceName);

            if (_mixer == null)
            {
                if (!_warnedMissing)
                {
                    _warnedMissing = true;
                    Debug.LogWarning("[AudioBuses] MainMixer introuvable — volumes en mode legacy");
                }

                return;
            }

            _musicGroup = FindFirstGroup("Music");
            _ambianceGroup = FindFirstGroup("Ambiance");
            _sfxGroup = FindFirstGroup("SFX");
            _normalSnapshot = _mixer.FindSnapshot(SnapshotNormal);
            _aimFocusSnapshot = _mixer.FindSnapshot(SnapshotAimFocus);
        }

        private static AudioMixerGroup FindFirstGroup(string name)
        {
            AudioMixerGroup[] groups = _mixer.FindMatchingGroups(name);
            if (groups == null || groups.Length == 0)
                return null;

            return groups[0];
        }

        private static float LinearToDb(float linear)
        {
            float v = Mathf.Clamp01(linear);
            if (v <= 0.0001f)
                return MinDb;

            return 20f * Mathf.Log10(v);
        }
    }
}
