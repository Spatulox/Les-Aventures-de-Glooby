# Renommage des nodes de `monde.tscn`

Suite à la fusion de `Banquise2` dans `Banquise` et à l'extraction de l'arène du boss,
les noms de nodes étaient devenus incohérents (numéros orphelins répartis entre deux lieux).

## Node de lieu

| Avant | Après |
|---|---|
| `ZoneBossCerf` (Node2D, lieu) | `AreneBoss` |

Le `Node2D` "lieu" portait le même nom que l'`Area2D` déclencheur qu'il contient.
L'`Area2D` garde son nom `ZoneBossCerf` (il correspond au script `ZoneBossCerf.cs`) ;
son chemin devient `AreneBoss/Interactifs/ZoneBossCerf`.

## `Banquise/Sol`

| Avant | Après |
|---|---|
| `SolBanquiseEmboutDroit` | `EmboutRampeDroit` |
| `SolBanquiseEmboutGauche` | `EmboutPlateauGauche` |
| `LedgeGrotte1` / `LedgeGrotte2` | `Ledge1` / `Ledge2` |

Les embouts portaient le nom de leur `.tscn` ; les rebords étaient nommés « Grotte »
alors qu'ils sont sur la banquise (x 3500 et 4550).

## `Banquise/Decor` — numérotation recontiguïsée

| Avant | Après |
|---|---|
| `Champi4` | `Champi3` (le `Champi3` était parti dans l'arène) |
| `Flaque1` | `Flaque` |
| `ColonneBrisee1` | `ColonneBrisee` |
| `VeineMur1` | `VeineMur` |

Les suffixes numériques ne sont conservés que là où il y a réellement plusieurs
exemplaires (`Rocher`/`Rocher2`/`Rocher3`, `ColonneGlace1..3`, etc.), et ils suivent
l'ordre ouest → est.

## `AreneBoss/*` — plus de numéros orphelins

Chaque prop étant unique dans son parent, le numéro disparaît.

| Avant | Après |
|---|---|
| `Sol1` / `Sol2` | `SolArene1` / `SolArene2` |
| `LedgeGrotte3` | `LedgeArene` |
| `ColonneGlace4` | `ColonneGlace` |
| `Champi3` | `Champi` |
| `Grappe3` | `Grappe` |
| `Lac3` | `Lac` |
| `Flaque2` | `Flaque` |
| `VeineMur2` | `VeineMur` |
| `ColonneBrisee2` | `ColonneBrisee` |
| `Fissure3` | `Fissure` |

## Divers

| Avant | Après |
|---|---|
| `Village/Interactifs/panneau_poteau` | `Village/Interactifs/PanneauVillage` |

Seul node en `snake_case` de la scène.

## Vérifications

- Aucune référence externe à ces noms (scripts C# et autres `.tscn`) — le seul `NodePath`
  concerné, `CheminBarre = NodePath("../BossHudBarre")`, est relatif et inchangé.
- Aucun doublon de nom au sein d'un même parent après renommage.
- `godot --headless --quit-after 200 scenes/niveaux/monde.tscn` : chargement propre,
  aucune erreur.

## À noter

`CLAUDE.md` décrit les lieux comme `Village` / `Grotte` (+ « futures arènes de boss ») ;
l'arène existe désormais sous le node `AreneBoss` — à intégrer dans la doc si besoin.
