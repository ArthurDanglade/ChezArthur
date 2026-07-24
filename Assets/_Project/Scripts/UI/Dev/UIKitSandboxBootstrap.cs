using ChezArthur.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI.Dev
{
    /// <summary>
    /// Bootstrap Play Mode de la sandbox UI Kit uniquement (hors build joueur).
    /// Initialise le TabBar et log les clics — aucun métier.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIKitSandboxBootstrap : MonoBehaviour
    {
        [SerializeField] private TabBarUI tabBar;

        private void Start()
        {
            if (tabBar == null)
                tabBar = GetComponentInChildren<TabBarUI>(true);

            if (tabBar != null)
            {
                string[] labels = { "Quotidien", "Hebdo", "Saison", "Permanent" };
                tabBar.Init(labels, OnTabSelected, defaultIndex: 0);
            }

            HubButtonUI[] buttons = GetComponentsInChildren<HubButtonUI>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;
                Button btn = buttons[i].Button;
                if (btn == null) continue;

                string name = buttons[i].gameObject.name;
                btn.onClick.AddListener(() => Debug.Log($"[UIKitSandbox] Bouton cliqué : {name}"));
            }
        }

        private static void OnTabSelected(int index)
        {
            Debug.Log($"[UIKitSandbox] TabBar sélection index={index}");
        }
    }
}
