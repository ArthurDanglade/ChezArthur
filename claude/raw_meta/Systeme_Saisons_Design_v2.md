> **STATUT : MATIÈRE PREMIÈRE (MT-D5) — v2, remplace intégralement la v1.**
> Consolidée en contrat par `claude/Plan_Execution_MT2_Saisons.md` §1–§4 (décisions actées + confrontation code).

Take Five Games

**Système de saisons**

Document de design --- v2 --- 5 août 2026

**1. Statut et portée**

Ce document définit le système de progression saisonnière : ce qui est
mesuré, comment les récompenses sont distribuées, comment la difficulté
est choisie par le joueur, et comment les univers tournent d'une semaine
à l'autre.

La version 1 contenait plusieurs erreurs factuelles sur l'économie du
jeu. Elles sont corrigées et tracées en section 14. Ce document remplace
intégralement la v1.

Les valeurs de Tals sont volontairement laissées en placeholder. Elles
seront calibrées après les premiers tests, sur des données réelles
plutôt que sur des estimations.

Une dépendance importante : le calibrage des crans de difficulté ne peut
pas être fait avant la refonte du système d'ennemis. Le système est
concevable maintenant, ses valeurs ne le sont pas.

**2. Principe directeur**

> *Les seuils restent, pour tout le monde, à vie. Le classement s'ajoute
> par-dessus et ne distribue que du prestige.*

Un classement lancé sur une population faible est contre-productif : le
bas du tableau constate qu'il est dernier sur trois cents, le haut sait
que sa position ne vaut rien. Les seuils, eux, fonctionnent dès le
premier joueur.

Mais remplacer les seuils par un classement plus tard serait un piège.
Un joueur qui recevait son palier découvrirait qu'il est quatre-millième
et ne reçoit rien. C'est un ressenti de perte, et l'un des schémas qui
génèrent le plus de désengagement en jeu service.

D'où le modèle retenu : les deux coexistent en permanence. Les seuils
sont la récompense de progression, accessible à tous. Le classement est
un miroir de maîtrise, purement symbolique. Sa mise en place devient
purement additive.

**3. La structure de la tour**

La tour est infinie. Elle se lit en deux parties.

  -----------------------------------------------------------------------
  **Étages**         **Contenu**                        **Difficulté**
  ------------------ ---------------------------------- -----------------
  1 -- 20            Univers en position 1              Faible

  21 -- 40           Univers en position 2              Croissante

  41 -- 60           Univers en position 3              Croissante

  61 -- 80           Univers en position 4              Croissante

  81 -- 100          Univers en position 5              Élevée

  101 et au-delà     Arène du train --- tous univers    Croissante sans
                     mélangés                           fin
  -----------------------------------------------------------------------

Au-delà de l'étage 100, le joueur combat dans l'arène du train : les
mobs et boss de tous les univers, mélangés aléatoirement. Il n'y a pas
de fin.

> *Règle fondamentale : la difficulté suit la POSITION dans la tour,
> jamais l'univers. Un univers placé en position 1 est calibré pour les
> étages 1 à 20, quel que soit cet univers.*

Conséquence de conception à porter dans la refonte des ennemis : chaque
univers doit fonctionner à toutes les positions. Le design d'un ennemi
doit être indépendant de sa position ; seules ses valeurs scalent. Un
passif qui n'a de sens qu'en fin de parcours est un défaut de
conception.

**4. La rotation des univers**

**4.1 Principe**

Chaque semaine, les univers se décalent d'une position. L'univers qui
était en position 1 passe en position 5, tous les autres avancent d'un
cran.

  ------------------------------------------------------------------------------
  **Semaine**   **Pos. 1**   **Pos. 2**   **Pos. 3**   **Pos. 4**   **Pos. 5**
  ------------- ------------ ------------ ------------ ------------ ------------
  1             A            B            C            D            E

  2             B            C            D            E            A

  3             C            D            E            A            B

  4             D            E            A            B            C

  5             E            A            B            C            D

  6             A            B            C            D            E
  ------------------------------------------------------------------------------

Le cycle complet dure cinq semaines. Sur une saison de six semaines,
chaque univers occupe donc chaque position au moins une fois.

**4.2 Ce que ça résout**

-   L'ÉVEIL DES SSR devient accessible quel que soit le personnage tiré.
    Sans rotation, un joueur qui obtient le SSR du cinquième univers
    doit franchir 80 étages avant de pouvoir l'éveiller.

