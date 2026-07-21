# Paramètres d'affichage — fenêtre embarquée de l'éditeur

## Problème

Les réglages d'affichage (mode, résolution) n'avaient aucun effet et le moteur répétait :

```
Embedded window can't be resized. / can't be moved. / only supports Windowed mode.
```

**Ces messages viennent de Godot, pas du code.** Depuis Godot 4.4, lancer le jeu depuis
l'éditeur l'exécute dans une **fenêtre embarquée** (onglet « Game ») — ici via le *Wayland
embedder*, la session étant Wayland. Une fenêtre embarquée refuse `WindowSetMode`,
`WindowSetSize` et `WindowSetPosition`.

Cause racine : `run/window_placement/game_embed_mode` est absent de
`~/.config/godot/editor_settings-4.6.tres`, donc à son défaut « Use Per-Project
Configuration » → embarquement actif. `Parametres.cs` était **correct** ; ses appels
étaient simplement ignorés.

## Correctif principal (hors code, à faire une fois)

**Editor → Editor Settings → Run → Window Placement → Game Embed Mode = `Disabled`**

Le jeu se relance dans une vraie fenêtre OS : mode, résolution et centrage fonctionnent,
les messages disparaissent. Réglage **éditeur, local à la machine** (non versionné).

## Changements de code

### `scripts/Core/Parametres.cs`

- Nouvel état public **`FenetrePilotable`** : faux dès que le moteur refuse un ordre de
  fenêtre. Le mode embarqué n'étant pas exposé au script (et détecté différemment selon
  l'OS — `--embedded` est macOS-only, Linux passe par le Wayland embedder), on ne le devine
  pas : on applique, on relit, on retient. Sens unique, jamais de retour à « pilotable ».
- `AppliquerMode()` : pose le mode via le nouveau `ModeMoteur()`, puis vérifie. La
  comparaison passe par `EstPleinEcran()` — les deux plein écran sont interchangeables, le
  moteur retombant de l'exclusif au fenêtré selon la plateforme ; une égalité stricte
  aurait signalé à tort une fenêtre non pilotable.
- Nouveau `AppliquerTailleFenetre()` (factorisé depuis `AppliquerMode` + `DefinirResolution`) :
  applique la taille, relit pour vérifier, recentre seulement si ça a pris.
- Quand `!FenetrePilotable`, les appels `DisplayServer` sont sautés — mais la valeur est
  **mémorisée et sauvegardée**, donc appliquée au prochain lancement non embarqué.
- `CentrerFenetre()` : garde Wayland rendu insensible à la casse (le moteur nomme ses
  pilotes en minuscules ; une mauvaise casse désarmait silencieusement le garde).

### `scripts/UI/EcranParametres.cs`

- Label d'avertissement discret sous la section Affichage :
  **« ⚠ Sera appliqué au prochain lancement (fenêtre embarquée dans l'éditeur). »**,
  visible seulement si `!FenetrePilotable`. Les listes **restent utilisables** : le choix
  est réellement enregistré, seul son effet est différé.
- `MettreAJourEtatResolution()` → **`MettreAJourEtatAffichage()`** : point unique de
  synchronisation (grisage de la résolution hors mode fenêtré + avertissement), désormais
  aussi appelé après un choix de résolution — le refus n'étant constaté qu'à la première
  tentative.

## Vérification

- `godot --headless --build-solutions --quit` → compile sans erreur ni warning
  (assembly reconstruit, horodatage vérifié).
- `godot --headless --quit-after 200` → aucune erreur imputable au changement. Les 11
  `Not supported by this display server.` restantes tracent toutes vers
  `EvenementEntree.Libelle` (`KeyboardGetLabelFromPhysical`) : **préexistantes et propres
  au headless** (pas de clavier), rien depuis `Parametres.cs`.
- **Reste à faire en jeu réel** (impossible en headless) : après passage de Game Embed Mode
  à `Disabled`, F5 puis Paramètres → Affichage — changer la résolution et le mode doit agir
  immédiatement, sans aucun message `Embedded window…`. Contre-épreuve : remettre
  `Embed Game`, l'avertissement doit s'afficher au premier changement ; choisir 1920×1080,
  quitter, relancer non embarqué → la fenêtre doit s'ouvrir en 1920×1080.
