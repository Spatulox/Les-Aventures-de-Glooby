# Zone de boss = salle caméra (fin des LimiteGauche/LimiteDroite)

**But :** ne plus saisir de taille de charge à la main pour le boss. La zone de
boss devient une « salle caméra » nommée (ex. `ZoneBossCerf`), avec un fond de
région, qui **verrouille la caméra** (pas de défilement une fois entré, façon
village) et dont le **rectangle sert de bornes au boss** (il ne peut pas en sortir).

## Code

- **`scripts/Common/DeclencheurZone.cs`** — remontée des helpers réutilisables
  (avant privés à `CameraZone`) : `CalculerLimitesDepuisForme` (rectangle de
  collision → AABB monde), `Contient(point)`, et le nouveau `AppliquerCommeSalle`
  (cale les limites de la `Camera2D` + fond de région). Mutualisé pour toute zone.
- **`scripts/Common/IZoneCamera.cs`** *(nouveau)* — interface `Contient` +
  `Appliquer(joueur)` : ce que le `Player` détecte pour piloter la caméra.
- **`scripts/Core/CameraZone.cs`** — implémente `IZoneCamera` ; ses copies locales
  de `Contient`/`CalculerLimitesDepuisForme` supprimées (héritées) ; `Appliquer`
  délègue à `AppliquerCommeSalle`.
- **`scripts/Core/ZoneBoss.cs`** — devient **aussi** une salle caméra
  (`IZoneCamera`) : s'inscrit au groupe `zones_camera`, expose `NomRegion` +
  `MargeChuteVide`, et `Appliquer` verrouille la caméra sur son rectangle. Garde
  `BodyEntered` pour l'apparition du boss.
- **`scripts/Core/ZoneBossCerf.cs`** — **exports `LimiteGauche`/`LimiteDroite`
  supprimés** ; `ConfigurerBoss` dérive les bornes de charge du Cerf du rectangle
  de l'arène (`CalculerLimitesDepuisForme`).
- **`scripts/Entities/Player/Player.cs`** — `_zoneCameraActive` typé `IZoneCamera`
  (au lieu de `CameraZone`) ; `MettreAJourZoneCamera` accepte toute `IZoneCamera`
  → l'arène de boss verrouille la caméra comme une salle normale.

## Scène (`monde.tscn`)

- **`RectZoneBoss`** : `3300×400` → **`640×256`** (taille village → caméra fixe).
- **`ZoneBossCerf`** : recentré sur `(5900, 200)` (fin de grotte, l'arène gardée
  par le Cerf) ; `LimiteGauche/Droite` retirés ; `NomRegion = "grotte"` ajouté.
  Bornes du boss dérivées : `5580..6220`.
- **`ZoneGrotte`** : rétrécie (`pos 4115`, `scale x 11.4453`) pour finir pile au
  bord gauche de l'arène (5580) → **passage de caméra net, sans chevauchement**.
- **8 stalactites-pièges** regroupées dans le plafond de l'arène (5610..6170) au
  lieu d'être étalées dans le couloir → le piétinement retombe bien à l'écran.

## À vérifier

Build C# + play-test (F5) non exécutés (build interrompu). Vérifier : entrée dans
l'arène → caméra figée, fond grotte, boss borné, stalactites à l'écran.
