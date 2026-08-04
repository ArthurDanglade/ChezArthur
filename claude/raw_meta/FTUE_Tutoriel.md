> **STATUT : MATIÈRE PREMIÈRE NON CONTRACTUELLE (MT-D5)**
> Source : docx Arthur (Downloads). À confronter au code à l'ouverture MT1 / MT2-0.
> **Attention Arthur** : certains éléments sont incorrects ou non figés — ne rien prendre pour argent comptant. Interview manager = trous, contradictions, points non figés uniquement.
> Original docx aussi versé dans ce dossier.
FTUE — TUTORIEL
Les 5 premières minutes — parcours complet, phrase par phrase
Document de référence — prêt pour scripts editor
Principes directeurs
Choix réel et respecté dès la première seconde : Tuto ou accès direct, sans culpabilisation. Le tuto reste accessible depuis les paramètres à tout moment après coup.
Un seul narrateur, une seule voix, du tuto jusqu'au lore des ennemis : direct, complice, jamais pédant, qui pique gentiment (y compris le joueur lui-même).
Chaque concept = une ligne maximum. Jamais de paragraphe explicatif. Si ça ne rentre pas en une phrase courte, c'est que le concept doit attendre un autre moment.
On apprend en FAISANT, jamais en lisant un écran inerte. Chaque explication est collée à une action réelle du joueur.
Rien n'est obligatoire à lire (lore, descriptions d'ennemis) — on montre que c'est possible, jamais on n'impose la lecture.
Le Super Lancer est introduit comme une DÉCOUVERTE après un premier lancer normal, jamais comme une leçon a priori.
Sacrifice et Gare ne sont PAS expliqués pendant le combat de tuto — chacun reçoit son propre texte contextuel, déclenché une seule fois, la première fois qu'il apparaît réellement en jeu (flag de sauvegarde par système : hasSeenSacrificeIntro, hasSeenGareIntro).

Mécanique clé — le personnage "prêté"
Le combat de tuto utilise un SSR fixe, choisi par le développeur (le plus abouti visuellement selon les assets disponibles), JAMAIS un tirage réel du joueur.
Ce personnage est ajouté temporairement à l'équipe pour la durée du tuto uniquement.
Il garantit un script fiable et reproductible à 100% (spés connues, stats connues, comportement ennemi calibré en conséquence).
Une fois le tuto terminé, il disparaît intégralement du compte du joueur — aucune trace dans l'inventaire ou la collection.
Si le joueur tire plus tard ce même SSR via la vraie invocation finale (ou une invocation future), c'est une vraie première possession pour son compte — aucun conflit ni duplication, puisque le perso prêté n'a jamais été réellement possédé.

Parcours complet
1 — Écran titre → Pseudo
Fond noir, simple boîte de dialogue. Pas de blabla avant.
Jeu : "Comment on vous appelle ?"
2 — Choix Tuto / Direct
Jeu : "[Pseudo], deux options : on vous montre comment tout ça fonctionne en 5 minutes, ou vous skip le tuto pour les flemmards. Il reste accessible dans les paramètres si vous changez d'avis."
Deux boutons : « Montrez-moi » / « Flemme »
3 — Si « Flemme »
Multi-invocation gratuite immédiate (10 tirages), libre choix du portail.
Puis accueil, liberté totale immédiate.

4 — Si « Montrez-moi » : Étape 1, la fiche personnage
Le jeu ouvre automatiquement la fiche du SSR prêté.
Jeu : "Ça, c'est une carte. En bas, les stats. Ici, le passif — ce qui rend ce perso unique en combat."
Jeu : "Pour les SSR (et LR, oups j'ai rien dit), deux spés différentes. Une de base, une autre à débloquer plus tard. Changez de spé quand vous voulez, en plein combat. Les SR ont une spé uniquement, ça ne veut pas dire qu'ils sont inutiles, bande d'élitistes pourris gâtés."
Étape 2 — Composer l'équipe
Jeu : "Les personnages que vous invoquerez pourront être ajoutés à votre équipe. Équipe de 4 maximum. Comme les 4 doigts de la main."
Le joueur place le perso prêté. Aucune sur-explication du geste de drag (déjà intuitif).

