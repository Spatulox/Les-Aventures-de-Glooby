# Deux boss de fin : `monde2` → `BossEnd`, qui choisit lequel spawner

**Besoin** : deux boss de fin, le second **caché**. La fin de `monde2.tscn` charge `BossEnd.tscn`, et c'est **`BossEnd` qui décide quel boss faire apparaître** :

- sans rien faire → **Père Noël** (fin normale)
- après avoir donné ses 50 poissons au lutin CGT → **Lutin Mecha** (fin cachée)

L'aiguillage est au **spawn du boss**, pas au changement de scène : une seule arène sert de fin normale ou de fin cachée.

**Rien à créer côté condition** : le don est déjà persisté par le dialogue (`ChoixDialogue.IdMemoire = "lutin_cgt_don_poissons"` dans `assets/dialogues/banquise_fin_lutin_cgt.tres` → `DeclencheurDialogue.ValiderChoix` → `GameState.MarquerConsomme`, sérialisé dans `DonneesSauvegarde.ElementsConsommes`). Constante : `LutinCgt.IdDonPoissons`.

## 1. L'aiguillage — `scripts/Core/ZoneBoss.cs`

4 exports sur la base réutilisable, donc réglables dans l'inspecteur de n'importe quelle arène :

| Export | Rôle |
|---|---|
| `MemoireRequise` | id `GameState.EstConsomme` qui bascule vers la variante |
| `SceneBossAlternative` | le boss caché |
| `NomBossAlternatif` | vide = garder `NomBoss` |
| `PvBossAlternatif` | 0 = garder `PvBoss` |

Trois propriétés résolvent l'embranchement — `SceneChoisie`, `NomChoisi` (public), `PvChoisis` — et **tout le reste passe par elles**, jamais par les exports bruts : test `EstBossVaincu`, `DefinirPvMax`, spawn, `MarquerBossVaincu`. `VariantePrise` exige mémoire **et** scène alternative ensemble : un câblage à moitié fait reste sans effet.

Effets de bord corrigés :
- `Barre.DefinirNom(NomChoisi)` avant de révéler la barre — sinon une arène à deux boss affichait le nom authoré sur `BossHudBarre`.
- `ZoneBossCerf` et `ZoneBossLutinMecha` marquaient `NomBoss` au lieu de `NomChoisi`.
- `GetParent().CallDeferred(AddChild, boss)` — même correctif que le commit `4cb85fc` de `main` (arrivé en parallèle), conservé ici avec l'aiguillage.

## 2. Bornes d'arène génériques — `scripts/Common/BossBorne.cs`

**Nécessaire au bon fonctionnement de la fin cachée** : chaque `ZoneBossXxx` posait les bornes via un cast vers *un* type de boss (`boss is BossCerf`, `boss is BossLutinMecha`). Dans une arène à deux boss de classes différentes, seul celui du cast était borné — l'autre gardait ses valeurs par défaut (80 / 2800) et débordait du décor.

Nouveau contrat `BossBorne` (`float LimiteGauche/LimiteDroite { set; }`), implémenté par `BossCerf`, `BossLutinMecha` et `BossPereNoel` (leurs deux champs `[Export]` deviennent des propriétés auto — aucun `.tscn` ne les surchargeait). `ZoneBoss.ConfigurerBoss` les pose génériquement, et les trois overrides devenus redondants ont disparu.

## 3. Le boss Père Noël

- **`scripts/Entities/Pnj/BossPereNoel.cs`** (`: Boss, BossBorne`) — il n'a **qu'une animation générée** (`assets/pnj/pere_noel/idle`, 5 frames, le dossier partagé avec le PNJ amical `PereNoel.cs`). Budget PixelLab clos → langage corporel entièrement procédural (`Effets` + tweens), et attaques qui **réutilisent des scènes existantes** :
  - **Salve de cadeaux** — télégraphe 0,9 s (écrasement du sprite + rougeoiement), largue des `MiniJouetExplosif`, puis reste essoufflé : coups doublés (`AjusterDegats`).
  - **Jet de givre** — télégraphe bleu, tire un `EclatGlace` ; éventail de trois en phase 2 (surcharge vectorielle de `Projectile.Initialiser`).
  - **Cheminée** — s'évapore et se rematérialise de l'autre côté du joueur, borné par l'arène. C'est son **seul déplacement**, faute d'animation de marche ; intouchable pendant le passage (`IsInvincibleToDamage`).
  - Deux phases (bascule à 50 % PV), `AnimationMort` renvoyée sur `idle` + affaissement procédural (pieds au sol). **Aucun `DamageSource` nouveau** : les deux projectiles portent déjà le leur.
