# Rapport — Système `DamageSource` / `Damageable` + rangement des entités

**Date :** 2026-07-11 · **Branche :** `main`

## Objectif

1. Introduire un système de dégâts générique : une source de dégâts (`enum
   DamageSource`) portant son montant, et une interface `Damageable` que toute
   entité pouvant subir des dégâts implémente — pour que la boule de neige
   frappe n'importe quelle cible sans connaître son type concret.
2. Ranger les entités de `scripts/Entities/` (jusque-là à plat) et les assets de
   PNJ dans des sous-dossiers par rôle.

## 1. Système de dégâts

**Nouveaux fichiers (`scripts/Common/`) :**

- **`DamageSource.cs`** — `enum DamageSource { Snowball, Fire }` + extension
  `MontantDegats()` associant chaque source à ses dégâts (`Snowball => 2`,
  `Fire => 1`). L'enum C# ne peut pas porter de valeur associée comme en
  Java/Kotlin, d'où la table centralisée dans l'extension.
- **`Damageable.cs`** — interface `Damageable` :
  `void TakeDamage(DamageSource)` + `bool IsInvincibleToDamage(DamageSource)`.

**Implémentations :**

- **`Boss`** (`: Damageable`) — `TakeDamage(s) => SubirDegats(s.MontantDegats())`,
  `IsInvincibleToDamage => EstVaincu`.
- **`Player`** (`: Damageable`) — `TakeDamage(s) => SubirDegats(0, s.MontantDegats())`,
  `IsInvincibleToDamage => EstInvincible`.

**`Snowball`** utilise désormais l'interface générique :

```csharp
if (body is Damageable cible)
    cible.TakeDamage(DamageSource.Snowball);
Eclater();
```

> À noter : le boss encaisse maintenant **2** dégâts par boule de neige
> (contre `1` codé en dur auparavant).

## 2. Rangement des entités et assets

Déplacements via `git mv` (paires `.cs` + `.cs.uid`, historique conservé) :

| Dossier | Scripts |
|---|---|
| **Entities/Pnj/** | `Boss`, `BossCerf` |
| **Entities/Player/** | `Player`, `Snowball`, `PouvoirChaleurPickup` |
| **Entities/Interactable/** | `MurFondable`, `StalactitePiege` |
| **Entities/Misc/** | `ElementRamassable`, `Checkpoint` |

- **Assets :** `assets/boss_cerf/` → `assets/pnj/boss_cerf/` (avec les `.import`) ;
  chemins mis à jour dans `BossCerf.cs` (5 animations) ; réimport propre.
- **Scènes :** réécriture des `path=` des ressources Script dans tous les `.tscn`
  concernés (les scènes référencent les scripts par chemin) — `boss_cerf`,
  `player`, `boule_de_neige`, `pouvoir_chaleur_pickup`, `mur_fondable`,
  `stalactite_piege`, `checkpoint_peche`, et `monde.tscn`.
- **`CLAUDE.md`** : sections *Architecture* et *Assets layout* mises à jour.

## Vérification

- `godot --headless --build-solutions --quit` → compilation propre (aucune erreur).
- `godot --headless --quit-after 200` → boot de `monde.tscn` sans erreur (exit 0).
- L'erreur d'import `Cannot navigate to '…/Snowball.cs'` est un simple marque-page
  de disposition d'éditeur périmé (dernier fichier ouvert), sans impact runtime.

## Notes

- Noms de l'API en anglais tels que demandés, mais en PascalCase C#
  (`TakeDamage`, `IsInvincibleToDamage`) pour rester cohérent avec le code.
- Travail **non commité** (conformément aux consignes).
