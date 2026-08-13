using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ChezArthur.Core;
using ChezArthur.Localization;

namespace ChezArthur.Backend
{
    /// <summary>
    /// Dialogue de conflit cloud save — overlay runtime bloquant (2 cartes + double confirmation).
    /// </summary>
    public class SaveConflictDialog : MonoBehaviour
    {
        private static SaveConflictDialog _instance;

        private Action _onLocal;
        private Action _onCloud;
        private int _confirmSide; // 0 none, 1 local, 2 cloud
        private Text _status;
        private Button _btnLocal;
        private Button _btnCloud;
        private Text _labelLocal;
        private Text _labelCloud;

        /// <summary>
        /// Affiche le dialogue. Jamais fermable sans choix.
        /// </summary>
        public static void Show(
            SaveSummary local,
            SaveSummary cloud,
            Action chooseLocal,
            Action chooseCloud)
        {
            try
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("[SaveConflictDialog]");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<SaveConflictDialog>();
                    _instance.BuildUi();
                }

                _instance._onLocal = chooseLocal;
                _instance._onCloud = chooseCloud;
                _instance._confirmSide = 0;
                _instance.Populate(local, cloud);
                _instance.gameObject.SetActive(true);
            }
            catch (Exception e)
            {
                Debug.LogError("[Cloud] Dialogue conflit impossible : " + e.Message);
            }
        }

        private void BuildUi()
        {
            EnsureEventSystem();

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();

            GameObject dim = CreatePanel(transform, "Dim", new Color(0f, 0f, 0f, 0.75f));
            StretchFull(dim.GetComponent<RectTransform>());

            GameObject panel = CreatePanel(transform, "Panel", new Color(0.12f, 0.12f, 0.16f, 0.98f));
            RectTransform pr = panel.GetComponent<RectTransform>();
            pr.anchorMin = new Vector2(0.08f, 0.2f);
            pr.anchorMax = new Vector2(0.92f, 0.8f);
            pr.offsetMin = Vector2.zero;
            pr.offsetMax = Vector2.zero;

            Text title = CreateText(panel.transform, "Title", 36,
                Loc.Tr("ui.cloud.conflict_title", "Sauvegardes différentes"));
            RectTransform tr = title.rectTransform;
            tr.anchorMin = new Vector2(0.05f, 0.88f);
            tr.anchorMax = new Vector2(0.95f, 0.98f);

            _status = CreateText(panel.transform, "Status", 26,
                Loc.Tr("ui.cloud.conflict_hint", "Choisis laquelle garder. 2e appui pour confirmer."));
            RectTransform sr = _status.rectTransform;
            sr.anchorMin = new Vector2(0.05f, 0.78f);
            sr.anchorMax = new Vector2(0.95f, 0.88f);

            // Carte local
            GameObject cardL = CreatePanel(panel.transform, "CardLocal", new Color(0.18f, 0.22f, 0.3f, 1f));
            RectTransform clr = cardL.GetComponent<RectTransform>();
            clr.anchorMin = new Vector2(0.04f, 0.28f);
            clr.anchorMax = new Vector2(0.48f, 0.76f);
            clr.offsetMin = Vector2.zero;
            clr.offsetMax = Vector2.zero;
            _labelLocal = CreateText(cardL.transform, "LocalBody", 28, "");
            StretchFull(_labelLocal.rectTransform, 12f);

            GameObject cardC = CreatePanel(panel.transform, "CardCloud", new Color(0.18f, 0.26f, 0.22f, 1f));
            RectTransform ccr = cardC.GetComponent<RectTransform>();
            ccr.anchorMin = new Vector2(0.52f, 0.28f);
            ccr.anchorMax = new Vector2(0.96f, 0.76f);
            ccr.offsetMin = Vector2.zero;
            ccr.offsetMax = Vector2.zero;
            _labelCloud = CreateText(cardC.transform, "CloudBody", 28, "");
            StretchFull(_labelCloud.rectTransform, 12f);

            _btnLocal = CreateButton(panel.transform, "BtnLocal",
                Loc.Tr("ui.cloud.keep_device", "Garder ce téléphone"),
                new Color(0.25f, 0.4f, 0.7f, 1f));
            RectTransform blr = _btnLocal.GetComponent<RectTransform>();
            blr.anchorMin = new Vector2(0.04f, 0.06f);
            blr.anchorMax = new Vector2(0.48f, 0.22f);
            blr.offsetMin = Vector2.zero;
            blr.offsetMax = Vector2.zero;
            _btnLocal.onClick.AddListener(OnLocalClicked);

            _btnCloud = CreateButton(panel.transform, "BtnCloud",
                Loc.Tr("ui.cloud.keep_cloud", "Récupérer le cloud"),
                new Color(0.25f, 0.55f, 0.35f, 1f));
            RectTransform bcr = _btnCloud.GetComponent<RectTransform>();
            bcr.anchorMin = new Vector2(0.52f, 0.06f);
            bcr.anchorMax = new Vector2(0.96f, 0.22f);
            bcr.offsetMin = Vector2.zero;
            bcr.offsetMax = Vector2.zero;
            _btnCloud.onClick.AddListener(OnCloudClicked);

            gameObject.SetActive(false);
        }

        private void Populate(SaveSummary local, SaveSummary cloud)
        {
            if (_labelLocal != null)
                _labelLocal.text = FormatCard(
                    Loc.Tr("ui.cloud.card_device", "CE TÉLÉPHONE"), local);
            if (_labelCloud != null)
                _labelCloud.text = FormatCard(
                    Loc.Tr("ui.cloud.card_cloud", "CLOUD"), cloud);
            ResetConfirmLabels();
        }

        private static string FormatCard(string title, SaveSummary s)
        {
            string when = FormatRelative(s.lastPlayedUtcTicks);
            return title + "\n\n"
                + Loc.Tr("ui.cloud.stat_chars", "Persos") + " : " + s.ownedCount + "\n"
                + "Tals : " + s.tals + "\n"
                + Loc.Tr("ui.cloud.stat_stage", "Meilleur étage") + " : " + s.bestStage + "\n"
                + Loc.Tr("ui.cloud.stat_score", "Score saison") + " : " + s.bestScoreThisSeason + "\n"
                + Loc.Tr("ui.cloud.stat_played", "Dernière partie") + " : " + when;
        }

        private static string FormatRelative(long ticks)
        {
            if (ticks <= 0)
                return "—";
            try
            {
                DateTime then = new DateTime(ticks, DateTimeKind.Utc);
                TimeSpan d = DateTime.UtcNow - then;
                if (d.TotalMinutes < 2)
                    return Loc.Tr("ui.cloud.just_now", "à l'instant");
                if (d.TotalHours < 1)
                    return ((int)d.TotalMinutes) + " min";
                if (d.TotalDays < 1)
                    return ((int)d.TotalHours) + " h";
                return ((int)d.TotalDays) + " j";
            }
            catch
            {
                return "—";
            }
        }

        private void OnLocalClicked()
        {
            if (_confirmSide != 1)
            {
                _confirmSide = 1;
                SetButtonLabel(_btnLocal, Loc.Tr("ui.cloud.confirm", "Confirmer ?"));
                SetButtonLabel(_btnCloud, Loc.Tr("ui.cloud.keep_cloud", "Récupérer le cloud"));
                if (_status != null)
                    _status.text = Loc.Tr("ui.cloud.confirm_device", "2e appui : garder ce téléphone");
                return;
            }

            Action cb = _onLocal;
            Hide();
            cb?.Invoke();
        }

        private void OnCloudClicked()
        {
            if (_confirmSide != 2)
            {
                _confirmSide = 2;
                SetButtonLabel(_btnCloud, Loc.Tr("ui.cloud.confirm", "Confirmer ?"));
                SetButtonLabel(_btnLocal, Loc.Tr("ui.cloud.keep_device", "Garder ce téléphone"));
                if (_status != null)
                    _status.text = Loc.Tr("ui.cloud.confirm_cloud", "2e appui : récupérer le cloud");
                return;
            }

            Action cb = _onCloud;
            Hide();
            cb?.Invoke();
        }

        private void ResetConfirmLabels()
        {
            _confirmSide = 0;
            SetButtonLabel(_btnLocal, Loc.Tr("ui.cloud.keep_device", "Garder ce téléphone"));
            SetButtonLabel(_btnCloud, Loc.Tr("ui.cloud.keep_cloud", "Récupérer le cloud"));
            if (_status != null)
            {
                _status.text = Loc.Tr(
                    "ui.cloud.conflict_hint",
                    "Choisis laquelle garder. 2e appui pour confirmer.");
            }
        }

        private void Hide()
        {
            gameObject.SetActive(false);
            _onLocal = null;
            _onCloud = null;
        }

        private static void SetButtonLabel(Button btn, string text)
        {
            if (btn == null)
                return;
            Text t = btn.GetComponentInChildren<Text>();
            if (t != null)
                t.text = text;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = color;
            go.AddComponent<RectTransform>();
            return go;
        }

        private static Text CreateText(Transform parent, string name, int size, string content)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = content ?? "";
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Color color)
        {
            GameObject go = CreatePanel(parent, name, color);
            Button btn = go.AddComponent<Button>();
            Text text = CreateText(go.transform, "Label", 30, label);
            text.alignment = TextAnchor.MiddleCenter;
            StretchFull(text.rectTransform);
            return btn;
        }

        private static void StretchFull(RectTransform rt, float pad = 0f)
        {
            if (rt == null)
                return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null)
                return;
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }
}
