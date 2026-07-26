# Dialogues à choix — le joueur peut répondre aux PNJ

## Objectif

Le dialogue était à sens unique (défilement de `Lignes`, ou réplique générée par le
LLM). On ajoute des **réponses pré-écrites** que le joueur choisit dans une liste,
éditables dans l'éditeur Godot, avec enchaînement (choix → réponse → nouveaux choix)
et effets de gameplay. Premier cas concret : le **lutin CGT** en grève à la fin de la
banquise, qui réclame 50 poissons pour son piquet.

## Principe retenu

`DeclencheurDialogue` détectait déjà ses capacités optionnelles par cast
(`TalkativeAutomatique`, `OllamaTalkative`). On ajoute une **3e interface d'extension**
sur le même patron : un PNJ qui ne la porte pas — ou dont la `Conversation` est vide —
garde exactement le comportement d'avant. Les données vivent dans des `.tres`
(patron `AmbianceSonore` / `VarianteAmbiance` / `PisteMusicale`), pas dans le code.

## Nouveaux fichiers

- **`scripts/Common/TalkativeAChoix.cs`** — l'interface : `Conversation` (racine de
  l'arbre) + hook `SurChoixRetenu(choix)`.
- **`scripts/Core/NoeudDialogue.cs`** (`[GlobalClass] Resource`) — une étape :
  `Repliques` (ce que dit le PNJ) + `Choix`, et `ChoixDisponibles()` qui filtre.
- **`scripts/Core/ChoixDialogue.cs`** (`[GlobalClass] Resource`) — une réponse du
  joueur : `Texte`, `Reponse` (du PNJ), `Suite` (nœud suivant, vide = fin),
  `IdMemoire` + `UneSeuleFois` (persisté via `GameState.MarquerConsomme`) et
  `CoutPoissons`.
- **`assets/dialogues/banquise_fin_lutin_cgt.tres`** — l'arbre du lutin gréviste
  (nommage `<lieu>_<pnj>.tres` : le nom dit où se trouve le PNJ).

## Fichiers modifiés

