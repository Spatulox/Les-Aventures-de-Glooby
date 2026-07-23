# Dialogues dynamiques via Ollama (LLM local, streaming)

## Objectif
Générer à la volée les répliques de certains PNJ avec un LLM local (Ollama), à partir d'un
contexte propre au PNJ + un contexte global partagé, et les afficher **en streaming** dans la
bulle existante. Ollama est **auto-provisionné** (aucune install manuelle) ; repli silencieux
sur les `Lignes` statiques si indisponible.

## Ce qui a été fait

### Nouveaux fichiers
- **`scripts/Common/OllamaTalkative.cs`** — interface `OllamaTalkative : Talkative`
  (`DialogueDynamiqueActif`, `Contexte`, `Invite`).
- **`scripts/Core/OllamaService.cs`** — autoload : provisionnement en tâche de fond,
  `ConstruireContexte()` (contexte global + `NomJoueur` + `FaitsGlobaux` + contexte PNJ),
  `GenererFlux()` (POST `/api/generate` streamé, NDJSON, callbacks rejoués sur le thread
  principal via `CallDeferred`), signaux `ProvisionnementProgresse`/`ProvisionnementTermine`,
  arrêt propre du serveur dans `_ExitTree`. `Disponible` = faux tant que non prêt.
- **`scripts/Core/ProvisionneurOllama.cs`** — garantit un Ollama utilisable en lançant tout
  seul la **procédure d'installation officielle** de l'OS (`OS.GetName()`) : **Windows** =
  `OllamaSetup.exe /VERYSILENT`, **macOS** = `Ollama.dmg` monté via `hdiutil` + copie de
  `Ollama.app`, **Linux** = archive `ollama-linux-amd64.tar.zst` (format officiel actuel ; le
  `.tgz` renvoie désormais 404) extraite par `tar --zstd` dans `user://ollama/`. Puis
  détection/lancement du serveur (`serve`) et `pull` du modèle si absent ; localisation du
  binaire dans les emplacements d'install standard. Chaque échec renseigne une **raison
  lisible** (`DerniereErreur`) ; tout échec ⇒ « indisponible » (non bloquant, repli statique).
- **`scripts/UI/EcranChargementOllama.cs`** + **`scenes/ui/ecran_chargement_ollama.tscn`** —
  **barre de chargement discrète ancrée en bas de l'écran** (`CanvasLayer` non bloquant),
  affichée **seulement** si un téléchargement est nécessaire, pendant le menu principal (elle
  survit au passage menu → monde). Phase + `ProgressBar`, se retire à la fin. Le menu reste
  pleinement utilisable dessous. **Les erreurs sont affichées** : en cas d'échec (404, pas de
  réseau, `pull` KO…) le bandeau montre la raison en rouge (`⚠ Dialogues IA indisponibles — …`)
  puis se retire seul après quelques secondes. Signal dédié `ProvisionnementErreur(message)`.

### Fichiers modifiés
- **`EcranParametres.cs`** — nouvel onglet **« Avancé »** (gestion d'Ollama) : case
  **Activer/désactiver** les dialogues IA (persistée ; contrôle le démarrage du serveur ET les
  appels), étiquette d'état (désactivé / disponible / indisponible), boutons **« Retélécharger
  Ollama »** et **« Supprimer Ollama »** (chacun avec confirmation ; grisés si désactivé).
  L'onglet **« Accessibilité »** (vide) a été **supprimé**.
- **`OllamaService.cs`** (compléments) — flag `Actif` persistant (`user://ollama.cfg`),
  `DefinirActif(bool)` (démarre/arrête le serveur + persiste), `SupprimerOllama()` (arrête le
  serveur + efface `user://ollama/`), `Reprovisionner()` (supprime puis réinstalle). Au boot,
  le provisionnement ne démarre que si `Actif`.
- **`PnjAmical.cs`** — implémente `OllamaTalkative` ; exports `DialogueDynamique`, `Contexte`,
  `Invite` ; `DialogueDynamiqueActif => DialogueDynamique && OllamaService prêt`.
- **`DeclencheurDialogue.cs`** — branche flux LLM prioritaire (`DemarrerFlux`), rendu
  incrémental, annulation en fin de dialogue, `ReplierSurStatique()`. Nouveau helper
  `PeutDialoguer()` pour qu'un PNJ IA **sans `Lignes`** reste parlant.
- **`BulleDialogue.cs`** — ajout `MettreAJourFlux(texte)` (rendu incrémental du streaming).
- **`project.godot`** — autoload `OllamaService` ajouté après `GameState`.

## Vérification
- `dotnet build` : **0 erreur, 0 avertissement**.
- Boot headless (`--quit-after 200`) : aucune erreur Ollama, sortie propre (repli statique
  sans réseau). Les erreurs « Not supported by this display server » sont pré-existantes
  (mode fenêtre en headless), sans rapport avec ces changements.

## Reste à faire (hors code)
- Confirmer/ajuster les URLs d'archives Ollama (`UrlBinaire*`) selon les cibles réelles.
- Câbler un PNJ exemple dans `monde.tscn` : cocher `DialogueDynamique` + renseigner `Contexte`
  (édition surgicale d'instance).
- Play-test manuel : 1er lancement avec réseau (écran de chargement), streaming de la bulle.
