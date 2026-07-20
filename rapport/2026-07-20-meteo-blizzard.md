# Système de météo — blizzard par zone caméra (2026-07-20)

Ajout d'une météo simple : beau temps par défaut, blizzard occasionnel
(assombrissement de l'écran + flocons qui tombent), tiré au sort à chaque
changement de zone caméra.

## Règles implémentées

- **10 %** de chance de déclencher un blizzard à chaque entrée dans une zone.
- Durée aléatoire **10–30 s**, puis arrêt automatique en fondu.
- **Mémorisé par zone** : sortir et rerentrer ne permet ni d'annuler le
  blizzard en cours, ni de re-tirer en boucle (délai de 15 s après un tirage
  raté).
- **Jamais en souterrain** : la grotte est marquée `Souterrain` ; y entrer coupe
  aussi un blizzard en cours.
- La minuterie tourne dans **toutes** les salles, même hors champ — un blizzard
  commencé s'épuise pendant que le joueur est ailleurs.

## Fichiers

| Fichier | Changement |
|---|---|
| `scripts/Core/MeteoZone.cs` | **nouveau** — état/tirage/minuterie d'une salle (objet simple, pas un Node) |
| `scripts/Core/GestionnaireMeteo.cs` | **nouveau** — singleton de rendu sur `CanvasLayer`, calqué sur `BackgroundManager` |
| `scripts/Core/CameraZone.cs` | + enum `TypeZone { Exterieur, Souterrain }`, champ `_meteo`, `Appliquer` étendu, `_Process` |
| `scripts/Common/Effets.cs` | + `Fondu(CanvasItem, alphaCible, duree)` — fondu réversible (sans `QueueFree`) |
| `scenes/meteo/blizzard.tscn` | **nouveau** — voile sombre + 3 couches de flocons |
| `assets/meteo/flocon_{clair,moyen,sombre}.png` | **nouveaux** — carrés bleus 4/3/2 px, **placeholders** |
| `scenes/niveaux/monde.tscn` | 2 édits : instance `Meteo` avant `MenuPause`, `Type = 1` sur `ZoneGrotte` |

## Choix de conception

- **Pas de modification de `Player.cs`** : `MettreAJourZoneCamera()` appelle déjà
  `zone.Appliquer()` une seule fois par changement de zone (hystérésis) — c'était
  exactement le point d'accroche voulu pour le tirage.
- **Un compte à rebours unique** (`_tempsRestant`) sert aux deux issues du
  tirage : c'est ce seul champ qui bloque l'exploit des allers-retours.
- **`CanvasLayer`** plutôt qu'un nœud enfant de la caméra : voile et flocons sont
  en espace écran, ils suivent la caméra sans calcul de position.
- **Enum** plutôt que booléen pour `TypeZone`, pour pouvoir ajouter `Interieur`,
  `Arene`… sans retoucher les zones déjà posées. Défaut `Exterieur` = seule la
  grotte a eu besoin d'un override.
- Les 3 variantes de flocons donnent une **profondeur** (fond lent/petit/sombre →
  avant rapide/gros/clair). `GestionnaireMeteo` les pilote par parcours des
  enfants, donc en ajouter une 4ᵉ ne touchera pas au script.
- Les PNG sont écrits par un script local : **aucun budget PixelLab consommé**.

## Vérification

- `godot --headless --build-solutions --quit` → compilation propre.
- `godot --headless --quit-after 300 scenes/niveaux/monde.tscn` → aucune erreur
  ni warning.
- Sondes temporaires (retirées depuis) : chaîne validée de bout en bout —
  déclenchement à l'entrée de zone, les 3 émetteurs de flocons bien trouvés et
  basculés, arrêt automatique à l'expiration de la minuterie, et `Type` lu
  correctement depuis `monde.tscn` (`ZoneGrotte = Souterrain`, les 3 autres
  `Exterieur`).

## Reste à faire (test manuel `godot`)

Le headless ne juge pas le rendu. À vérifier en jeu :
- lisibilité du voile et des flocons (densité, vitesse, contraste des 3 bleus) ;
- que le blizzard ne gêne pas la lecture des plateformes et des télégraphes.

Pour tester vite, monter `MeteoZone.ChanceBlizzard` à `1f` (et le remettre à
`0.1f` ensuite).

Les textures de flocons sont des **carrés unis placeholder** — à remplacer par de
vrais flocons pixel-art.
