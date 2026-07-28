# Câblage du lot Noël : checkpoint cadeau, porte bois, attaques du Père Noël

Quatre demandes dans cette conversation, un fil commun : **des assets livrés « nus »**
(commits `30fb7c7`, `0618988`, `f81b869` — tous « assets uniquement ») qu'il fallait
brancher. Aucune génération PixelLab, aucun nouvel asset.

---

# 1. Checkpoint « cadeau mécanique »

`Checkpoint.cs` était verrouillé sur les noms de nœuds `TrouInactif`/`TrouActif` du trou de
pêche. Trois points d'extension ouverts **sans rien casser** (la classe reste concrète : elle
est instanciée par `checkpoint_peche.tscn` **et 5 scènes de décor de la grotte florale**) :

- `PreparerVisuel()` et `AfficherEtat(bool)` passent en `protected virtual` ;
- l'offset de respawn codé en dur `(-20, 0)` devient `[Export] OffsetRespawn`, même valeur par
  défaut → zéro changement pour les instances existantes.

**Neuf** : `scripts/Entities/Misc/CheckpointCadeau.cs` + `scenes/interactifs/checkpoint_cadeau.tscn`.
Le `SpriteFrames` est **inline dans la scène** (donc visible dans l'éditeur, pas besoin d'un
nœud `Apercu`). L'animation `ouverture` n'est pas bouclée et sa dernière frame *est* l'état
ouvert : Godot la maintient, aucun `AnimationFinished` à câbler. Un garde
`_premierAffichageFait` évite que le cadeau se rejoue à chaque `GameState.Charger()` (le signal
`CheckpointActif` est ré-émis au chargement). Fermeture instantanée (choix utilisateur).

Assets : `cadeau_checkpoint_ferme` == `transition_01` et `confettis` == `confettis_01`
(doublons binaires vérifiés au md5) — le fichier non numéroté est ignoré. Sprite à
l'échelle 0,4, `offset (0, -83)` pour que l'origine se pose sur la surface de marche.

> Aucune instance posée dans `03-monde2.tscn` (choix utilisateur) : **l'usine reste sans point
> de sauvegarde** tant que des instances n'y sont pas déposées.

---

# 2. Porte en bois de l'usine

**Neuf** : `scripts/Entities/Interactable/PorteBois.cs` + `scenes/interactifs/PorteBois.tscn`.
`StaticBody2D` qui barre le passage fermé, libère le passage ouvert. 4 animations dont
`fermeture` = les frames de `ouverture` à l'envers (aucun asset de plus).

**Le skew a dicté la structure** : la physique 2D de Godot ne gère pas une transformation
cisaillée. Le skew se pose donc sur le nœud **`Visuel`** (purement graphique) et jamais sur la
racine — les trois collisions restent d'aplomb dessous.

**Profondeur (2e passe)** : le cadre est coupé en deux, non pas en deux PNG mais en **deux
`Sprite2D` affichant chacun une moitié du même fichier via `region_rect`** — zéro nouvel asset,
ligne de coupe réglable dans l'inspecteur. Le joueur (`z_index = 1`) passe **devant** le montant
gauche (z −1, avec le battant) et **derrière** le montant droit (z 3).

⚠️ Un `z_index` posé sur la racine `PorteBois` casserait l'effet : Godot ajoute le z du parent
à celui des enfants (`z_as_relative`), les deux moitiés repasseraient du même côté du joueur.

## Interaction — mécanisme réutilisé, pas réinventé

- L'action `action` existait déjà (**Entrée + Espace**).
- Le rappel de touche réutilise `BulleDialogue.AfficherRappel()`.
- Le conflit Espace = saut est réglé par le flag existant, **généralisé de
  `DialogueDisponible` en `InteractionDisponible`** (`GameState`, + `DeclencheurDialogue`,
  `ZoneBoss`, `Player`) : il ne concerne plus seulement les PNJ parlants. Comportement
  inchangé — à l'arrêt on manœuvre, en marchant on saute.

**Softlock bouché** : refermer la porte sur le joueur l'emmurait dans un `StaticBody2D`. La
porte refuse de se fermer tant que l'embrasure est occupée (`ZoneBattant`), et se rouvre si
quelqu'un s'y glisse pendant la descente du battant.

