#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ChezArthur.Characters;
using UnityEditor;
using UnityEngine;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Importe les exports Aseprite (PNG + JSON Array) hors Assets vers sheets
    /// compactés + AnimatedPortraitData, et câble CharacterData (prime / déchu).
    /// Idempotent : ré-exécution met à jour en place.
    /// </summary>
    public static class SsrPortraitImporter
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string SourcesFolder = "ArtSources/PortraitsSSR";
        private const string SheetsAssetFolder =
            "Assets/_Project/Art/Resources/CharacterPortraitsSSR";
        private const string PortraitDataFolder = "Assets/_Project/Data/Portraits";
        private const string ResourcesPathPrefix = "CharacterPortraitsSSR/";
        private const string StatePrime = "prime";
        private const string StateDechu = "dechu";
        private const string PropAnimatedPrime = "animatedPortraitPrime";
        private const string PropAnimatedDechu = "animatedPortraitDechu";
        private const int MaxSheetSide = 2048;
        private const int FrameBudgetWarning = 24;
        private const int BytesPerPixelRgba = 4;
        private const float MsToSeconds = 1000f;
        private const float BytesPerMegabyte = 1024f * 1024f;

        private static readonly Regex FileNameRegex = new Regex(
            @"^portrait_(?<id>[a-z0-9]+)_(?<state>prime|dechu)$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        // ═══════════════════════════════════════════
        // DTOs ASEPRITE (JsonUtility)
        // ═══════════════════════════════════════════
        [Serializable]
        private class AseDoc
        {
            public AseFrame[] frames;
        }

        [Serializable]
        private class AseFrame
        {
            public string filename;
            public AseRect frame;
            public bool rotated;
            public bool trimmed;
            public AseSize sourceSize;
            public int duration;
        }

        [Serializable]
        private class AseRect
        {
            public int x;
            public int y;
            public int w;
            public int h;
        }

        [Serializable]
        private class AseSize
        {
            public int w;
            public int h;
        }

        private struct SourcePair
        {
            public string Id;
            public string State;
            public string PngPath;
            public string JsonPath;
        }

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/Art/Importer portraits SSR (sources → sheets)")]
        private static void ImportAllMenu()
        {
            string sourcesAbs = Path.GetFullPath(
                Path.Combine(Directory.GetCurrentDirectory(), SourcesFolder));

            if (!Directory.Exists(sourcesAbs))
            {
                Directory.CreateDirectory(sourcesAbs);
                Debug.Log(
                    $"[SsrPortraitImporter] Dossier source créé : {sourcesAbs}\n" +
                    "Déposez les paires portrait_{id}_{state}.png/.json ici, puis relancez le menu.");
                return;
            }

            List<SourcePair> pairs = CollectPairs(sourcesAbs);
            int okCount = 0;
            int skipCount = 0;

            try
            {
                for (int i = 0; i < pairs.Count; i++)
                {
                    SourcePair pair = pairs[i];
                    try
                    {
                        if (ProcessPair(pair))
                            okCount++;
                        else
                            skipCount++;
                    }
                    catch (Exception ex)
                    {
                        skipCount++;
                        Debug.LogError(
                            $"[SsrPortraitImporter] Échec {pair.Id}/{pair.State} : {ex.Message}\n{ex}");
                    }
                }
            }
            finally
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log(
                $"[SsrPortraitImporter] Terminé — OK : {okCount} | skippées : {skipCount} " +
                $"(sur {pairs.Count} paires).");
        }

        // ═══════════════════════════════════════════
        // COLLECTE DES PAIRES
        // ═══════════════════════════════════════════

        private static List<SourcePair> CollectPairs(string sourcesAbs)
        {
            // key = nom sans extension (portrait_id_state)
            Dictionary<string, SourcePair> map = new Dictionary<string, SourcePair>(32);
            string[] files = Directory.GetFiles(sourcesAbs);

            for (int i = 0; i < files.Length; i++)
            {
                string filePath = files[i];
                string ext = Path.GetExtension(filePath);
                if (ext == null)
                    continue;

                string extLower = ext.ToLowerInvariant();
                if (extLower != ".png" && extLower != ".json")
                    continue;

                string name = Path.GetFileNameWithoutExtension(filePath);
                Match match = FileNameRegex.Match(name);
                if (!match.Success)
                {
                    Debug.LogWarning(
                        $"[SsrPortraitImporter] Fichier ignoré (nom hors convention) : {Path.GetFileName(filePath)}");
                    continue;
                }

                string id = match.Groups["id"].Value;
                string state = match.Groups["state"].Value;

                if (!map.TryGetValue(name, out SourcePair pair))
                {
                    pair = new SourcePair
                    {
                        Id = id,
                        State = state,
                        PngPath = null,
                        JsonPath = null
                    };
                }

                if (extLower == ".png")
                    pair.PngPath = filePath;
                else
                    pair.JsonPath = filePath;

                map[name] = pair;
            }

            List<SourcePair> result = new List<SourcePair>(map.Count);
            foreach (KeyValuePair<string, SourcePair> kvp in map)
            {
                SourcePair pair = kvp.Value;
                if (string.IsNullOrEmpty(pair.PngPath) || string.IsNullOrEmpty(pair.JsonPath))
                {
                    string missing = string.IsNullOrEmpty(pair.PngPath) ? "PNG" : "JSON";
                    Debug.LogError(
                        $"[SsrPortraitImporter] Paire incomplète '{kvp.Key}' — {missing} manquant. Skip.");
                    continue;
                }

                result.Add(pair);
            }

            result.Sort((a, b) =>
            {
                int idCmp = string.CompareOrdinal(a.Id, b.Id);
                return idCmp != 0 ? idCmp : string.CompareOrdinal(a.State, b.State);
            });

            return result;
        }

        // ═══════════════════════════════════════════
        // TRAITEMENT D'UNE PAIRE
        // ═══════════════════════════════════════════

        private static bool ProcessPair(SourcePair pair)
        {
            Texture2D sourceTex = null;
            Texture2D sheetTex = null;

            try
            {
                if (!TryParseAseJson(pair.JsonPath, out AseDoc doc, out string jsonError))
                {
                    Debug.LogError(
                        $"[SsrPortraitImporter] {pair.Id}/{pair.State} : JSON invalide — {jsonError}. Skip.");
                    return false;
                }

                if (!TryValidateFrames(doc.frames, out int cellW, out int cellH, out string frameError))
                {
                    Debug.LogError(
                        $"[SsrPortraitImporter] {pair.Id}/{pair.State} : {frameError}. Skip.");
                    return false;
                }

                sourceTex = LoadSourceTexture(pair.PngPath);
                if (sourceTex == null)
                {
                    Debug.LogError(
                        $"[SsrPortraitImporter] {pair.Id}/{pair.State} : échec chargement PNG. Skip.");
                    return false;
                }

                AseFrame[] aseFrames = doc.frames;
                int sourceCount = aseFrames.Length;

                List<Color32[]> uniquePixels = new List<Color32[]>(sourceCount);
                int[] cellBySource = new int[sourceCount];

                for (int i = 0; i < sourceCount; i++)
                {
                    Color32[] pixels = ExtractFramePixels(sourceTex, aseFrames[i].frame);
                    int cellIndex = FindExactPixelMatch(uniquePixels, pixels);
                    if (cellIndex < 0)
                    {
                        cellIndex = uniquePixels.Count;
                        uniquePixels.Add(pixels);
                    }

                    cellBySource[i] = cellIndex;
                }

                List<PortraitFrame> timeline = BuildTimeline(aseFrames, cellBySource);
                int uniqueCount = uniquePixels.Count;

                if (uniqueCount > FrameBudgetWarning)
                {
                    Debug.LogWarning(
                        $"[SsrPortraitImporter] {pair.Id}/{pair.State} : {uniqueCount} cellules uniques " +
                        $"> budget {FrameBudgetWarning} (contrat Dharu).");
                }

                if (!TryChooseGrid(uniqueCount, cellW, cellH, out int cols, out int rows))
                {
                    Debug.LogError(
                        $"[SsrPortraitImporter] {pair.Id}/{pair.State} : aucune grille " +
                        $"≤ {MaxSheetSide}px pour {uniqueCount} cellules {cellW}x{cellH}. Skip.");
                    return false;
                }

                int sheetW = cols * cellW;
                int sheetH = rows * cellH;
                sheetTex = ComposeSheet(uniquePixels, cols, cellW, cellH, sheetW, sheetH);

                EnsureAssetFolder(SheetsAssetFolder);
                string pngAssetPath =
                    $"{SheetsAssetFolder}/portrait_{pair.Id}_{pair.State}.png";
                string pngAbs = Path.GetFullPath(
                    Path.Combine(Directory.GetCurrentDirectory(), pngAssetPath));
                File.WriteAllBytes(pngAbs, sheetTex.EncodeToPNG());
                AssetDatabase.ImportAsset(pngAssetPath, ImportAssetOptions.ForceUpdate);

                string resourcesPath = $"{ResourcesPathPrefix}portrait_{pair.Id}_{pair.State}";
                AnimatedPortraitData data = UpsertPortraitData(
                    pair.Id, pair.State, resourcesPath, cols, rows, cellW, cellH, timeline);

                TryWireCharacterData(pair.Id, pair.State, data);

                float totalSec = 0f;
                for (int s = 0; s < timeline.Count; s++)
                    totalSec += timeline[s].duration;

                float vramMo = (sheetW * sheetH * BytesPerPixelRgba) / BytesPerMegabyte;

                Debug.Log(
                    $"[SsrPortraitImporter] {pair.Id}/{pair.State} : {sourceCount} frames sources → " +
                    $"{uniqueCount} uniques | grille {cols}x{rows} = {sheetW}x{sheetH}px | " +
                    $"{timeline.Count} segments, boucle {totalSec:F1}s | VRAM ≈ {vramMo:F2} Mo");

                return true;
            }
            finally
            {
                if (sourceTex != null)
                    UnityEngine.Object.DestroyImmediate(sourceTex);
                if (sheetTex != null)
                    UnityEngine.Object.DestroyImmediate(sheetTex);
            }
        }

        // ═══════════════════════════════════════════
        // JSON / TEXTURE SOURCE
        // ═══════════════════════════════════════════

        private static bool TryParseAseJson(string jsonPath, out AseDoc doc, out string error)
        {
            doc = null;
            error = null;

            string json = File.ReadAllText(jsonPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "fichier vide";
                return false;
            }

            try
            {
                doc = JsonUtility.FromJson<AseDoc>(json);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }

            if (doc == null || doc.frames == null || doc.frames.Length == 0)
            {
                error = "frames vide ou absentes";
                return false;
            }

            return true;
        }

        private static bool TryValidateFrames(
            AseFrame[] frames,
            out int cellW,
            out int cellH,
            out string error)
        {
            cellW = 0;
            cellH = 0;
            error = null;

            for (int i = 0; i < frames.Length; i++)
            {
                AseFrame f = frames[i];
                if (f == null || f.frame == null)
                {
                    error = $"frame[{i}] nulle";
                    return false;
                }

                if (f.rotated)
                {
                    error = $"frame[{i}] rotated=true (non supporté)";
                    return false;
                }

                if (f.trimmed)
                {
                    error = $"frame[{i}] trimmed=true (non supporté)";
                    return false;
                }

                if (i == 0)
                {
                    cellW = f.frame.w;
                    cellH = f.frame.h;
                    if (cellW < 1 || cellH < 1)
                    {
                        error = "taille de cellule invalide";
                        return false;
                    }
                }
                else if (f.frame.w != cellW || f.frame.h != cellH)
                {
                    error =
                        $"tailles de cellule hétérogènes (frame0={cellW}x{cellH}, " +
                        $"frame{i}={f.frame.w}x{f.frame.h})";
                    return false;
                }
            }

            return true;
        }

        private static Texture2D LoadSourceTexture(string pngPath)
        {
            byte[] bytes = File.ReadAllBytes(pngPath);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(tex, bytes, false))
            {
                UnityEngine.Object.DestroyImmediate(tex);
                return null;
            }

            return tex;
        }

        /// <summary>
        /// Extraction avec inversion Y n°1 : rect Aseprite (origine haut-gauche)
        /// → GetPixels Unity (origine bas-gauche).
        /// </summary>
        private static Color32[] ExtractFramePixels(Texture2D tex, AseRect rect)
        {
            int yBottom = tex.height - rect.y - rect.h;
            Color[] colors = tex.GetPixels(rect.x, yBottom, rect.w, rect.h);
            Color32[] pixels = new Color32[colors.Length];
            for (int i = 0; i < colors.Length; i++)
                pixels[i] = colors[i];
            return pixels;
        }

        // ═══════════════════════════════════════════
        // DÉDUPLICATION + TIMELINE
        // ═══════════════════════════════════════════

        private static int FindExactPixelMatch(List<Color32[]> uniques, Color32[] candidate)
        {
            for (int u = 0; u < uniques.Count; u++)
            {
                if (PixelsEqual(uniques[u], candidate))
                    return u;
            }

            return -1;
        }

        private static bool PixelsEqual(Color32[] a, Color32[] b)
        {
            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                Color32 pa = a[i];
                Color32 pb = b[i];
                if (pa.r != pb.r || pa.g != pb.g || pa.b != pb.b || pa.a != pb.a)
                    return false;
            }

            return true;
        }

        private static List<PortraitFrame> BuildTimeline(AseFrame[] aseFrames, int[] cellBySource)
        {
            List<PortraitFrame> timeline = new List<PortraitFrame>(aseFrames.Length);

            for (int i = 0; i < aseFrames.Length; i++)
            {
                int cellIndex = cellBySource[i];
                float durationSec = aseFrames[i].duration / MsToSeconds;

                if (timeline.Count > 0)
                {
                    int last = timeline.Count - 1;
                    PortraitFrame prev = timeline[last];
                    if (prev.cellIndex == cellIndex)
                    {
                        prev.duration += durationSec;
                        timeline[last] = prev;
                        continue;
                    }
                }

                timeline.Add(new PortraitFrame
                {
                    cellIndex = cellIndex,
                    duration = durationSec
                });
            }

            return timeline;
        }

        // ═══════════════════════════════════════════
        // GRILLE + COMPOSITION SHEET
        // ═══════════════════════════════════════════

        private static bool TryChooseGrid(int cellCount, int cellW, int cellH, out int cols, out int rows)
        {
            cols = 0;
            rows = 0;
            if (cellCount < 1)
                return false;

            int bestWasted = int.MaxValue;
            int bestAspectDiff = int.MaxValue;
            bool found = false;

            for (int c = 1; c <= cellCount; c++)
            {
                int r = (cellCount + c - 1) / c;
                int widthPx = c * cellW;
                int heightPx = r * cellH;
                if (widthPx > MaxSheetSide || heightPx > MaxSheetSide)
                    continue;

                int wasted = (c * r) - cellCount;
                int aspectDiff = Mathf.Abs(widthPx - heightPx);

                if (!found
                    || wasted < bestWasted
                    || (wasted == bestWasted && aspectDiff < bestAspectDiff))
                {
                    found = true;
                    bestWasted = wasted;
                    bestAspectDiff = aspectDiff;
                    cols = c;
                    rows = r;
                }
            }

            return found;
        }

        /// <summary>
        /// Composition avec inversion Y n°2 : index cellule G→D puis HAUT→BAS
        /// (symétrique de AnimatedPortraitData.GetCellUvRect).
        /// </summary>
        private static Texture2D ComposeSheet(
            List<Color32[]> uniquePixels,
            int cols,
            int cellW,
            int cellH,
            int sheetW,
            int sheetH)
        {
            Texture2D sheet = new Texture2D(sheetW, sheetH, TextureFormat.RGBA32, false);
            Color32[] clear = new Color32[sheetW * sheetH];
            sheet.SetPixels32(clear);

            for (int i = 0; i < uniquePixels.Count; i++)
            {
                int col = i % cols;
                int row = i / cols;
                int xPx = col * cellW;
                int yPxBottom = sheetH - (row + 1) * cellH;
                sheet.SetPixels32(xPx, yPxBottom, cellW, cellH, uniquePixels[i]);
            }

            sheet.Apply(false, false);
            return sheet;
        }

        // ═══════════════════════════════════════════
        // ASSETS + CÂBLAGE
        // ═══════════════════════════════════════════

        private static AnimatedPortraitData UpsertPortraitData(
            string id,
            string state,
            string resourcesPath,
            int cols,
            int rows,
            int cellW,
            int cellH,
            List<PortraitFrame> timeline)
        {
            EnsureAssetFolder(PortraitDataFolder);
            string assetPath = $"{PortraitDataFolder}/PA_{id}_{state}.asset";

            AnimatedPortraitData data =
                AssetDatabase.LoadAssetAtPath<AnimatedPortraitData>(assetPath);

            if (data != null)
            {
                data.EditorInitialize(resourcesPath, cols, rows, cellW, cellH, timeline);
                EditorUtility.SetDirty(data);
            }
            else
            {
                data = ScriptableObject.CreateInstance<AnimatedPortraitData>();
                data.EditorInitialize(resourcesPath, cols, rows, cellW, cellH, timeline);
                AssetDatabase.CreateAsset(data, assetPath);
            }

            return data;
        }

        private static void TryWireCharacterData(
            string id,
            string state,
            AnimatedPortraitData portraitData)
        {
            string[] guids = AssetDatabase.FindAssets("t:CharacterData");
            CharacterData match = null;
            int matchCount = 0;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
                if (character == null)
                    continue;

                string characterId = character.Id != null
                    ? character.Id.ToLowerInvariant()
                    : string.Empty;

                if (!string.Equals(characterId, id, StringComparison.Ordinal))
                    continue;

                matchCount++;
                match = character;
            }

            if (matchCount > 1)
            {
                Debug.LogError(
                    $"[SsrPortraitImporter] Plusieurs CharacterData avec id='{id}' " +
                    $"— câblage {state} annulé.");
                return;
            }

            if (matchCount == 0 || match == null)
            {
                Debug.LogWarning(
                    $"[SsrPortraitImporter] Aucun CharacterData avec id='{id}' " +
                    $"— PA_{id}_{state} créé, câblage manuel possible.");
                return;
            }

            string propertyName;
            if (state == StatePrime)
                propertyName = PropAnimatedPrime;
            else if (state == StateDechu)
                propertyName = PropAnimatedDechu;
            else
            {
                Debug.LogError(
                    $"[SsrPortraitImporter] État inconnu '{state}' pour id='{id}' — câblage annulé.");
                return;
            }

            SerializedObject so = new SerializedObject(match);
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.LogError(
                    $"[SsrPortraitImporter] Propriété '{propertyName}' introuvable sur {match.name}.");
                return;
            }

            prop.objectReferenceValue = portraitData;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(match);
        }

        private static void EnsureAssetFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder))
                return;

            string[] parts = assetFolder.Split('/');
            if (parts.Length < 2)
                return;

            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
