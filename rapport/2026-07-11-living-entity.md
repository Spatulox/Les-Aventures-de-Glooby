# FriendlyLivingEntity + rangement des entités de dégâts

## Objectif
Permettre à certains PNJ de ne subir **aucun** dégât (boule de neige, pouvoir de
chaleur…), et clarifier le rangement des entités liées aux dégâts.

## Changements

### Nouvelle interface `FriendlyLivingEntity`
- `scripts/Common/FriendlyLivingEntity.cs` — interface marqueur (vide). Un PNJ qui
  l'implémente est insensible à toute source de dégâts, même s'il est par ailleurs
  `Damageable`.

### Point d'entrée unique des dégâts : `Degats.Infliger`
- Ajout de la classe statique `Degats` dans `scripts/Common/Damageable.cs`.
- `Degats.Infliger(Node cible, DamageSource source)` centralise les règles communes :
  court-circuite les cibles `FriendlyLivingEntity`, respecte `IsInvincibleToDamage`,
  puis appelle `TakeDamage`. Toute source de dégâts passe désormais par là.
- `Snowball.OnBodyEntered` utilise `Degats.Infliger` (au lieu d'appeler `TakeDamage`
  directement) — la boule éclate toujours au contact, mais ne blesse plus une entité
  amicale.

### Rangement des scripts
- `Snowball.cs` (+ `.uid`) déplacé `Entities/Player/` → **`Entities/Damage/`** (nouveau
  dossier des entités qui infligent des dégâts).
- `PouvoirChaleurPickup.cs` (+ `.uid`) déplacé `Entities/Player/` → **`Entities/Interactable/`**
  (le ramassable est séparé des entités de dégâts).
- Chemins `script` mis à jour dans `boule_de_neige.tscn`, `monde.tscn`,
  `pouvoir_chaleur_pickup.tscn`.
- `CLAUDE.md` mis à jour (sections `Common/` et `Entities/`).

## Vérification
- `dotnet build` : génération réussie, 0 avertissement.
- Aucune référence résiduelle aux anciens chemins.

## Base commune `LivingEntity`
Mutualisation d'un socle entre le joueur et les boss (et tout futur PNJ).

- Nouvelle classe abstraite `scripts/Entities/LivingEntity.cs` (`: CharacterBody2D, Damageable`) :
  - **PV** : `Pv`/`PvMax`/`EstVaincu`, signaux `PvChanges`/`Vaincu`, `DefinirPvMax`.
  - **Dégâts** (`Damageable`) : unique entrée `TakeDamage(DamageSource)` + hooks
    `AjusterDegats`/`ApresDegats`/`Mourir`.
  - **Déplacement** : aides réutilisables `AppliquerGravite`, `AppliquerFriction`, `Sauter`
    (réglages `Gravity`/`MaxFallSpeed`/`Friction`/`JumpVelocity`).
- `Boss` devient `: LivingEntity` — ne garde que la partie *animée* (chargement d'anims,
  mort animée qui surcharge `Mourir`). Tout le générique PV/dégâts est remonté dans la base.
- `Player` devient `: LivingEntity` — utilise les aides de déplacement. Ses PV restent
  dans `GameState` (persistants, HUD, respawn) : il **surcharge** `TakeDamage(DamageSource)`
  (route vers `GameState`) et `IsInvincibleToDamage` (invincibilité post-coup). Le point
  d'entrée directionnel `Blesser(int direction, DamageSource source)` porte le recul ;
  appelants mis à jour dans `BossCerf` (×2) et `StalactitePiege` (×1).
- `CLAUDE.md` mis à jour (section « LivingEntity, Player & Boss » + layout `Entities/`).
- `ZoneBoss`/`BossHudBarre`/`ZoneBossCerf` inchangés : ils accèdent aux membres PV via le
  type `Boss`, désormais hérités de `LivingEntity`.

Vérification : `dotnet build` réussi (0 avertissement) + boot headless propre.

## Toute forme de dégât = `DamageSource`
Les dégâts subis par le joueur (stalactites, attaques du boss) étaient appliqués avec
une quantité brute codée en dur ; ils passent désormais tous par un `DamageSource`.

- `scripts/Common/DamageSource.cs` : nouvelles sources subies par le joueur —
  `Stalactite` (1), `ChargeBoss` (1), `SouffleGivre` (2) (montants inchangés).
- `LivingEntity.TakeDamage(DamageSource)` rendu `virtual`.
- `Player` : `Blesser(int direction, DamageSource source)` (le montant vient de la
  source, plus de quantité brute) ; `TakeDamage` surchargé → `Blesser(0, source)` ;
  `SubirDegats(int)` réduit à un filet de sécurité. Duplication évitée via une
  fabrique privée `Encaisser(int quantite, int direction)`.
- Appelants : `StalactitePiege` → `DamageSource.Stalactite` ; `BossCerf` charge →
  `DamageSource.ChargeBoss`, souffle de givre → `DamageSource.SouffleGivre`.
- Les stalactites blessent bien le joueur (détection OK : joueur sur `collision_layer 1`,
  masque de la stalactite par défaut à 1).

Vérification : `dotnet build` réussi (0 avertissement) + boot headless propre.

## Non fait / à noter
- `project.godot` référence un autoload **non commité `TestManger` → `res://scripts/TestManger.cs`
  (inexistant)** : erreur au boot, hors périmètre (modif locale). À créer ou retirer.
- Les PV du joueur restent volontairement dans `GameState` (non déplacés dans `LivingEntity`) :
  respawn, checkpoints, poissons et HUD en dépendent.
- Le pouvoir de chaleur ne fait toujours que fondre les murs (`Player.UtiliserPouvoirChaleur`),
  il n'inflige pas encore de dégâts aux entités — quand ce sera le cas, il devra passer
  par `Degats.Infliger(..., DamageSource.Fire)` pour respecter `FriendlyLivingEntity`.
- Aucun PNJ n'implémente encore `FriendlyLivingEntity` (interface prête à l'emploi).
