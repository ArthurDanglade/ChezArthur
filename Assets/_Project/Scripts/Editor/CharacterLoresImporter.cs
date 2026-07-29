#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ChezArthur.Characters;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Injecte les lores-carte validés (doc Lores_Personnages) dans CharacterData.backstory.
    /// Met aussi à jour characterName quand le doc a acté un rename.
    /// </summary>
    public static class CharacterLoresImporter
    {
        private readonly struct LoreEntry
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly string Backstory;

            public LoreEntry(string id, string displayName, string backstory)
            {
                Id = id;
                DisplayName = displayName;
                Backstory = backstory;
            }
        }

        /// <summary>
        /// Table figée du doc Lores_Personnages(2).
        /// Faille a son lore injecté via CharacterData dédié.
        /// </summary>
        private static readonly LoreEntry[] LoreTable =
        {
            new LoreEntry("loupzeur", "Loup Zeur",
                "Viré de sa meute pour un coup d'état raté, chassé à coups de crocs. Le train est venu le chercher quand même. Il raconte que c'est lui qui est parti. Un peu comme ces mecs qui disent que c'est eux qui ont rompu, alors qu'ils se sont fait jeter comme de vieilles chaussettes. Faut assumer les mecs."),
            new LoreEntry("pusamair", "Pusam Air",
                "Banni de 14 mondes pour son odeur. Plus personne ne voulait de lui, alors le train est venu le chercher quand même. Tout le monde fait semblant que ça va. Au moins avec lui, on sait pourquoi on respire par la bouche."),
            new LoreEntry("frigor", "Le Frige",
                "Si costaud qu'on dirait un frigo. Quand on l'a récupéré, quelqu'un l'a surnommé le frige et c'est resté. On en a complètement oublié son prénom..."),
            new LoreEntry("zoneur", "Ekaze",
                "Seul dans son monde, il est ravi d'être à bord pour voir du monde. Mais avoir vécu seul l'a rendu socialement étrange. Restez dans sa zone plutôt que près de lui — c'est plus sûr pour tout le monde."),
            new LoreEntry("elfert", "Elfert",
                "Il remportait tous ses paris sportifs dans son monde. Si bien qu'il en a été expulsé. Il est monté dans le train car on passait par là — on n'avait pas prévu de le récupérer, les paris sportifs c'est mal. Un coup de bol d'être tombé sur le train..."),
            new LoreEntry("lumino", "Lumino",
                "À vrai dire c'est un cas particulier. Lumino n'est ni déprimé, ni triste, ni quoi que ce soit. Il va très bien. Il est là parce que quand les fusibles pètent, on est pas dans le noir."),
            new LoreEntry("kramhoisi", "Kram Hoisi",
                "Triste de s'être vengé. S'est vengé parce qu'on lui a volé son goûter. Et qu'on a tué toute sa famille. Je sais pas pour laquelle des deux il s'est vengé, demandez-lui."),
            new LoreEntry("ronss", "TeeTree",
                "Avoir un arbre dans le train ajoute une touche de nature pas négligeable. Il faut penser à l'arroser, il est pas autonome. Merci de ne pas faire pipi contre lui."),
            new LoreEntry("morgan", "Morgan",
                "Essaie de vaincre l'alcoolisme. Il prévoit d'arrêter quand le train s'arrêtera. Il faut peut-être lui expliquer le concept... En attendant il traîne au wagon bar."),
            new LoreEntry("phil", "Phil Antrope",
                "Encore un dont le savoir est une malédiction. J'ai toujours pensé que les stupides sont plus heureux que les génies. Vous choisiriez quoi vous ?"),
            new LoreEntry("lanshimmer", "Lans Himmer",
                "Lancier dans son monde. Il en a marre de se battre, lui ce qu'il veut c'est faire de la musique. Mais il est pas là pour ça."),
            new LoreEntry("spenda", "Spenda",
                "Créature qui voyage entre les univers. On l'a récupéré parce qu'il fait de la concurrence au train. Notre réputation est en jeu tout de même."),
            new LoreEntry("leuk", "Leuk",
                "Croupier dans le plus grand casino de son monde. La chance a commencé à le suivre IRL. Il gagne à tout, tout le temps. Ça a l'air cool ? Ça l'est pas. Personne veut jouer avec lui, personne veut être son ami. Le train est venu le chercher parce que même la chance a besoin d'un peu de compagnie, parfois."),
            new LoreEntry("shado", "Shado",
                "Trop d4rk. Tout son clan a été assassiné par son frère. Du coup il fait le mystérieux et cherche à se venger. Faut lui dire qu'il est ridicule. Moi je laisse comme ça, c'est drôle de se moquer de lui."),
            new LoreEntry("daupou", "Daupou",
                "Il adore pousser et être poussé. Quand on le pousse, ça peut provoquer une réaction en chaîne et des dégâts irréparables. Un jour il a trébuché dans le train. Ça nous a propulsés dans des centaines d'univers différents. Je pense qu'il l'a fait exprès."),
            new LoreEntry("tribulle", "Walli",
                "Fait tout par 3. Il a des tocs, et c'est comme ça. Il dort 3 heures par nuit, mange 3 fruits et légumes par jour... Il vient de l'univers 3333, ça n'a aucun sens mais c'est moi qui décide."),
            new LoreEntry("bouclar", "Tess Theur",
                "Testeur de bouclier de père en fils. Oui, c'est un métier qui existe, Tess est l'un des meilleurs. On l'a récupéré parce qu'il a tué son père après un test qui a foiré. C'est les risques du métier."),
            new LoreEntry("revvie", "Rev",
                "Adore la mort. Ça a commencé un jour où il était entre la vie et la mort. La mort est venue le chercher, il en est tombé amoureux. Depuis, il refuse qu'elle touche à ses proches — la Mort, ça le concerne lui, et personne d'autre. Quand elle le voit, elle s'éloigne. Elle n'aime pas être stalkée. Ça peut être pratique."),
            new LoreEntry("antycype", "Anty Cype",
                "Ce personnage est capable de lire l'avenir. Mais seulement de 3 secondes dans le futur. Ça aurait pu être super pratique, le problème c'est que ça met 3 secondes à se déclencher. Du coup ça sert à rien."),
            new LoreEntry("goat", "Goat",
                "Cette chèvre est montée dans le train quand les portes étaient ouvertes. Personne n'arrive à la faire descendre. C'est devenue la mascotte du train. Pourquoi a-t-elle le regard vide comme ça ?"),
            new LoreEntry("brooke_heune", "Broke Heune",
                "Personne ne l'a jamais battue. Les légendes racontent des tas de choses à son sujet. Elle s'en vante, et ceux qui arrivent à l'obtenir aussi."),
            new LoreEntry("ardacula", "Ardacula",
                "Dracu Dracu Dracu... la classe comme tous les vampires. C'est un peu moins classe de se faire tuer et ressusciter en boucle, par la même famille, génération après génération. Il prépare sa vengeance depuis cinquante ans. Il a un plan. Il a juste besoin de bras. Beaucoup de bras."),
            new LoreEntry("troplin", "Troplin",
                "Ce nain vivait avec son peuple dans une montagne. Un vilain dragon pas gentil en a pris le contrôle. Ils ont mené une expédition pour récupérer la montagne. Ah, Troplin me dit que c'était simplement pour récupérer toute la bière située dans les caves de la montagne. Ça en valait pas la peine, mais vous savez les nains..."),
            new LoreEntry("don_costardo", "Don Costardo",
                "Dirigeait le plus gros cartel de son univers. Ou plutôt \"dirigeait\". Il s'est fait démanteler par un type en costume de chauve-souris. Depuis, il se méfie de tout ce qui porte une cape. Ne répétez pas qu'il a été vaincu par une chauve-souris, sinon il vous retrouvera."),
            new LoreEntry("ancien", "L'Ancien N°1 Mondial",
                "Plusieurs titres de champion du monde à son compteur, mais le dernier remonte à si longtemps qu'on a arrêté de compter. La jeunesse est passée devant. Le problème, c'est qu'il ne peut pas mourir de vieillesse — il a tout le temps du monde pour y repenser. Il rêve d'un dernier titre, et surtout d'un endroit où il ne verra pas tous ses amis et ses ennemis vieillir et mourir sans lui."),
        };

        [MenuItem("Chez Arthur/Characters/Injecter lores-carte validés")]
        public static void InjectLores()
        {
            CharacterData[] all = LoadAllCharacterData();
            Dictionary<string, CharacterData> byId = BuildIdMap(all);

            int written = 0;
            int upToDate = 0;
            int missing = 0;
            bool anyWrite = false;

            for (int i = 0; i < LoreTable.Length; i++)
            {
                LoreEntry entry = LoreTable[i];
                if (!byId.TryGetValue(entry.Id, out CharacterData character) || character == null)
                {
                    missing++;
                    Debug.LogWarning($"[LoresImporter] INTROUVABLE id={entry.Id} ({entry.DisplayName})");
                    continue;
                }

                if (!TryWriteLore(character, entry, out bool wrote))
                    continue;

                if (wrote)
                {
                    written++;
                    anyWrite = true;
                    Debug.Log($"[LoresImporter] {entry.DisplayName} ({entry.Id}) : lore + nom écrits");
                }
                else
                {
                    upToDate++;
                }
            }

            if (anyWrite)
                AssetDatabase.SaveAssets();

            Debug.Log(
                $"[LoresImporter] Terminé — {written} écrits, {upToDate} à jour, {missing} introuvables.");
        }

        private static CharacterData[] LoadAllCharacterData()
        {
            string[] guids = AssetDatabase.FindAssets("t:CharacterData");
            var list = new List<CharacterData>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                CharacterData data = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
                if (data != null)
                    list.Add(data);
            }
            return list.ToArray();
        }

        private static Dictionary<string, CharacterData> BuildIdMap(CharacterData[] all)
        {
            var map = new Dictionary<string, CharacterData>(all.Length);
            for (int i = 0; i < all.Length; i++)
            {
                CharacterData c = all[i];
                if (c == null || string.IsNullOrEmpty(c.Id)) continue;
                map[c.Id] = c;
            }
            return map;
        }

        private static bool TryWriteLore(CharacterData character, LoreEntry entry, out bool wrote)
        {
            wrote = false;
            SerializedObject so = new SerializedObject(character);
            SerializedProperty nameProp = so.FindProperty("characterName");
            SerializedProperty loreProp = so.FindProperty("backstory");
            if (nameProp == null || loreProp == null)
            {
                Debug.LogError($"[LoresImporter] Propriétés manquantes sur {character.name}");
                return false;
            }

            bool nameChanged = nameProp.stringValue != entry.DisplayName;
            bool loreChanged = loreProp.stringValue != entry.Backstory;
            if (!nameChanged && !loreChanged)
                return true;

            if (nameChanged)
                nameProp.stringValue = entry.DisplayName;
            if (loreChanged)
                loreProp.stringValue = entry.Backstory;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(character);
            wrote = true;
            return true;
        }
    }
}
#endif