-   LE BOSS RUSH se remplit plus vite : chaque boss passe en position
    accessible une fois toutes les cinq semaines.

-   LA VARIÉTÉ : la run change de visage chaque semaine sans qu'aucun
    contenu ne soit produit.

**4.3 Règles**

-   La rotation a lieu le DIMANCHE SOIR, à une heure fixe déterminée par
    le temps serveur.

-   Elle est SYNCHRONISÉE pour tous les joueurs. Tout le monde a le même
    univers en position 1 la même semaine.

-   Elle s'applique AU LANCEMENT d'une run, jamais en cours. Une run
    démarrée avant la rotation conserve son ordre jusqu'à sa fin.

La synchronisation a un bénéfice secondaire important : chaque semaine
devient un événement partagé. « Cette semaine on commence par Troplin »
est une conversation, un sujet de communauté, et un contenu de
communication hebdomadaire qui ne coûte aucune production.

**5. Les crans de difficulté**

**5.1 Principe**

À chaque lancement de run, le joueur choisit son cran de difficulté. Le
cran multiplie la dangerosité des ennemis et, en contrepartie, le score
obtenu.

> *Les crans sont une progression de COMPTE, permanente. Ils ne sont
> jamais réinitialisés entre les saisons.*

Un joueur qui a débloqué x3 le conserve à vie. Certains joueurs ne
débloqueront jamais les crans les plus élevés, et c'est voulu.

**5.2 Les cinq crans du lancement**

  ------------------------------------------------------------------------
  **Cran**     **Condition de            **Note**
               déblocage**               
  ------------ ------------------------- ---------------------------------
  x1           ---                       Disponible dès le premier
                                         lancement

  x1,5         Étage 50 en x1            ---

  x2           Étage 50 en x1,5          ---

  x3           Étage 50 en x2            ---

  x5           Étage 50 en x3            Dernier cran au lancement
  ------------------------------------------------------------------------

La règle de déblocage est unique et vaut partout : atteindre l'étage 50
dans un cran débloque le cran suivant. Aucun tableau à mémoriser, aucune
exception.

Le premier déblocage intervient donc à mi-parcours des cinq univers, ce
qui fait découvrir l'échelle tôt. La difficulté vient ensuite du fait
qu'atteindre l'étage 50 en x5 est incomparablement plus dur qu'en x1.

**5.3 Extension au-delà de cinq crans**

Cinq crans au lancement, extensibles ensuite. Chaque nouveau cran ajouté
en cours de vie du jeu constitue une annonce de contenu à coût de
production quasi nul.

Ne pas lancer avec dix crans : les valeurs des crans hauts seraient des
estimations invérifiables. On les ajoute lorsqu'on observe où les
joueurs plafonnent réellement.

**5.4 Ce qu'un cran doit modifier**

Point critique. Si un cran ne fait qu'augmenter les points de vie et les
dégâts, la difficulté n'est pas plus intéressante : elle est plus lente.
Le joueur fait la même chose plus longtemps.

Un cran doit modifier plusieurs axes :

-   Statistiques des ennemis --- le levier de base

-   Nombre d'ennemis par étage

-   Fréquence d'apparition des élites

-   Patterns supplémentaires sur les boss

Cette section dépend directement de la refonte du système d'ennemis et
ne peut pas être calibrée avant elle.

**6. Le score de saison**

**6.1 Formule**

score = meilleur (étage atteint × multiplicateur) sur une seule run

Une seule run. Pas un cumul. Le score retenu est le meilleur obtenu au
cours de la saison.

  ------------------------------------------------------------------------
  **Étage atteint**  **Cran**           **Score**
  ------------------ ------------------ ----------------------------------
  100                x1                 100

  100                x2                 200

  50                 x3                 150

  80                 x5                 400

  250                x1                 250

  150                x2                 300

  10                 x5                 50
  ------------------------------------------------------------------------

Deux voies mènent au même palier : aller plus loin, ou aller aussi loin
dans un cran plus difficile. Les deux styles de jeu sont valorisés.

**6.2 Pourquoi le score plutôt que l'étage**

Une tour infinie mesure mal la puissance. Un joueur deux fois plus fort
ne va pas deux fois plus loin : il va un peu plus loin, en jouant
beaucoup plus longtemps. La progression devient horizontale --- plus de
temps pour la même expérience.

