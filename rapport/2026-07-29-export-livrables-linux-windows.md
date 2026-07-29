# Export du jeu en livrables Linux + Windows

## Ce qui manquait

- **Aucun `export_presets.cfg`** — le projet n'avait jamais été exporté.
- **Aucun modèle d'exportation installé** (`~/.local/share/godot/export_templates/` vide).
- **Aucun `.sln`** — bloquant : sans solution, Godot log `Export .NET Project: no solution file was found` et **le `.pck` part sans les assemblies C#** (jeu injouable).

## Ce qui a été fait

| Fichier | Rôle |
|---|---|
| `export_presets.cfg` | 3 presets : `Linux` (x86_64), `Linux ARM64`, `Windows Desktop` (x86_64), sortie dans `builds/`, `*.md` / `rapport/` / `local-tasks/` / `test/` exclus du pack |
| `Les Aventures de Glooby.sln` | solution générée à la main avec les configs **`ExportDebug` / `ExportRelease`** qu'exige Godot .NET (`dotnet sln add` ne les crée pas) |
| `exporter.sh` | build + les deux exports + zip, reproductible en une commande |
| `.gitignore` | `builds/` ignoré |

Modèles d'exportation installés : `Godot_v4.6.3-stable_mono_export_templates.tpz` (1,1 Go) décompressé dans
`~/.local/share/godot/export_templates/4.6.3.stable.mono/`. **La version doit être exactement celle de l'éditeur** (`4.6.3.stable.mono`).

## Commandes

```bash
./exporter.sh                                   # tout : build + 2 exports + zips
godot --headless --export-release "Linux"       # un seul preset
godot --headless --export-release "Windows Desktop"
```

## Résultat (`builds/`)

| Livrable | Taille |
|---|---|
| `GloobyAventures-linux-x86_64.zip` | 119 Mo |
| `GloobyAventures-linux-arm64.zip` | 117 Mo |
| `GloobyAventures-windows-x86_64.zip` | 128 Mo |

Chaque dossier contient **3 éléments indissociables** : l'exécutable, le `.pck`, et
`data_Les Aventures de Glooby_<plateforme>_x86_64/` (les assemblies .NET). Zipper le dossier entier,
jamais l'exe seul.

## Vérifications (après correctif, recompilation complète)

| Étape | Résultat |
|---|---|
| `dotnet build -c ExportRelease` (`obj/`+`bin/` supprimés) | **0 avertissement, 0 erreur** |
| `./exporter.sh` (3 exports + zips) | log **vide** de toute erreur/warning, exit 0 |
| `godot --headless --quit-after 200` (projet source) | 12 × `Not supported by this display server` + 2 lignes de fin |
| Build **exporté** lancé en headless | **profil identique** au projet source, 0 « aucune frame » |

Les 12 erreurs sont un **artefact du headless** : `keyboard_get_label_from_physical`
(`display_server.cpp:1215`) une fois par action de `CatalogueActions` (12 actions), quand l'écran
Paramètres résout le libellé des touches — il n'y a pas de disposition clavier sans serveur
d'affichage. `resources still in use` / `ObjectDB instances leaked` sont le bruit de sortie habituel
de Godot. Rien de tout ça ne concerne le jeu lancé normalement.

Point important : **le build exporté produit exactement le même log que le projet source** — c'est ce
qui manquait avant le correctif `FichiersProjet`.

Note : `exporter.sh` n'envoie plus la compilation C# dans `/dev/null` (c'était le seul endroit où
passaient les warnings du compilateur).

## Vérifications initiales

