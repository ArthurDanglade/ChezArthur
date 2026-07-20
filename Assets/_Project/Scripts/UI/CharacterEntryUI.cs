using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Gameplay;
using ChezArthur.Characters;

namespace ChezArthur.UI
{
    /// <summary>
    /// Affiche les infos d'un personnage (portrait, nom, stats).
    /// </summary>
    public class CharacterEntryUI : MonoBehaviour
    {
        [Header("Références")]
        [SerializeField] private Image portraitImage;
        [SerializeField] private Image rarityAccent; // cadre/anneau coloré par rareté
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI atkText;
        [SerializeField] private TextMeshProUGUI defText;
        [SerializeField] private TextMeshProUGUI speedText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Button cardButton;

        private CharacterBall _character;
        private Action<CharacterBall> _onClickCallback;

        private void Awake()
        {
            // Repli si le prefab n'a pas câblé le SerializeField.
            if (cardButton == null)
                cardButton = GetComponent<Button>();

            if (cardButton != null)
                cardButton.onClick.AddListener(OnCardClicked);
        }

        private void OnDestroy()
        {
            if (cardButton != null)
                cardButton.onClick.RemoveListener(OnCardClicked);
        }

        /// <summary>
        /// Remplit l'entrée avec les données du personnage.
        /// </summary>
        public void Setup(CharacterBall character, Action<CharacterBall> onClickCallback = null)
        {
            if (character == null) return;

            _character = character;
            _onClickCallback = onClickCallback;

            var data = character.Data;

            if (rarityAccent != null && data != null)
                rarityAccent.color = GetRarityColor(data.Rarity);

            // Portrait et nom
            if (portraitImage != null && data != null && data.Icon != null)
                portraitImage.sprite = data.Icon;

            if (nameText != null)
                nameText.text = character.Name;

            if (levelText != null)
                levelText.text = "Niv. " + character.CharacterLevel;

            // Stats : effective (base)
            if (hpText != null)
            {
                int baseHp = data != null ? data.BaseHp : 0;
                hpText.text = $"HP: {character.CurrentHp}/{character.EffectiveMaxHp} <color=#888>(base: {baseHp})</color>";
            }

            if (atkText != null)
            {
                int baseAtk = data != null ? data.BaseAtk : 0;
                atkText.text = $"ATK: {character.EffectiveAtk} <color=#888>(base: {baseAtk})</color>";
            }

            if (defText != null)
            {
                int baseDef = data != null ? data.BaseDef : 0;
                defText.text = $"DEF: {character.EffectiveDef} <color=#888>(base: {baseDef})</color>";
            }

            if (speedText != null)
            {
                int baseSpeed = data != null ? data.BaseSpeed : 0;
                speedText.text = $"SPD: {character.EffectiveSpeed} <color=#888>(base: {baseSpeed})</color>";
            }
        }

        private void OnCardClicked()
        {
            _onClickCallback?.Invoke(_character);
        }

        private Color GetRarityColor(CharacterRarity rarity)
        {
            return rarity switch
            {
                CharacterRarity.SR => new Color(0.6f, 0.8f, 1f),   // bleu clair
                CharacterRarity.SSR => new Color(1f, 0.84f, 0f),    // or
                CharacterRarity.LR => new Color(0.8f, 0.5f, 1f),   // violet
                _ => new Color(0.30f, 0.33f, 0.40f)                 // neutre
            };
        }
    }
}