> **Non fait** : l'état de la porte n'est pas sauvegardé (elle repart sur `OuverteAuDepart`).
> `GameState.EstConsomme` ne colle pas — une porte se rouvre.

---

# 3. Père Noël : punch au sol + lancer de cadeau explosif

Le boss n'avait que 2 animations (`idle`, `marche`) et 3 patterns. Il en a **4 animations et
4 patterns** : `SalveCadeaux` 30 % | `LancerCadeau` 30 % | `PunchSol` 25 % | `Cheminee` 15 %.

Comme convenu : le lancer **remplace le jet de givre** (l'`EclatGlace` disparaît du Père Noël,
il reste au Lutin Mecha) et la salve garde son `MiniJouetExplosif` au parachute.

Les animations `punch_sol`/`lancer_bas` ne se jouent **qu'au déclenchement** : les télégraphes
restent sur la pose de repos (flash + écrasement), sinon l'animation mangerait la fenêtre
d'esquive. `punch_onde` n'entre pas dans le `SpriteFrames` du boss — cadre 204×74 incompatible
avec son ancrage (−63 sur un cadre 128).

## Deux briques neuves

**`CadeauExplosif`** (`scripts/Entities/Damage/` + `scenes/projectiles/`) — calqué sur
`EclatGlace`, deux écarts : les frames `vol` et `explosion` cohabitent **à plat dans le même
dossier** (d'où un paramètre `prefixe` optionnel ajouté à `AnimationsSprite.ChargerFrames` —
défaut vide, aucun appel existant modifié) ; et `Disparaitre()` ajoute un **souffle de zone**
testé à la distance, comme `MiniJouetExplosif`, pour ne pas toucher une forme pendant un flush
physique.

