# Props purement visuels → Sprite2D inline (suppression des `.tscn` déco)

## Contexte
Les `.tscn` de `scenes/decors/props/` n'étaient que des `Sprite2D` sans collision ni script → un wrapper inutile. Décision : les props purement visuels ne doivent plus avoir de `.tscn` dédié ; ils deviennent des `Sprite2D` inline dans `monde.tscn`.

## Changements
- **`scenes/niveaux/monde.tscn`** : 16 `ext_resource type="PackedScene"` repointés en `Texture2D` (PNG direct, même `id`) ; **48 instances de props converties** en nœuds `Sprite2D` inline (`texture`, `z_index = -1`, + `offset` préservé pour Rocher `16.375`, CristalGros `9`, FleurGivre `4`). Positions **inchangées** (0 ligne `position` supprimée). Édits ciblés, aucun déplacement de décor.
- **Conservé** : `grotte/GlaceEmpilee.tscn` (3 instances) — porte un `StaticBody2D` + `CollisionShape2D`, ce n'est donc pas de la déco pure.
- **Supprimé** : les 17 `.tscn` orphelins de `decors/props/` (dont `StalactiteDecor.tscn`, déjà mort). PNG conservés.
- **`CLAUDE.md`** : nouvelle convention — prop sans collision ni script = `Sprite2D` inline, jamais de `.tscn`.

## Vérification
- `godot --headless --build-solutions --quit` : compile OK.
- `godot --headless --quit-after 200` : `monde.tscn` boote sans erreur de chargement de ressource (`Failed loading` / `Invalid ExtResource` : aucune). Les `Not supported by this display server` sont du bruit headless préexistant, sans lien.
- Reste une seule référence `decors/props` dans `monde.tscn` : `GlaceEmpilee` (attendu).
- Play-test manuel recommandé pour confirmer visuellement le placement (offsets Rocher/CristalGros/FleurGivre).
