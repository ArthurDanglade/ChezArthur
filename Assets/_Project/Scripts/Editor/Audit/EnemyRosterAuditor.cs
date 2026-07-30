#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Enemies.Passives.Handlers;
using UnityEditor;
using UnityEngine;

namespace ChezArthur.EditorTools.Audit
{
    /// <summary>
    /// Audit lecture seule du roster ennemis Univers 1 (Gate 0 — refonte ennemis).
    /// Ne modifie aucun asset — écrit uniquement un rapport Markdown hors Assets/.
    /// </summary>
    public static class EnemyRosterAuditor
    {
        // ═══════════════════════════════════════════
        // CONSTANTES — contrat §5
        // ═══════════════════════════════════════════

        private const int TARGET_UNIVERSE = 1;
        private const int MIN_UNIVERSE_INDEX = 0;
        private const int MAX_UNIVERSE_INDEX = 5;

        private sealed class ExpectedEnemy
        {
            public string Id;
            public bool HasTypeRole;
            public EnemyType ExpectedType;
            public EnemyRole ExpectedRole;
            public string CreationStatus;
        }

        /// <summary>
        /// Table des attendus du contrat Refonte_Ennemis_Design_U1_v1 §5 — seule donnée embarquée.
        /// </summary>
        private static readonly ExpectedEnemy[] ExpectedRoster =
        {
            new ExpectedEnemy
            {
                Id = "disciple_trop_motive",
                HasTypeRole = true,
                ExpectedType = EnemyType.MobWeak,
                ExpectedRole = EnemyRole.Basique,
                CreationStatus = "existant"
            },
            new ExpectedEnemy
            {
                Id = "archere_precise",
                HasTypeRole = true,
                ExpectedType = EnemyType.MobStandard,
                ExpectedRole = EnemyRole.Basique,
                CreationStatus = "existant"
            },
            new ExpectedEnemy
            {
                Id = "garde_aux_pieux",
                HasTypeRole = true,
                ExpectedType = EnemyType.MobElite,
                ExpectedRole = EnemyRole.Basique,
                CreationStatus = "existant"
            },
            new ExpectedEnemy
            {
                Id = "le_confesseur",
                HasTypeRole = true,
                ExpectedType = EnemyType.MiniBoss,
                ExpectedRole = EnemyRole.MiniBoss,
                CreationStatus = "existant"
            },
            new ExpectedEnemy
            {
                Id = "la_veuve_en_deuil",
                HasTypeRole = true,
                ExpectedType = EnemyType.Boss,
                ExpectedRole = EnemyRole.Boss,
                CreationStatus = "existant"
            },
            new ExpectedEnemy
            {
                Id = "alucadra",
                HasTypeRole = true,
                ExpectedType = EnemyType.Boss,
                ExpectedRole = EnemyRole.MiniBoss,
                CreationStatus = "à créer (G6a) — exception actée D30 (slot étage 15)"
            },
            new ExpectedEnemy
            {
                Id = "patriarche_vandermont",
                HasTypeRole = true,
                ExpectedType = EnemyType.Boss,
                ExpectedRole = EnemyRole.Boss,
                CreationStatus = "existant"
            },
            new ExpectedEnemy
            {
                Id = "epee_volante",
                HasTypeRole = false,
                ExpectedType = EnemyType.MobWeak,
                ExpectedRole = EnemyRole.Basique,
                CreationStatus = "à créer (G6a) — compagnon, type/rôle tranchés au G6c"
            }
        };

        private const string UnexpectedDernierPieuId = "dernier_pieu";
        private const string UnexpectedDernierPieuNote = "retrait prévu G6a — D29";

        // ═══════════════════════════════════════════
        // STRUCTURES DE COLLECTE
        // ═══════════════════════════════════════════

        private sealed class PassiveSlotInfo
        {
            public int Index;
            public bool IsNone;
            public bool IsMissing;
            public string PassiveAssetPath;
            public string PassiveAssetName;
            public string Trigger;
            public string Condition;
            public string Effect;
            public string SpecialHandlerId;
            public bool PassiveNameEmpty;
            public bool DescriptionEmpty;
            public bool PoolAMissing;
            public bool PoolBMissing;
        }

