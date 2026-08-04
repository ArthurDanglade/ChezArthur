> **STATUT : MATIÈRE PREMIÈRE NON CONTRACTUELLE (MT-D5)**
> Source : docx Arthur (Downloads). À confronter au code à l'ouverture MT1 / MT2-0.
> **Attention Arthur** : certains éléments sont incorrects ou non figés — ne rien prendre pour argent comptant. Interview manager = trous, contradictions, points non figés uniquement.
> Original docx aussi versé dans ce dossier.
Take Five Games
Système de saisons
Document de design — v1 — statut : à valider

1. Statut et portée
Ce document définit le système de progression saisonnière : ce qui est mesuré, comment les récompenses sont distribuées, et comment un classement compétitif viendra s’y ajouter plus tard sans rien retirer aux joueurs.
Il couvre le game design et les règles d’intégrité. Il ne couvre pas l’implémentation technique, qui dépend d’un choix d’infrastructure backend non arrêté à ce jour. Toutes les décisions de ce document sont conçues pour être indépendantes de ce choix.
Les valeurs chiffrées (nombre de paliers, seuils, quantités de récompenses) sont des propositions calibrées sur une intuition de la courbe de progression, pas sur des données. Elles devront être ajustées après les premiers tests. La structure, elle, est faite pour durer.
2. Principe directeur
Les seuils restent, pour tout le monde, à vie. Le classement s’ajoute par-dessus et ne distribue que du prestige.
Le raisonnement est le suivant. Un classement compétitif lancé sur une population faible est contre-productif : le bas du tableau constate qu’il est dernier sur trois cents, le haut sait que sa position ne vaut rien. Les seuils, eux, fonctionnent dès le premier joueur.
Mais la bascule vers un classement est un piège si elle remplace les seuils. Un joueur qui atteignait l’étage 40 et recevait son palier découvrirait qu’il est quatre-millième et ne reçoit rien. C’est un ressenti de perte, et c’est l’un des schémas qui génèrent le plus de désengagement en jeu service.
D’où le modèle retenu : les deux coexistent en permanence. Les seuils sont la récompense de progression, accessible à tous. Le classement est un miroir de maîtrise, qui ne distribue que du symbolique. Sa mise en place devient purement additive : personne ne perd rien, et aucune refonte d’économie n’est nécessaire quand la population grandit.
3. La métrique
3.1 Ce qui est mesuré
Le plus haut étage atteint dans une seule run, au cours de la saison en cours.
Une seule run. Pas un cumul. Un joueur qui atteint l’étage 40 lors de sa meilleure tentative est au même niveau qu’un joueur qui y arrive du premier coup. La métrique récompense la performance de pointe, pas le temps passé.
3.2 Contrainte technique majeure
Le projet dispose déjà de PersistentManager.UpdateBestStage(), qui maintient un record à vie, monotone croissant. Cette valeur ne peut pas servir de métrique de saison.
Un joueur ayant atteint l’étage 60 en saison 1 démarrerait la saison 2 avec le palier maximal déjà validé, sans avoir joué. Il faut donc deux compteurs distincts :
bestStage             // record à vie, existant, inchangé
bestStageThisSeason   // nouveau, remis à zéro à chaque saison
Le record à vie continue d’exister et de s’afficher — c’est une fierté personnelle. Il n’a simplement aucun effet sur les récompenses de saison.
3.3 Périmètre de la métrique
Le Boss Rush est EXCLU. C’est un mode distinct, avec sa propre structure de difficulté. Mélanger les deux rendrait le nombre illisible et le comparatif injuste.
L’étage compté est le compteur continu de run, tel qu’il existe déjà dans RunManager. Il ne se réinitialise pas au passage d’univers.
Une run abandonnée volontairement compte pour l’étage atteint au moment de l’abandon.
Les runs de développement ou de debug ne doivent jamais alimenter la métrique. Un garde explicite est nécessaire.

4. Structure de saison
4.1 Durée
Recommandation : six semaines.
Quatre semaines impose un rythme de production de récompenses trop soutenu pour un développeur solo. Au-delà de huit, le joueur qui a atteint son plafond décroche en milieu de saison. Six semaines laisse à un joueur occasionnel le temps d’atteindre les paliers intermédiaires, tout en conservant un caractère événementiel.
4.2 Ce qui est remis à zéro
Donnée
À la fin de saison
Note

Meilleur étage de la saison
REMIS À ZÉRO
Métrique de la saison

Paliers réclamés
REMIS À ZÉRO
Nouvelle piste de récompenses