- Export Linux et Windows : **0 erreur**, `dotnet publish` OK sur les deux.
- Binaire Linux lancé en headless : démarre, le C# tourne (autoloads + `MenuPrincipal`). Les seules
  erreurs sont des artefacts headless (`keyboard_get_label_from_physical` non supporté sans serveur
  d'affichage), identiques à `godot --headless` sur le projet source.
- **Non vérifié** : le `.exe` Windows (pas de Windows/Wine ici) et un lancement fenêtré réel.

## Bug d'export corrigé : assets invisibles (joueur / PNJ / boss)

**Symptôme** : dans le build, aucune entité animée n'a de sprite. Dans l'éditeur, tout va bien.

**Cause** : à l'export, un dossier ne contient plus `0.png` mais **`0.png.import`** (l'image réelle est
un `.ctex` de `.godot/imported/`), et les ressources texte deviennent `banquise.tres.remap`.
`GD.Load("res://.../0.png")` résout toujours — mais l'**énumération** (`DirAccess.GetFilesAt`) renvoie
les noms suffixés, donc tout filtre `EndsWith(".png")` trouve **zéro fichier**.

Vérifié directement sur le `.pck` (script GDScript sur le pack chargé) :

| | |
|---|---|
| Noms réels dans le pack | `0.png.import`, `1.png.import`… |
| Ancien filtre `.png` | **0 frames** ← le bug |
| Nouveau filtre | **5 frames**, `load()` renvoie un `CompressedTexture2D` |

**Correctif** : nouveau helper partagé `scripts/Common/FichiersProjet.cs` (`Lister` / `NomOrigine`)
qui retire les suffixes `.import` / `.remap` et dédoublonne (dans l'éditeur un asset apparaît en
`0.png` **et** `0.png.import`). **Tout scan de dossier du projet doit passer par lui.** Répercuté sur
les 4 sites qui scannaient un dossier :

| Fichier | Ce qui était cassé à l'export |
|---|---|
| `Common/AnimationsSprite.cs` | **toutes** les animations d'entités (joueur, PNJ, boss) — le bug signalé |
| `UI/MenuPrincipal.cs` | fond aléatoire du menu (`.png`) |
| `UI/EcranScenesDebug.cs` | liste des niveaux (`.tscn` → `.tscn.remap`) : écran vide |
| `Core/GestionnaireAudio.cs` | déjà géré (`.remap`) — repassé sur le helper pour ne pas dupliquer |

`ChargerFrames` journalise désormais un `PushWarning` quand un dossier d'animation ressort vide :
la prochaine régression de ce type se verra dans la console au lieu de produire une entité invisible.

## Configuration requise (mesurée sur les binaires produits)

- **Linux** : glibc ≥ 2.28, **kernel ≥ 5.15** (note ABI du binaire), **Vulkan 1.0**. Le binaire ne se lie
  qu'à la libc ; X11/Wayland, `libvulkan.so.1`, ALSA/PulseAudio, udev et dbus sont chargés en `dlopen`.
- **Windows** : `project.godot` force `rendering_device/driver.windows="d3d12"` → c'est **Direct3D 12
  (feature level 12_0)** qui compte, pas Vulkan. Les DLL de l'Agility SDK **ne sont pas embarquées**
  (`export_d3d12=0` + SDK absent des réglages de l'éditeur) : le jeu utilise le runtime D3D12 du système.
  Les replis `fallback_to_vulkan` / `fallback_to_opengl3` sont présents dans le binaire.
- **.NET** : runtime embarqué sur les 3 cibles (`libcoreclr.so` / `coreclr.dll` + `System.Private.CoreLib`),
  donc **rien à installer** sur la machine cible.

## Points d'attention pour la livraison

- **Ollama** : le jeu provisionne son binaire + son modèle au premier lancement dans `user://ollama`
  (`ProvisionneurOllama` gère déjà `ollama.exe` côté Windows). Le livrable ne les embarque pas →
  premier démarrage = téléchargement, et machine hors-ligne = dialogues LLM indisponibles
  (`ollama.actif = false` dans les paramètres pour s'en passer).
- **Icône Windows** : `application/modify_resources=false` dans le preset — l'exe garde l'icône Godot.
  Pour l'icône du jeu il faut un `.ico` + `rcedit` (à configurer dans les réglages de l'éditeur).
- Aucune signature de code (Windows affichera un avertissement SmartScreen).
