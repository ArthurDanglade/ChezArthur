using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Gameplay;

namespace ChezArthur.Gameplay.Buffs
{
    /// <summary>
    /// Gère les buffs temporaires appliqués sur ce personnage par d'autres personnages ou par des effets.
    /// Attaché à chaque CharacterBall.
    /// </summary>
    public class BuffReceiver : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private List<BuffData> _activeBuffs;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public int ActiveBuffCount => _activeBuffs != null ? _activeBuffs.Count : 0;
        public IReadOnlyList<BuffData> ActiveBuffs => _activeBuffs;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            _activeBuffs = new List<BuffData>(8);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Ajoute un buff. Gère l'unicité (UniquePerSource, UniqueGlobal) : remplace si existe déjà.
        /// </summary>
        public void AddBuff(BuffData buff)
        {
            if (buff == null || _activeBuffs == null) return;

            if (buff.UniqueGlobal)
            {
                for (int i = _activeBuffs.Count - 1; i >= 0; i--)
                {
                    BuffData b = _activeBuffs[i];
                    if (b != null && b.BuffId == buff.BuffId)
                        _activeBuffs.RemoveAt(i);
                }
            }
            else if (buff.UniquePerSource)
            {
                for (int i = _activeBuffs.Count - 1; i >= 0; i--)
                {
                    BuffData b = _activeBuffs[i];
                    if (b != null && b.BuffId == buff.BuffId && ReferenceEquals(b.Source, buff.Source))
                        _activeBuffs.RemoveAt(i);
                }
            }

            _activeBuffs.Add(buff);
        }

        /// <summary>
        /// Supprime tous les buffs avec le buffId donné.
        /// </summary>
        public void RemoveBuffsById(string buffId)
        {
            if (string.IsNullOrEmpty(buffId) || _activeBuffs == null) return;

            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                BuffData b = _activeBuffs[i];
                if (b != null && b.BuffId == buffId)
                    _activeBuffs.RemoveAt(i);
            }
        }

        /// <summary>
        /// Supprime tous les buffs provenant d'une source donnée.
        /// </summary>
        public void RemoveBuffsBySource(CharacterBall source)
        {
            if (_activeBuffs == null) return;

            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                BuffData b = _activeBuffs[i];
                if (b != null && ReferenceEquals(b.Source, source))
                    _activeBuffs.RemoveAt(i);
            }
        }

        /// <summary>
        /// Vérifie si le personnage a un buff actif avec le buffId donné.
        /// </summary>
        public bool HasBuff(string buffId)
        {
            if (string.IsNullOrEmpty(buffId) || _activeBuffs == null) return false;

            for (int i = 0; i < _activeBuffs.Count; i++)
            {
                BuffData b = _activeBuffs[i];
                if (b != null && b.BuffId == buffId)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Prolonge la durée en tours des buffs correspondant au buffId.
        /// Ignore les buffs permanents (RemainingTurns &lt;= 0).
        /// </summary>
        public void ExtendBuffTurns(string buffId, int extraTurns)
        {
            if (string.IsNullOrEmpty(buffId) || _activeBuffs == null) return;
            if (extraTurns <= 0) return;

            for (int i = 0; i < _activeBuffs.Count; i++)
            {
                BuffData b = _activeBuffs[i];
                if (b == null || b.BuffId != buffId) continue;
                if (b.RemainingTurns > 0)
                    b.RemainingTurns += extraTurns;
            }
        }

        /// <summary>
        /// Retourne le modificateur total (percent, flat) pour un BuffStatType donné.
        /// Appelé par EffectiveAtk, EffectiveDef, etc. sur CharacterBall.
        /// Pas d'allocation.
        /// </summary>
        public (float percent, float flat) GetStatModifier(BuffStatType statType)
        {
            if (_activeBuffs == null) return (0f, 0f);

            float percent = 0f;
            float flat = 0f;

            for (int i = 0; i < _activeBuffs.Count; i++)
            {
                BuffData b = _activeBuffs[i];
                if (b == null || b.StatType != statType) continue;

                if (b.IsPercent)
                    percent += b.Value;
                else
                    flat += b.Value;
            }

            return (percent, flat);
        }

        /// <summary>
        /// Retourne la valeur totale de bouclier actif (somme des buffs Shield).
        /// </summary>
        public float GetShieldAmount()
        {
            if (_activeBuffs == null) return 0f;

            float total = 0f;
            for (int i = 0; i < _activeBuffs.Count; i++)
            {
                BuffData b = _activeBuffs[i];
                if (b != null && b.StatType == BuffStatType.Shield && b.Value > 0f)
                    total += b.Value;
            }
            return total;
        }

        /// <summary>
        /// Absorbe des dégâts avec le bouclier. Retourne les dégâts restants après absorption.
        /// </summary>
        public int AbsorbDamageWithShield(int damage)
        {
            if (damage <= 0 || _activeBuffs == null) return damage;

            int remaining = damage;

            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                BuffData b = _activeBuffs[i];
                if (b == null || b.StatType != BuffStatType.Shield) continue;

                if (b.Value <= 0f)
                {
                    _activeBuffs.RemoveAt(i);
                    continue;
                }

                int shieldHp = Mathf.Max(0, Mathf.RoundToInt(b.Value));
                if (shieldHp <= 0)
                {
                    _activeBuffs.RemoveAt(i);
                    continue;
                }

                int absorb = remaining < shieldHp ? remaining : shieldHp;
                remaining -= absorb;
                b.Value -= absorb;

                if (b.Value <= 0.001f)
                    _activeBuffs.RemoveAt(i);

                if (remaining <= 0)
                    return 0;
            }

            return remaining;
        }

        /// <summary>
        /// Décrémente la durée en tours du porteur. Appelé en fin de tour du personnage.
        /// Supprime les buffs expirés.
        /// </summary>
        public void TickTurn()
        {
            if (_activeBuffs == null) return;

            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                BuffData b = _activeBuffs[i];
                if (b == null)
                {
                    _activeBuffs.RemoveAt(i);
                    continue;
                }

                if (b.RemainingTurns > 0)
                {
                    b.RemainingTurns--;
                    if (b.RemainingTurns == 0)
                        _activeBuffs.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Décrémente la durée en cycles. Appelé en fin de cycle complet.
        /// Supprime les buffs expirés.
        /// </summary>
        public void TickCycle()
        {
            if (_activeBuffs == null) return;

            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                BuffData b = _activeBuffs[i];
                if (b == null)
                {
                    _activeBuffs.RemoveAt(i);
                    continue;
                }

                if (b.RemainingCycles > 0)
                {
                    b.RemainingCycles--;
                    if (b.RemainingCycles == 0)
                        _activeBuffs.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Supprime tous les buffs (reset complet, ex: nouvel étage si voulu).
        /// </summary>
        public void ClearAll()
        {
            _activeBuffs?.Clear();
        }
    }
}
