# Pouvoir de Glace — plateformes temporaires + mana

Ajout d'un second pouvoir, bâti sur la même base que le Pouvoir de Chaleur
(récupération par le joueur, flag persistant, action d'entrée).

## Mécanique
- Touche **S** maintenue : pose des plateformes de glace **éphémères** devant le
  joueur pendant qu'il avance, pour combler les trous.
- Jauge de **mana** (0→100). Utilisable seulement si assez de mana ; chaque
  plateforme en consomme. Après la dernière pose, régénération **différée de 5 s**,
  puis remontée progressive (**0→max en 30 s**).

## Changements

**`scripts/Core/GameState.cs`**
- Flag persistant `PouvoirGlaceActif` + `ObtenirPouvoirGlace()` + signal `PouvoirGlaceObtenu`.
- Mana transient (non sauvegardé) : `ManaGlace`, exports `ManaGlaceMax=100`,
  `DureeRegenGlace=30`, `DelaiRegenGlace=5`, `CoutPlateformeGlace=12`.
- `PeutUtiliserPouvoirGlace(cout)`, `ConsommerManaGlace(cout)`, régénération dans
  un nouveau `_Process` (signal `ManaGlaceChanges` émis seulement si la valeur change).
- Nouvelle action d'entrée `pouvoir_glace` (Key.S).

**`scripts/Core/DonneesSauvegarde.cs`** — sérialisation du flag `PouvoirGlaceActif`
(clé `pouvoirGlace`). Le mana n'est pas sauvegardé.

**`scripts/Plateformes/PlateformeGlace.cs` + `scenes/plateformes/PlateformeGlace.tscn`** (nouveaux)
- `: PlateformeUnidirectionnelle` (réutilise le one-way + layer traversable).
- Teinte glacée, pop d'apparition, puis fonte auto après `DureeVie=4s`
  (`Effets.Disparaitre`). Collision plus petite (130×24) pour combler un trou.

**`scripts/Entities/Player/Player.cs` + `scenes/entites/player.tscn`**
- Export `ScenePlateformeGlace` (→ `PlateformeGlace.tscn`), cadence `IntervallePoseGlace=0.22s`.
- `UtiliserPouvoirGlace()` : maintien de la touche → pose une plateforme devant
  (offsets `OffsetPoseGlaceX=40`, `OffsetPoseGlaceY=40`), consomme du mana, flash
  bleu. Rien si mana à sec.
- Ajustement : `OffsetPoseGlaceY` 22 → **40** pour poser les plateformes plus bas
  (elles apparaissaient trop haut).

**`scripts/Entities/Interactable/PouvoirGlacePickup.cs` + `scenes/interactifs/pouvoir_glace_pickup.tscn`** (nouveaux)
- Copie stricte du pickup de chaleur (`: ElementRamassable`). Sprite réutilisé =
  `assets/props/cristal_petit.png`.
- **Placé dans `monde.tscn`** à la sortie est du village (`Village/Interactifs`,
  position `(1180, 276)`), sur le dernier bout de sol avant le trou de la banquise.

**`scripts/UI/Hud.cs` + `scenes/ui/hud.tscn`** — jauge de mana (ColorRect fond +
remplissage) sous le compteur de poissons, cachée tant que le pouvoir n'est pas
débloqué ; liée à `PouvoirGlaceObtenu` / `ManaGlaceChanges`.

## Vérification
- `godot --headless --build-solutions --quit` : build .NET OK, 0 erreur/warning.
- `godot --headless --quit-after 200` : aucune erreur nouvelle (seules subsistent
  les erreurs préexistantes `KeyboardGetLabelFromPhysical` du menu en headless).
- Play-test manuel recommandé (game-feel du pont, cadence, lisibilité de la jauge).

## Notes
- Valeurs (mana, coût, durée de vie, cadence, offsets) = exports ajustables.