        private sealed class EnemyEntry
        {
            public string AssetPath;
            public string AssetFileName;
            public string Id;
            public string EnemyName;
            public EnemyType EnemyType;
            public EnemyRole EnemyRole;
            public int UniverseIndex;
            public int BaseHp;
            public int BaseAtk;
            public int BaseDef;
            public int BaseSpeed;
            public bool CombatSpriteNone;
            public bool CombatSpriteMissing;
            public readonly List<PassiveSlotInfo> Passives = new List<PassiveSlotInfo>(4);
        }

        private sealed class MissingRefEntry
        {
            public string CarrierPath;
            public string Field;
        }

        private sealed class DeadHandlerEntry
        {
            public string HandlerId;
            public string EnemyId;
            public string PassivePath;
        }

        private sealed class AuditData
        {
            public DateTime GeneratedAt;
            public readonly int[] CountByUniverse = new int[MAX_UNIVERSE_INDEX + 1];
            public int CountOutsideUniverseRange;
            public int TotalEnemyCount;
            public readonly List<EnemyEntry> U1Enemies = new List<EnemyEntry>(16);

            public readonly List<string> PresentExpectedIds = new List<string>(8);
            public readonly List<ExpectedEnemy> AbsentExpected = new List<ExpectedEnemy>(8);
            public readonly List<EnemyEntry> UnexpectedPresent = new List<EnemyEntry>(8);

            public int PassiveCount;
            public int EmptyPassiveNameCount;
            public int EmptyDescriptionCount;

            public readonly List<MissingRefEntry> MissingRefs = new List<MissingRefEntry>(16);
            public bool HandlerRegistryReadable = true;
            public readonly List<DeadHandlerEntry> DeadHandlers = new List<DeadHandlerEntry>(16);

            public readonly List<string> CriticalFieldLines = new List<string>(32);

            public readonly List<string> MatrixViolationLines = new List<string>(16);
            public readonly List<string> MatrixExceptionLines = new List<string>(4);
            public readonly List<string> ContractGapLines = new List<string>(16);

            public int Section1Findings;
            public int Section2Findings;
            public int Section3Findings;
            public int Section4Findings;
            public int Section5Findings;
        }

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/Audit/Auditer les ennemis U1 (G0)")]
        public static void AuditEnemyRosterU1()
        {
            AuditData data = Collect();
            string reportPath = WriteMarkdownReport(data);
            LogConsoleSummary(data, reportPath);
        }

        // ═══════════════════════════════════════════
        // COLLECTE (lecture seule)
        // ═══════════════════════════════════════════

        private static AuditData Collect()
        {
            var data = new AuditData
            {
                GeneratedAt = DateTime.Now
            };

            HashSet<string> validHandlerIds = TryLoadHandlerIds(data);

            string[] guids = AssetDatabase.FindAssets("t:EnemyData");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                EnemyData enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
                if (enemy == null)
                    continue;

                data.TotalEnemyCount++;
                int u = enemy.UniverseIndex;
                if (u >= MIN_UNIVERSE_INDEX && u <= MAX_UNIVERSE_INDEX)
                    data.CountByUniverse[u]++;
                else
                    data.CountOutsideUniverseRange++;

                EnemyEntry entry = BuildEnemyEntry(path, enemy, validHandlerIds, data);
                if (entry.UniverseIndex == TARGET_UNIVERSE)
                    data.U1Enemies.Add(entry);
            }

            data.U1Enemies.Sort(CompareEnemyByIdThenPath);
            ClassifyAgainstContract(data);
            BuildCriticalFields(data);
            BuildTypeRoleInconsistencies(data);
            ComputeSectionFindings(data);
            return data;
        }

        private static HashSet<string> TryLoadHandlerIds(AuditData data)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            EnemyPassiveHandlerRegistry.RegisterAll();

            FieldInfo field = typeof(EnemyPassiveRuntime).GetField(
                "HandlerFactories",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (field == null)
            {
                data.HandlerRegistryReadable = false;
                return ids;
            }

            object raw = field.GetValue(null);
            if (!(raw is System.Collections.IDictionary dict))
            {
                data.HandlerRegistryReadable = false;
                return ids;
            }

            foreach (object key in dict.Keys)
            {
                if (key is string s && !string.IsNullOrEmpty(s))
                    ids.Add(s);
            }

            return ids;
        }

