using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChezArthur.Localization
{
    /// <summary>
    /// Applique Loc.Tr sur un TMP_Text ou un Text legacy. Réagit au changement de langue.
    /// </summary>
    [DisallowMultipleComponent]
    public class LocalizedText : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private string key;
        [SerializeField] [TextArea(1, 3)] private string frDefault;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private TMP_Text _tmp;
        private Text _legacy;
        private bool _bound;
        private bool _inert;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public string Key => key;
        public string FrDefault => frDefault;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            BindTarget();
        }

        private void OnEnable()
        {
            if (!_bound)
                BindTarget();

            Apply();
            Loc.OnLanguageChanged += Apply;
        }

        private void OnDisable()
        {
            Loc.OnLanguageChanged -= Apply;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Applique la traduction courante sur la cible texte.
        /// </summary>
        public void Apply()
        {
            if (_inert)
                return;

            string value = Loc.Tr(key, frDefault);
            if (_tmp != null)
                _tmp.text = value;
            else if (_legacy != null)
                _legacy.text = value;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Config éditeur (builder). Ne re-key jamais silencieusement.
        /// </summary>
        public void EditorSetup(string newKey, string newFrDefault)
        {
            key = newKey ?? "";
            frDefault = newFrDefault ?? "";
        }
#endif

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void BindTarget()
        {
            if (_bound)
                return;

            _bound = true;
            _tmp = GetComponent<TMP_Text>();
            if (_tmp == null)
                _legacy = GetComponent<Text>();

            if (_tmp == null && _legacy == null)
            {
                _inert = true;
                Debug.LogWarning($"[LocalizedText] Aucune cible TMP/Text sur « {name} » — composant inerte.");
            }
        }
    }
}
