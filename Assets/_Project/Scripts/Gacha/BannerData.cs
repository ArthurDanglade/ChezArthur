using System;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Characters;

namespace ChezArthur.Gacha
{
    /// <summary>
    /// Définit une bannière de gacha (Gate 6.a — champs Hub + legacy GachaManager).
    /// </summary>
    [CreateAssetMenu(fileName = "NewBanner", menuName = "Chez Arthur/Gacha/Banner")]
    public class BannerData : ScriptableObject
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS — Hub 6.a
        // ═══════════════════════════════════════════
        [Header("Identité (Hub)")]
        [SerializeField] private string bannerId;
        [Tooltip("Titre affiché (FR).")]
        [SerializeField] private string displayTitle;
        [SerializeField] private Sprite artwork;

        [Header("Saison")]
        [SerializeField] private bool hasDuration;
        [Tooltip("Fin de saison — ticks UTC (0 = illimité si hasDuration false).")]
        [SerializeField] private long dateFinSaisonTicks;

        [Header("Featured / pool (Hub)")]
        [SerializeField] private List<CharacterData> featuredCharacters = new List<CharacterData>();
        [SerializeField] private List<CharacterData> poolCharacters = new List<CharacterData>();
        [Tooltip("Taux d'apparition des personnages en vedette (%). 0 = ligne masquee dans le popup Taux.")]
        [SerializeField] private float featuredRatePercent = 0f;

        [Header("Coûts (Tals)")]
        [SerializeField] private int costSingle = 100;
        [SerializeField] private int costMulti = 1000;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS — legacy gacha (conservés)
        // ═══════════════════════════════════════════
        [Header("Legacy identité")]
        [SerializeField] private string id;
        [SerializeField] private string bannerName;
        [SerializeField] private Sprite bannerImage;

        [Header("Legacy Rate Up")]
        [SerializeField] private CharacterData rateUpSSR;
        [SerializeField] private List<CharacterData> rateUpLR;

        [Header("Legacy pools par rareté")]
        [SerializeField] private List<CharacterData> srPool;
        [SerializeField] private List<CharacterData> ssrPool;
        [SerializeField] private List<CharacterData> lrPool;

        [Header("Taux (en %)")]
        [SerializeField] private float rateSR = 90f;
        [SerializeField] private float rateSSR = 9f;
        [SerializeField] private float rateLR = 1f;

        [Header("Pity")]
        [SerializeField] private int pityThreshold = 100;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS — Hub 6.a
        // ═══════════════════════════════════════════
        public string BannerId => !string.IsNullOrEmpty(bannerId) ? bannerId : id;
        public string DisplayTitle => !string.IsNullOrEmpty(displayTitle) ? displayTitle : bannerName;
        public Sprite Artwork => artwork != null ? artwork : bannerImage;
        public long DateFinSaisonTicks => dateFinSaisonTicks;
        public DateTime DateFinSaison =>
            dateFinSaisonTicks > 0
                ? new DateTime(dateFinSaisonTicks, DateTimeKind.Utc)
                : DateTime.MaxValue;
        public IReadOnlyList<CharacterData> FeaturedCharacters =>
            featuredCharacters ?? (IReadOnlyList<CharacterData>)Array.Empty<CharacterData>();
        public IReadOnlyList<CharacterData> PoolCharacters =>
            poolCharacters ?? (IReadOnlyList<CharacterData>)Array.Empty<CharacterData>();
        public float FeaturedRatePercent => featuredRatePercent;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS — legacy (GachaManager / UI)
        // ═══════════════════════════════════════════
        public string Id => BannerId;
        public string BannerName => DisplayTitle;
        public Sprite BannerImage => Artwork;
        public CharacterData RateUpSSR => rateUpSSR;
        public List<CharacterData> RateUpLR => rateUpLR ?? new List<CharacterData>();
        public List<CharacterData> SRPool => srPool ?? new List<CharacterData>();
        public List<CharacterData> SSRPool => ssrPool ?? new List<CharacterData>();
        public List<CharacterData> LRPool => lrPool ?? new List<CharacterData>();
        public int CostSingle => costSingle;
        public int CostMulti => costMulti;
        public float RateSR => rateSR;
        public float RateSSR => rateSSR;
        public float RateLR => rateLR;
        public int PityThreshold => pityThreshold;
        public bool HasDuration => hasDuration;
        public DateTime StartDate => DateTime.MinValue;
        public DateTime EndDate => DateFinSaison;

        // ═══════════════════════════════════════════
        // API
        // ═══════════════════════════════════════════

        /// <summary> True si la bannière est active (pas de durée, ou avant dateFinSaison). </summary>
        public bool IsActive()
        {
            if (!hasDuration)
                return true;
            if (dateFinSaisonTicks <= 0)
                return true;
            return DateTime.UtcNow.Ticks <= dateFinSaisonTicks;
        }

        /// <summary> Temps restant avant fin de saison. </summary>
        public TimeSpan GetTimeRemaining()
        {
            if (!hasDuration || dateFinSaisonTicks <= 0)
                return TimeSpan.MaxValue;
            long left = dateFinSaisonTicks - DateTime.UtcNow.Ticks;
            return left <= 0 ? TimeSpan.Zero : TimeSpan.FromTicks(left);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Miroir legacy ← Hub pour assets existants.
            if (!string.IsNullOrEmpty(bannerId) && string.IsNullOrEmpty(id))
                id = bannerId;
            if (!string.IsNullOrEmpty(id) && string.IsNullOrEmpty(bannerId))
                bannerId = id;

            if (!string.IsNullOrEmpty(displayTitle) && string.IsNullOrEmpty(bannerName))
                bannerName = displayTitle;
            if (!string.IsNullOrEmpty(bannerName) && string.IsNullOrEmpty(displayTitle))
                displayTitle = bannerName;

            if (artwork != null && bannerImage == null)
                bannerImage = artwork;
            if (bannerImage != null && artwork == null)
                artwork = bannerImage;
        }
#endif
    }
}
