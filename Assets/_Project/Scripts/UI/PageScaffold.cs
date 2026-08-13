using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// Anatomie de page RUI : Header / Titre / Scroll / Footer — zones réservées.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class PageScaffold : MonoBehaviour
    {
        [SerializeField] private RectTransform headerZone;
        [SerializeField] private RectTransform titleZone;
        [SerializeField] private RectTransform scrollZone;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform footerZone;

        public RectTransform HeaderZone => headerZone;
        public RectTransform TitleZone => titleZone;
        public RectTransform ScrollZone => scrollZone;
        public ScrollRect Scroll => scrollRect;
        public RectTransform FooterZone => footerZone;

        public void Bind(
            RectTransform header,
            RectTransform title,
            RectTransform scroll,
            ScrollRect scrollR,
            RectTransform footer)
        {
            headerZone = header;
            titleZone = title;
            scrollZone = scroll;
            scrollRect = scrollR;
            footerZone = footer;
        }
    }
}