> *Le score remplace une progression par endurance par une progression
> par maîtrise. Le joueur est récompensé pour bien jouer, pas pour jouer
> longtemps.*

Conséquence concrète : un joueur puissant n'a pas besoin de faire 300
étages pour progresser dans la piste. Il fait l'étage 100 en x5 --- une
run de durée normale, mais exigeante.

**6.3 Contrainte technique**

PersistentManager.UpdateBestStage() maintient un record à vie, monotone
croissant. Cette valeur ne peut pas servir de métrique de saison : un
joueur ayant atteint l'étage 60 en saison 1 démarrerait la saison 2 avec
des paliers déjà validés sans avoir joué.

bestStage // record à vie, existant, inchangé

bestScoreThisSeason // nouveau, remis à zéro à chaque saison

Le record à vie continue d'exister. Il n'a simplement aucun effet sur la
progression de saison.

**6.4 Périmètre**

-   Le BOSS RUSH est exclu du score. Mode distinct, structure de
    difficulté propre.

-   L'étage compté est le compteur continu de run. Il ne se réinitialise
    pas au passage d'univers.

-   Une run abandonnée volontairement compte pour l'étage atteint au
    moment de l'abandon.

-   Le cran est verrouillé au lancement et ne peut pas être modifié en
    cours de run.

-   Les runs de développement ou de debug ne doivent jamais alimenter le
    score. Un garde explicite est nécessaire.

**7. La piste de saison**

**7.1 Grille**

  ----------------------------------------------------------------------------
  **Palier**   **Score requis** **Récompense**           **Note**
  ------------ ---------------- ------------------------ ---------------------
  1            20               Tals                     Fin du premier
                                                         univers

  2            40               Tals                     ---

  3            60               Tals                     ---

  4            80               Tals                     ---

  5            100              LR de saison --- niveau  Fin des cinq univers
                                1                        

  6            130              Tals                     ---

  7            160              Tals                     ---

  8            200              LR de saison --- niveau  ---
                                2                        

  9            250              Tals                     ---

  10           320              LR de saison --- niveau  ---
                                3                        

  11           400              Tals                     ---

  12           500              LR de saison --- niveau  Palier maximal
                                4                        

  P+           +150 par palier  Tals                     Paliers de prestige
                                                         répétables
  ----------------------------------------------------------------------------

Les valeurs de Tals sont en placeholder et seront calibrées après les
premiers tests. Repère de départ proposé : la piste complète représente
30 à 40 % des Tals gagnés par un joueur sur une saison. Le reste vient
du jeu.

Ce rapport est important : si la piste donne trop, jouer devient
accessoire --- le joueur récupère sa monnaie en franchissant des paliers
plutôt qu'en jouant.

**7.2 Philosophie des paliers**

1.  Le premier palier doit être atteignable dès la première session. Un
    joueur qui termine sa première partie sans avoir rien réclamé n'a
    aucune raison de revenir.

2.  L'espacement est croissant. Dense en bas, où se trouve la majorité
    des joueurs. Large en haut, où le palier devient un objectif.

3.  L'écart avec le palier suivant est toujours visible. C'est le seul
    chiffre qui motive : « il me manque 40 points » est actionnable, «
    je suis au palier 6 » ne l'est pas.

Les paliers de prestige résolvent le problème du plafond atteint tôt. Un
joueur qui franchit le palier 12 en première semaine conserve un
objectif : tous les 150 points de score supplémentaires, une récompense
réduite mais réelle.

**7.3 Le LR de saison**

Le LR est l'objectif de puissance de la saison. Il entre au palier 5 et
monte en niveau aux paliers 8, 10 et 12.

Le système de doublons du jeu s'applique : chaque exemplaire
supplémentaire d'un personnage augmente son niveau, ce qui donne des
statistiques et des passifs additionnels. Un joueur qui atteint le
palier 12 dispose donc du LR au niveau 4.

**Disponibilité**

-   PENDANT sa saison : le LR n'est PAS invocable. La piste est la seule
    voie.

-   À PARTIR DE LA SAISON SUIVANTE : il rejoint un portail cumulatif
    regroupant tous les LR des saisons passées.

Ce portail crée un arbitrage réel : y invoquer signifie ne pas invoquer
sur le SSR de la saison en cours, avec un taux plus faible puisqu'il
s'agit d'un LR. Le joueur qui a obtenu le LR par la piste a économisé ce
choix.

