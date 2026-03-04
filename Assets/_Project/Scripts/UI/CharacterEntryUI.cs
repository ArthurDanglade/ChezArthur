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
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI atkText;
        [SerializeField] private TextMeshProUGUI defText;
        [SerializeField] private TextMeshProUGUI speedText;

        /// <summary>
        /// Remplit l'entrée avec les données du personnage.
        /// </summary>
        public void Setup(CharacterBall character)
        {
            if (character == null) return;

            var data = character.Data;

            // Portrait et nom
            if (portraitImage != null && data != null && data.Icon != null)
                portraitImage.sprite = data.Icon;

            if (nameText != null)
                nameText.text = character.Name;

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
    }
}
