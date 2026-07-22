# Renommage « fonceur » → « ours_de_neige »

Renommage complet du PNJ méchant *fonceur* en *ours de neige*, partout dans le projet.

## Changements

- **Script** — `scripts/Entities/Pnj/Fonceur.cs` → `OursDeNeige.cs` (+ `.uid`), via `git mv`.
  - Classe `Fonceur` → `OursDeNeige`.
  - Chemins d'animations `res://assets/pnj/fonceur/{idle,marche}` → `.../ours_de_neige/…`.
  - Commentaires de classe et de méthodes mis à jour.
- **Scène** — `scenes/entites/fonceur.tscn` → `ours_de_neige.tscn`, via `git mv`.
  - Nœud racine `Fonceur` → `OursDeNeige`.
  - Chemin du script `ext_resource` pointé vers `OursDeNeige.cs`.
- **Assets** — dossier `assets/pnj/fonceur/` → `assets/pnj/ours_de_neige/` (frames + `.import`).
  - `source_file` des `.import` réécrits vers le nouveau chemin, réimportés proprement par Godot.
- **PnjMechant.cs** — commentaire de classe (« fonceur » → « ours de neige »).

## Vérifications

- Build C# propre (`godot --headless --build-solutions --quit`, exit 0).
- Plus aucune occurrence de « fonceur » dans le code, les scènes ou les imports.
- La scène n'est instanciée nulle part (absente de `monde.tscn`) : aucun recâblage supplémentaire nécessaire.

Renommages effectués avec `git mv` (historique préservé). Aucun commit.
