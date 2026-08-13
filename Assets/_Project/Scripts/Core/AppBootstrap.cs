using UnityEngine;

namespace ChezArthur.Core
{
    /// <summary>
    /// Réglages d'application appliqués avant le chargement de la première scène.
    /// Aucun GameObject requis, fonctionne dans toutes les scènes.
    /// </summary>
    public static class AppBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // Verrouillage à 60 fps. Un tour par tour n'a rien à gagner au-delà,
            // et tout à perdre en batterie et en throttling thermique.
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            // L'écran ne se met pas en veille pendant une partie.
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // UGS (Auth + temps serveur) — jamais bloquant, offline-first (MT4-G1).
            try
            {
                ChezArthur.Backend.BackendService.Initialize();
            }
            catch
            {
                // Silencieux : device offline = expérience actuelle exacte.
            }
        }
    }
}
