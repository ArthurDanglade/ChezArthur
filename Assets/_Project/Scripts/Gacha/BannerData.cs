using System;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Characters;

namespace ChezArthur.Gacha
{
    /// <summary>
    /// Définit une bannière de gacha.
    /// Note : DateTime n'est pas sérialisé par Unity dans l'Inspector ; les champs startDate/endDate seront à éditer plus tard (string/long).
    /// </summary>
    [CreateAssetMenu(fileName = "NewBanner", menuName = "Chez Arthur/Gacha/Banner Data")]
    public class BannerData : ScriptableObject
    {
        [Header("Identité")]
        [SerializeField] private string id;
        [SerializeField] private string bannerName;
        [SerializeField] private Sprite bannerImage;

        [Header("Durée")]
        [SerializeField] private bool hasDuration;
        [SerializeField] private DateTime startDate;
        [SerializeField] private DateTime endDate;

        [Header("Rate Up")]
        [SerializeField] private CharacterData rateUpSSR;
        [SerializeField] private List<CharacterData> rateUpLR;  // LR spéciaux si applicable

        [Header("Pool de personnages")]
        [SerializeField] private List<CharacterData> srPool;   // Tous les SR disponibles
        [SerializeField] private List<CharacterData> ssrPool;  // SSR hors rate up (pour plus tard)
        [SerializeField] private List<CharacterData> lrPool;   // LR disponibles

        [Header("Coûts")]
        [SerializeField] private int costSingle = 100;
        [SerializeField] private int costMulti = 1000;         // x10

        [Header("Taux (en %)")]
        [SerializeField] private float rateSR = 90f;
        [SerializeField] private float rateSSR = 9f;
        [SerializeField] private float rateLR = 1f;

        [Header("Pity")]
        [SerializeField] private int pityThreshold = 100;      // Nombre de multi pour garantie

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public string Id => id;
        public string BannerName => bannerName;
        public Sprite BannerImage => bannerImage;
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
        public DateTime StartDate => startDate;
        public DateTime EndDate => endDate;

        /// <summary>
        /// Vérifie si la bannière est active.
        /// </summary>
        public bool IsActive()
        {
            if (!hasDuration) return true;
            DateTime now = DateTime.Now;
            return now >= startDate && now <= endDate;
        }

        /// <summary>
        /// Retourne le temps restant avant la fin de la bannière.
        /// </summary>
        public TimeSpan GetTimeRemaining()
        {
            if (!hasDuration) return TimeSpan.MaxValue;
            return endDate - DateTime.Now;
        }
    }
}
