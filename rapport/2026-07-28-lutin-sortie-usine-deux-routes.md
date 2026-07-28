# Lutin de la sortie d'usine : un discours par route

## Besoin

Le `LutinNoel` posté devant `ZoneSortieUsine` (`03-monde2.tscn`, dernier PNJ avant l'arène finale) tenait le même discours que le joueur ait suivi la route « lutin CGT » (don des 50 poissons) ou la route Père Noël classique — alors que l'arène `04-BossEnd` s'aiguille déjà sur cette mémoire.

## Ce qui manquait

L'unique aiguillage de route du jeu vivait dans `ZoneBoss` (`VariantePrise`) et échangeait des **scènes de PNJ entières** — inutilisable pour un PNJ posé librement dans un niveau. Côté dialogue, `ChoixDialogue` n'a qu'une condition négative (`MasqueSiMemoire`) et `PnjAmical` n'exposait qu'**un seul** `Contexte`.

## Changements

### `scripts/Entities/Pnj/PnjAmical.cs` (seul fichier C# touché, ~18 lignes)

Nouveau couple d'exports, calqué sur le vocabulaire de `ZoneBoss` :

| Membre | Rôle |
|---|---|
| `[Export] string MemoireRequise` | clé `GameState` qui bascule le discours (vide = comportement d'origine) |
| `[Export] string ContexteAlternatif` | contexte IA joué quand la mémoire est consommée |
| `protected bool VariantePrise` | `EstConsomme(MemoireRequise)` |
| `public string ContexteCourant` | le contexte réellement retenu |
| `string OllamaTalkative.Contexte => ContexteCourant` | implémentation **explicite** de l'interface |

L'export public `Contexte` **garde son nom et son type** : les 5 scènes qui le renseignent (`01-monde1.tscn`) sont intactes. Seul le moteur, qui lit par l'interface (`DeclencheurDialogue.ContexteAvecHistorique`), reçoit la variante. Propriété calculée relue à chaque conversation ⇒ bascule immédiate, sans rechargement de scène.

Bénéfice au-delà du besoin : **tout PNJ amical** peut désormais avoir un discours d'avant/après un jalon de l'histoire, sans une ligne de C# de plus.

### `scenes/niveaux/03-monde2.tscn` — nœud `LutinNoel`

Trois valeurs d'instance ajoutées :
- `MemoireRequise = "lutin_cgt_don_poissons"` (= `LutinCgt.IdDonPoissons`, déjà la clé de `04-BossEnd.tscn`)
- `Contexte` — route classique : lutin débordé à deux jours de Noël, qui presse Glooby d'aller voir le Père Noël.
- `ContexteAlternatif` — route CGT : lutin paniqué et rancunier, la chaîne est à l'arrêt, le Lutin Mecha tient les machines, et il a reconnu celui qui a rempli la caisse de grève.

Textes retouchables dans l'inspecteur : c'est de la donnée, pas du code.

### `scripts/Core/OptionDebug.cs` — cohérence du mode debug

La case « Route lutin CGT » posait la mémoire du don **sans prélever les 50 poissons**, alors qu'en partie réelle le choix les coûte (`ChoixDon.CoutPoissons = 50`). La partie de test démarrait donc en « j'ai tout donné » avec la réserve pleine — de quoi se soigner indûment, et de quoi tromper les choix conditionnés à la réserve (`ChoixDialogue.SiReserveInsuffisante`).

L'option vide désormais la réserve entière (`DepenserPoissons(etat.Poissons)`) plutôt que de recopier un 50 qui vit dans le `.tres` : le don, c'est « tous ses poissons ».

### Reformulation des deux contextes

Première version : le contexte CGT parlait de Glooby à la **troisième personne** (« ce petit pingouin… c'est *lui* qui a rempli la caisse »), en contradiction avec le prompt système (« Glooby n'est PAS toi »). Un tirage sur trois partait en méta (« je suis un personnage du jeu, je ne peux pas discuter avec Glooby car il n'est pas là »). Les deux textes partageaient en plus leurs 70 premiers caractères, alors que le modèle ne produit qu'une phrase de ~10 mots.

Réécrits en adressage direct et distincts dès les premiers mots ; 5 tirages par route contre `mistral-nemo:12b` : plus aucune dérive méta et les deux routes s'entendent immédiatement (« Je suis très occupé, passe vite cette porte ! » / « Je suis coincé ici à cause de toi, Glooby ! »).

## Vérification

- `godot --headless --build-solutions --quit` : compile propre ; l'assemblage contient bien `get_ContexteAlternatif`, `get_ContexteCourant` et l'implémentation explicite `OllamaTalkative.Contexte`.
- `godot --headless --quit-after 200 scenes/niveaux/03-monde2.tscn` : scène chargée sans erreur (aucune propriété inconnue), seul le bruit habituel de fuite d'objets à la sortie abrupte.
- Sondes headless jetables (supprimées depuis) sur le chemin exact du mode debug, `NouvellePartieDebug` :

| Case « Route lutin CGT » | mémoire du don | poissons | contexte lu par le moteur |
|---|---|---|---|
| décochée | `False` | 50 | Contexte normal |
| cochée | `True` | **0** | ContexteAlternatif |

## Reste à faire (test humain)

Comparer les deux discours en jeu, l'option de debug **« Route lutin CGT »** (`CatalogueOptionsDebug.RouteLutinCgt`) évitant de rejouer `01-monde1` pour poser la mémoire.

## Point ouvert (hérité)

`Lignes` étant vide sur ce lutin, il reste muet si Ollama est coupé — comportement inchangé. Un repli écrit demanderait un `LignesAlternatives` sur le même patron (3 lignes).
