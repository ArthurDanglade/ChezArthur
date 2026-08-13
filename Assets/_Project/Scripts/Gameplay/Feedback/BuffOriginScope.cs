using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Gameplay.Feedback
{
    /// <summary>
    /// Origine d'un buff / label (F5-L2) — attribution pour suffixes et callouts.
    /// </summary>
    public enum BuffOrigin
    {
        None,
        Passif,
        Valise,
        Objet
    }

    /// <summary>
    /// Pile statique d'origine de buff (systems-only).
    /// LIMITE SYNCHRONE : couvre uniquement la fenêtre push→pop.
    /// Un handler qui pose son effet dans une coroutine sort du scope
    /// (Origin=None, pas de callout, aucun crash). Liste des absents → contrôle L2 / L3.
    /// </summary>
    public static class BuffOriginScope
    {
        // ═══════════════════════════════════════════
        // TYPES
        // ═══════════════════════════════════════════
        public readonly struct Frame
        {
            public readonly BuffOrigin Origin;
            public readonly Transform SourceUnit;
            public readonly string DisplayName;
            public readonly string PassiveId;
            public readonly bool Silent;
            public readonly int ActivationId;

            public Frame(
                BuffOrigin origin,
                Transform sourceUnit,
                string displayName,
                string passiveId,
                bool silent,
                int activationId)
            {
                Origin = origin;
                SourceUnit = sourceUnit;
                DisplayName = displayName;
                PassiveId = passiveId;
                Silent = silent;
                ActivationId = activationId;
            }
        }

        // ═══════════════════════════════════════════
        // CONSTANTES / ÉTAT
        // ═══════════════════════════════════════════
        private const int MaxDepth = 8;
        private static readonly List<Frame> _stack = new List<Frame>(MaxDepth);
        private static int _nextActivationId = 1;
        private static readonly Frame DefaultFrame =
            new Frame(BuffOrigin.None, null, null, null, false, 0);

        // ═══════════════════════════════════════════
        // API
        // ═══════════════════════════════════════════
        public static Frame Current =>
            _stack.Count > 0 ? _stack[_stack.Count - 1] : DefaultFrame;

        /// <summary>
        /// Empile un cadre. Retourne l'ActivationId attribué (incrémental).
        /// </summary>
        public static int Push(
            BuffOrigin origin,
            Transform sourceUnit,
            string displayName,
            string passiveId,
            bool silent)
        {
            int id = _nextActivationId++;
            if (_stack.Count >= MaxDepth)
            {
                Debug.LogWarning("[BuffOriginScope] profondeur max — Pop manquant ?");
                return id;
            }

            _stack.Add(new Frame(origin, sourceUnit, displayName, passiveId, silent, id));
            return id;
        }

        public static void Pop()
        {
            if (_stack.Count == 0)
                return;
            _stack.RemoveAt(_stack.Count - 1);
        }
    }
}
