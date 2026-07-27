# L'écran de fin devient un générique défilant, éditable sans code

**Besoin** : `ecran_fin.tscn` était un placeholder (« Acte 2 terminé », trois `Label` en
offsets absolus, une touche → `monde1`), et son texte était **périmé** — la vraie fin,
c'est le pantalon ramassé après `BossEnd`, plus Rodolphe. À la place : un vrai générique
qui monte tout seul, avec les rôles, et **modifiable dans l'inspecteur Godot**.

Cadrage retenu : crédits seulement (le texte « Acte 2 terminé » disparaît), contenu dans
une ressource `.tres` dédiée, et fin du générique (ou appui touche) → `monde1.tscn`
(comportement conservé).

## 1. Le contenu vit dans une ressource, pas dans la scène

C'est tout l'enjeu de la demande : **ajouter quelqu'un aux crédits ne doit toucher ni le
C# ni la scène**. Même patron que `AmbianceSonore` / `NoeudDialogue` — ressource racine +
tableau de sous-ressources, toutes deux `[GlobalClass]` (c'est ça qui les rend éditables
dans l'inspecteur et disponibles dans « New Resource »).

| Fichier | Rôle |
|---|---|
| `scripts/UI/EntreeCredits.cs` | un bloc : `Categorie` + `Noms` (`string[]`) |
| `scripts/UI/CreditsGenerique.cs` | le générique : `Titre`, `Entrees`, `Remerciements`, `VitesseDefilement`, `TailleTitre/Categorie/Nom`, `EspaceEntreBlocs` |
| `assets/credits/generique.tres` | **le seul fichier à éditer** pour changer les crédits |

Les deux ressources sont rangées dans `scripts/UI/` et non `scripts/Core/` (où vivent
toutes les autres) : elles ne servent qu'à cet écran et n'ont aucune existence gameplay.
Signalé dans `CLAUDE.md`, avec le nouveau dossier `assets/credits/`.

Une entrée sans nom reste valable — elle sert d'intertitre. Contenu initial :

| Catégorie | Noms |
|---|---|
| Développement | Spatulox, alexandreDjazz |
| Game design & level design | Spatulox, Alexandre |
| Art pixel & animations | PixelLab |
| Musiques & ambiances | **« À compléter »** — je ne connais pas la source des `.mp3` |
| Assistance IA & code | Claude Code (Anthropic) |
| Dialogues dynamiques | Ollama (modèle local) |
| Moteur | Godot Engine 4.6 |
| Projet | ESGI — 4ᵉ année, IA générative |

## 2. La scène — `scenes/ui/ecran_fin.tscn`

Racine passée de `Node2D` à **`Control` plein écran** (`anchors_preset = 15`), comme
`menu_principal.tscn` : les anciens offsets absolus ne pouvaient pas porter un défilement.

```
EcranFin (Control, script EcranFin.cs, Credits = generique.tres)
├── Fond    ColorRect plein écran, Color(0.06, 0.08, 0.14) — couleur charte du jeu
└── Zone    Control plein écran, clip_contents = true
    └── Colonne  VBoxContainer, x 40 → 600, separation 4
```

`Colonne` est **vide dans l'éditeur** et remplie au runtime, comme les lignes de touches
d'`EcranParametres` : c'est normal, pas un oubli.

## 3. Le script — `scripts/UI/EcranFin.cs`

Il ne connaît **aucun nom** : il déroule ce que contient `Credits`.

- `_Ready` construit les `Label` (titre → par entrée : intertitre + noms, séparés par un
  `Control` espaceur → remerciements en `Autowrap`), en réutilisant
  **`MenuFabrique.AjouterLigne`** (le `Label` centré existait déjà) et en ne surchargeant
  que `font_size`. Puis pose la colonne juste sous le bas du canvas.
- `_Process` la fait monter de `VitesseDefilement × delta` ; quand la dernière ligne passe
  au-dessus du bord haut (`Position.Y + hauteur < 0`), on enchaîne sur `CheminSuite`.
- `_UnhandledInput` : n'importe quelle touche **ou** `action`/`menu` → on passe.
- `Terminer()` porte un verrou : le défilement et une touche pourraient sinon déclencher
  deux fois le changement de scène.
- `Credits == null` → `PushWarning` et écran vide, mais **toujours passable** : on ne
  bloque jamais le joueur sur un fichier oublié.

Trois exports pour l'auteur : `Credits`, `CheminSuite` (`PropertyHint.File`) et
`NomAmbiance`, laissé **vide** — la musique du combat final continue, faute d'ambiance
« fin ». Le jour où `assets/audio/ambiances/fin.tres` existera, il suffira de taper `fin`
dans l'inspecteur.

Rien à changer côté `PantalonPickup` : il fait déjà `Effets.FondreAuNoirPuis`, l'écran
arrive donc en fondu (ne pas redoubler).

### Deux correctifs au passage

- **Le HUD restait affiché par-dessus l'écran de fin** — cœurs et compteur de poissons.
  `Hud` est un autoload ; ajout de `GetNodeOrNull<Hud>("/root/Hud")?.Masquer()`, le même
  geste que `MenuPrincipal._Ready`.
- **L'écran n'était pas passable à la manette** : l'ancien code filtrait sur
  `InputEventKey` seul, alors que tout le reste du jeu passe par l'InputMap.

## Vérification

- `godot --headless --build-solutions --quit` : **0 erreur, 0 avertissement**.
- `godot --headless res://scenes/ui/ecran_fin.tscn` : aucune erreur, **et aucun warning
  « Credits manquant »** — c'est ce qui prouve que le `.tres` se charge (`.cs.uid` bien
  générés, `Array[EntreeCredits]` bien parsé).
- **Bascule de fin testée pour de vrai** : `VitesseDefilement` poussé temporairement à
  5000 dans le `.tres` → le run headless charge `monde1.tscn` (sa signature d'erreurs
  pré-existantes apparaît). Double preuve : le défilement va bien jusqu'au bout, et
  l'éditer dans le `.tres` change bien le comportement. Valeur remise à **25**
  (≈ 40 s de générique).

## Reste à faire

- **Un vrai F5** : le headless ne juge ni la lisibilité à 640×360 ×2, ni le confort de la
  vitesse de lecture.
- **Renseigner la ligne « Musiques & ambiances »** dans `assets/credits/generique.tres`.
- Optionnel : créer une ambiance `fin.tres` et la nommer dans `NomAmbiance` pour que le
  générique ait sa propre musique.
- ⚠️ Si l'éditeur Godot était ouvert sur `ecran_fin.tscn`, **recharger la scène** avant
  d'y retoucher (piège connu : il réécrit le fichier en sortant).
