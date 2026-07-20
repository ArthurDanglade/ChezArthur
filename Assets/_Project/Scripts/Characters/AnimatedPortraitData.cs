using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Characters
{
    /// <summary>
    /// Une frame de timeline portrait animé (index cellule + durée en secondes).
    /// </summary>
    [Serializable]
    public struct PortraitFrame
    {
        public int cellIndex;
        public float duration;
    }

    /// <summary>
    /// Données d'un sheet portrait animé SSR/LR : grille, cellules, timeline.
    /// Conversion index → UV centralisée ici (origine UV Unity = bas-gauche).
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewAnimatedPortrait",
        menuName = "Chez Arthur/Animated Portrait Data")]
    public class AnimatedPortraitData : ScriptableObject
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const int MIN_GRID_AXIS = 1;
        private const int MIN_CELL_SIZE = 1;
        private const float MIN_FRAME_DURATION = 0.01f;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Tooltip("Chemin Resources sans extension (ex. CharacterPortraitsSSR/portrait_ardacula_dechu).")]
        [SerializeField] private string resourcesPath;

        [Tooltip("Nombre de colonnes du sheet.")]
        [SerializeField] private int columns = MIN_GRID_AXIS;

        [Tooltip("Nombre de lignes du sheet.")]
        [SerializeField] private int rows = MIN_GRID_AXIS;

        [Tooltip("Largeur d'une cellule en pixels.")]
        [SerializeField] private int cellWidth = MIN_CELL_SIZE;

        [Tooltip("Hauteur d'une cellule en pixels.")]
        [SerializeField] private int cellHeight = MIN_CELL_SIZE;

        [Tooltip("Segments d'animation (cellIndex, duration). Un index peut se répéter.")]
        [SerializeField] private List<PortraitFrame> timeline = new List<PortraitFrame>();

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        [NonSerialized] private float _cachedTotalDuration = -1f;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public string ResourcesPath => resourcesPath;
        public int Columns => columns;
        public int Rows => rows;
        public int CellWidth => cellWidth;
        public int CellHeight => cellHeight;
        public IReadOnlyList<PortraitFrame> Timeline => timeline;

        /// <summary> Somme des durées de la timeline (cache invalidé dans OnValidate). </summary>
        public float TotalDuration
        {
            get
            {
                if (_cachedTotalDuration < 0f)
                    _cachedTotalDuration = ComputeTotalDuration();
                return _cachedTotalDuration;
            }
        }

        /// <summary> True si pas d'anim (null ou une seule frame). </summary>
        public bool IsStatic => timeline == null || timeline.Count <= 1;

        /// <summary> Nombre total de cellules dans la grille. </summary>
        public int CellCount => columns * rows;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// UV d'une cellule. Indexation G→D puis HAUT→BAS ; origine UV Unity = bas-gauche
        /// (inversion Y uniquement ici).
        /// </summary>
        public Rect GetCellUvRect(int cellIndex)
        {
            int count = CellCount;
            if (count <= 0)
            {
                Debug.LogWarning(
                    $"[AnimatedPortraitData] {name} : grille invalide (columns={columns}, rows={rows}).",
                    this);
                return new Rect(0f, 0f, 1f, 1f);
            }

            if (cellIndex < 0 || cellIndex >= count)
            {
                Debug.LogWarning(
                    $"[AnimatedPortraitData] {name} : cellIndex {cellIndex} hors [0, {count - 1}].",
                    this);
                cellIndex = Mathf.Clamp(cellIndex, 0, count - 1);
            }

            int col = cellIndex % columns;
            int row = cellIndex / columns;
            float uvW = 1f / columns;
            float uvH = 1f / rows;
            float x = col * uvW;
            float y = 1f - (row + 1) * uvH;
            return new Rect(x, y, uvW, uvH);
        }

#if UNITY_EDITOR
        /// <summary> Init éditeur (outil d'import Gate 2). Invalide le cache TotalDuration. </summary>
        public void EditorInitialize(
            string path,
            int columnCount,
            int rowCount,
            int cellPixelWidth,
            int cellPixelHeight,
            List<PortraitFrame> frames)
        {
            resourcesPath = path;
            columns = columnCount;
            rows = rowCount;
            cellWidth = cellPixelWidth;
            cellHeight = cellPixelHeight;
            // Copie défensive : l'outil d'import (Gate 2) réutilise des listes de
            // travail en batch, on ne doit jamais aliaser une liste externe.
            timeline = frames != null
                ? new List<PortraitFrame>(frames)
                : new List<PortraitFrame>();
            InvalidateTotalDurationCache();
        }
#endif

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void OnValidate()
        {
            columns = Mathf.Max(MIN_GRID_AXIS, columns);
            rows = Mathf.Max(MIN_GRID_AXIS, rows);
            cellWidth = Mathf.Max(MIN_CELL_SIZE, cellWidth);
            cellHeight = Mathf.Max(MIN_CELL_SIZE, cellHeight);

            if (timeline != null)
            {
                for (int i = 0; i < timeline.Count; i++)
                {
                    PortraitFrame frame = timeline[i];
                    if (frame.duration < MIN_FRAME_DURATION)
                    {
                        frame.duration = MIN_FRAME_DURATION;
                        timeline[i] = frame;
                    }
                }
            }

            InvalidateTotalDurationCache();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void InvalidateTotalDurationCache()
        {
            _cachedTotalDuration = -1f;
        }

        private float ComputeTotalDuration()
        {
            if (timeline == null || timeline.Count == 0)
                return 0f;

            float sum = 0f;
            for (int i = 0; i < timeline.Count; i++)
                sum += timeline[i].duration;
            return sum;
        }
    }
}
