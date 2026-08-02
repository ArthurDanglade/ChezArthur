#if UNITY_EDITOR
using ChezArthur.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Câble CombatMusic vers le groupe Music du MainMixer (idempotent).
    /// Exécution / commit de scène séparés du code F1-P1 (protocole G6).
    /// </summary>
    public static class AudioSceneRoutingBuilder
    {
        private const string CombatMusicObjectName = "CombatMusic";

        [MenuItem("Chez Arthur/Audio/Câbler Audio Scène Combat")]
        public static void WireCombatMusic()
        {
            AudioMixerGroup musicGroup = AudioBuses.MusicGroup;
            if (musicGroup == null)
            {
                Debug.LogError("[AudioSceneRoutingBuilder] MusicGroup introuvable — créer MainMixer dans Resources d'abord.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[AudioSceneRoutingBuilder] Aucune scène active chargée.");
                return;
            }

            GameObject combatMusic = GameObject.Find(CombatMusicObjectName);
            if (combatMusic == null)
            {
                // Recherche inclusive des objets inactifs.
                Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] == null || all[i].name != CombatMusicObjectName)
                        continue;
                    if (!all[i].gameObject.scene.IsValid() || all[i].gameObject.scene != scene)
                        continue;
                    combatMusic = all[i].gameObject;
                    break;
                }
            }

            if (combatMusic == null)
            {
                Debug.LogWarning("[AudioSceneRoutingBuilder] GameObject CombatMusic introuvable dans la scène active.");
                return;
            }

            AudioSource source = combatMusic.GetComponent<AudioSource>();
            if (source == null)
            {
                Debug.LogWarning("[AudioSceneRoutingBuilder] CombatMusic n'a pas d'AudioSource.");
                return;
            }

            if (source.outputAudioMixerGroup == musicGroup)
            {
                Debug.Log("[AudioSceneRoutingBuilder] Déjà câblé — aucun changement.");
                return;
            }

            Undo.RecordObject(source, "Câbler CombatMusic → Music");
            source.outputAudioMixerGroup = musicGroup;
            EditorUtility.SetDirty(source);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[AudioSceneRoutingBuilder] CombatMusic → groupe Music. Pense à sauver la scène (commit séparé).");
        }
    }
}
#endif