        private static EnemyEntry BuildEnemyEntry(
            string path,
            EnemyData enemy,
            HashSet<string> validHandlerIds,
            AuditData data)
        {
            var entry = new EnemyEntry
            {
                AssetPath = path,
                AssetFileName = Path.GetFileName(path),
                Id = enemy.Id ?? string.Empty,
                EnemyName = enemy.EnemyName ?? string.Empty,
                EnemyType = enemy.EnemyType,
                EnemyRole = enemy.EnemyRole,
                UniverseIndex = enemy.UniverseIndex,
                BaseHp = enemy.BaseHp,
                BaseAtk = enemy.BaseAtk,
                BaseDef = enemy.BaseDef,
                BaseSpeed = enemy.BaseSpeed
            };

            var so = new SerializedObject(enemy);
            ClassifyObjectRef(
                so.FindProperty("combatSprite"),
                out entry.CombatSpriteNone,
                out entry.CombatSpriteMissing);

            if (entry.CombatSpriteMissing)
            {
                data.MissingRefs.Add(new MissingRefEntry
                {
                    CarrierPath = path,
                    Field = "combatSprite"
                });
            }

            SerializedProperty passivesProp = so.FindProperty("enemyPassives");
            if (passivesProp != null && passivesProp.isArray)
            {
                for (int i = 0; i < passivesProp.arraySize; i++)
                {
                    SerializedProperty element = passivesProp.GetArrayElementAtIndex(i);
                    PassiveSlotInfo slot = BuildPassiveSlot(
                        path, i, element, validHandlerIds, data, entry.Id);
                    entry.Passives.Add(slot);
                }
            }

            return entry;
        }

        private static PassiveSlotInfo BuildPassiveSlot(
            string enemyPath,
            int index,
            SerializedProperty element,
            HashSet<string> validHandlerIds,
            AuditData data,
            string enemyId)
        {
            var slot = new PassiveSlotInfo { Index = index };

            ClassifyObjectRef(element, out slot.IsNone, out slot.IsMissing);

            if (slot.IsMissing)
            {
                data.MissingRefs.Add(new MissingRefEntry
                {
                    CarrierPath = enemyPath,
                    Field = $"enemyPassives[{index}]"
                });
                return slot;
            }

            if (slot.IsNone)
                return slot;

            EnemyPassiveData passive = element.objectReferenceValue as EnemyPassiveData;
            if (passive == null)
                return slot;

            string passivePath = AssetDatabase.GetAssetPath(passive);
            slot.PassiveAssetPath = passivePath;
            slot.PassiveAssetName = Path.GetFileName(passivePath);
            slot.Trigger = passive.Trigger.ToString();
            slot.Condition = passive.Condition.ToString();
            slot.Effect = passive.Effect.ToString();
            slot.SpecialHandlerId = passive.SpecialHandlerId ?? string.Empty;
            slot.PassiveNameEmpty = string.IsNullOrEmpty(passive.PassiveName);
            slot.DescriptionEmpty = string.IsNullOrEmpty(passive.Description);

            var passiveSo = new SerializedObject(passive);
            ClassifyObjectRef(
                passiveSo.FindProperty("poolPassiveA"),
                out bool poolANone,
                out slot.PoolAMissing);
            ClassifyObjectRef(
                passiveSo.FindProperty("poolPassiveB"),
                out bool poolBNone,
                out slot.PoolBMissing);
            _ = poolANone;
            _ = poolBNone;

            if (slot.PoolAMissing)
            {
                data.MissingRefs.Add(new MissingRefEntry
                {
                    CarrierPath = passivePath,
                    Field = "poolPassiveA"
                });
            }

            if (slot.PoolBMissing)
            {
                data.MissingRefs.Add(new MissingRefEntry
                {
                    CarrierPath = passivePath,
                    Field = "poolPassiveB"
                });
            }

            if (data.HandlerRegistryReadable
                && !string.IsNullOrEmpty(slot.SpecialHandlerId)
                && !validHandlerIds.Contains(slot.SpecialHandlerId))
            {
                data.DeadHandlers.Add(new DeadHandlerEntry
                {
                    HandlerId = slot.SpecialHandlerId,
                    EnemyId = enemyId,
                    PassivePath = passivePath
                });
            }

            return slot;
        }