Étape 3 — Premier combat (scripté)
Temps 1 — le lancer
Jeu : "Votre tour. Visez, tirez, c'est tout."
Premier lancer libre, sans aucune contrainte.
Juste après ce premier lancer — découverte du Super Lancer :
Jeu : "Vous avez vu cet anneau pendant que vous visiez ? Relâchez dedans, dans la zone qui brille, et c'est un Super Lancer — pour les Super joueurs que vous êtes. Tentez le prochain."
Temps 2 — le cycle de tours
Jeu : "Maintenant c'est leur tour. On alterne comme ça jusqu'à la fin. Le personnage avec la SPD la plus élevée commence, puis ça alterne allié > ennemi > allié, etc."
Changement de spé — scripté comme une réponse à une vraie menace, pas une démo gratuite
Jeu : "C'est de nouveau à votre tour. L'ennemi va vous tuer à son prochain tour : passez en spé défensive — c'est la spé secondaire de ce personnage — pour encaisser son attaque. Appuyez sur votre personnage avant de viser."
L'ennemi joue, le joueur encaisse (dégâts scriptés, valeur fictive garantie non-létale).
Jeu : "Bien, c'est de nouveau votre tour. Passez en spé attaque et finissez le combat."
Inspection d'ennemi — montrée, jamais imposée
Possibilité d'appuyer longuement sur un ennemi pour voir nom, description, stats et passif — mentionnée une fois si l'occasion se présente naturellement, mais aucune obligation de le faire, aucun blocage du combat si le joueur l'ignore.

Étape 4 — Fin de combat, écran de bonus
Jeu : "Une valise ou un objet, à vous de voir. Chacun change votre run différemment. Vous pouvez avoir 3 valises maximum, et 7 items maxsiuuumum. Certains duo de valises peuvent peut-être donner des bonus particuliers supplémentaires. A vous de les découvrir les loulou."
Pas de mention du sacrifice ni de la Gare ici — ils ont leur propre déclenchement contextuel (voir section dédiée).
Bouton « Retour à l'accueil ».
Étape 5 — Retour à l'accueil
Jeu : "Les Tals, c'est votre monnaie. Ça invoque, ça achète, ça vous sort du statut de noob. Vous en gagnez en jouant, ou en remplissant des missions, là."
(le texte pointe l'icône des missions au moment de la dernière phrase)
Jeu : "Et ici, votre playlist. Calme, combat, ce que vous voulez, quand vous voulez — même en plein combat si l'ambiance vous dit."
Étape 6 — La vraie invocation (le perso prêté disparaît à cet instant)
Jeu : "Comme on est gentil, on vous offre 10 tirages pour commencer. C'est bien parce que c'est vous."
C'est l'UNIQUE multi-invocation offerte du parcours — pas une répétition de l'étape 3, c'est la première vraie invocation du joueur. Choix libre du portail. Aucun commentaire pendant l'animation de révélation elle-même.
Étape 7 — Dernière ligne
Jeu : "Une dernière chose : liez votre compte dans les paramètres, histoire de ne jamais perdre votre progression. Après ça, c'est votre jeu. Enfin non, c'est le mien, mais c'est vous le perso principal."
Liberté totale. Le perso prêté a disparu du compte. Place au vrai roster du joueur.

Déclenchements contextuels — hors tuto principal
Chacun se déclenche UNE SEULE FOIS dans la vie du compte, la première fois que l'écran apparaît réellement en jeu (pas pendant le combat de tuto scripté).
Premier sacrifice
Jeu : "Premier choix qui pique un peu : remplacez une valise par une autre. Même si vous en avez 3 que vous adorez, une doit sauter. Pas de panique, elle peut toujours retomber plus tard dans vos bonus."
Si une synergie active est menacée par ce sacrifice : avertissement explicite affiché ("Attention, vous allez perdre [nom de la synergie] en faisant ça"), dès cette toute première fois.
Point à vérifier en implémentation : A aucun moment on a défini qu’il y aurait un itmer à cette étape, le joueur prend le temps qu’il veut.
Première entrée en Gare
Jeu : "Bienvenue à la Gare. On souffle ici après chaque gros boss : achetez avec vos Tals, ou foncez direct si vous êtes du genre économe. Une fois dépensés, vos Tals ne reviennent pas — à vous de choisir entre booster votre run maintenant ou garder de quoi invoquer plus tard. Les prix sont salés, vous voilà prévenus."
Aucun second texte si le joueur explore longtemps sans agir — l'interface (icônes, prix) doit suffire à guider seule.
