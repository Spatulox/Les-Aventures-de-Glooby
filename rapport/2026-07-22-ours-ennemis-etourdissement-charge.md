# Ours de neige : dossier ennemis, étourdissement partagé, charge « nerfée »

## Objectif
Aligner l'ours de neige sur la convention des autres ennemis, le rendre étourdissable comme le
bonhomme de neige (via une interface partagée), et « nerfer » sa charge (esquivable, brève).

## Changements

### Interface partagée
- **Créé `scripts/Common/Etourdissable.cs`** : interface `Etourdissable { void Etourdir(float duree); }`
  — contrat commun des ennemis que le joueur peut figer avec une boule de neige.

### Bonhomme de neige (branché sur l'interface)
- `scripts/Entities/Ennemis/BonhommeDeNeige.cs` : implémente désormais `Etourdissable` ; la méthode
  privée `EntrerEtourdi()` devient `public void Etourdir(float duree)` (durée paramétrée).
  Comportement inchangé — simple extraction du point d'entrée.

### Ours de neige déplacé dans les dossiers « ennemis »
- `scripts/Entities/Pnj/OursDeNeige.cs` → **`scripts/Entities/Ennemis/OursDeNeige.cs`** (`git mv`).
- `scenes/entites/ours_de_neige.tscn` → **`scenes/ennemis/OursDeNeige.tscn`** (PascalCase, comme
  `BonhommeDeNeige.tscn`) ; chemin du script mis à jour dans le `.tscn`.
- `scenes/niveaux/monde.tscn` : Edit chirurgical d'une seule chaîne de chemin (uid conservé → les 2
  instances `OursDeNeigeLaby` / `OursDeNeigeLaby2` restent liées).

### Ours de neige : étourdissement + charge nerfée
- Reste `: PnjMechant` (réutilise patrouille, gravité, contact-dégâts, anim), ajoute `Etourdissable`.
- **Machine à états** `Patrouille → Charge → Recuperation` :
  - `VitesseCharge = 260` (> `Player.Speed = 220`) : dépasse le joueur, mais **seulement**
    pendant `DureeCharge = 0.6 s`, **direction verrouillée au départ** (ligne droite, esquivable —
    plus de suivi frame par frame).
  - `DureeRecuperation = 1.2 s` : l'ours s'arrête et ne peut pas relancer (fenêtre d'esquive/tir).
- **Étourdi, jamais tué** : `TakeDamage`/`IsInvincibleToDamage` surchargés — seule la boule de neige
  passe et appelle `Etourdir(DureeEtourdissement = 1.5 s)` (flash bleu, aucune perte de PV).

## Vérification
- `godot --headless --build-solutions --quit` : compilation propre (0 erreur).
- `godot --headless --quit-after 120 scenes/niveaux/monde.tscn` : scène chargée, aucun échec de
  résolution des instances de l'ours (seul l'avertissement générique « resources still in use at
  exit » subsiste, sans rapport).
- `grep` : aucune référence résiduelle aux anciens chemins, plus de `EntrerEtourdi`.
- Play-test manuel (F5) dans la grotte encore recommandé pour le ressenti (charge esquivable, stun).