- **`scenes/entites/boss/BossPereNoel.tscn`** — au format des autres boss : `Apercu`, `AnimatedSprite2D` à y = −44, collision 48×78, `PvMax = 45`, layers PNJ.
- **`scripts/Core/ZoneBossPereNoel.cs`** + **`scenes/boss/zone_boss_pere_noel.tscn`** — l'arène finale : persistance de la défaite + enchaînement sur l'écran de fin. Elle ne connaît le type d'aucun des deux boss.
- **`scenes/test/TestBossPereNoel.tscn`** — arène jouable pour le tester seul.

## 4. Câblage

**`scenes/niveaux/BossEnd.tscn`** (arène remplie sur le patron de `ReindeerBoss.tscn` : sol usine 2752 px, `BossHudBarre`, `PointEntree` d'Id `bossEnd`, fond usine) :

```
Arene/ZoneBossFinale  (zone_boss_pere_noel.tscn)
  SceneBoss            = BossPereNoel.tscn     NomBoss           = "Pere Noel"   PvBoss           = 45
  MemoireRequise       = lutin_cgt_don_poissons
  SceneBossAlternative = BossLutinMecha.tscn   NomBossAlternatif = "Lutin Mecha" PvBossAlternatif = 40
  CheminSceneVictoire  = res://scenes/ui/ecran_fin.tscn
```

**`scenes/niveaux/monde2.tscn`** — sortie est sous `UsinePereNoel/Interactifs`, transition **simple, sans condition**, vers `BossEnd` (`PointEntreeCible = bossEnd`). `x = 4700` est juste avant le bord est de `ZoneUsine` ; le `y` est un **placeholder**, `UsinePereNoel/Sol` étant encore vide.

**`scenes/test/TestBossLutinMecha.tscn`** — deux chemins morts corrigés (`scenes/boss/BossLutinMecha.tscn` → `scenes/entites/boss/`, `scenes/decors/usine/SolUsineBois.tscn` → `scenes/sol/usine/`), séquelles d'un déplacement de dossiers.

## 5. `MiniJouetExplosif` — dégâts à l'explosion uniquement

`scripts/Entities/Ennemis/MiniJouetExplosif.cs`, deux corrections :

1. **Zone de contact désarmée pendant la descente.** Elle était armée dès le largage, donc frôler un jouet **encore suspendu à son parachute** le faisait exploser à bout portant — imparable sous une pluie de cadeaux. Elle ne s'arme plus qu'au largage du parachute (`Etat.Fonce`), via `ArmerZoneDegats(bool)` en `SetDeferred` (on peut être en plein flush physique) ; `SurContact` vérifie l'état en plus.
2. **Battement entre le contact et le souffle** (`DelaiSouffle`, 0.25 s). Les dégâts étaient déjà les seuls du jouet et vivaient dans `Exploser()`, mais ils tombaient sur **la frame même du contact** : contact et explosion se confondaient. Le souffle est maintenant appliqué par `AppliquerSouffle()` après le battement, et seulement au joueur **encore** dans `RayonSouffle` — toucher le jouet amorce l'éclatement, ça ne blesse pas.

**Quantités larguées** : 1 jouet en phase 1, 3 en phase 2 (`JouetsPhase1/2` du Mecha, `CadeauxPhase1/2` du Père Noël qui largue le même jouet).

## 6. `BossLutinMecha` — le drop de jouets ne sortait presque jamais

`ChoisirPattern()` réutilisait **le même tirage** pour le test de déplacement et pour le choix d'attaque : en phase 1 le tirage retenu ne couvrait plus `[0,1)` mais `[0.35,1)`, ce qui écrasait la répartition et reléguait `DropJouets` — calé tout en haut de l'intervalle — à environ une sortie par minute (mesuré : 2 drops en 60 s de combat).

Tirage désormais **propre** au choix d'attaque, et les trois attaques à parts égales (34/33/33 au lieu de 60/25/15). Le Père Noël avait hérité du même défaut : corrigé aussi.

## 7. Point d'apparition par `Marker2D`, et arène raccourcie

`ZoneBoss` gagne un export `MarqueurApparition` (`NodePath` vers un `Marker2D`), prioritaire sur `PositionApparition` — qui reste en repli, donc aucune arène existante ne casse. On déplace le point de spawn à la souris et il reste visible dans l'éditeur, au lieu de coordonnées recopiées qui se désynchronisent du décor dès qu'on retouche l'arène.

`CalculerApparition()` renvoie la position **dans l'espace du parent de la zone** (c'est là que le boss est ajouté, et il n'est pas encore dans l'arbre — `GlobalPosition` n'aurait pas encore de sens), via `parent.ToLocal(marqueur.GlobalPosition)` : le marqueur peut donc être posé n'importe où, y compris dans une arène translatée comme `ArenBoss` (+8128 en x).

Câblé sur les deux arènes :
- `BossEnd.tscn` → `Arene/ApparitionBoss` à (1450, 408) ;
- `ReindeerBoss.tscn` → `ArenBoss/ApparitionBoss` à (1600, 440), soit exactement l'ancienne valeur en dur : Rodolphe apparaît au même endroit qu'avant.

**Arène de `BossEnd` raccourcie** : sol de 6 → 3 segments centraux, soit 2752 → **1720 px** (−37 %), et la zone suivie (centre x 860, `scale.x` 6.71875) pour que ses bornes collent au nouveau sol — ce sont elles qui bornent le déplacement des deux boss.

## Vérification

- `godot --headless --build-solutions --quit` → 0 erreur C#.
- **Aiguillage testé dans les deux sens** (harnais jetable, supprimé depuis, posant la mémoire puis chargeant `BossEnd`) :
  - sans don → `variante=False nom=Pere Noel classe=BossPereNoel pv=45 bornes=True`
  - avec don → `variante=True nom=Lutin Mecha classe=BossLutinMecha pv=40 bornes=True`
  - `bornes=True` des deux côtés = le contrat `BossBorne` borne bien les deux classes.
- 25 s de jeu simulé sur `TestBossPereNoel` : les trois patterns s'enchaînent, cadeaux et éclats compris, sans une erreur.
- **Jouet explosif testé au banc** (sonde jetable, supprimée depuis, larguant un jouet sur le joueur) :
  - pendant la descente sous parachute → `etat=Chute`, **PV inchangés** : plus aucun dégât de contact ;
  - joueur immobile dans le rayon au moment du souffle → 2 PV perdus (`JouetExplosif`) ;
  - joueur qui s'écarte pendant le battement `DelaiSouffle` → **0 dégât**. C'est la preuve que c'est bien l'explosion qui blesse, et non le contact.
- **Répartition du Mecha remesurée** sur 180 s : 8 `SautEcrasant` / 5 `TirGlace` / 3 `DropJouets` (contre 1 seul drop auparavant sur la même durée).
- **Apparition par marqueur vérifiée dans les deux arènes** (trace temporaire, retirée depuis) :
  - `BossEnd` → `marqueur=ok pos_locale=(1450, 408) parent=Arene` ;
  - `ReindeerBoss` → `marqueur=ok pos_locale=(1600, 440) parent=ArenBoss`, identique à l'ancienne valeur en dur. Testé en déplaçant temporairement le `Joueur` dans l'arène (le fichier a été restauré ensuite), puisqu'au boot il démarre au Sanctuaire et n'atteint jamais l'arène tout seul.
  - `TestBossPereNoel` garde `PositionApparition` : le repli reste exercé.
- `BossEnd`, `TestBossPereNoel`, `TestBossLutinMecha`, `TestMiniJouetExplosif` et `ReindeerBoss` (non-régression du refactor des bornes) bootent 900 frames sans erreur.
- **Non testé en jeu** : `UsinePereNoel/Sol` n'a aucun sol, le joueur ne peut pas atteindre `x = 4700`. La transition `monde2 → BossEnd` ne sera jouable qu'une fois le plancher de l'usine posé.

## Reste à faire (éditeur)

1. Poser le sol de `UsinePereNoel` dans `monde2`, puis recaler le `y` de `ZoneSortieUsine`.
2. Habiller `BossEnd` (décor d'usine, ambiance) et ajuster taille d'arène / `PositionApparition`.
3. Tuning des PV/dégâts du Père Noël (45 PV, cadeaux/éclats hérités) — non validé par un playtest, comme le Cerf et le Mecha (`DECISIONS.md`).

## Points ouverts

- **`ZoneBossCerf` et `ZoneBossPereNoel` sont désormais identiques** (persistance + `CheminSceneVictoire`), et `ZoneBossLutinMecha` en est un sous-ensemble. Depuis que les bornes sont génériques, plus rien n'y est propre à un boss : les trois pourraient fondre dans `ZoneBoss`. Pas fait ici — ça toucherait `ReindeerBoss.tscn` et du code arrivé récemment de `main`.
- `DeclencheurDialogue.ValiderChoix` fait `MarquerConsomme` **sans** `Sauvegarder()`. Le don survit au changement de scène (`GameState` est un autoload) mais pas à un quit avant le prochain checkpoint — les poissons dépensés non plus, donc c'est cohérent, mais à trancher si l'accès au boss caché doit être acquis définitivement.
