#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using ChezArthur.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace ChezArthur.EditorTools.Audit
{
    /// <summary>
    /// Audit lecture seule du routage audio (mixer + sources des scènes ouvertes).
    /// </summary>
    public static class AudioRoutingAuditor
    {
        private const string MixerAssetPath = "Assets/_Project/Audio/Resources/MainMixer.mixer";
        private const string MixerResourceName = "MainMixer";

        [MenuItem("Chez Arthur/Audio/Audit Routage Audio")]
        public static void RunAudit()
        {
            var sb = new StringBuilder(4096);
            DateTime now = DateTime.Now;
            sb.AppendLine("# Audit routage audio");
            sb.AppendLine();
            sb.AppendLine($"- **Date** : {now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- **Scènes ouvertes** : {SceneManager.sceneCount}");
            sb.AppendLine();

            AppendMixerSection(sb);
            AppendSceneSourcesSection(sb);

            string auditsRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Audits"));
            Directory.CreateDirectory(auditsRoot);
            string fileName = $"AudioRouting_{now:yyyyMMdd_HHmm}.md";
            string fullPath = Path.Combine(auditsRoot, fileName);
            File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);

            Debug.Log($"[AudioRoutingAuditor] Rapport écrit : {fullPath}");
            EditorUtility.RevealInFinder(fullPath);
        }

        private static void AppendMixerSection(StringBuilder sb)
        {
            sb.AppendLine("## Mixer MainMixer");
            sb.AppendLine();

            AudioMixer asset = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerAssetPath);
            AudioMixer resources = Resources.Load<AudioMixer>(MixerResourceName);

            AppendCheck(sb, "Asset présent", asset != null, MixerAssetPath);
            AppendCheck(sb, "Resources.Load(\"MainMixer\")", resources != null, "Assets/_Project/Audio/Resources/");

            AudioMixer mixer = asset != null ? asset : resources;
            if (mixer == null)
            {
                sb.AppendLine();
                sb.AppendLine("> ❌ Mixer introuvable — suite de conformité impossible.");
                sb.AppendLine();
                return;
            }

            AppendGroupCheck(sb, mixer, "Music");
            AppendGroupCheck(sb, mixer, "Ambiance");
            AppendGroupCheck(sb, mixer, "SFX");

            AppendCheck(sb, "Param exposé MusicVolume", HasExposedParam(mixer, "MusicVolume"), "Exposed Parameters");
            AppendCheck(sb, "Param exposé SfxVolume", HasExposedParam(mixer, "SfxVolume"), "Exposed Parameters");

            AudioMixerSnapshot normal = mixer.FindSnapshot("Normal");
            AudioMixerSnapshot aim = mixer.FindSnapshot("AimFocus");
            AppendCheck(sb, "Snapshot Normal", normal != null, "Snapshots");
            AppendCheck(sb, "Snapshot AimFocus", aim != null, "Snapshots");

            if (aim != null)
            {
                // Lecture dB du snapshot via API runtime non exposée — on note la cible contrat.
                sb.AppendLine("- ℹ️ Snapshot `AimFocus` : volume Music attendu **−13 dB** (contrat F1-P1) — vérifier à l'oreille / Inspector.");
            }

            sb.AppendLine();
        }

        private static void AppendSceneSourcesSection(StringBuilder sb)
        {
            sb.AppendLine("## AudioSources (scènes ouvertes)");
            sb.AppendLine();

            var unrouted = new StringBuilder();
            int total = 0;
            int routed = 0;

            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                Scene scene = SceneManager.GetSceneAt(s);
                if (!scene.isLoaded)
                    continue;

                GameObject[] roots = scene.GetRootGameObjects();
                for (int r = 0; r < roots.Length; r++)
                    CollectSources(roots[r].transform, roots[r].name, ref total, ref routed, unrouted);
            }

            sb.AppendLine($"- Sources scannées : **{total}**");
            sb.AppendLine($"- Routées (groupe non null) : **{routed}**");
            sb.AppendLine($"- À router : **{total - routed}**");
            sb.AppendLine();

            if (total - routed > 0)
            {
                sb.AppendLine("### À router");
                sb.AppendLine();
                sb.Append(unrouted);
            }
            else
            {
                sb.AppendLine("_Aucune source non routée._");
            }

            sb.AppendLine();
            sb.AppendLine($"## AudioBuses.IsAvailable = `{AudioBuses.IsAvailable}`");
            sb.AppendLine();
        }

        private static void CollectSources(
            Transform t, string path, ref int total, ref int routed, StringBuilder unrouted)
        {
            AudioSource[] sources = t.GetComponents<AudioSource>();
            for (int i = 0; i < sources.Length; i++)
            {
                total++;
                AudioSource src = sources[i];
                if (src.outputAudioMixerGroup != null)
                {
                    routed++;
                }
                else
                {
                    unrouted.AppendLine($"- `{path}` — groupe null");
                }
            }

            for (int c = 0; c < t.childCount; c++)
            {
                Transform child = t.GetChild(c);
                CollectSources(child, path + "/" + child.name, ref total, ref routed, unrouted);
            }
        }

        private static void AppendGroupCheck(StringBuilder sb, AudioMixer mixer, string groupName)
        {
            AudioMixerGroup[] groups = mixer.FindMatchingGroups(groupName);
            bool ok = groups != null && groups.Length > 0;
            AppendCheck(sb, $"Groupe {groupName}", ok, "Master → enfants");
        }

        private static bool HasExposedParam(AudioMixer mixer, string paramName)
        {
            // SetFloat échoue silencieusement si le param n'existe pas — on teste via GetFloat.
            float unused;
            return mixer.GetFloat(paramName, out unused);
        }

        private static void AppendCheck(StringBuilder sb, string label, bool ok, string hint)
        {
            sb.AppendLine(ok
                ? $"- ✅ **{label}**"
                : $"- ❌ **{label}** — {hint}");
        }
    }
}
#endif