        private static void ClassifyObjectRef(
            SerializedProperty prop,
            out bool isNone,
            out bool isMissing)
        {
            isNone = false;
            isMissing = false;
            if (prop == null)
            {
                isNone = true;
                return;
            }

            if (prop.objectReferenceValue != null)
                return;

            if (prop.objectReferenceInstanceIDValue != 0)
                isMissing = true;
            else
                isNone = true;
        }

        private static void ClassifyAgainstContract(AuditData data)
        {
            var presentById = new Dictionary<string, EnemyEntry>(StringComparer.Ordinal);
            for (int i = 0; i < data.U1Enemies.Count; i++)
            {
                EnemyEntry e = data.U1Enemies[i];
                if (string.IsNullOrEmpty(e.Id))
                    continue;
                if (!presentById.ContainsKey(e.Id))
                    presentById[e.Id] = e;
            }

            var expectedIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < ExpectedRoster.Length; i++)
            {
                ExpectedEnemy exp = ExpectedRoster[i];
                expectedIds.Add(exp.Id);
                if (presentById.ContainsKey(exp.Id))
                    data.PresentExpectedIds.Add(exp.Id);
                else
                    data.AbsentExpected.Add(exp);
            }

            data.PresentExpectedIds.Sort(StringComparer.Ordinal);

