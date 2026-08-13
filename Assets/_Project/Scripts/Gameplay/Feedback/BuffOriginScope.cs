using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Gameplay.Buffs;

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
    /// Règle générale : estampiller l'attribution sur le BuffData à la pose ;
    /// pour effets différés (carrier, trail, coroutine), rejouer via PushCaptured / ApplyAttribution.
    /// LIMITE : sans Capture au moment du déclenchement sync, pas de callout (Origin silencieux).
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

            public bool HasCallout =>
                SourceUnit != null && !string.IsNullOrEmpty(DisplayName) && !Silent;

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

        /// <summary>
        /// Rejoue une attribution capturée (trail / carrier / différé) avec un nouvel ActivationId.
        /// </summary>
        public static int PushCaptured(Frame captured)
        {
            if (string.IsNullOrEmpty(captured.DisplayName))
                return 0;

            return Push(
                captured.Origin != BuffOrigin.None ? captured.Origin : BuffOrigin.Passif,
                captured.SourceUnit,
                captured.DisplayName,
                captured.PassiveId,
                captured.Silent);
        }

        /// <summary>
        /// Rejoue l'attribution estampillée sur un buff (ex. carrier → poison/brûlure).
        /// </summary>
        public static int PushFromBuff(BuffData buff)
        {
            if (buff == null || string.IsNullOrEmpty(buff.CalloutDisplayName))
                return 0;

            return Push(
                buff.Origin != BuffOrigin.None ? buff.Origin : BuffOrigin.Passif,
                buff.CalloutSource,
                buff.CalloutDisplayName,
                buff.CalloutPassiveId,
                buff.CalloutSilent);
        }

        public static void Pop()
        {
            if (_stack.Count == 0)
                return;
            _stack.RemoveAt(_stack.Count - 1);
        }

        /// <summary>
        /// Écrit l'attribution courante (ou capturée) sur un buff — sans écraser un stamp déjà présent.
        /// </summary>
        public static void ApplyAttribution(BuffData buff, Frame frame)
        {
            if (buff == null)
                return;

            if (frame.Origin != BuffOrigin.None)
                buff.Origin = frame.Origin;

            if (!string.IsNullOrEmpty(buff.CalloutDisplayName))
                return;

            if (string.IsNullOrEmpty(frame.DisplayName) && frame.SourceUnit == null)
                return;

            buff.CalloutSource = frame.SourceUnit;
            buff.CalloutDisplayName = frame.DisplayName;
            buff.CalloutPassiveId = frame.PassiveId;
            buff.CalloutSilent = frame.Silent;
            buff.CalloutActivationId = frame.ActivationId;
        }

        /// <summary> Copie l'attribution d'un buff source (carrier) vers un buff dérivé. </summary>
        public static void CopyAttribution(BuffData from, BuffData to)
        {
            if (from == null || to == null)
                return;

            if (from.Origin != BuffOrigin.None)
                to.Origin = from.Origin;

            if (string.IsNullOrEmpty(from.CalloutDisplayName))
                return;

            to.CalloutSource = from.CalloutSource;
            to.CalloutDisplayName = from.CalloutDisplayName;
            to.CalloutPassiveId = from.CalloutPassiveId;
            to.CalloutSilent = from.CalloutSilent;
            to.CalloutActivationId = from.CalloutActivationId;
        }
    }
}
