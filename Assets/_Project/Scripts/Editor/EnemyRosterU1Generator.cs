#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using ChezArthur.Characters;
using ChezArthur.Debugging;
using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Enemies.Passives.Handlers;
using ChezArthur.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// G6a-P3 — générateur idempotent du roster U1 (renames GUID-safe, passifs, purge forêt, scène).
    /// </summary>
    public static class EnemyRosterU1Generator
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════

        private const string EnemiesFolder = "Assets/_Project/ScriptableObjects/Enemies/Univers1";
        private const string PassivesFolder = "Assets/_Project/ScriptableObjects/Enemies/Passives";
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string AuditsFolder = "Audits";

        private const string LoreAlucadra =
            "Le vampire que les Vandermont ont réellement tué, en croyant tenir Ardacula. Personne n'a jamais osé leur dire. Lui, si.";

        private const string LorePatriarche =
            "Le tueur de légendes. Il porte encore le Dernier Pieu — entre nous, c'est peut-être bien le pieu qui a tout fait.";

        // ═══════════════════════════════════════════
        // TABLES — CONTRAT
        // ═══════════════════════════════════════════

        private sealed class PassiveSpec
        {
            public string FileName;
            public string PassiveName;
            public string Description;
            public EnemyPassiveTrigger Trigger;
            public EnemyPassiveCondition Condition;
            public CharacterRole ConditionRole;
            public EnemyPassiveEffect Effect;
            public float Value;
            public bool IsPercentage;
            public int MaxStacks;
            public float StackValue;
            public int DurationTurns;
            public EnemyPassiveMultiHitPolicy MultiHitPolicy;
            public string SharedBuffId;
            public bool ExpiresWithSource;
            public string SpecialHandlerId;
            public float SpecialValue1;
            public float SpecialValue2;
            public float SpecialValue3;
            /// <summary> Texte carte seul (effect None) — toujours branchable. </summary>
            public bool CardOnly;
        }

        private sealed class EnemySpec
        {
            public string TargetFileName;
            public string SourceFileName;
            public string Id;
            public string EnemyName;
            public EnemyType Type;
            public EnemyRole Role;
            public int SlotOrder;
            public int Hp;
            public int Atk;
            public int Def;
            public int Spd;
            public EnemyArchetype Archetype;
            public float LaunchForce;
            public CharacterRole[] PriorityRoles;
            public bool CreateIfMissing;
            public bool PreserveStatsAndLore;
            public string LoreIfEmpty;
            public float ColliderWidth;
            public float ColliderHeight;
            public float CombatVisualScale;
            public int TalsReward;
            public bool SetColliders;
            public string[] PassiveFileNames;
        }

        private static readonly PassiveSpec[] PassiveTable =
        {
            // Disciple
            new PassiveSpec
            {
                FileName = "Passive_Disciple_ChasseurDeSoigneurs",
                PassiveName = "Chasseur de Soigneurs",
                Description =
                    "Il a lu trois livres : il sait qu'il faut taper les soigneurs. ATK +30 % contre les personnages en spé Soutien.",
                Trigger = EnemyPassiveTrigger.OnSpecificRoleHit,
                Condition = EnemyPassiveCondition.TargetAllyRole,
                ConditionRole = CharacterRole.Support,
                Effect = EnemyPassiveEffect.BuffSelfATK,
                Value = 0.30f,
                IsPercentage = true,
                DurationTurns = 1,
                MultiHitPolicy = EnemyPassiveMultiHitPolicy.PerHit
            },
            new PassiveSpec
            {
                FileName = "Passive_Disciple_RepliStudieux",
                PassiveName = "Repli Studieux",
                Description =
                    "Pas de soigneur à chasser ? Il révise sa défense. DEF +30 % tant qu'aucun allié n'est en spé Soutien.",
                Trigger = EnemyPassiveTrigger.Permanent,
                Condition = EnemyPassiveCondition.NoAllyOfRole,
                ConditionRole = CharacterRole.Support,
                Effect = EnemyPassiveEffect.BuffSelfDEF,
                Value = 0.30f,
                IsPercentage = true
            },
            new PassiveSpec
            {
                FileName = "Passive_Disciple_TropMotive",
                PassiveName = "Trop Motivé",
                Description = "Il y met tout son cœur. Force de charge +10 %.",
                Trigger = EnemyPassiveTrigger.Permanent,
                Effect = EnemyPassiveEffect.BuffSelfLaunchForce,
                Value = 0.10f,
                IsPercentage = true
            },
            // Archère
            new PassiveSpec
            {
                FileName = "Passive_Archere_Branches",
                PassiveName = "Aucun",
                Description =
                    "Elle s'appelle comme ça à cause de son sprite. Elle n'a aucun arc.",
                Trigger = EnemyPassiveTrigger.OnStageStart,
                Effect = EnemyPassiveEffect.SpecialHandler,
                SpecialHandlerId = "archere_branches",
                Value = 0.20f,
                SpecialValue1 = 2.5f,
                SpecialValue2 = 40f,
                SpecialValue3 = 55f,
                ExpiresWithSource = true
            },
            // Garde
            new PassiveSpec
            {
                FileName = "Passive_Garde_MurDePieux",
                PassiveName = "Mur de Pieux",
                Description =
                    "Tant qu'il monte la garde, tous les ennemis se sentent protégés : DEF +30 % pour toute l'équipe ennemie (lui compris).",
                Trigger = EnemyPassiveTrigger.Permanent,
                Effect = EnemyPassiveEffect.BuffEnemyTeamDEF,
                Value = 0.30f,
                IsPercentage = true,
                SharedBuffId = "garde_mur",
                ExpiresWithSource = true
            },
            new PassiveSpec
            {
                FileName = "Passive_Garde_ColereDuRempart",
                PassiveName = "Colère du Rempart",
                Description =
                    "Le frapper met les autres en rogne : quand il subit des dégâts, tous les AUTRES ennemis gagnent ATK +10 % (max 5).",
                Trigger = EnemyPassiveTrigger.OnTakeDamage,
                Effect = EnemyPassiveEffect.BuffOtherMatesATK,
                Value = 0.10f,
                IsPercentage = true,
                StackValue = 0.10f,
                MaxStacks = 5,
                MultiHitPolicy = EnemyPassiveMultiHitPolicy.PerCycle
            },
            new PassiveSpec
            {
                FileName = "Passive_Garde_CoupDePieu",
                PassiveName = "Coup de Pieu",
                Description =
                    "Frappe l'allié le plus proche à portée ; sinon il se retranche.",
                Trigger = EnemyPassiveTrigger.OnStageStart,
                Effect = EnemyPassiveEffect.SpecialHandler,
                SpecialHandlerId = "fixed_strike",
                SpecialValue1 = 2.0f,
                SpecialValue2 = 30f
            },
            // Confesseur
            new PassiveSpec
            {
                FileName = "Passive_Confesseur_Lien",
                PassiveName = "Confession Forcée",
                Description =
                    "Un de vos personnages lui est lié : il subit 20 % des dégâts infligés au Confesseur (sans jamais en mourir), et chaque soin reçu par ce personnage soigne aussi le Confesseur.",
                Trigger = EnemyPassiveTrigger.OnStageStart,
                Effect = EnemyPassiveEffect.SpecialHandler,
                SpecialHandlerId = "confesseur_lien",
                Value = 0.20f,
                SpecialValue1 = 1.0f,
                SpecialValue2 = 35f
            },
            // Veuve
            new PassiveSpec
            {
                FileName = "Passive_Veuve_ChagrinRageur",
                PassiveName = "Chagrin Rageur",
                Description =
                    "Plus elle est entière, plus sa rage est intacte : ATK jusqu'à +40 % à pleine vie, qui s'apaise à mesure qu'elle faiblit.",
                Trigger = EnemyPassiveTrigger.OnStageStart,
                Effect = EnemyPassiveEffect.SpecialHandler,
                SpecialHandlerId = "veuve_courbe",
                Value = 0.40f,
                SpecialValue1 = 0.50f, // Voile : réduction soins reçus alliés
                SpecialValue2 = 0.20f  // Seuil Déchirure (ratio PV)
            },
            // Carte-seulement (pattern : effect None, Permanent, condition None — texte pur, zéro runtime)
            new PassiveSpec
            {
                FileName = "Passive_Veuve_CarapaceDeDeuil",
                PassiveName = "Carapace de Deuil",
                Description =
                    "Plus elle souffre, plus elle se ferme : DEF augmente quand ses PV baissent, jusqu'à +40 %.",
                Trigger = EnemyPassiveTrigger.Permanent,
                Effect = EnemyPassiveEffect.None,
                CardOnly = true
            },
            new PassiveSpec
            {
                FileName = "Passive_Veuve_VoileDeDeuil",
                PassiveName = "Voile de Deuil",
                Description =
                    "Son chagrin étouffe l'espoir : vos soins sont réduits de 50 %.",
                Trigger = EnemyPassiveTrigger.Permanent,
                Effect = EnemyPassiveEffect.None,
                CardOnly = true
            },
            new PassiveSpec
            {
                FileName = "Passive_Veuve_LeVoileSeDechire",
                PassiveName = "Le Voile se Déchire",
                Description =
                    "Sous 20 % PV, le voile tombe — vos soins redeviennent entiers.",
                Trigger = EnemyPassiveTrigger.Permanent,
                Effect = EnemyPassiveEffect.None,
                CardOnly = true
            },
            // Alucadra
            new PassiveSpec
            {
                FileName = "Passive_Alucadra_Epee",
                PassiveName = "L'Épée Volante",
                Description =
                    "Tant que son Épée vole à ses côtés, il subit 50 % de dégâts en moins.",
                Trigger = EnemyPassiveTrigger.OnStageStart,
                Effect = EnemyPassiveEffect.SpecialHandler,
                SpecialHandlerId = "alucadra_epee",
                Value = 0.50f,
                SpecialValue1 = 70f,
                SpecialValue2 = 0.25f,
                SpecialValue3 = 2f
            },
            new PassiveSpec
            {
                FileName = "Passive_Alucadra_Legerete",
                PassiveName = "Légèreté",
                Description =
                    "Débarrassé de son arme, il ne pèse plus rien : DEF −20 %, Vitesse +30 %.",
                Trigger = EnemyPassiveTrigger.OnStageStart,
                Effect = EnemyPassiveEffect.SpecialHandler,
                SpecialHandlerId = "alucadra_loup",
                Value = 0.10f, // Pas Prédateur d'Équilibre (ATK +10 % / rôle distinct)
                SpecialValue1 = 0.20f,
                SpecialValue2 = 0.30f,
                SpecialValue3 = 0.40f
            },
            new PassiveSpec
            {
                FileName = "Passive_Alucadra_Predateur",
                PassiveName = "Prédateur d'Équilibre",
                Description =
                    "Une équipe équilibrée est un festin : ATK +10 % par spécialisation différente active dans votre équipe (max +30 %).",
                Trigger = EnemyPassiveTrigger.Permanent,
                Effect = EnemyPassiveEffect.None,
                CardOnly = true
            },
            // Patriarche
            new PassiveSpec
            {
                FileName = "Passive_Patriarche_Chaine",
                PassiveName = "Chaîne Tournante",
                Description =
                    "Sa chaîne fauche tout ce qui approche : il subit 20 % de dégâts en moins et en renvoie 20 % à l'attaquant.",
                Trigger = EnemyPassiveTrigger.OnStageStart,
                Effect = EnemyPassiveEffect.SpecialHandler,
                SpecialHandlerId = "patriarche_chaine",
                Value = 0.20f,
                SpecialValue1 = 0.20f,
                SpecialValue2 = 0.15f
            },
            new PassiveSpec
            {
                FileName = "Passive_Patriarche_Eaux",
                PassiveName = "Eaux Bénites",
                Description =
                    "À son tour, des eaux bénites s'abattent sur l'arène. Les traverser brûle ; y finir son tour — ou en recevoir une — fait très mal.",
                Trigger = EnemyPassiveTrigger.OnStageStart,
                Effect = EnemyPassiveEffect.SpecialHandler,
                SpecialHandlerId = "patriarche_eaux",
                Value = 2f,
                SpecialValue1 = 90f,
                SpecialValue2 = 0.60f,
                SpecialValue3 = 0.30f
            }
        };

        private static readonly EnemySpec[] EnemyTable =
        {
            new EnemySpec
            {
                TargetFileName = "Enemy_DiscipleTropMotive",
                SourceFileName = "Enemy_Champignon",
                Id = "disciple_trop_motive",
                EnemyName = "Disciple Trop Motivé",
                Type = EnemyType.MobWeak,
                Role = EnemyRole.Basique,
                SlotOrder = 0,
                Hp = 130, Atk = 35, Def = 10, Spd = 60,
                Archetype = EnemyArchetype.Mobile,
                LaunchForce = 33f,
                PriorityRoles = new[] { CharacterRole.Support },
                PassiveFileNames = new[]
                {
                    "Passive_Disciple_ChasseurDeSoigneurs",
                    "Passive_Disciple_RepliStudieux",
                    "Passive_Disciple_TropMotive"
                }
            },
            new EnemySpec
            {
                TargetFileName = "Enemy_ArcherePrecise",
                SourceFileName = "Enemy_Trukver",
                Id = "archere_precise",
                EnemyName = "Archère Précise",
                Type = EnemyType.MobStandard,
                Role = EnemyRole.Basique,
                SlotOrder = 1,
                Hp = 160, Atk = 40, Def = 12, Spd = 45,
                Archetype = EnemyArchetype.Fixed,
                LaunchForce = 0f,
                PriorityRoles = new[] { CharacterRole.Defender },
                PassiveFileNames = new[] { "Passive_Archere_Branches" }
            },
            new EnemySpec
            {
                TargetFileName = "Enemy_GardeAuxPieux",
                SourceFileName = "Enemy_RaceHine",
                Id = "garde_aux_pieux",
                EnemyName = "Garde aux Pieux",
                Type = EnemyType.MobElite,
                Role = EnemyRole.Basique,
                SlotOrder = 2,
                Hp = 260, Atk = 25, Def = 30, Spd = 30,
                Archetype = EnemyArchetype.Fixed,
                LaunchForce = 0f,
                PriorityRoles = Array.Empty<CharacterRole>(),
                PassiveFileNames = new[]
                {
                    "Passive_Garde_MurDePieux",
                    "Passive_Garde_ColereDuRempart",
                    "Passive_Garde_CoupDePieu"
                }
            },
            new EnemySpec
            {
                TargetFileName = "Enemy_LeConfesseur",
                SourceFileName = "Enemy_Epine",
                Id = "le_confesseur",
                EnemyName = "Le Confesseur",
                Type = EnemyType.MiniBoss,
                Role = EnemyRole.MiniBoss,
                SlotOrder = 0,
                Hp = 900, Atk = 45, Def = 35, Spd = 40,
                Archetype = EnemyArchetype.Fixed,
                LaunchForce = 0f,
                PriorityRoles = new[]
                {
                    CharacterRole.Defender, CharacterRole.Support, CharacterRole.Attacker
                },
                PassiveFileNames = new[] { "Passive_Confesseur_Lien" }
            },
            new EnemySpec
            {
                TargetFileName = "Enemy_LaVeuveEnDeuil",
                SourceFileName = "Enemy_MereRaceHine",
                Id = "la_veuve_en_deuil",
                EnemyName = "La Veuve en Deuil",
                Type = EnemyType.Boss,
                Role = EnemyRole.Boss,
                SlotOrder = 0,
                Hp = 2200, Atk = 70, Def = 45, Spd = 45,
                Archetype = EnemyArchetype.Mobile,
                LaunchForce = 33f,
                PriorityRoles = Array.Empty<CharacterRole>(),
                PassiveFileNames = new[]
                {
                    "Passive_Veuve_ChagrinRageur",
                    "Passive_Veuve_CarapaceDeDeuil",
                    "Passive_Veuve_VoileDeDeuil",
                    "Passive_Veuve_LeVoileSeDechire"
                }
            },
            new EnemySpec
            {
                TargetFileName = "Enemy_Alucadra",
                SourceFileName = null,
                Id = "alucadra",
                EnemyName = "Alucadra",
                Type = EnemyType.Boss,
                Role = EnemyRole.MiniBoss,
                SlotOrder = 1,
                Hp = 3200, Atk = 85, Def = 55, Spd = 40,
                Archetype = EnemyArchetype.Fixed,
                LaunchForce = 33f,
                PriorityRoles = Array.Empty<CharacterRole>(),
                CreateIfMissing = true,
                LoreIfEmpty = LoreAlucadra,
                SetColliders = true,
                ColliderWidth = 1.3f,
                ColliderHeight = 1.3f,
                CombatVisualScale = 1.4f,
                TalsReward = 30,
                PassiveFileNames = new[]
                {
                    "Passive_Alucadra_Epee",
                    "Passive_Alucadra_Legerete",
                    "Passive_Alucadra_Predateur"
                }
            },
            new EnemySpec
            {
                TargetFileName = "Enemy_PatriarcheVandermont",
                SourceFileName = "Enemy_ArbreRoi",
                Id = "patriarche_vandermont",
                EnemyName = "Le Patriarche Vandermont",
                Type = EnemyType.Boss,
                Role = EnemyRole.Boss,
                SlotOrder = 1,
                Hp = 4500, Atk = 100, Def = 60, Spd = 25,
                Archetype = EnemyArchetype.Fixed,
                LaunchForce = 0f,
                PriorityRoles = Array.Empty<CharacterRole>(),
                LoreIfEmpty = LorePatriarche,
                PassiveFileNames = new[]
                {
                    "Passive_Patriarche_Chaine",
                    "Passive_Patriarche_Eaux"
                }
            },
            new EnemySpec
            {
                TargetFileName = "Enemy_EpeeVolante",
                SourceFileName = null,
                Id = "epee_volante",
                EnemyName = "L'Épée Volante",
                Type = EnemyType.MobWeak,
                Role = EnemyRole.Compagnon,
                SlotOrder = 99,
                Hp = 1000, Atk = 500, Def = 150, Spd = 1,
                Archetype = EnemyArchetype.Fixed,
                LaunchForce = 0f,
                PriorityRoles = Array.Empty<CharacterRole>(),
                CreateIfMissing = true,
                SetColliders = true,
                ColliderWidth = 0.8f,
                ColliderHeight = 1.6f,
                CombatVisualScale = 1.2f,
                TalsReward = 0,
                PassiveFileNames = Array.Empty<string>()
            },
            new EnemySpec
            {
                TargetFileName = "Enemy_ConscienceForet",
                SourceFileName = "Enemy_ConscienceForet",
                Id = "dernier_pieu",
                EnemyName = null,
                PreserveStatsAndLore = true,
                Role = EnemyRole.Compagnon,
                SlotOrder = 99,
                PassiveFileNames = Array.Empty<string>()
            }
        };

        private static readonly string[] ForestPassivesToPurge =
        {
            "Passive_ArbreRoi_DebuffPerAlly 1",
            "Passive_ArbreRoi_DebuffPerAlly",
            "Passive_ArbreRoi_SpikeLowHP 1",
            "Passive_ArbreRoi_SpikeLowHP",
            "Passive_Champignon_ATKStack",
            "Passive_Champignon_DEFStack",
            "Passive_Conscience_LaunchForce",
            "Passive_Conscience_TeamBonus 1",
            "Passive_Conscience_TeamBonus",
            "Passive_Epine_ATKvsRole 1",
            "Passive_Epine_ATKvsRole",
            "Passive_Epine_Reflect",
            "Passive_MereRaceHine_ATKDEFStack 1",
            "Passive_MereRaceHine_ATKDEFStack",
            "Passive_MereRaceHine_HealCycle",
            "Passive_MereRaceHine_HealLowHP",
            "Passive_RaceHine_ATKLowHP",
            "Passive_RaceHine_LaunchForce",
            "Passive_Trukver_ATKLinear"
        };

        private static readonly string[] U1PoolIds =
        {
            "disciple_trop_motive",
            "archere_precise",
            "garde_aux_pieux",
            "le_confesseur",
            "la_veuve_en_deuil",
            "alucadra",
            "patriarche_vandermont",
            "epee_volante"
        };

        // ═══════════════════════════════════════════
        // RAPPORT
        // ═══════════════════════════════════════════

        private sealed class OpReport
        {
            public readonly List<string> Renamed = new List<string>(16);
            public readonly List<string> Created = new List<string>(16);
            public readonly List<string> FieldChanges = new List<string>(128);
            public readonly List<string> Purged = new List<string>(24);
            public readonly List<string> Deferred = new List<string>(16);
            public readonly List<string> Notes = new List<string>(32);
            public int ChangeCount;
        }

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/Contenu/Générer roster U1 (G6a-P3)")]
        public static void GenerateRosterU1()
        {
            var report = new OpReport();
            HashSet<string> handlerIds = LoadHandlerIds();

            EnsureFolder(PassivesFolder);
            EnsureFolder(EnemiesFolder);

            // 1 — Passifs (créés avant branchement)
            var passivesByFile = new Dictionary<string, EnemyPassiveData>(StringComparer.Ordinal);
            for (int i = 0; i < PassiveTable.Length; i++)
            {
                PassiveSpec spec = PassiveTable[i];
                EnemyPassiveData asset = UpsertPassive(spec, report);
                passivesByFile[spec.FileName] = asset;
            }

            // 2 — Ennemis (rename / create / fields)
            var enemiesById = new Dictionary<string, EnemyData>(StringComparer.Ordinal);
            for (int i = 0; i < EnemyTable.Length; i++)
            {
                EnemySpec spec = EnemyTable[i];
                EnemyData enemy = UpsertEnemy(spec, report);
                if (enemy == null)
                    continue;
                enemiesById[spec.Id ?? enemy.Id] = enemy;
            }

            // 3 — Câblage sprites (outil existant)
            try
            {
                CombatSpriteTools.WireCombatSpritesById();
                report.Notes.Add("Câblage sprites : CombatSpriteTools.WireCombatSpritesById() appelé.");
            }
            catch (Exception ex)
            {
                report.Notes.Add("Câblage sprites échoué : " + ex.Message);
            }

            // 4 — Branchement phase-aware
            for (int i = 0; i < EnemyTable.Length; i++)
            {
                EnemySpec spec = EnemyTable[i];
                if (!enemiesById.TryGetValue(spec.Id ?? string.Empty, out EnemyData enemy) || enemy == null)
                    continue;
                WireEnemyPassives(enemy, spec, passivesByFile, handlerIds, report);
            }

            // 5 — Purge forêt
            PurgeForestPassives(report);

            // 6 — Scène
            UpdateGameScenePools(enemiesById, report);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string reportPath = WriteReport(report);
            Debug.Log(
                "[EnemyRosterU1Generator] Terminé — changements=" + report.ChangeCount +
                " · rapport=" + reportPath +
                "\nLancer Chez Arthur/Audit/Auditer les ennemis U1 (G0) pour le contrôle croisé.");
        }

        // ═══════════════════════════════════════════
        // HANDLERS
        // ═══════════════════════════════════════════

        private static HashSet<string> LoadHandlerIds()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            EnemyPassiveHandlerRegistry.RegisterAll();

            FieldInfo field = typeof(EnemyPassiveRuntime).GetField(
                "HandlerFactories",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null)
                return ids;

            object raw = field.GetValue(null);
            if (!(raw is System.Collections.IDictionary dict))
                return ids;

            foreach (object key in dict.Keys)
            {
                if (key is string s && !string.IsNullOrEmpty(s))
                    ids.Add(s);
            }

            return ids;
        }

        private static bool CanWirePassive(PassiveSpec spec, HashSet<string> handlerIds)
        {
            if (spec.CardOnly || spec.Effect == EnemyPassiveEffect.None)
                return true;
            if (spec.Effect != EnemyPassiveEffect.SpecialHandler)
                return true;
            if (string.IsNullOrEmpty(spec.SpecialHandlerId))
                return true;
            return handlerIds != null && handlerIds.Contains(spec.SpecialHandlerId);
        }

        // ═══════════════════════════════════════════
        // PASSIFS
        // ═══════════════════════════════════════════

        private static EnemyPassiveData UpsertPassive(PassiveSpec spec, OpReport report)
        {
            string path = PassivesFolder + "/" + spec.FileName + ".asset";
            EnemyPassiveData asset = AssetDatabase.LoadAssetAtPath<EnemyPassiveData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<EnemyPassiveData>();
                AssetDatabase.CreateAsset(asset, path);
                report.Created.Add(path);
                report.ChangeCount++;
            }

            var so = new SerializedObject(asset);
            SetString(so, "passiveName", spec.PassiveName, path, report);
            SetString(so, "description", spec.Description, path, report);
            SetEnum(so, "trigger", (int)spec.Trigger, path, report);
            SetEnum(so, "condition", (int)spec.Condition, path, report);
            SetEnum(so, "conditionRole", (int)spec.ConditionRole, path, report);
            SetEnum(so, "effect", (int)spec.Effect, path, report);
            SetFloat(so, "value", spec.Value, path, report);
            SetBool(so, "isPercentage", spec.IsPercentage, path, report);
            SetInt(so, "maxStacks", spec.MaxStacks, path, report);
            SetFloat(so, "stackValue", spec.StackValue, path, report);
            SetInt(so, "durationTurns", spec.DurationTurns, path, report);
            SetEnum(so, "multiHitPolicy", (int)spec.MultiHitPolicy, path, report);
            SetString(so, "sharedBuffId", spec.SharedBuffId ?? string.Empty, path, report);
            SetBool(so, "expiresWithSource", spec.ExpiresWithSource, path, report);
            SetString(so, "specialHandlerId", spec.SpecialHandlerId ?? string.Empty, path, report);
            SetFloat(so, "specialValue1", spec.SpecialValue1, path, report);
            SetFloat(so, "specialValue2", spec.SpecialValue2, path, report);
            SetFloat(so, "specialValue3", spec.SpecialValue3, path, report);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        // ═══════════════════════════════════════════
        // ENNEMIS
        // ═══════════════════════════════════════════

        private static EnemyData UpsertEnemy(EnemySpec spec, OpReport report)
        {
            string targetPath = EnemiesFolder + "/" + spec.TargetFileName + ".asset";
            EnemyData enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(targetPath);

            if (enemy == null && !string.IsNullOrEmpty(spec.SourceFileName))
            {
                string sourcePath = EnemiesFolder + "/" + spec.SourceFileName + ".asset";
                EnemyData source = AssetDatabase.LoadAssetAtPath<EnemyData>(sourcePath);
                if (source != null && sourcePath != targetPath)
                {
                    string err = AssetDatabase.RenameAsset(sourcePath, spec.TargetFileName);
                    if (!string.IsNullOrEmpty(err))
                    {
                        report.Notes.Add("ERREUR rename " + sourcePath + " → " + spec.TargetFileName + " : " + err);
                    }
                    else
                    {
                        report.Renamed.Add(spec.SourceFileName + " → " + spec.TargetFileName);
                        report.ChangeCount++;
                        AssetDatabase.SaveAssets();
                    }

                    enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(targetPath);
                }
            }

            if (enemy == null)
                enemy = FindEnemyById(spec.Id);

            if (enemy == null)
            {
                if (!spec.CreateIfMissing)
                {
                    report.Notes.Add("ENNEMI INTROUVABLE : " + spec.TargetFileName + " / id=" + spec.Id);
                    return null;
                }

                enemy = ScriptableObject.CreateInstance<EnemyData>();
                AssetDatabase.CreateAsset(enemy, targetPath);
                report.Created.Add(targetPath);
                report.ChangeCount++;
            }
            else
            {
                string currentPath = AssetDatabase.GetAssetPath(enemy);
                if (!string.IsNullOrEmpty(currentPath)
                    && !string.Equals(currentPath, targetPath, StringComparison.Ordinal)
                    && !string.IsNullOrEmpty(spec.TargetFileName)
                    && Path.GetFileNameWithoutExtension(currentPath) != spec.TargetFileName)
                {
                    string err = AssetDatabase.RenameAsset(currentPath, spec.TargetFileName);
                    if (string.IsNullOrEmpty(err))
                    {
                        report.Renamed.Add(Path.GetFileNameWithoutExtension(currentPath) + " → " + spec.TargetFileName);
                        report.ChangeCount++;
                    }
                }
            }

            string path = AssetDatabase.GetAssetPath(enemy);
            var so = new SerializedObject(enemy);

            if (spec.PreserveStatsAndLore)
            {
                SetEnum(so, "enemyRole", (int)spec.Role, path, report);
                SetInt(so, "slotOrder", spec.SlotOrder, path, report);
                // Passifs forêt débranchés plus bas via WireEnemyPassives (liste vide).
            }
            else
            {
                SetString(so, "id", spec.Id, path, report);
                SetString(so, "enemyName", spec.EnemyName, path, report);
                SetEnum(so, "enemyType", (int)spec.Type, path, report);
                SetInt(so, "universeIndex", 1, path, report);
                SetEnum(so, "enemyRole", (int)spec.Role, path, report);
                SetInt(so, "slotOrder", spec.SlotOrder, path, report);
                SetInt(so, "baseHp", spec.Hp, path, report);
                SetInt(so, "baseAtk", spec.Atk, path, report);
                SetInt(so, "baseDef", spec.Def, path, report);
                SetInt(so, "baseSpeed", spec.Spd, path, report);
                SetEnum(so, "defaultArchetype", (int)spec.Archetype, path, report);
                SetFloat(so, "launchForce", spec.LaunchForce, path, report);
                SetPriorityRoles(so, spec.PriorityRoles, path, report);

                if (spec.SetColliders)
                {
                    SetFloat(so, "colliderWidth", spec.ColliderWidth, path, report);
                    SetFloat(so, "colliderHeight", spec.ColliderHeight, path, report);
                    SetFloat(so, "combatVisualScale", spec.CombatVisualScale, path, report);
                    SetInt(so, "talsReward", spec.TalsReward, path, report);
                }

                // Lore : jamais écraser une description non vide (voix d'Arthur).
                SerializedProperty descProp = so.FindProperty("description");
                if (descProp != null)
                {
                    if (string.IsNullOrEmpty(descProp.stringValue))
                    {
                        if (!string.IsNullOrEmpty(spec.LoreIfEmpty))
                            SetString(so, "description", spec.LoreIfEmpty, path, report);
                        else
                            report.Notes.Add("description vide (non écrasée) : " + path);
                    }
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(enemy);
            return enemy;
        }

        private static EnemyData FindEnemyById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            string[] guids = AssetDatabase.FindAssets("t:EnemyData");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
                if (data != null && string.Equals(data.Id, id, StringComparison.Ordinal))
                    return data;
            }

            return null;
        }

        private static void WireEnemyPassives(
            EnemyData enemy,
            EnemySpec spec,
            Dictionary<string, EnemyPassiveData> passivesByFile,
            HashSet<string> handlerIds,
            OpReport report)
        {
            string path = AssetDatabase.GetAssetPath(enemy);
            var wired = new List<EnemyPassiveData>(8);
            var specsByFile = new Dictionary<string, PassiveSpec>(StringComparer.Ordinal);
            for (int i = 0; i < PassiveTable.Length; i++)
                specsByFile[PassiveTable[i].FileName] = PassiveTable[i];

            if (spec.PassiveFileNames != null)
            {
                for (int i = 0; i < spec.PassiveFileNames.Length; i++)
                {
                    string file = spec.PassiveFileNames[i];
                    if (!passivesByFile.TryGetValue(file, out EnemyPassiveData passive) || passive == null)
                    {
                        report.Notes.Add("Passif manquant pour branchement : " + file);
                        continue;
                    }

                    if (!specsByFile.TryGetValue(file, out PassiveSpec pSpec))
                    {
                        wired.Add(passive);
                        continue;
                    }

                    if (!CanWirePassive(pSpec, handlerIds))
                    {
                        string gate = string.IsNullOrEmpty(pSpec.SpecialHandlerId)
                            ? "?"
                            : pSpec.SpecialHandlerId;
                        report.Deferred.Add(
                            "`" + file + "` sur `" + (spec.Id ?? enemy.Id) +
                            "` — différé (handler `" + gate + "` absent — G6b/G6c/P4)");
                        continue;
                    }

                    wired.Add(passive);
                }
            }

            var so = new SerializedObject(enemy);
            SerializedProperty list = so.FindProperty("enemyPassives");
            if (list == null || !list.isArray)
                return;

            bool same = list.arraySize == wired.Count;
            if (same)
            {
                for (int i = 0; i < wired.Count; i++)
                {
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue != wired[i])
                    {
                        same = false;
                        break;
                    }
                }
            }

            if (!same)
            {
                list.arraySize = wired.Count;
                for (int i = 0; i < wired.Count; i++)
                    list.GetArrayElementAtIndex(i).objectReferenceValue = wired[i];
                report.FieldChanges.Add(path + " · enemyPassives → " + wired.Count + " slot(s)");
                report.ChangeCount++;
            }

            SetBool(so, "hasPassive", wired.Count > 0, path, report);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(enemy);
        }

        // ═══════════════════════════════════════════
        // PURGE
        // ═══════════════════════════════════════════

        private static void PurgeForestPassives(OpReport report)
        {
            // Débrancher partout avant DeleteAsset
            string[] enemyGuids = AssetDatabase.FindAssets("t:EnemyData");
            var purgeSet = new HashSet<string>(ForestPassivesToPurge, StringComparer.Ordinal);

            for (int i = 0; i < enemyGuids.Length; i++)
            {
                string ePath = AssetDatabase.GUIDToAssetPath(enemyGuids[i]);
                EnemyData enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(ePath);
                if (enemy == null)
                    continue;

                var so = new SerializedObject(enemy);
                SerializedProperty list = so.FindProperty("enemyPassives");
                if (list == null || !list.isArray)
                    continue;

                bool changed = false;
                for (int p = list.arraySize - 1; p >= 0; p--)
                {
                    var refObj = list.GetArrayElementAtIndex(p).objectReferenceValue as EnemyPassiveData;
                    if (refObj == null)
                        continue;
                    string name = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(refObj));
                    if (!purgeSet.Contains(name))
                        continue;
                    list.DeleteArrayElementAtIndex(p);
                    // Unity double-null quirk
                    if (p < list.arraySize && list.GetArrayElementAtIndex(p).objectReferenceValue == null)
                        list.DeleteArrayElementAtIndex(p);
                    changed = true;
                }

                if (changed)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(enemy);
                    report.FieldChanges.Add(ePath + " · enemyPassives : purge refs forêt");
                    report.ChangeCount++;
                }
            }

            for (int i = 0; i < ForestPassivesToPurge.Length; i++)
            {
                string file = ForestPassivesToPurge[i];
                string path = PassivesFolder + "/" + file + ".asset";
                if (!File.Exists(Path.GetFullPath(path)) && AssetDatabase.LoadAssetAtPath<EnemyPassiveData>(path) == null)
                {
                    report.Notes.Add("Purge skip (déjà absent) : " + path);
                    continue;
                }

                if (AssetDatabase.DeleteAsset(path))
                {
                    report.Purged.Add(path);
                    report.ChangeCount++;
                }
                else if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
                {
                    report.Notes.Add("Purge ÉCHEC : " + path);
                }
            }
        }

        // ═══════════════════════════════════════════
        // SCÈNE
        // ═══════════════════════════════════════════

        private static void UpdateGameScenePools(
            Dictionary<string, EnemyData> enemiesById,
            OpReport report)
        {
            if (!File.Exists(GameScenePath) && !AssetDatabase.LoadAssetAtPath<SceneAsset>(GameScenePath))
            {
                report.Notes.Add("Scène introuvable : " + GameScenePath);
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

            StageGenerator[] stages = UnityEngine.Object.FindObjectsOfType<StageGenerator>(true);
            bool sceneChanged = false;
            for (int i = 0; i < stages.Length; i++)
            {
                if (PatchAllEnemiesList(stages[i], "StageGenerator", enemiesById, report))
                    sceneChanged = true;
            }

            DebugMenu[] menus = UnityEngine.Object.FindObjectsOfType<DebugMenu>(true);
            for (int i = 0; i < menus.Length; i++)
            {
                if (PatchAllEnemiesList(menus[i], "DebugMenu", enemiesById, report))
                    sceneChanged = true;
            }

            if (sceneChanged)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                report.Notes.Add("Scène sauvegardée : " + GameScenePath);
            }
            else
            {
                report.Notes.Add("Scène déjà conforme (pas de save) : " + GameScenePath);
            }
        }

        private static bool PatchAllEnemiesList(
            UnityEngine.Object component,
            string label,
            Dictionary<string, EnemyData> enemiesById,
            OpReport report)
        {
            if (component == null)
                return false;

            var so = new SerializedObject(component);
            SerializedProperty list = so.FindProperty("allEnemies");
            if (list == null || !list.isArray)
                return false;

            var kept = new List<EnemyData>(list.arraySize + 8);
            bool removedDernier = false;

            for (int i = 0; i < list.arraySize; i++)
            {
                var e = list.GetArrayElementAtIndex(i).objectReferenceValue as EnemyData;
                if (e == null)
                    continue;
                if (string.Equals(e.Id, "dernier_pieu", StringComparison.Ordinal))
                {
                    removedDernier = true;
                    continue;
                }

                kept.Add(e);
            }

            var presentIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < kept.Count; i++)
            {
                if (kept[i] != null && !string.IsNullOrEmpty(kept[i].Id))
                    presentIds.Add(kept[i].Id);
            }

            var added = new List<string>(8);
            for (int i = 0; i < U1PoolIds.Length; i++)
            {
                string id = U1PoolIds[i];
                if (presentIds.Contains(id))
                    continue;
                if (!enemiesById.TryGetValue(id, out EnemyData data) || data == null)
                {
                    data = FindEnemyById(id);
                    if (data == null)
                    {
                        report.Notes.Add(label + " : U1 manquant introuvable id=" + id);
                        continue;
                    }
                }

                kept.Add(data);
                added.Add(id);
                presentIds.Add(id);
            }

            bool changed = removedDernier || added.Count > 0 || list.arraySize != kept.Count;
            if (!changed)
            {
                for (int i = 0; i < kept.Count; i++)
                {
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue != kept[i])
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (!changed)
                return false;

            list.arraySize = kept.Count;
            for (int i = 0; i < kept.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = kept[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(component);

            string msg = label + ".allEnemies";
            if (removedDernier)
                msg += " −dernier_pieu";
            if (added.Count > 0)
                msg += " +" + string.Join(",", added);
            report.FieldChanges.Add(msg);
            report.ChangeCount++;
            return true;
        }

        // ═══════════════════════════════════════════
        // SERIALIZED HELPERS
        // ═══════════════════════════════════════════

        private static void SetString(
            SerializedObject so, string name, string value, string path, OpReport report)
        {
            if (value == null)
                value = string.Empty;
            SerializedProperty p = so.FindProperty(name);
            if (p == null)
                return;
            if (p.stringValue == value)
                return;
            report.FieldChanges.Add(path + " · " + name + " : \"" + p.stringValue + "\" → \"" + value + "\"");
            p.stringValue = value;
            report.ChangeCount++;
        }

        private static void SetInt(
            SerializedObject so, string name, int value, string path, OpReport report)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p == null)
                return;
            if (p.intValue == value)
                return;
            report.FieldChanges.Add(path + " · " + name + " : " + p.intValue + " → " + value);
            p.intValue = value;
            report.ChangeCount++;
        }

        private static void SetFloat(
            SerializedObject so, string name, float value, string path, OpReport report)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p == null)
                return;
            if (Mathf.Approximately(p.floatValue, value))
                return;
            report.FieldChanges.Add(path + " · " + name + " : " + p.floatValue + " → " + value);
            p.floatValue = value;
            report.ChangeCount++;
        }

        private static void SetBool(
            SerializedObject so, string name, bool value, string path, OpReport report)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p == null)
                return;
            if (p.boolValue == value)
                return;
            report.FieldChanges.Add(path + " · " + name + " : " + p.boolValue + " → " + value);
            p.boolValue = value;
            report.ChangeCount++;
        }

        private static void SetEnum(
            SerializedObject so, string name, int value, string path, OpReport report)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p == null)
                return;
            if (p.intValue == value)
                return;

            report.FieldChanges.Add(path + " · " + name + " : " + p.intValue + " → " + value);
            p.intValue = value;
            report.ChangeCount++;
        }

        private static void SetPriorityRoles(
            SerializedObject so, CharacterRole[] roles, string path, OpReport report)
        {
            SerializedProperty root = so.FindProperty("targetSelector");
            if (root == null)
                return;
            SerializedProperty list = root.FindPropertyRelative("priorityRoles");
            if (list == null || !list.isArray)
                return;

            roles = roles ?? Array.Empty<CharacterRole>();
            bool same = list.arraySize == roles.Length;
            if (same)
            {
                for (int i = 0; i < roles.Length; i++)
                {
                    if (list.GetArrayElementAtIndex(i).enumValueIndex != (int)roles[i]
                        && list.GetArrayElementAtIndex(i).intValue != (int)roles[i])
                    {
                        same = false;
                        break;
                    }
                }
            }

            if (same)
                return;

            list.arraySize = roles.Length;
            for (int i = 0; i < roles.Length; i++)
                list.GetArrayElementAtIndex(i).intValue = (int)roles[i];
            report.FieldChanges.Add(path + " · targetSelector.priorityRoles → [" + roles.Length + "]");
            report.ChangeCount++;
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return;

            string[] parts = assetPath.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        // ═══════════════════════════════════════════
        // RAPPORT MD
        // ═══════════════════════════════════════════

        private static string WriteReport(OpReport report)
        {
            if (!Directory.Exists(AuditsFolder))
                Directory.CreateDirectory(AuditsFolder);

            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string reportPath = Path.Combine(AuditsFolder, "RosterU1_" + stamp + ".md");

            var sb = new StringBuilder(8192);
            sb.AppendLine("# Roster U1 — G6a-P3");
            sb.AppendLine();
            sb.AppendLine("Généré : " + DateTime.Now.ToString("O"));
            sb.AppendLine("ChangeCount : **" + report.ChangeCount + "**");
            if (report.ChangeCount == 0)
                sb.AppendLine();
            sb.AppendLine(report.ChangeCount == 0
                ? "> **zéro modification** (idempotent OK)"
                : "> Modifications appliquées — relancer l'outil pour confirmer l'idempotence.");
            sb.AppendLine();

            AppendSection(sb, "Renommés", report.Renamed);
            AppendSection(sb, "Créés", report.Created);
            AppendSection(sb, "Champs modifiés", report.FieldChanges);
            AppendSection(sb, "Purgés", report.Purged);
            AppendSection(sb, "Différés (G6b/G6c/P4)", report.Deferred);
            AppendSection(sb, "Notes", report.Notes);

            sb.AppendLine("## Suite");
            sb.AppendLine();
            sb.AppendLine("1. Relancer `Chez Arthur/Contenu/Générer roster U1 (G6a-P3)` → attendu ChangeCount 0.");
            sb.AppendLine("2. Lancer `Chez Arthur/Audit/Auditer les ennemis U1 (G0)` pour le contrôle croisé.");
            sb.AppendLine();

            File.WriteAllText(reportPath, sb.ToString(), Encoding.UTF8);
            return reportPath;
        }

        private static void AppendSection(StringBuilder sb, string title, List<string> lines)
        {
            sb.AppendLine("## " + title);
            sb.AppendLine();
            if (lines == null || lines.Count == 0)
            {
                sb.AppendLine("_Aucun._");
            }
            else
            {
                for (int i = 0; i < lines.Count; i++)
                    sb.AppendLine("- " + lines[i]);
            }

            sb.AppendLine();
        }
    }
}
#endif
