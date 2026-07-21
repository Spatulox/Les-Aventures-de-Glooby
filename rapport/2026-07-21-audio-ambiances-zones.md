# Ambiances sonores & musiques par zone (2026-07-21)

Mise en place du système audio complet, puis branchement de la **première piste
réelle** sur le menu principal et le village.

## Ce qui est en place

- **Clé sonore distincte de la région visuelle.** Le village et la banquise
  partagent le fond `banquise` mais plus la musique : `NomAmbiance` (avec repli
  sur `NomRegion`) porte la clé audio.
- **Playlists tirées au sort**, avec enchaînement automatique en fin de morceau
  et non-répétition de la piste précédente.
- **Variantes par état météo** (`normal` / `blizzard`), avec repli sur `normal` :
  un blizzard peut se déclencher n'importe où sans obliger chaque lieu à définir
  sa variante tempête.
- **Deux canaux** (Musique / Ambiance) sur deux bus, en fondu croisé de 1.5s.

## Fichiers

| Fichier | Changement |
|---|---|
| `default_bus_layout.tres` | **nouveau** — bus `Musique` et `Ambiance` (send → Master) |
| `project.godot` | `[audio]` bus layout + autoload `GestionnaireAudio` |
| `scripts/Core/VarianteAmbiance.cs` | **nouveau** — `[GlobalClass] : Resource`, playlists d'un état |
| `scripts/Core/AmbianceSonore.cs` | **nouveau** — un lieu + ses variantes, `Trouver(etat)` avec repli |
| `scripts/Core/GestionnaireAudio.cs` | **nouveau** — autoload, 2 canaux, fondus, tirage aléatoire |
| `scripts/Core/GestionnaireMeteo.cs` | +1 ligne : `DefinirEtat` à la bascule du blizzard visible |
| `scripts/Core/CameraZone.cs` | + export `NomAmbiance` |
| `scripts/Core/ZoneBoss.cs` | + export `NomAmbiance` ; `JouerMusique` délègue, `_lecteurMusique` supprimé |
| `scripts/Common/DeclencheurZone.cs` | `AppliquerCommeSalle` + paramètre ambiance (point de branchement unique) |
| `scripts/Core/BackgroundManager.cs` | commentaire du hook musique périmé remplacé |
| `scenes/niveaux/monde.tscn` | 3 lignes `NomAmbiance` (édits chirurgicaux) |
| `assets/audio/ambiances/*.tres` | **nouveaux** — `menu`, `village` (avec la piste), `banquise`, `grotte`, `boss_cerf` (vides) |
| `assets/audio/musiques/ice_cave_lofi.ogg` | **nouveau** — « Ice Cave (Royalty Free) 8-Bit Lofi Hip Hop », 293 s, importé en `loop = false` |
| `scripts/UI/MenuPrincipal.cs` | demande l'ambiance `menu` dans `_Ready` |

Ce sont les **premières `Resource` C# et les premiers `.tres`** du projet.

## Musique du menu et du village

Le menu n'a pas de `CameraZone` pour demander son ambiance : il s'adresse
directement au gestionnaire (un autoload, donc déjà là). Menu et village
partagent **la même piste**, ce qui donne un effet utile gratuitement : en
lançant une partie, la musique **enchaîne sans coupure** — le gestionnaire voit
que la piste demandée est déjà en cours et ne la relance pas.

Les trois autres ambiances (`banquise`, `grotte`, `boss_cerf`) restent vides :
entrer dans ces zones **coupe donc la musique** (voir la règle ci-dessous),
jusqu'à ce qu'on leur donne leurs propres pistes.

## Règles de lecture

- **Bouclage** : assuré par l'enchaînement de fin de morceau, pas par l'import.
  Une zone à une seule piste la rejoue indéfiniment ; une zone à plusieurs pistes
  passe à une autre. Les `.ogg` de musique sont donc importés en `loop = false` —
  un `.ogg` bouclé à l'import n'émettrait jamais `Finished` et resterait coincé
  sur la même piste.
- **Changement de zone** : la musique change si la nouvelle zone en définit une
  autre, et **s'arrête en fondu** si la zone n'en définit aucune.
- **Changement de météo** : au contraire, une variante `blizzard` qui ne
  renseigne que `Ambiances` **laisse la musique continuer**. Sans cette
  distinction, le moindre coup de vent couperait le morceau en cours. C'est le
  paramètre `couperSiVide` de `BasculerCanal` qui sépare les deux cas — même
  code, deux intentions.
- **Pause** : la musique continue (`ProcessMode = Always` sur l'autoload, hérité
  par les lecteurs et les tweens de fondu ; sans ça `GetTree().Paused` les
  suspendrait).
- **Piste déjà en cours** : jamais relancée. C'est ce qui rend le passage
  menu → village continu, les deux partageant la même piste.

## Choix de conception

- **Un seul point de branchement** : `AppliquerCommeSalle` est le seul appelant
  commun à `CameraZone` et `ZoneBoss`, et il n'est atteint qu'une fois par entrée
  de salle (hystérésis de `Player.MettreAJourZoneCamera`). Aucune détection à
  réécrire.
- **Pas de signal pour la météo.** Le plan initial prévoyait un
  `[Signal] BlizzardChange` connecté depuis `GestionnaireAudio._Ready` — c'était
  **impossible** : `GestionnaireMeteo` est un nœud de `monde.tscn`, pas un
  autoload, donc il n'existe pas encore quand l'autoload démarre. Remplacé par un
  appel direct `GestionnaireAudio.Instance?.DefinirEtat(...)`, l'idiome déjà
  employé partout (`BackgroundManager.Instance?.AfficherRegion(...)`).
