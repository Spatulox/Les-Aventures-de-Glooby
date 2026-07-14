# Rapport — PNJ méchants (lanceur & fonceur)

**Date :** 2026-07-14
**Objectif :** ajouter une famille de PNJ hostiles `PnjMechant : LivingEntity` et deux méchants
qui en dérivent, sur le même modèle placeholder que les lutins (carré de couleur, animations
prêtes mais dossiers de frames encore vides).

## Changements

### Base réutilisable
- **`scripts/Entities/Pnj/PnjMechant.cs`** (`: LivingEntity`) — pendant hostile de `PnjAmical` :
  déambule en va-et-vient, carré placeholder + bascule auto vers l'`AnimatedSprite2D` quand des
  frames existeront. Blesse le joueur au contact via une `Area2D` enfant `ZoneContact` (recul
  directionnel). Hook `DeciderMouvement(...)` surchargeable + helpers réutilisables `Patrouiller`,
  `JoueurLePlusProche`, `DefinirOrientation`.

### Deux méchants dérivés
- **`LanceurBouleNeige.cs`** — carré **rouge**. À portée, s'arrête, fait face au joueur et lui
  envoie des boules de neige ; **réutilise** `boule_de_neige.tscn` / le `Projectile` existant.
- **`Fonceur.cs`** — carré **orange**. À portée, **fonce** sur le joueur ; dégâts via la
  `ZoneContact` héritée.

### Dégâts
- **`scripts/Common/DamageSource.cs`** — nouvelle source `ContactMechant` (1 dégât), utilisée pour
  le contact/charge des méchants.

### Assets & scènes
- Placeholders 24×24 : `assets/pnj/lanceur_boule_neige/placeholder.png` (rouge),
  `assets/pnj/fonceur/placeholder.png` (orange) — importés.
- Scènes droppables : `scenes/entites/lanceur_boule_neige.tscn`, `scenes/entites/fonceur.tscn`.

## Réglages (`[Export]`)
`PorteeDetection`, patrouille (`DistancePatrouille`, `VitessePatrouille`, `TempsPause`),
`IntervalleTir` (lanceur), `VitesseCharge` (fonceur).

## Vérification
- `dotnet build` : **0 erreur**. Assets importés (`godot --headless --import`).

## Non fait (volontaire)
- Pas d'instanciation dans `monde.tscn` (non demandé, map éditée à la main) : à glisser depuis
  l'éditeur sous un groupe `Interactifs`.
- La charge du fonceur inflige 1 dégât **à l'entrée** de la `ZoneContact` (le joueur le traverse) :
  suffisant pour ce premier jet placeholder ; rebond/stun/sprites à affiner plus tard.