- **`scripts/Core/DeclencheurDialogue.cs`** — parcours de l'arbre : `DemarrerNoeud`,
  `JouerLignes` (défilement générique + suite à exécuter), `AfficherChoix`,
  `GererNavigationChoix`, `ValiderChoix`. Le flux LLM accepte désormais une invite et
  une suite (`surFin`, jusqu'ici inutilisé, sert enfin), et garde une **mémoire courte
  de l'échange** (4 dernières répliques) réinjectée dans le contexte du modèle.
  Un PNJ à `Conversation` ne démarre **jamais** au passage : il attend la touche.
- **`scripts/UI/BulleDialogue.cs`** — mode « liste de réponses » (`AfficherChoix`) :
  une option par ligne, sélection en bandeau plein + curseur `>`. `Composer` a été
  scindé (mesure ↔ `Appliquer`) pour que les deux modes partagent couleurs, taille de
  fond et recadrage caméra. Une 2e instance de la bulle est posée **sur le joueur**
  (c'est lui qui parle).
- **`scripts/Entities/Pnj/PnjAmical.cs`** — implémente `TalkativeAChoix` : export
  `Conversation` + `SurChoixRetenu` virtuel.
- **`scripts/Core/CatalogueActions.cs`** — action **`haut`** (Flèche Haut / D-pad,
  jusqu'ici non liée), symétrique de `bas`. Apparaît seule dans Paramètres > Touches.
- **`scripts/Core/GameState.cs`** — flag `DialogueModal` (frère de
  `DialogueDisponible`) et `DepenserPoissons(int)`, pendant générique de
  `ManagerPoisson()`.
- **`scripts/Entities/Player/Player.cs`** — garde en tête de `_PhysicsProcess` : en
  dialogue modal, gravité + `MoveAndSlide` + idle, puis `return` avant toute lecture
  d'entrée. Neutralise d'un coup saut, glissade, `bas`, lancer et pouvoirs.
- **`scripts/Entities/Pnj/LutinCgt.cs`** — `SurChoixRetenu` : la pancarte passe à
  « MERCI CAMARADE » après le don (constante `IdDonPoissons`).
- **`scenes/niveaux/monde1.tscn`** — édition chirurgicale : `ConversationRessource`
  branchée sur le nœud `LutinCgt` (celui placé par l'auteur, en 4867/259). Le doublon
  `LutinCgtPiquet` que j'avais ajouté sous `Banquise/Pnj` a été retiré. Aucun décor
  déplacé.
- **`CLAUDE.md`** — la doc affirmait encore que les actions se règlent dans
  `GameState.ConfigurerActionsParDefaut()` (supprimée depuis) ; corrigé vers
  `CatalogueActions` + `Parametres`. Ajout des nouvelles briques dans `Common/`/`Core/`.

## Choix de conception

- **Modal sur toute la conversation**, pas seulement pendant la liste : sinon le
  joueur peut s'éloigner entre deux répliques et Espace redevient le saut au milieu
  d'un échange. Sortie = un choix terminal (`Suite` vide) ; par sécurité un nœud sans
  choix disponible referme la conversation, et `TerminerDialogue` remet
  `DialogueModal` à faux sur **tous** les chemins.
- **`CoutPoissons` porté par le choix** (et pas par le code du lutin) : un choix trop
  cher n'est pas proposé, donc « tiens, prends mes 50 poissons » ne peut pas mentir,
  et n'importe quel futur PNJ marchand en profite sans une ligne de code.
- **L'IA passe devant, le texte écrit est le repli** (comme partout ailleurs dans le
  jeu). `NoeudDialogue.Repliques` et `ChoixDialogue.Reponse` ne sont donc pas la
  réplique finale : ce sont l'**intention** donnée au modèle (« fais passer cette idée
  avec TES mots »), et le texte exact rejoué en repli. Sans texte écrit, le PNJ
  improvise à partir de son seul contexte ; sans IA, le texte écrit est joué tel quel.
  Pourquoi une intention plutôt qu'une invite libre : les choix qui suivent répondent à
  ce que le PNJ vient de dire — une génération hors-sol casserait l'arbre.
- **Une seule sortie de streaming** (`SortirDuFlux`), commune à la fin normale, à la
  coupure par le joueur et à l'échec : si **rien** n'a été généré (modèle muet, coupure
  avant le 1er token, erreur réseau), le texte écrit reprend la main. Sans ça, une
  coupure trop rapide laissait une bulle vide.

## Piège rencontré : export typé + script `[Tool]`

Premier câblage : `[Export] public NoeudDialogue Conversation`. À l'ouverture de la
scène, l'éditeur levait
`InvalidCastException: Unable to cast 'Godot.Resource' to 'NoeudDialogue'` dans le
setter généré de `PnjAmical`, **perdait la liaison, puis l'effaçait à la sauvegarde**.

Cause isolée par bissection : le même câblage sur un PNJ **non-`[Tool]`** (Pingouin2)
ne pose aucun problème ; sur `LutinCgt`, qui est `[Tool]`, l'éditeur pose la propriété
avant que la ressource n'ait son instance C#, donc le Variant porte un `Resource` nu.
Le type déclaré dans la ligne `ext_resource` n'y change rien (testé avec `Resource`
puis `NoeudDialogue`). À l'exécution, en revanche, la ressource est correctement typée.

Correctif : exporter la ressource en **`Resource` + filtre d'inspecteur**
(`[Export(PropertyHint.ResourceType, nameof(NoeudDialogue))] Resource
ConversationRessource`) et convertir une seule fois dans l'implémentation explicite
`TalkativeAChoix.Conversation => ConversationRessource as NoeudDialogue`. Le
glisser-déposer typé est conservé dans l'inspecteur, le cast ne peut plus échouer.

## Second piège : rappel de touche sans issue

`lutin_cgt.tscn` est réglé `AuPassage = true`. `SurEntreeJoueur` renvoie bien un PNJ à
`Conversation` vers le rappel de touche (une conversation modale ne doit pas démarrer
toute seule), mais `_Process` refusait de démarrer sur `action` tant que
`DeclencheAuPassage` était vrai : la bulle « Espace » s'affichait **et rien ne
s'ouvrait jamais**. La condition de démarrage accepte désormais explicitement le cas
`ConversationAChoix`, comme `SurEntreeJoueur`.

Ajout de **`scenes/test/test_dialogue_choix.tscn`** (+ `.gd`) pour ne plus attraper ce
genre de bug à la relecture : entrées scriptées en headless sur la vraie scène.

## Vérification

- `dotnet build` / `godot --headless --build-solutions` : **0 erreur, 0 avertissement**.
- Boot headless de `monde1.tscn` (200 frames) : aucun warning « aucun Talkative »,
  aucune erreur de chargement du `.tres` (les seuls warnings restants sont les UID
  pré-existants de `scenes/sol/` et `scenes/plateformes/`).
- Chargement du `.tres` vérifié au script : racine = 2 répliques + 3 choix ; le choix
  don porte bien `cout=50`, `unique=true`, `id=lutin_cgt_don_poissons`, 2 lignes de
  réponse et une suite ; le choix « revendications » a 0 réponse écrite (branche IA)
  et mène à un nœud à 2 choix ; le choix « bon courage » termine.
- Ouverture de `monde1.tscn` dans l'éditeur (headless) : plus aucune
  `InvalidCastException`, et la ligne `ConversationRessource` survit au chargement.
- Type réel à l'exécution vérifié par instrumentation temporaire de
  `PreparerDeclencheur` : `LutinCgt aChoix=True conversation=True` (les autres PNJ,
  sans arbre, restent à `conversation=False` — donc comportement inchangé pour eux).
- **Traversée scriptée headless** (`scenes/test/test_dialogue_choix.tscn`, Ollama
  disponible) : rappel de touche → 1er appui ouvre la conversation (`DialogueModal`
  passe à vrai) → réplique générée → liste de choix → validation du don : **poissons
  50 → 0** → fin de branche, **modal relâché** (Glooby rendu au joueur). Un 2e passage
  rejoue l'arbre sans le choix du don, filtré (usage unique + réserve vide).
- **Génération d'ouverture vérifiée** sur une invite réelle : à partir de l'intention
  « Halte-là ! Piquet de grève… la caisse est vide et les ventres aussi », le modèle a
  produit « Je tiens le piquet, camarade ! Mais on a besoin de vivres pour tenir.
  Manque encore 50 poissons, sans ça, la grève s'effondre. » — reformulé, dans le
  personnage, et fidèle au chiffre.
- **Attention au temps de génération** : avec `mistral-nemo:12b` sur CPU, la première
  réplique met plusieurs dizaines de secondes (bulle « … » en attendant). Appuyer sur
  action pendant l'attente coupe la génération et affiche le texte écrit. Un modèle
  plus petit (Paramètres > Dialogue IA) rend l'échange nettement plus vif.
- **Reste à faire : un vrai F5.** Le headless ne peut pas juger le rendu de la liste
  (lisibilité de la surbrillance, position de la bulle sous Glooby) ni le ressenti du
  gel. À vérifier manuellement : navigation Haut/Bas, don qui décrémente le HUD,
  choix don absent si moins de 50 poissons, et repli propre avec les dialogues IA
  désactivés dans Paramètres.

## Limite connue

Déclencher la conversation en pleine glissade fige le lutin… pardon, fige **Glooby**
dans sa pose de glissade (les minuteurs de glissade sont suspendus pendant le modal,
puis reprennent). Cosmétique, jamais bloquant.
