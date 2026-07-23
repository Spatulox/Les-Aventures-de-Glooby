# Fix : "state bizarre" après Continuer une partie debug

## Problème

Lancer le monde en **Debug**, quitter, puis cliquer **Continuer** donnait un état
incohérent : tous les pouvoirs débloqués, mais pas de mana infini, pas de barre
de mana visible, et les mobs ne mouraient plus en un coup.

Deux défauts indépendants :

1. **Défaut réel (toute partie).** `Hud` est un autoload : son `_Ready` lit
   `PouvoirGlaceActif` une seule fois au boot (alors `false`) → barre de mana
   masquée. `GameState.Charger()` ré-émettait `PvChanges`/`PoissonsChanges`/
   `CheckpointActif` mais **pas** `PouvoirGlaceObtenu`/`ManaGlaceChanges` → la
   barre ne réapparaissait jamais après un Continuer, même dans une vraie partie.
2. **Demi-état debug.** `ModeDebug` est volontairement hors sauvegarde, alors que
   les booléens de pouvoir vivent dans `DonneesSauvegarde` et sont restaurés.
   Continuer restaurait donc les pouvoirs mais pas `ModeDebug` → plus de mana
   infini ni de oneshot.

## Décision

Le **mode debug ne sauvegarde jamais** (respecte l'intention « debug = session
jetable, ne doit pas contaminer une save »). Continuer reprend toujours une vraie
partie : le demi-état debug ne peut plus exister.

## Changements

Un seul fichier : `scripts/Core/GameState.cs`.

- **`Sauvegarder()`** : early-return si `ModeDebug` → aucune écriture en debug.
  Couvre les deux appelants (`Checkpoint.cs`, `ZoneBossCerf.cs`). Effet voulu : une
  session purement debug ne laisse aucune save, donc **Continuer** reste grisé si
  aucune vraie partie n'existe.
- **`Charger()`** : ré-émet en plus `PouvoirChaleurObtenu`/`PouvoirGlaceObtenu`
  (conditionnels aux flags) et `ManaGlaceChanges`, pour que le HUD ré-affiche la
  jauge de mana. Vérifié : seul `Hud` écoute ces signaux, aucun effet
  sonore/pickup parasite.

## Vérification

- Compilation propre (`godot --headless --build-solutions --quit`).
- Aucun changement de scène, d'asset ni du format `DonneesSauvegarde` (pas de
  migration).
