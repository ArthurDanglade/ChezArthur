using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Données de configuration d'une synergie entre valises.
    /// </summary>
    [CreateAssetMenu(fileName = "New Synergy", menuName = "Chez Arthur/Roguelike/Synergy Data")]
    public class SynergyData : ScriptableObject
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Identité")]
        [SerializeField] private string synergyId;
        [SerializeField] private List<string> requiredValiseIds = new List<string>();

        [Header("Affichage joueur")]
        [SerializeField] private string displayName;
        [SerializeField] [TextArea(2, 4)] private string description;

        [Header("SFX (optionnels — fallback sur les sons globaux du SfxManager)")]
        [SerializeField] private AudioClip activationSfx;
        [SerializeField] private AudioClip deactivationSfx;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary> Identifiant technique unique de la synergie. </summary>
        public string Id => synergyId;

        /// <summary> Identifiants des valises requises (toutes doivent être actives). </summary>
        public IReadOnlyList<string> RequiredValiseIds => requiredValiseIds;

        /// <summary> Nom affiché au joueur (bandeau, menu pause). </summary>
        public string DisplayName => displayName;

        /// <summary> Ligne d'effet affichée au joueur. </summary>
        public string Description => description;

        /// <summary> Son d'activation (optionnel ; null = défaut SfxManager). </summary>
        public AudioClip ActivationSfx => activationSfx;

        /// <summary> Son de désactivation (optionnel ; null = défaut SfxManager). </summary>
        public AudioClip DeactivationSfx => deactivationSfx;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (synergyId != null)
                synergyId = synergyId.Trim();

            if (requiredValiseIds != null)
            {
                for (int i = 0; i < requiredValiseIds.Count; i++)
                {
                    if (requiredValiseIds[i] != null)
                        requiredValiseIds[i] = requiredValiseIds[i].Trim();
                }
            }

            string assetName = name;

            if (string.IsNullOrEmpty(synergyId))
                Debug.LogWarning($"[SynergyData] {assetName} : synergyId est vide.", this);

            if (requiredValiseIds == null || requiredValiseIds.Count < 2)
            {
                Debug.LogWarning(
                    $"[SynergyData] {assetName} : requiredValiseIds doit contenir au moins 2 entrées.",
                    this);
            }
            else
            {
                for (int i = 0; i < requiredValiseIds.Count; i++)
                {
                    if (string.IsNullOrEmpty(requiredValiseIds[i]))
                    {
                        Debug.LogWarning(
                            $"[SynergyData] {assetName} : requiredValiseIds contient une entrée vide (index {i}).",
                            this);
                    }
                }
            }
        }
#endif
    }
}
