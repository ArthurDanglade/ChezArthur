using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Gameplay;

namespace ChezArthur.UI
{
    /// <summary>
    /// Barre de vie pour un allié : nom, fill image, texte HP optionnel, couleur selon le ratio.
    /// </summary>
    public class AllyHPBar : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image fillImage;
        [SerializeField] private TextMeshProUGUI hpText;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private CharacterBall _character;
        private Action<int> _damagedHandler;
        private Action<int> _healedHandler;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Associe la barre à un personnage, s'abonne à OnDamaged et met à jour l'affichage.
        /// </summary>
        public void Initialize(CharacterBall character)
        {
            if (_character != null)
            {
                if (_damagedHandler != null)
                    _character.OnDamaged -= _damagedHandler;
                if (_healedHandler != null)
                    _character.OnHealed -= _healedHandler;
            }

            _character = character;

            if (character == null)
            {
                if (nameText != null) nameText.text = "";
                if (fillImage != null) fillImage.fillAmount = 0f;
                if (hpText != null) hpText.text = "";
                return;
            }

            if (nameText != null)
                nameText.text = character.Name;

            _damagedHandler = _ => UpdateDisplay();
            character.OnDamaged += _damagedHandler;

            _healedHandler = _ => UpdateDisplay();
            character.OnHealed += _healedHandler;

            UpdateDisplay();
        }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void OnDestroy()
        {
            if (_character != null)
            {
                if (_damagedHandler != null)
                    _character.OnDamaged -= _damagedHandler;
                if (_healedHandler != null)
                    _character.OnHealed -= _healedHandler;
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void UpdateDisplay()
        {
            if (_character == null) return;

            int current = _character.CurrentHp;
            int max = Mathf.Max(1, _character.MaxHp);
            float ratio = (float)current / max;

            if (fillImage != null)
            {
                fillImage.fillAmount = ratio;
                fillImage.color = GetColorForRatio(ratio);
            }

            if (hpText != null)
                hpText.text = $"{current}/{max}";
        }

        /// <summary>
        /// Retourne une couleur selon le ratio PV : vert > jaune > rouge.
        /// </summary>
        private static Color GetColorForRatio(float ratio)
        {
            if (ratio <= 0.25f)
                return Color.red;
            if (ratio <= 0.5f)
                return Color.Lerp(Color.red, Color.yellow, (ratio - 0.25f) * 4f);
            return Color.Lerp(Color.yellow, Color.green, (ratio - 0.5f) * 2f);
        }
    }
}
