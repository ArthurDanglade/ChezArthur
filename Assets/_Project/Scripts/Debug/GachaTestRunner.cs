using UnityEngine;
using ChezArthur.Core;
using ChezArthur.Gacha;

namespace ChezArthur.Testing
{
    /// <summary>
    /// Script de test pour le système gacha. À supprimer après les tests.
    /// </summary>
    public class GachaTestRunner : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private BannerData testBanner;

        [Header("Debug - Ajouter des Tals")]
        [SerializeField] private int talsToAdd = 10000;

        [ContextMenu("Ajouter des Tals")]
        public void AddTals()
        {
            if (PersistentManager.Instance == null)
            {
                UnityEngine.Debug.LogError("[GachaTest] PersistentManager non trouvé.");
                return;
            }

            PersistentManager.Instance.AddTals(talsToAdd);
            UnityEngine.Debug.Log($"[GachaTest] {talsToAdd} Tals ajoutés. Total : {PersistentManager.Instance.Tals}");
        }

        [ContextMenu("Test Pull Single (x1)")]
        public void TestPullSingle()
        {
            if (!ValidateSetup()) return;

            var result = PersistentManager.Instance.Gacha.PullSingle(testBanner);
            LogResult(result, "Single");
        }

        [ContextMenu("Test Pull Multi (x10)")]
        public void TestPullMulti()
        {
            if (!ValidateSetup()) return;

            var result = PersistentManager.Instance.Gacha.PullMulti(testBanner);
            LogResult(result, "Multi");
        }

        [ContextMenu("Afficher Pity Count")]
        public void ShowPityCount()
        {
            if (!ValidateSetup()) return;

            int pity = PersistentManager.Instance.Gacha.GetPityCount(testBanner.Id);
            UnityEngine.Debug.Log($"[GachaTest] Pity pour '{testBanner.BannerName}' : {pity}/{testBanner.PityThreshold}");
        }

        [ContextMenu("Afficher Tals")]
        public void ShowTals()
        {
            if (PersistentManager.Instance == null)
            {
                UnityEngine.Debug.LogError("[GachaTest] PersistentManager non trouvé.");
                return;
            }

            UnityEngine.Debug.Log($"[GachaTest] Tals : {PersistentManager.Instance.Tals}");
        }

        private bool ValidateSetup()
        {
            if (PersistentManager.Instance == null)
            {
                UnityEngine.Debug.LogError("[GachaTest] PersistentManager non trouvé.");
                return false;
            }

            if (PersistentManager.Instance.Gacha == null)
            {
                UnityEngine.Debug.LogError("[GachaTest] GachaManager non initialisé.");
                return false;
            }

            if (testBanner == null)
            {
                UnityEngine.Debug.LogError("[GachaTest] Aucune bannière assignée.");
                return false;
            }

            return true;
        }

        private void LogResult(GachaPullResult result, string pullType)
        {
            if (result == null)
            {
                UnityEngine.Debug.LogWarning($"[GachaTest] Pull {pullType} échoué (pas assez de Tals ?)");
                return;
            }

            UnityEngine.Debug.Log($"══════════════════════════════════════");
            UnityEngine.Debug.Log($"[GachaTest] RÉSULTAT PULL {pullType.ToUpper()}");
            UnityEngine.Debug.Log($"Tals dépensés : {result.talsSpent}");
            UnityEngine.Debug.Log($"Personnages obtenus : {result.characters.Count}");

            foreach (var pulled in result.characters)
            {
                string status = pulled.isNew ? "NOUVEAU !" : $"DOUBLON (Nv.{pulled.previousLevel} → Nv.{pulled.newLevel})";
                string rateUp = pulled.isRateUp ? " [RATE UP]" : "";
                UnityEngine.Debug.Log($"  • [{pulled.rarity}] {pulled.characterId} - {status}{rateUp}");
            }

            UnityEngine.Debug.Log($"Tals restants : {PersistentManager.Instance.Tals}");
            UnityEngine.Debug.Log($"══════════════════════════════════════");
        }
    }
}