            for (int i = 0; i < data.U1Enemies.Count; i++)
            {
                EnemyEntry e = data.U1Enemies[i];
                if (string.IsNullOrEmpty(e.Id) || expectedIds.Contains(e.Id))
                    continue;
                data.UnexpectedPresent.Add(e);
            }
        }

        private static void BuildCriticalFields(AuditData data)
        {
            for (int i = 0; i < data.U1Enemies.Count; i++)
            {
                EnemyEntry e = data.U1Enemies[i];

                if (string.IsNullOrEmpty(e.Id))
                    data.CriticalFieldLines.Add($"- `{e.AssetPath}` — `id` vide");
                if (string.IsNullOrEmpty(e.EnemyName))
                    data.CriticalFieldLines.Add($"- `{e.AssetPath}` — `enemyName` vide");
                if (e.CombatSpriteNone)
                    data.CriticalFieldLines.Add($"- `{e.AssetPath}` — `combatSprite` None");
                if (e.BaseHp <= 0)
                    data.CriticalFieldLines.Add($"- `{e.AssetPath}` — `baseHp` ≤ 0 ({e.BaseHp})");
                if (e.BaseAtk <= 0)
                    data.CriticalFieldLines.Add($"- `{e.AssetPath}` — `baseAtk` ≤ 0 ({e.BaseAtk})");
                if (e.BaseDef <= 0)
                    data.CriticalFieldLines.Add($"- `{e.AssetPath}` — `baseDef` ≤ 0 ({e.BaseDef})");
                if (e.BaseSpeed <= 0)
                    data.CriticalFieldLines.Add($"- `{e.AssetPath}` — `baseSpeed` ≤ 0 ({e.BaseSpeed})");
                if (e.UniverseIndex < MIN_UNIVERSE_INDEX || e.UniverseIndex > MAX_UNIVERSE_INDEX)
                {
                    data.CriticalFieldLines.Add(
                        $"- `{e.AssetPath}` — `universeIndex` hors [0..5] ({e.UniverseIndex})");
                }
            }
        }

        private static void BuildTypeRoleInconsistencies(AuditData data)
        {
            var expectedById = new Dictionary<string, ExpectedEnemy>(StringComparer.Ordinal);
            for (int i = 0; i < ExpectedRoster.Length; i++)
                expectedById[ExpectedRoster[i].Id] = ExpectedRoster[i];

            for (int i = 0; i < data.U1Enemies.Count; i++)
            {
                EnemyEntry e = data.U1Enemies[i];
                bool matrixOk = IsInternalMatrixPair(e.EnemyType, e.EnemyRole);

                ExpectedEnemy exp = null;
                bool hasExp = !string.IsNullOrEmpty(e.Id) && expectedById.TryGetValue(e.Id, out exp);

                if (!matrixOk)
                {
                    bool isActedException = hasExp
                        && exp.HasTypeRole
                        && exp.ExpectedType == e.EnemyType
                        && exp.ExpectedRole == e.EnemyRole;

                    string line =
                        $"- `{e.Id}` (`{e.AssetFileName}`) — {e.EnemyType} / {e.EnemyRole}";

                    if (isActedException)
                    {
                        data.MatrixExceptionLines.Add(
                            line + " — exception actée (D30, slot étage 15)");
                    }
                    else
                    {
                        data.MatrixViolationLines.Add(line);
                    }
                }

                if (hasExp && exp.HasTypeRole)
                {
                    if (e.EnemyType != exp.ExpectedType || e.EnemyRole != exp.ExpectedRole)
                    {
                        data.ContractGapLines.Add(
                            $"- `{e.Id}` — réel {e.EnemyType}/{e.EnemyRole} · attendu {exp.ExpectedType}/{exp.ExpectedRole}");
                    }
                }
            }
        }

        private static bool IsInternalMatrixPair(EnemyType type, EnemyRole role)
        {
            switch (type)
            {
                case EnemyType.MobWeak:
                case EnemyType.MobStandard:
                case EnemyType.MobElite:
                    return role == EnemyRole.Basique;
                case EnemyType.MiniBoss:
                    return role == EnemyRole.MiniBoss;
                case EnemyType.Boss:
                    return role == EnemyRole.Boss;
                default:
                    return false;
            }
        }

        private static void ComputeSectionFindings(AuditData data)
        {
            data.PassiveCount = 0;
            data.EmptyPassiveNameCount = 0;
            data.EmptyDescriptionCount = 0;

            for (int i = 0; i < data.U1Enemies.Count; i++)
            {
                EnemyEntry e = data.U1Enemies[i];
                for (int p = 0; p < e.Passives.Count; p++)
                {
                    PassiveSlotInfo slot = e.Passives[p];
                    if (slot.IsNone || slot.IsMissing || string.IsNullOrEmpty(slot.PassiveAssetPath))
                        continue;

                    data.PassiveCount++;
                    if (slot.PassiveNameEmpty)
                        data.EmptyPassiveNameCount++;
                    if (slot.DescriptionEmpty)
                        data.EmptyDescriptionCount++;
                }
            }

            data.Section1Findings =
                data.AbsentExpected.Count + data.UnexpectedPresent.Count;
            data.Section2Findings =
                data.EmptyPassiveNameCount + data.EmptyDescriptionCount;
            data.Section3Findings =
                data.MissingRefs.Count
                + (data.HandlerRegistryReadable ? data.DeadHandlers.Count : 1);
            data.Section4Findings = data.CriticalFieldLines.Count;
            data.Section5Findings =
                data.MatrixViolationLines.Count
                + data.MatrixExceptionLines.Count
                + data.ContractGapLines.Count;
        }

        private static int CompareEnemyByIdThenPath(EnemyEntry a, EnemyEntry b)
        {
            int c = string.CompareOrdinal(a.Id, b.Id);
            if (c != 0)
                return c;
            return string.CompareOrdinal(a.AssetPath, b.AssetPath);
        }

        // ═══════════════════════════════════════════
        // RAPPORT MARKDOWN
        // ═══════════════════════════════════════════

        private static string WriteMarkdownReport(AuditData data)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string auditsDir = Path.Combine(projectRoot, "Audits");
            if (!Directory.Exists(auditsDir))
                Directory.CreateDirectory(auditsDir);

            string stamp = data.GeneratedAt.ToString("yyyyMMdd_HHmm");
            string fileName = $"EnemyAudit_U1_{stamp}.md";
            string fullPath = Path.Combine(auditsDir, fileName);

            var sb = new StringBuilder(16384);
            AppendHeader(sb, data);
            AppendRoster(sb, data);
            AppendPassives(sb, data);
            AppendBrokenRefs(sb, data);
            AppendCriticalFields(sb, data);
            AppendTypeRole(sb, data);
            AppendSynthesis(sb, data);

            File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
            return fullPath;
        }

        private static void AppendHeader(StringBuilder sb, AuditData data)
        {
            sb.AppendLine("# Audit roster ennemis U1 — lecture seule (G0)");
            sb.AppendLine();
            sb.AppendLine($"- **Date** : {data.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- **EnemyData total** : {data.TotalEnemyCount}");
            sb.AppendLine("- **Compte par universeIndex** :");
            for (int u = MIN_UNIVERSE_INDEX; u <= MAX_UNIVERSE_INDEX; u++)
                sb.AppendLine($"  - U{u} : {data.CountByUniverse[u]}");
            if (data.CountOutsideUniverseRange > 0)
                sb.AppendLine($"  - hors [0..5] : {data.CountOutsideUniverseRange}");
            sb.AppendLine($"- **Roster U1 (corps)** : {data.U1Enemies.Count}");
            sb.AppendLine();
            sb.AppendLine("> Outil G0 — lecture seule, aucune modification d'asset.");
            sb.AppendLine();
        }

        private static void AppendRoster(StringBuilder sb, AuditData data)
        {
            sb.AppendLine("## 1. Roster réel U1");
            sb.AppendLine();
            sb.AppendLine("| Fichier | id | enemyName | enemyType | enemyRole | PV | ATK | DEF | SPD |");
            sb.AppendLine("|---|---|---|---|---|---:|---:|---:|---:|");

            if (data.U1Enemies.Count == 0)
            {
                sb.AppendLine("| _aucun_ | | | | | | | | |");
            }
            else
            {
                for (int i = 0; i < data.U1Enemies.Count; i++)
                {
                    EnemyEntry e = data.U1Enemies[i];
                    sb.AppendLine(
                        $"| `{e.AssetFileName}` | `{EscapePipe(e.Id)}` | {EscapePipe(e.EnemyName)} | {e.EnemyType} | {e.EnemyRole} | {e.BaseHp} | {e.BaseAtk} | {e.BaseDef} | {e.BaseSpeed} |");
                }
            }

            sb.AppendLine();
            sb.AppendLine("### Attendus présents");
            sb.AppendLine();
            if (data.PresentExpectedIds.Count == 0)
            {
                sb.AppendLine("_Aucun id attendu trouvé._");
            }
            else
            {
                for (int i = 0; i < data.PresentExpectedIds.Count; i++)
                    sb.AppendLine($"- `{data.PresentExpectedIds[i]}`");
            }

            sb.AppendLine();
            sb.AppendLine("### Attendus absents");
            sb.AppendLine();
            if (data.AbsentExpected.Count == 0)
            {
                sb.AppendLine("_Aucun._");
            }
            else
            {
                for (int i = 0; i < data.AbsentExpected.Count; i++)
                {
                    ExpectedEnemy exp = data.AbsentExpected[i];
                    sb.AppendLine($"- `{exp.Id}` — {exp.CreationStatus}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("### Présents non-attendus");
            sb.AppendLine();
            if (data.UnexpectedPresent.Count == 0)
            {
                sb.AppendLine("_Aucun._");
            }
            else
            {
                for (int i = 0; i < data.UnexpectedPresent.Count; i++)
                {
                    EnemyEntry e = data.UnexpectedPresent[i];
                    string note = string.Equals(e.Id, UnexpectedDernierPieuId, StringComparison.Ordinal)
                        ? $" — {UnexpectedDernierPieuNote}"
                        : string.Empty;
                    sb.AppendLine($"- `{e.Id}` (`{e.AssetFileName}`){note}");
                }
            }

            sb.AppendLine();
        }

        private static void AppendPassives(StringBuilder sb, AuditData data)
        {
            sb.AppendLine("## 2. Passifs (EnemyPassiveData)");
            sb.AppendLine();

            for (int i = 0; i < data.U1Enemies.Count; i++)
            {
                EnemyEntry e = data.U1Enemies[i];
                sb.AppendLine($"### `{EscapePipe(e.Id)}` — {e.AssetFileName}");
                sb.AppendLine();

                if (e.Passives.Count == 0)
                {
                    sb.AppendLine("_Aucun passif branché._");
                    sb.AppendLine();
                    continue;
                }

                for (int p = 0; p < e.Passives.Count; p++)
                {
                    PassiveSlotInfo slot = e.Passives[p];
                    if (slot.IsMissing)
                    {
                        sb.AppendLine($"- `[{slot.Index}]` **Missing**");
                        continue;
                    }

                    if (slot.IsNone)
                    {
                        sb.AppendLine($"- `[{slot.Index}]` None");
                        continue;
                    }

                    var flags = new StringBuilder();
                    if (slot.PassiveNameEmpty)
                        flags.Append(" `[passiveName VIDE]`");
                    if (slot.DescriptionEmpty)
                        flags.Append(" `[description VIDE]`");

                    string handler = string.IsNullOrEmpty(slot.SpecialHandlerId)
                        ? "(vide)"
                        : $"`{slot.SpecialHandlerId}`";

                    sb.AppendLine(
                        $"- `[{slot.Index}]` `{slot.PassiveAssetName}` — trigger={slot.Trigger}, condition={slot.Condition}, effect={slot.Effect}, specialHandlerId={handler}{flags}");
                }

                sb.AppendLine();
            }

            sb.AppendLine("### Totaux passifs");
            sb.AppendLine();
            sb.AppendLine($"- Passifs branchés (résolus) : **{data.PassiveCount}**");
            sb.AppendLine($"- `passiveName` vides : **{data.EmptyPassiveNameCount}**");
            sb.AppendLine($"- `description` vides : **{data.EmptyDescriptionCount}**");
            sb.AppendLine();
        }

        private static void AppendBrokenRefs(StringBuilder sb, AuditData data)
        {
            sb.AppendLine("## 3. GUID / refs cassées");
            sb.AppendLine();
            sb.AppendLine("### 3.a Références Missing");
            sb.AppendLine();

            if (data.MissingRefs.Count == 0)
            {
                sb.AppendLine("_Aucune référence Missing._");
            }
            else
            {
                data.MissingRefs.Sort(CompareMissingRef);
                for (int i = 0; i < data.MissingRefs.Count; i++)
                {
                    MissingRefEntry m = data.MissingRefs[i];
                    sb.AppendLine($"- `{m.CarrierPath}` → `{m.Field}`");
                }
            }

            sb.AppendLine();
            sb.AppendLine("### 3.b Réfs logiques (`specialHandlerId`)");
            sb.AppendLine();

            if (!data.HandlerRegistryReadable)
            {
                sb.AppendLine("registre illisible par réflexion");
            }
            else if (data.DeadHandlers.Count == 0)
            {
                sb.AppendLine("_Aucun `specialHandlerId` mort._");
            }
            else
            {
                data.DeadHandlers.Sort(CompareDeadHandler);
                string currentId = null;
                for (int i = 0; i < data.DeadHandlers.Count; i++)
                {
                    DeadHandlerEntry d = data.DeadHandlers[i];
                    if (currentId != d.HandlerId)
                    {
                        currentId = d.HandlerId;
                        sb.AppendLine($"- **`{d.HandlerId}`**");
                    }

                    sb.AppendLine($"  - ennemi `{d.EnemyId}` · passif `{d.PassivePath}`");
                }
            }

            sb.AppendLine();
        }

        private static void AppendCriticalFields(StringBuilder sb, AuditData data)
        {
            sb.AppendLine("## 4. Champs vides critiques");
            sb.AppendLine();

            if (data.CriticalFieldLines.Count == 0)
            {
                sb.AppendLine("_Aucun champ critique vide / invalide sur le roster U1._");
            }
            else
            {
                for (int i = 0; i < data.CriticalFieldLines.Count; i++)
                    sb.AppendLine(data.CriticalFieldLines[i]);
            }

            sb.AppendLine();
            sb.AppendLine(
                $"Rappel section 2 — champs joueur vides : `passiveName`={data.EmptyPassiveNameCount}, `description`={data.EmptyDescriptionCount}.");
            sb.AppendLine();
        }

        private static void AppendTypeRole(StringBuilder sb, AuditData data)
        {
            sb.AppendLine("## 5. Incohérences enemyType / enemyRole");
            sb.AppendLine();
            sb.AppendLine("### 5.a Matrice interne");
            sb.AppendLine();
            sb.AppendLine("Règle : MobWeak/MobStandard/MobElite ↔ Basique ; MiniBoss ↔ MiniBoss ; Boss ↔ Boss.");
            sb.AppendLine();

            if (data.MatrixViolationLines.Count == 0)
            {
                sb.AppendLine("_Aucune violation de matrice._");
            }
            else
            {
                for (int i = 0; i < data.MatrixViolationLines.Count; i++)
                    sb.AppendLine(data.MatrixViolationLines[i]);
            }

            sb.AppendLine();
            sb.AppendLine("#### Exceptions actées");
            sb.AppendLine();
            if (data.MatrixExceptionLines.Count == 0)
            {
                sb.AppendLine("_Aucune._");
            }
            else
            {
                for (int i = 0; i < data.MatrixExceptionLines.Count; i++)
                    sb.AppendLine(data.MatrixExceptionLines[i]);
            }

            sb.AppendLine();
            sb.AppendLine("### 5.b Écarts vs contrat");
            sb.AppendLine();
            if (data.ContractGapLines.Count == 0)
            {
                sb.AppendLine("_Aucun écart (ou aucun id attendu présent avec type/rôle)._");
            }
            else
            {
                for (int i = 0; i < data.ContractGapLines.Count; i++)
                    sb.AppendLine(data.ContractGapLines[i]);
            }

            sb.AppendLine();
        }

        private static void AppendSynthesis(StringBuilder sb, AuditData data)
        {
            sb.AppendLine("## Synthèse");
            sb.AppendLine();
            sb.AppendLine($"- Section 1 (roster vs contrat) : **{data.Section1Findings}** constat(s)");
            sb.AppendLine($"- Section 2 (passifs / champs joueur vides) : **{data.Section2Findings}** constat(s)");
            sb.AppendLine($"- Section 3 (refs cassées) : **{data.Section3Findings}** constat(s)");
            sb.AppendLine($"- Section 4 (champs critiques) : **{data.Section4Findings}** constat(s)");
            sb.AppendLine($"- Section 5 (type/rôle) : **{data.Section5Findings}** constat(s)");
            sb.AppendLine();
        }

        // ═══════════════════════════════════════════
        // CONSOLE
        // ═══════════════════════════════════════════

        private static void LogConsoleSummary(AuditData data, string reportPath)
        {
            Debug.Log(
                $"[EnemyRosterAuditor] U1 — {data.U1Enemies.Count} ennemis " +
                $"(global {data.TotalEnemyCount}), " +
                $"S1={data.Section1Findings}, S2={data.Section2Findings}, " +
                $"S3={data.Section3Findings}, S4={data.Section4Findings}, S5={data.Section5Findings}. " +
                $"Rapport : {reportPath}");
        }

        // ═══════════════════════════════════════════
        // UTILITAIRES
        // ═══════════════════════════════════════════

        private static int CompareMissingRef(MissingRefEntry a, MissingRefEntry b)
        {
            int c = string.CompareOrdinal(a.CarrierPath, b.CarrierPath);
            if (c != 0)
                return c;
            return string.CompareOrdinal(a.Field, b.Field);
        }

        private static int CompareDeadHandler(DeadHandlerEntry a, DeadHandlerEntry b)
        {
            int c = string.CompareOrdinal(a.HandlerId, b.HandlerId);
            if (c != 0)
                return c;
            c = string.CompareOrdinal(a.EnemyId, b.EnemyId);
            if (c != 0)
                return c;
            return string.CompareOrdinal(a.PassivePath, b.PassivePath);
        }

        private static string EscapePipe(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            return value.Replace("|", "\\|");
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Untitled";

            char[] invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                bool bad = false;
                for (int j = 0; j < invalid.Length; j++)
                {
                    if (c == invalid[j])
                    {
                        bad = true;
                        break;
                    }
                }

                sb.Append(bad ? '_' : c);
            }

            return sb.ToString();
        }
    }
}
#endif
