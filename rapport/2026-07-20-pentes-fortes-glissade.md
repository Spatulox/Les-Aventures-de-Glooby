# Pentes fortes : glissade obligatoire (2026-07-20)

## Demande

Le joueur ne doit plus pouvoir grimper / rester en idle / marcher / courir sur une pente
au-delà d'un certain angle (celles des `PenteBanquiseForte*.tscn`) — il ne peut que la
glisser.

## Mesure des pentes existantes

Angles déduits des `CollisionPolygon2D` des scènes (344 px de large) :

| Scène | Dénivelé écran | Angle |
|---|---|---|
| `PenteBanquiseDouce*` | 136 px | ~21,6° — reste praticable |
| `PenteBanquiseForte*` | 342 px | ~44,8° — glissade forcée |

Seuil retenu : **35°**, entre les deux.

## Changements — `scripts/Entities/Player/Player.cs`

- Nouvel export **`AnglePenteMaxDegres = 35f`** (réglable dans l'éditeur, commenté avec
  les angles réels des deux pentes).
- Nouvelle méthode **`GererPenteRaide(bool auSol)`**, appelée chaque frame dans
  `_PhysicsProcess` avant la branche glissade / marche :
  - au-dessus du seuil (`GetFloorAngle()`), la direction est forcée vers la descente
    (signe de `GetFloorNormal().X`) et la glissade est démarrée si elle ne l'est pas ;
  - le **cooldown de glissade est ignoré** — sinon le joueur resterait planté sur la
    pente en attendant son expiration ;
  - une glissade arrivant **par le bas** est retournée vers la descente au lieu de
    grimper sur son élan.
- Commentaire de classe mis à jour.

Effet : plus d'idle / marche / course / montée possible sur une pente forte, seule la
glissade la parcourt. `GererElanPente` gelait déjà le minuteur en descente, donc la
glissade tient jusqu'en bas puis se prolonge sur le plat comme avant — aucun réglage
existant modifié.

## Vérification

- `godot --headless --build-solutions --quit` : compile propre.
- `godot --headless --quit-after 200` : seules les erreurs préexistantes du menu
  (`MenuPrincipal.ToucheDe` → *Not supported by this display server*), sans rapport.
- Play-test manuel non fait (ressenti des pentes à valider en F5).

## Point laissé ouvert

Le **saut reste autorisé** pendant la glissade : on peut techniquement remonter une pente
forte par sauts successifs (chaque atterrissage relance la glissade descendante, mais un
enchaînement rapide gagne du terrain). Laissé tel quel — bloquer le saut sur pente raide
donne une sensation de blocage désagréable. À trancher au play-test.