Compteur de paliers de prestige
REMIS À ZÉRO


Meilleur étage à vie
CONSERVÉ
Record personnel, indépendant des saisons

Tals
CONSERVÉS
Monnaie persistante

Personnages possédés et niveaux
CONSERVÉS


Progression d’éveil des SSR
CONSERVÉE


Cosmétiques obtenus
CONSERVÉS
Y compris ceux des saisons passées

Boss Rush
CONSERVÉ
Mode distinct, hors métrique de saison


4.3 Bornes temporelles
Le début et la fin de saison sont déterminés par le temps serveur, jamais par l’horloge de l’appareil. Ce point est développé en section 8 ; il est structurant et ne peut pas être repoussé.
5. Les paliers
5.1 Philosophie
Trois règles, dans cet ordre de priorité :
Le premier palier doit être atteignable dès la première session. Un joueur qui termine sa première partie sans avoir rien réclamé n’a aucune raison de revenir.
L’espacement est croissant. Dense en bas, où se trouve la majorité des joueurs, et où chaque palier franchi entretient l’élan. Large en haut, où le palier devient un objectif et non une étape.
L’écart avec le palier suivant est toujours visible. C’est le seul chiffre qui motive réellement : « il me manque quatre étages » est actionnable, « je suis au palier 6 » ne l’est pas.
5.2 Grille proposée
Palier
Étage requis
Écart
Note

1
3
+3
Atteint en première session — obligatoire

2
6
+3
Fin de première session pour un joueur régulier

3
10
+4
Première semaine

4
15
+5


5
21
+6
Palier de mi-parcours pour le joueur moyen

6
28
+7


7
36
+8


8
45
+9
Récompense marquante — cosmétique

9
56
+11


10
68
+12


11
82
+14


12
100
+18
Palier maximal — récompense majeure

P+
+15 par palier
—
Paliers de prestige répétables, récompense réduite

Les paliers de prestige résolvent le problème du plafond atteint tôt. Un joueur qui franchit le palier 12 en première semaine conserve un objectif : tous les quinze étages supplémentaires, il obtient une récompense réduite mais réelle. Sans ce mécanisme, la saison est terminée pour lui au bout de sept jours.
6. Les récompenses
6.1 Principe non négociable
Aucune puissance de jeu exclusive derrière les paliers hauts.
Un personnage, une valise ou un effet obtenable uniquement en atteignant le palier 12 crée un écart de puissance que les joueurs suivants ne pourront jamais combler. Sur un jeu à saisons, cet écart se cumule saison après saison jusqu’à rendre le jeu illisible pour tout nouveau venu.
Les paliers hauts donnent de l’accélération et du prestige : plus de monnaie, plus vite, et des cosmétiques uniques. Jamais du contenu de jeu inaccessible autrement.
6.2 Catégories
Catégorie
Paliers concernés
Quantité
Note

Tals
Tous les paliers
Croissante
Jamais rare — c’est le fil conducteur

Monnaie d’invocation
Paliers 3, 5, 7, 9, 11, 12
Croissante
Le vrai moteur de désir

Valises / objets
Paliers 2, 4, 6, 8, 10
Rareté croissante
À caler sur l’économie de run

Cosmétiques
Paliers 8 et 12
Unique par saison
Cadre, titre, vignette — non rejouable

Fragments de personnage
Paliers 10 et 12
Élevée
Jamais un personnage exclusif

Paliers de prestige
Au-delà du palier 12
Faible et répétable
Anti-plafond, pas une source de puissance

Les quantités exactes ne peuvent pas être fixées dans ce document : elles dépendent de l’équilibrage de l’économie d’invocation, qui n’est pas figée. Elles constituent un point ouvert de la section 11.
7. Le classement — phase 2
7.1 Condition de déclenchement
Le classement n’est activé que lorsque la population le justifie. Le seuil exact est à définir, mais le critère est qualitatif : il faut qu’un joueur moyen puisse se voir dans un rang qui a du sens. Un classement où le joueur médian est 150ᵉ sur 300 ne motive personne.
7.2 Ce qu’il mesure
La même métrique que les seuils : le plus haut étage atteint en une run sur la saison. Utiliser deux métriques différentes obligerait le joueur à entretenir deux modèles mentaux, et rendrait la page de saison illisible.
7.3 Ce qu’il distribue
Un titre affichable, propre à la saison.
Un cadre ou une bordure de profil.
Une entrée permanente dans un tableau d’honneur, consultable après la fin de la saison.
Rien d’autre. Aucune monnaie, aucun personnage, aucun objet, aucune statistique.
Ce cloisonnement est ce qui rend l’ajout du classement indolore. Un joueur non compétitif ne perd strictement rien à son arrivée.
7.4 Coexistence
Les seuils ne sont jamais désactivés, réduits ni remplacés. Le classement s’affiche à côté d’eux, sur la même page, comme une seconde lecture de la même performance.