> *Le principe n'est pas « aucune puissance exclusive » mais « aucune
> puissance DÉFINITIVEMENT exclusive ». Le LR redevient obtenable, avec
> un décalage payé par un arbitrage.*

Sans objectif de puissance, atteindre les hauts paliers n'aurait aucune
raison d'être poursuivi. Un élément purement décoratif ne fait pas jouer
quelqu'un vingt heures.

Point ouvert : le portail cumulatif grossit d'une saison à l'autre. En
saison 5 il contiendra quatre LR, donc une chance sur quatre de tirer
celui visé. La dilution devient gênante vers cinq ou six LR. Un système
de sélection ou des portails séparés seront à envisager, mais pas avant
un an.

**8. Structure de saison**

**8.1 Durée**

Six semaines. Quatre imposent un rythme de production trop soutenu pour
un développeur solo. Au-delà de huit, le joueur qui a atteint son
plafond décroche en milieu de saison.

Six semaines contiennent également un cycle de rotation complet plus une
semaine.

**8.2 Ce qui est remis à zéro**

  -----------------------------------------------------------------------
  **Donnée**                **À la fin de    **Note**
                            saison**         
  ------------------------- ---------------- ----------------------------
  Score de saison           REMIS À ZÉRO     Métrique de la saison

  Paliers réclamés          REMIS À ZÉRO     Nouvelle piste

  Compteur de prestige      REMIS À ZÉRO     ---

  Meilleur étage à vie      CONSERVÉ         Record personnel

  Crans de difficulté       CONSERVÉS        Progression de compte,
  débloqués                                  jamais réinitialisée

  Tals                      CONSERVÉS        Monnaie unique

  Personnages, niveaux,     CONSERVÉS        ---
  doublons                                   

  Progression d'éveil des   CONSERVÉE        ---
  SSR                                        

  LR obtenus et leurs       CONSERVÉS        ---
  niveaux                                    
  -----------------------------------------------------------------------

Le point le plus important de ce tableau : les CRANS DE DIFFICULTÉ ne
sont jamais réinitialisés. Ils appartiennent au compte. Un joueur qui a
prouvé sa maîtrise n'a pas à la prouver de nouveau chaque saison.

**8.3 Bornes temporelles**

Début et fin de saison sont déterminés par le temps serveur, jamais par
l'horloge de l'appareil. Voir section 11.

**9. Fin de saison et récapitulatif**

**9.1 Mécanisme**

Au premier lancement suivant la fin d'une saison, le joueur reçoit un
écran de récapitulatif avant d'entrer dans le hub.

**Contenu de l'écran**

-   Score final atteint

-   Meilleur étage et cran correspondant

-   Dernier palier franchi

-   Nombre de runs effectuées

-   Rang au classement, si le classement existe

-   Les récompenses, révélées et créditées à ce moment

**9.2 Règles**

-   LES RÉCOMPENSES SONT ACQUISES à la fin de la saison, indépendamment
    de la date de connexion. Le récapitulatif les révèle et les crédite,
    il ne les conditionne pas. Un joueur absent trois semaines ne perd
    rien.

-   L'écran s'affiche UNE FOIS, en bloquant l'accès au hub. C'est un
    moment de bilan, il mérite l'attention entière.

-   Il reste CONSULTABLE ensuite depuis la page de saison, pour le
    joueur qui l'aurait fermé trop vite.

-   Le versement a lieu AU MOMENT DU RÉCAPITULATIF. Voir un gain
    apparaître est plus satisfaisant que découvrir un solde déjà
    crédité.

Un joueur ayant franchi plusieurs paliers le dernier jour reçoit tout, y
compris les montées de niveau du LR, sans condition supplémentaire.

**10. Interface**

**10.1 Le header**

Le bouton Saison est placé AU CENTRE du header, en gros, entre le pseudo
et le compteur de Tals.

L'affichage du record d'étage est SUPPRIMÉ du header. Il reste
consultable ailleurs, mais il n'a pas sa place dans la barre permanente
: ce n'est plus la métrique active.

**10.2 La page de saison**

Accessible par le bouton du header. Elle doit répondre à quatre
questions, dans cet ordre de lecture :

4.  OÙ EN SUIS-JE --- score actuel, dernier palier franchi.

