# Écran Paramètres — redimensionnement dynamique + défilement + palier des modèles

**Fichier touché : `scripts/UI/EcranParametres.cs` uniquement.** Aucune modification de
`project.godot` ni des autres écrans.

## Contexte

L'écran Paramètres est construit par code, avec des dimensions en dur (largeur de colonne 220,
boutons 120/124/36/200/220, polices 28/22/18, marges 28) calibrées pour le canvas 640×360.
Objectif : que l'écran **suive la taille réelle de la fenêtre** et **défile** au lieu de couper le
texte, et afficher le **palier de taille** de chaque modèle Ollama installé.

## Changements

### 1. Redimensionnement proportionnel à la taille réelle de la fenêtre
- **Registre + 4 helpers réutilisables** — `Min()`, `Police()`, `Marge()`, `Sep()` : chacun mémorise
  la valeur *de base* d'un élément et applique le facteur courant à la création.
- `FacteurEchelle()` = `DisplayServer.WindowGetSize().Y / 720`, **borné `[0.75, 1.5]`** (le canvas
  restant fixe, au-delà les colonnes déborderaient).
- `AppliquerEchelle()` réapplique base × facteur à tout le registre ; appelée en différé en fin de
  `_Ready` et branchée sur `GetTree().Root.SizeChanged` (désabonnement en `_ExitTree`). Purge les
  nœuds libérés (la liste des modèles se reconstruit).
- **Toutes** les dimensions en dur passent désormais par ces helpers (plus rien de codé en dur).

### 2. Défilement vertical sur débordement
- `EnvelopperDefilement()` enveloppe chaque section (Touches, Affichage, Audio, Dialogue IA) dans un
  `ScrollContainer` **vertical uniquement** : une section trop haute (grande police, longue liste)
  **défile** au lieu de tronquer le texte. Le défilement horizontal reste désactivé (autowrap).

### 3. Palier de taille des modèles installés
- Dans la liste **Modèles installés**, chaque ligne affiche son palier
  (**Minuscule / Petit / Moyen / Lourd**) en repère gris discret, entre le tag et le bouton
  *Supprimer*.
- Nouveau helper `PalierModele(tag)` : déduit le palier du catalogue unique `OllamaService.Modeles`
  (premier mot du libellé, ex. « Minuscule (2.0 Go) » → « Minuscule »). Rien affiché si le tag est
  hors catalogue.

## Vérification
- `godot --headless --build-solutions --quit` → build OK, sans erreur ni avertissement.
- Boot headless : même nombre d'erreurs que sur `main` (11 erreurs préexistantes « display server »
  liées aux libellés clavier en headless) ; **aucune** nouvelle erreur du code ajouté.
- Play-test manuel recommandé : ouvrir Paramètres (menu principal + pause), changer de résolution et
  vérifier la mise à l'échelle sans débordement horizontal + le défilement des sections hautes.