- **Le filtrage par salle n'est pas dupliqué.** Le son bascule dans
  `AfficherBlizzard`, après le filtre `_zoneActive` : les minuteries tournent dans
  *toutes* les salles, et sans ce filtre l'expiration d'un blizzard à l'autre bout
  de la carte couperait la musique du joueur.
- **Ambiances découvertes par dossier**, pas par export : un autoload en script nu
  n'a pas d'inspecteur, donc déposer un `.tres` dans `assets/audio/ambiances/`
  suffit à enregistrer un lieu (même esprit que le parcours des enfants de
  `BackgroundManager`).
- **L'arène de boss avait deux zones caméra concurrentes** — `ZoneArenaBoss`
  (`NomRegion = "boss_cerf"`) et l'`Area2D` `ZoneBossCerf` (`NomRegion = "grotte"`),
  toutes deux dans le groupe `zones_camera` : celle qui gagne dépendait de l'ordre
  de parcours. Sans correctif, la musique de l'arène aurait été tirée au hasard
  entre les deux. Les **deux** nœuds fixent donc `NomAmbiance = "boss_cerf"`.
- **`ZoneBoss.JouerMusique` corrigé** : il empilait un `AudioStreamPlayer` par
  entrée, sans jamais l'arrêter ni le libérer. Il délègue maintenant au
  gestionnaire (lecteur unique par canal, avec fondu).
- **Un canal = une classe interne `Canal`**, pour que musique et ambiance passent
  par le même `BasculerCanal` sans dupliquer la mécanique de fondu.
- **Bouclage réglé à l'import**, pas dans le code : ambiances en `loop = true`,
  musiques en `loop = false` (c'est l'enchaînement aléatoire qui prend le relais).

## Vérification

- `godot --headless --build-solutions --quit` → compilation propre.
- `godot --headless --quit-after 400 scenes/niveaux/monde.tscn` → aucune erreur ni
  warning (playlists vides ⇒ silence sans bruit dans les logs).
- Sondes temporaires (retirées depuis), traversée scriptée par téléportation :
  - les 4 `.tres` se chargent (`banquise, boss_cerf, grotte, village`) ;
  - **village → banquise change bien d'ambiance** malgré le même `NomRegion` —
    c'était le cœur du besoin ;
  - grotte puis arène résolvent `grotte` puis `boss_cerf`, l'arène de façon
    **déterministe** quelle que soit la zone qui gagne ;
  - chaque `JouerAmbiance` n'est appelée **qu'une fois** par entrée (l'early-return
    absorbe le sondage à chaque frame).
- Blizzard (`ChanceBlizzard` temporairement à `1f`, durées à 2-3 s — **valeurs
  restaurées à `0.2f` / 10-30 s**) :
  - la bascule audio suit exactement le rendu (village en blizzard, retour à
    `normal` à l'entrée de la grotte `Souterrain`) ;
  - **cas critique validé** : les minuteries des salles quittées expirent à
    distance **sans jamais** déclencher de changement audio.

- Avec la piste réelle : les `.tres` `menu` et `village` chargent leur playlist
  (1 piste, 293 s), un lecteur unique démarre sur le bus `Musique` (`Playing =
  true`), et le passage menu → monde journalise bien « piste inchangée, lecture
  poursuivie » — la musique n'est pas relancée.
- Sonde scriptée sur les trois règles (retirée depuis) :
  - **bouclage** — `Finished` émis à la main : la piste repart de 0 sur le même
    lecteur ;
  - **pause** — `Paused = true`, la position avance de 1.02 s à 2.98 s : la
    lecture continue ;
  - **zone muette** — téléport en banquise (playlist vide) : le volume descend
    (−39.9 dB pendant le fondu) puis le lecteur disparaît → silence.

### Un faux positif écarté

Les runs headless affichent par intermittence `ObjectDB instances leaked at exit`
/ `2 resources still in use at exit` dès qu'une musique joue. Ce **n'est pas** un
défaut du gestionnaire : ont été successivement disculpés le tween de fondu, la
lambda `Finished`, le cache d'ambiances et un `_ExitTree` qui coupait les
lecteurs (aucun n'y change rien). Une scène GDScript de 6 lignes — un
`AudioStreamPlayer` nu, sans autoload ni tween — reproduit la fuite à
l'identique sous `--quit-after`, alors qu'un `quit()` gracieux est propre. C'est
donc un artefact de démontage du moteur quand `--quit-after` coupe une lecture
en cours. Le `_ExitTree` ajouté pour l'occasion a été **retiré** : il ne
corrigeait rien et son commentaire affirmait une cause fausse.

## Reste à faire

- **Test manuel (`godot`) non fait** : le headless ne juge pas le son. À vérifier
  à l'oreille — le fondu de 1.5 s, le volume relatif de la piste, le respawn sans
  coupure, et le nombre d'`AudioStreamPlayer` au moniteur (≤ 2 par canal pendant
  un fondu, 1 après).
- Donner leurs propres pistes à `banquise`, `grotte` et `boss_cerf`, et une
  variante `blizzard` (au minimum un vent dans `Ambiances`, en laissant
  `Musiques` vide pour ne pas couper la musique du lieu).
- Reporté volontairement : `EmetteurAmbiance` (sons localisés type gouttes de
  grotte) — sans asset ni instanciation, ce serait du code mort ; il viendra avec
  les premiers `.ogg`.
- Extensions naturelles : SFX gameplay, sliders de volume dans les menus (le bus
  layout en est le prérequis), sourdine à la pause, passe-bas sur le bus `Musique`
  pendant le blizzard (se règle dans le bus layout, sans code).
