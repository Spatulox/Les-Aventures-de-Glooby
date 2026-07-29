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

## Vérifications

- Export Linux et Windows : **0 erreur**, `dotnet publish` OK sur les deux.
- Binaire Linux lancé en headless : démarre, le C# tourne (autoloads + `MenuPrincipal`). Les seules
  erreurs sont des artefacts headless (`keyboard_get_label_from_physical` non supporté sans serveur
  d'affichage), identiques à `godot --headless` sur le projet source.
- **Non vérifié** : le `.exe` Windows (pas de Windows/Wine ici) et un lancement fenêtré réel.

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