**`OndeDeChoc`** (idem) — la brique qui manquait : `BossLutinMecha` et `BossCerf` refaisaient
chacun une onde procédurale en `ColorRect`. Elle reprend leur idiome (zone étalée par tween,
touche une seule fois, **ignore un joueur en l'air**) avec les vraies frames, et son
`DossierFrames` est exporté pour qu'un autre boss la réutilise.
**Le Mecha n'a pas été migré** — hors périmètre.

## Réglage à surveiller

Le boss tient une bande de 120→220 px alors que l'art de l'onde ne couvre que ~102 px de chaque
côté : tiré tel quel, le punch raterait la moitié du temps. D'où `PorteeOnde = 160` (le sprite
est mis à l'échelle pour suivre) **et** un punch tiré uniquement si le joueur est à portée,
sinon repli sur le lancer. **C'est le réglage le plus incertain du lot.**

## Renommages d'exports (scène mise à jour en conséquence)

`DelaiArmementGivre` → `DelaiArmementLancer`, `VitesseEclat` → `VitesseCadeau`,
`SceneEclatGlace` → `SceneCadeauExplosif`, + `ScenePunchOnde`.
⚠️ Renommer un `[Export]` sans toucher la scène fait **perdre l'override en silence** —
`scenes/boss/BossPereNoel.tscn` a été corrigé.

---

# 4. Correctif : projectiles retournés vers la gauche (signalé par Marc)

**Bug de la base `Projectile`, donc TOUS les projectiles du jeu** — cadeau explosif comme boule
de neige du joueur. `_PhysicsProcess` faisait `Rotation = VelociteCourante.Angle()` : vers la
gauche l'angle vaut ~180°, ce qui **retourne** le sprite au lieu de le **miroiter**.

Un tir vers la gauche est le *miroir* d'un tir vers la droite, pas sa rotation d'un demi-tour.
`OrienterSurLaVitesse()` compose donc un `FlipH` **sur le sprite** avec l'angle du vecteur
opposé — les deux se combinent exactement en la réflexion voulue. Le flip va sur le sprite et
jamais sur la racine : flipper la racine déformerait aussi la forme de collision.

Les 5 scènes de projectile portent toutes le nœud `AnimatedSprite2D` attendu, le correctif les
couvre donc sans retoucher aucune d'elles.

---

# 5. Mecha jouet lanceur → cadeaux explosifs

`Tirer()` était typé en dur sur `Instantiate<BouleDeNeige>()` : un simple échange de scène
aurait planté. Il est passé sur la **base `Projectile`**, ce qui rend l'ennemi agnostique —
n'importe quel projectile s'y branche désormais depuis la scène. Export `SceneBoule` renommé
`SceneProjectile` et `scenes/ennemis/usine/MechaJouetLanceur.tscn` pointe sur
`CadeauExplosif.tscn`. Gravité et arc inchangés (les deux projectiles partagent `Gravite = 480`),
la trajectoire ne bouge pas.

> ⚠️ **Saut de difficulté** : la boule faisait 1 dégât au contact ; le cadeau en fait 2 **plus
> un souffle de rayon 46**. Le `RayonSouffle` vit dans `CadeauExplosif.tscn`, **partagé avec le
> boss** — un cadeau plus faible pour l'ennemi de base demanderait une scène variante.

---

# Fichiers

**Neufs** — `scripts/Entities/Misc/CheckpointCadeau.cs`,
`scripts/Entities/Interactable/PorteBois.cs`, `scripts/Entities/Damage/CadeauExplosif.cs`,
`scripts/Entities/Damage/OndeDeChoc.cs` + les 4 `.tscn` correspondants
(`scenes/interactifs/checkpoint_cadeau.tscn`, `scenes/interactifs/PorteBois.tscn`,
`scenes/projectiles/CadeauExplosif.tscn`, `scenes/projectiles/OndeDeChoc.tscn`).

**Modifiés** — `Checkpoint.cs`, `GameState.cs`, `DeclencheurDialogue.cs`, `ZoneBoss.cs`,
`Player.cs`, `AnimationsSprite.cs`, `DamageSource.cs`, `Projectile.cs`, `BossPereNoel.cs`,
`MechaJouetLanceur.cs`, `scenes/boss/BossPereNoel.tscn`,
`scenes/ennemis/usine/MechaJouetLanceur.tscn`.

`03-monde2.tscn` **n'a pas été touché** (travail en parallèle côté Marc).

---

# Vérification

`dotnet build` propre, **0 avertissement**.

⚠️ **`godot --headless --build-solutions` avale les erreurs de compilation** — il a affiché un
build « réussi » sur un `error CS0122`. **`dotnet build` est le seul contrôle fiable.**

Harnais headless temporaires, tous supprimés depuis :

| Vérifié | Résultat |
|---|---|
| Porte : cycle complet | `ferme/bloque` → `ouverture/passage libre` → `fermeture` → **blocage réarmé en fin d'animation** |
| Porte : anti-softlock | joueur dans l'embrasure → **reste ouverte** |
| Porte : conflit Espace | joueur à l'arrêt devant la porte → `velY = 0.0`, **il ne saute pas** |
| Boss : les 4 patterns | `punch_sol`, `lancer_bas`, `CadeauExplosif`, `OndeDeChoc`, `MiniJouetExplosif` sortent tous ; nœuds bien libérés |
| Onde : règle d'esquive | **au sol 5→3 PV** (2 dégâts) / **en l'air 5→5 PV, 0 dégât** |
| Orientation vers la gauche | `haut=(0,-1)`, `nez=(-1,0)`, `flipH=True` → **miroir, pas demi-tour** |
| Non-régression | 0 exception sur `TestBossPereNoel`, `TestBossLutinMecha`, `TestEnnemisUsine`, `01-monde1` |

Le `ObjectDB instances leaked at exit` des scènes de boss est **préexistant** (identique sur le
Lutin Mecha, non touché). Sauvegarde `pantalon.json` copiée puis restaurée autour de chaque run.

**Play-test manuel fait par Marc → le comportement est bon.** Restent non jugeables en
headless : la lisibilité des deux nouveaux télégraphes du boss à la cadence actuelle, et la
portée réelle de l'onde (`PorteeOnde`).

---

# À signaler

- Les `.import` des trois lots d'assets étaient **non trackés** — ils doivent partir avec le
  code, sinon les scènes cassent chez les autres.
- `BUDGET.md` n'a pas d'entrée pour ces lots.
- Rien n'a été commité.
