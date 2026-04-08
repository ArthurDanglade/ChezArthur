using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Enemies;

namespace ChezArthur.UI
{
    /// <summary>
    /// Card d'inspection d'un ennemi : affiche nom, stats
    /// et passifs. S'affiche au press et disparaît au release.
    /// </summary>
    public class EnemyCardUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Container")]
        [SerializeField] private GameObject cardRoot;

        [Header("Identité")]
        [SerializeField] private TextMeshProUGUI nameText;

        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI atkText;
        [SerializeField] private TextMeshProUGUI defText;
        [SerializeField] private TextMeshProUGUI spdText;

        [Header("Passifs")]
        [Tooltip("Texte unique qui liste tous les passifs séparés par des retours à la ligne.")]
        [SerializeField] private TextMeshProUGUI passivesText;

        // ═══════════════════════════════════════════
        // SINGLETON
        // ═══════════════════════════════════════════
        public static EnemyCardUI Instance { get; private set; }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Affiche la card avec les données de l'ennemi.
        /// </summary>
        public void Show(Enemy enemy)
        {
            if (enemy == null || enemy.Data == null) return;

            EnemyData data = enemy.Data;

            if (nameText != null)
                nameText.text = data.EnemyName;

            if (hpText != null)
                hpText.text = $"HP  {enemy.CurrentHp} / {enemy.MaxHp}";

            if (atkText != null)
                atkText.text = $"ATK  {enemy.EffectiveAtk}";

            if (defText != null)
                defText.text = $"DEF  {enemy.EffectiveDef}";

            if (spdText != null)
                spdText.text = $"SPD  {enemy.Speed}";

            if (passivesText != null)
            {
                var sb = new System.Text.StringBuilder();
                if (data.Passives != null)
                {
                    for (int i = 0; i < data.Passives.Count; i++)
                    {
                        var passive = data.Passives[i];
                        if (passive == null) continue;
                        if (!string.IsNullOrEmpty(passive.PassiveName))
                            sb.AppendLine(passive.PassiveName);
                        if (!string.IsNullOrEmpty(passive.Description))
                            sb.AppendLine(passive.Description);
                        if (i < data.Passives.Count - 1)
                            sb.AppendLine("──────────");
                    }
                }
                passivesText.text = sb.ToString().TrimEnd();
            }

            if (cardRoot != null) cardRoot.SetActive(true);
        }

        /// <summary>
        /// Masque la card.
        /// </summary>
        public void Hide()
        {
            if (cardRoot != null) cardRoot.SetActive(false);
        }
    }
}