5.  QU'EST-CE QUI ME MANQUE --- points restants avant le palier suivant,
    et son contenu. C'est l'information la plus importante de la page.

6.  COMBIEN DE TEMPS ME RESTE-T-IL --- compte à rebours, en jours puis
    en heures dans la dernière journée.

7.  QU'EST-CE QUE J'AI DÉJÀ ATTEINT --- piste complète, paliers franchis
    marqués.

La piste doit être défilable et montrer d'emblée la position actuelle,
pas le début. Un joueur au palier 9 ne doit pas avoir à faire défiler
pour se trouver.

Les statistiques de saison --- meilleur étage, cran atteint, nombre de
runs --- sont également affichées sur cette page.

**10.3 Le lancement d'une run**

Le bouton « Lancer une run » n'enchaîne plus directement. Il ouvre un
sélecteur de difficulté.

Lancer une run

→ sélecteur : x1 · x1,5 · x2 · x3 · x5

· crans débloqués : sélectionnables

· crans non débloqués : grisés, avec la condition affichée

→ le joueur choisit

→ la run démarre

L'ordre de rotation en vigueur est visible sur cet écran, sans étape
supplémentaire. Le joueur voit quel univers l'attend en position 1 avant
de choisir sa difficulté.

Contrainte d'UX : cet écran ne doit pas ralentir le lancement. Deux
touches maximum entre l'intention de jouer et le début de la run.

**11. Intégrité et dépendance serveur**

**11.1 Le temps serveur est obligatoire dès la saison 1**

C'est le point qu'on croit pouvoir repousser et qu'on ne peut pas. Dès
qu'une récompense est datée, l'horloge de l'appareil devient un vecteur
d'exploitation : avancer la date, terminer la saison, réclamer, revenir
en arrière, recommencer.

La rotation hebdomadaire aggrave le besoin : elle doit être synchronisée
pour tous les joueurs, ce qui est impossible sans source de temps
commune.

**11.2 Ce qui doit être validé, et quand**

-   PHASE 1, seuils seuls : le temps. Le score peut rester local, le
    risque de triche est limité puisque le joueur n'est comparé à
    personne.

-   PHASE 2, avec classement : le score devient obligatoirement validé
    côté serveur. Un classement alimenté par une valeur déclarée par le
    client n'a aucune valeur.

**11.3 Conséquence sur la phase 1**

Même si le score reste local, les données enregistrées doivent être
auditables : horodatage de la run, étage atteint, cran utilisé, équipe,
durée. Sans ces éléments, il sera impossible de détecter rétroactivement
les anomalies au moment de la bascule.

**12. Le classement --- phase 2**

**12.1 Condition de déclenchement**

Le classement n'est activé que lorsque la population le justifie. Le
critère est qualitatif : il faut qu'un joueur moyen puisse se voir dans
un rang qui a du sens.

**12.2 Ce qu'il mesure et ce qu'il distribue**

La même métrique que les seuils : le meilleur score de la saison.
Utiliser deux métriques différentes obligerait le joueur à entretenir
deux modèles mentaux.

Le classement ne distribue AUCUNE puissance, aucune monnaie, aucun
personnage. Uniquement du symbolique : un titre affichable, une entrée
dans un tableau d'honneur consultable après la fin de la saison.

Ce cloisonnement est ce qui rend l'ajout du classement indolore. Un
joueur non compétitif ne perd strictement rien à son arrivée.

**13. Impact sur la sauvegarde**

Nouvelles données à persister :

seasonId // identifiant de la saison en cours

bestScoreThisSeason // métrique de saison

bestStageThisSeason // pour l'affichage des statistiques

bestTierThisSeason // cran atteint

runsThisSeason // nombre de runs

claimedTiers // paliers réclamés

prestigeTiersClaimed // compteur de prestige

unlockedDifficulties // crans débloqués --- JAMAIS réinitialisé

lastSeasonRollover // horodatage serveur du dernier changement

pendingSeasonRecap // récapitulatif non encore affiché

Le champ unlockedDifficulties doit être clairement séparé des données de
saison dans la structure, pour qu'aucune réinitialisation de saison ne
puisse l'atteindre par erreur.

L'ajout de ces données est le moment où la sauvegarde a besoin d'un
système de versionnage. Voir le document dédié au chantier backend et
sauvegarde.

