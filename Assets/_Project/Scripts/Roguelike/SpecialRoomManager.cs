using System;
using UnityEngine;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Gère le modificateur de salle spéciale actif.
    /// </summary>
    public class SpecialRoomManager : MonoBehaviour
    {
        public static SpecialRoomManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private float happyHourHealMultiplier = 2f;
        [SerializeField] private int hordeExtraEnemies = 4;
        [SerializeField] private float clientVIPTalsMultiplier = 2f;

        private SpecialRoomType _currentRoomType = SpecialRoomType.None;

        public SpecialRoomType CurrentRoomType => _currentRoomType;
        public bool IsSpecialRoom => _currentRoomType != SpecialRoomType.None;

        // Modificateurs
        public float HealMultiplier => _currentRoomType == SpecialRoomType.HappyHour ? happyHourHealMultiplier : 1f;
        public int ExtraEnemyCount => _currentRoomType == SpecialRoomType.Horde ? hordeExtraEnemies : 0;
        public float TalsMultiplier => _currentRoomType == SpecialRoomType.ClientVIP ? clientVIPTalsMultiplier : 1f;
        public bool IsClientVIP => _currentRoomType == SpecialRoomType.ClientVIP;

        public event Action<SpecialRoomType> OnSpecialRoomChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Active un type de salle spéciale.
        /// </summary>
        public void SetSpecialRoom(SpecialRoomType roomType)
        {
            _currentRoomType = roomType;
            OnSpecialRoomChanged?.Invoke(roomType);

            if (roomType != SpecialRoomType.None)
                Debug.Log($"[SpecialRoomManager] Salle spéciale : {roomType}");
        }

        /// <summary>
        /// Désactive la salle spéciale.
        /// </summary>
        public void ClearSpecialRoom()
        {
            SetSpecialRoom(SpecialRoomType.None);
        }
    }
}