8. Intégrité et dépendance serveur
8.1 Le temps serveur est obligatoire dès la saison 1
C’est le point que l’on croit pouvoir repousser, et qu’on ne peut pas. Dès qu’une récompense est datée, l’horloge de l’appareil devient un vecteur d’exploitation : le joueur avance sa date, la saison se termine, il réclame ses récompenses, il revient en arrière, il recommence.
Un système de seuils sans classement n’échappe pas à ce problème. Il faut une source de temps de confiance dès la première saison.
8.2 Ce qui doit être validé côté serveur, et quand
Phase 1 (seuils seuls) : le temps. La métrique peut rester locale, avec un risque de triche limité — un tricheur se ment surtout à lui-même puisqu’il n’est comparé à personne.
Phase 2 (classement) : la métrique devient obligatoirement validée côté serveur. Un classement alimenté par une valeur déclarée par le client n’a aucune valeur.
8.3 Conséquence sur la phase 1
Même si la métrique reste locale en phase 1, les données enregistrées doivent déjà être auditables : horodatage de la run, étage atteint, équipe utilisée, durée. Sans ces éléments, il sera impossible de détecter rétroactivement les anomalies au moment de la bascule.
9. Affichage et UX
La page de saison doit répondre à quatre questions, dans cet ordre de lecture :
Où en suis-je ? — palier actuel, meilleur étage de la saison.
Qu’est-ce qui me manque ? — étages restants avant le palier suivant, et ce que ce palier contient. C’est l’information la plus importante de la page.
Combien de temps me reste-t-il ? — fin de saison, en jours puis en heures dans la dernière journée.
Qu’est-ce que j’ai déjà obtenu ? — piste complète des paliers, avec état réclamé ou non.
La piste des paliers doit être défilable et montrer d’emblée la position actuelle, pas le début de la piste. Un joueur au palier 9 ne doit pas avoir à faire défiler pour se trouver.
Emplacement : à trancher entre une page dédiée dans la navigation principale et une intégration à la page Missions. Point ouvert.
10. Impact sur la sauvegarde
L’ajout des données de saison est le moment où la sauvegarde a besoin d’un système de versionnage. À ce jour, PersistentManager ne présente pas de champ de version identifiable.
Nouvelles données à persister :
seasonId                 // identifiant de la saison en cours
bestStageThisSeason      // métrique
claimedTiers             // paliers réclamés
prestigeTiersClaimed     // compteur de paliers de prestige
lastSeasonRollover       // horodatage serveur du dernier changement de saison
Un champ de version doit être ajouté avant ces données, avec un chemin de migration défini. Ajouté aujourd’hui, c’est une modification de dix minutes. Ajouté après la mise en production, c’est une semaine de travail et des tickets de support.
11. Points ouverts
Quantités exactes de récompenses — dépendent de l’équilibrage de l’économie d’invocation, non figée.
Durée de saison définitive — six semaines proposées, à confirmer.
Seuil de population déclenchant le classement.
Emplacement de la page de saison dans la navigation.
Existe-t-il une piste payante en parallèle de la piste gratuite ? Cette question touche à la monétisation et n’est pas tranchée.
Choix de l’infrastructure backend — bloque toute implémentation, ne bloque aucune décision de ce document.
Thématique de la saison 1 : une saison est-elle rattachée à un univers, à un personnage, ou purement mécanique ?
12. Journal des décisions
Réf.
Décision
Statut

D1
Modèle hybride : seuils permanents pour tous, classement additif ensuite
Validé

D2
Le classement ne distribue que du prestige, jamais de la puissance
Proposé

D3
Métrique de saison distincte du record à vie
Proposé

D4
Boss Rush exclu de la métrique de saison
Proposé

D5
Paliers à espacement croissant, premier palier atteignable en session 1
Proposé

D6
Réclamation automatique des paliers en fin de saison
Proposé

D7
Paliers de prestige répétables au-delà du maximum
Proposé

D8
Temps serveur obligatoire, même sans classement
Proposé

Les décisions marquées « Proposé » sont des recommandations argumentées dans le corps du document. Elles passent en « Validé » après relecture.

Fin du document — v1.