**14. Erratums v1 → v2**

  ---------------------------------------------------------------------------
  **Réf.**   **Modification**              **Motif**
  ---------- ----------------------------- ----------------------------------
  E1         Suppression de toutes les     Erreur factuelle. Les valises et
             récompenses de type valise et objets sont des bonus de run, ils
             objet                         n'ont aucun rapport avec la
                                           progression de saison.

  E2         Suppression de la « monnaie   Erreur factuelle. Le jeu n'a
             d'invocation »                qu'une seule monnaie, les Tals, et
                                           elle sert à invoquer.

  E3         Suppression des récompenses   Il n'existe pas de cosmétiques
             cosmétiques                   dans le jeu. Les personnages sont
                                           le contenu collectionnable.

  E4         Suppression des fragments de  N'existe pas dans le jeu.
             personnage                    

  E5         Ajout de la rotation          Nouveau système. Rend l'éveil des
             hebdomadaire des univers      SSR accessible quel que soit le
                                           personnage tiré.

  E6         Ajout du système de crans de  Nouveau système. Remplace la
             difficulté et du score        progression par endurance par une
                                           progression par maîtrise.

  E7         La métrique passe de «        Conséquence de E6.
             meilleur étage » à « meilleur 
             score »                       

  E8         Le LR de saison entre dans la Décision assumée : sans objectif
             piste au palier 5             de puissance, atteindre les hauts
                                           paliers n'a pas de raison d'être
                                           poursuivi.

  E9         Le principe « aucune          Le LR redevient obtenable au
             puissance exclusive » devient portail cumulatif dès la saison
             « aucune puissance            suivante. Ce n'est pas une
             définitivement exclusive »    exclusivité mais un décalage.

  E10        Ajout de l'écran de           Transforme une distribution
             récapitulatif de fin de       silencieuse en moment de bilan.
             saison                        
  ---------------------------------------------------------------------------

**15. Points ouverts**

-   Quantités exactes de Tals par palier --- à calibrer après les
    premiers tests.

-   Valeurs précises des multiplicateurs --- dépend de la refonte des
    ennemis.

-   Ce qu'un cran modifie au-delà des statistiques --- même dépendance.

-   Seuil de population déclenchant le classement.

-   Dilution du portail cumulatif de LR au-delà de cinq saisons.

-   Existe-t-il une piste payante en parallèle de la piste gratuite ?
    Question de monétisation, non tranchée.

-   Thématique de la saison 1 : rattachée à un univers, à un personnage,
    ou purement mécanique ?

-   Heure exacte de la rotation le dimanche soir, et fuseau de
    référence.

**16. Journal des décisions**

  ---------------------------------------------------------------------------
  **Réf.**   **Décision**                                       **Statut**
  ---------- -------------------------------------------------- -------------
  D1         Modèle hybride : seuils permanents, classement     Validé
             additif ensuite                                    

  D2         Le classement ne distribue que du prestige         Proposé

  D3         Score de saison distinct du record à vie           Validé

  D4         Boss Rush exclu de la métrique                     Proposé

  D5         Paliers à espacement croissant                     Validé

  D6         Versement des récompenses au moment du récap de    Validé
             fin de saison                                      

  D7         Paliers de prestige répétables au-delà du palier   Validé
             12                                                 

  D8         Temps serveur obligatoire dès la saison 1          Validé

  D9         Rotation hebdomadaire des univers, dimanche soir,  Validé
             synchronisée                                       

  D10        La difficulté suit la POSITION dans la tour,       Validé
             jamais l'univers                                   

  D11        Score = étage × multiplicateur                     Validé

  D12        Crans de difficulté permanents, liés au compte     Validé

  D13        Déblocage d'un cran : atteindre l'étage 50 dans le Validé
             cran précédent                                     

  D14        5 crans au lancement, extensibles en cours de vie  Validé
             du jeu                                             

  D15        Le LR de saison n'est pas invocable pendant sa     Validé
             propre saison                                      

  D16        Les doublons de LR montent son niveau              Validé

  D17        Sélecteur de difficulté à chaque lancement de run  Validé

  D18        Une seule monnaie : les Tals                       Validé

  D19        Bouton Saison au centre du header, record d'étage  Validé
             supprimé                                           
  ---------------------------------------------------------------------------

Les entrées « Proposé » sont argumentées dans le corps du document et
passent en « Validé » après relecture.

*Fin du document --- v2.*
