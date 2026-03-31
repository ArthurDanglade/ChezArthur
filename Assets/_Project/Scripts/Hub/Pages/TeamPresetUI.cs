using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Core;
using ChezArthur.Characters;

namespace ChezArthur.Hub.Pages
{
    /// <summary>
    /// Gère l'UI des 5 presets d'équipe (état actif/inactif + switch de preset).
    /// </summary>
    public class TeamPresetUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Boutons Preset")]
        [SerializeField] private Button[] presetButtons; // 5 boutons

        [Header("Sprites chiffres — inactif")]
        [SerializeField] private Sprite[] numberInactiveSprites; // 5 sprites (preset_1 à preset_5)

        [Header("Sprites chiffres — actif")]
        [SerializeField] private Sprite[] numberActiveSprites; // 5 sprites (preset_1_active à preset_5)

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Image[] _bgActiveImages;
        private Image[] _numberIconImages;
        private UnityEngine.Events.UnityAction[] _clickHandlers;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            int count = presetButtons != null ? presetButtons.Length : 0;
            _bgActiveImages = new Image[count];
            _numberIconImages = new Image[count];
            _clickHandlers = new UnityEngine.Events.UnityAction[count];

            for (int i = 0; i < count; i++)
            {
                Button btn = presetButtons[i];
                if (btn == null) continue;

                Transform t = btn.transform;
                if (t.childCount > 0)
                    _bgActiveImages[i] = t.GetChild(0).GetComponent<Image>(); // BgActive
                if (t.childCount > 1)
                    _numberIconImages[i] = t.GetChild(1).GetComponent<Image>(); // NumberIcon

                int capturedIndex = i;
                _clickHandlers[i] = () => OnPresetClicked(capturedIndex);
                btn.onClick.AddListener(_clickHandlers[i]);
            }
        }

        private void OnEnable()
        {
            CharacterManager characters = GetCharacterManager();
            if (characters != null)
                characters.OnTeamChanged += HandleTeamChanged;

            UpdateVisuals();
        }

        private void OnDisable()
        {
            CharacterManager characters = GetCharacterManager();
            if (characters != null)
                characters.OnTeamChanged -= HandleTeamChanged;
        }

        private void OnDestroy()
        {
            if (presetButtons == null) return;

            for (int i = 0; i < presetButtons.Length; i++)
            {
                Button btn = presetButtons[i];
                if (btn == null) continue;
                if (_clickHandlers != null && i < _clickHandlers.Length && _clickHandlers[i] != null)
                    btn.onClick.RemoveListener(_clickHandlers[i]);
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Rafraîchit l'état visuel des 5 boutons preset selon le preset actif.
        /// </summary>
        public void UpdateVisuals()
        {
            CharacterManager characters = GetCharacterManager();
            int activeIndex = characters != null ? characters.ActivePresetIndex : 0;

            int count = presetButtons != null ? presetButtons.Length : 0;
            for (int i = 0; i < count; i++)
            {
                bool isActive = i == activeIndex;

                if (_bgActiveImages != null && i < _bgActiveImages.Length && _bgActiveImages[i] != null)
                    _bgActiveImages[i].enabled = isActive;

                if (_numberIconImages != null && i < _numberIconImages.Length && _numberIconImages[i] != null)
                {
                    Sprite targetSprite = isActive
                        ? GetSprite(numberActiveSprites, i)
                        : GetSprite(numberInactiveSprites, i);

                    if (targetSprite != null)
                        _numberIconImages[i].sprite = targetSprite;
                }
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════
        private void OnPresetClicked(int index)
        {
            CharacterManager characters = GetCharacterManager();
            if (characters == null) return;

            characters.SwitchPreset(index);

            if (PersistentManager.Instance != null)
                PersistentManager.Instance.SaveGame();

            UpdateVisuals();
        }

        private void HandleTeamChanged()
        {
            UpdateVisuals();
        }

        private static CharacterManager GetCharacterManager()
        {
            if (PersistentManager.Instance == null) return null;
            return PersistentManager.Instance.Characters;
        }

        private static Sprite GetSprite(Sprite[] sprites, int index)
        {
            if (sprites == null || index < 0 || index >= sprites.Length) return null;
            return sprites[index];
        }
    }
}

