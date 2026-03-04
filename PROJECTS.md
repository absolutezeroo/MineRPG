# Projet : Minecraft RPG — Godot 4 (C#)

## Vision du jeu

Un jeu en monde ouvert voxel qui fusionne les mécaniques sandbox de Minecraft (minage, crafting, construction, exploration, survie) avec une couche RPG complète (progression, classes, quêtes, lore, loot, donjons, PNJ). Le joueur évolue dans un monde procédural par blocs mais avec une profondeur de gameplay RPG type Terraria / Cube World / Hytale.

- **Moteur :** Godot 4.x (.NET / C#)
- **Langage :** C# exclusivement (pas de GDScript)
- **Vue :** 3D première/troisième personne (toggle)
- **Cible :** PC d'abord, architecture pensée pour du cross-platform ensuite

---

## Contraintes techniques Godot 4 + C#

### Configuration projet

- Utiliser le template Godot .NET (nécessite .NET 8 SDK minimum)
- Solution `.sln` + `.csproj` gérés proprement, compatible JetBrains Rider
- `.editorconfig` strict pour les conventions C# (naming, formatting, severity)
- Activer `<Nullable>enable</Nullable>` et `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` dans le `.csproj`
- Utiliser les analyseurs Roslyn pour garantir la qualité du code

### Interop Godot / C# — Règles critiques

- **Hériter de Node uniquement quand nécessaire** : seules les classes qui DOIVENT vivre dans le SceneTree héritent de `Node`, `Node3D`, etc. La logique métier pure vit dans des classes C# classiques (POCO) sans dépendance Godot
- **Éviter `GetNode<T>()` et les NodePaths hardcodés** : utiliser `[Export]` pour exposer les références dans l'éditeur, ou l'injection via code au `_Ready()`
- **Signaux C# typés** : déclarer les signaux via `[Signal] public delegate void EventNameEventHandler(args)` — ne jamais utiliser les signaux string-based
- **Pas de `GD.Print()` en production** : logger via un système de logging centralisé avec niveaux (Debug, Info, Warning, Error)
- **Utiliser `partial class`** : Godot 4 C# génère du code via source generators, toutes les classes Node doivent être `partial`
- **Marshalling** : minimiser les conversions entre types Godot (Variant, GodotArray, GodotDictionary) et types C# natifs. Utiliser les types C# (`List<T>`, `Dictionary<K,V>`) dans la logique interne et ne convertir qu'à la frontière Godot
- **StringName** : utiliser `StringName` pour les noms d'actions input, les noms de signaux, les noms d'animations — éviter les allocations string répétées

---

## Optimisation & Performance (ne pas tourner à 5 FPS)

### Rendu voxel — Le point critique

- **Greedy Meshing** : fusionner les faces adjacentes identiques pour réduire drastiquement le nombre de vertices. Un chunk naïf = ~100k+ faces, greedy meshing = ~5-15k
- **Face Culling** : ne générer que les faces exposées à l'air (pas les faces entre deux blocs solides). Vérifier les voisins dans les 6 directions
- **Chunk Mesh via `ArrayMesh` / `SurfaceTool`** : construire les meshes manuellement avec `SurfaceTool` ou `ArrayMesh` côté C#. Ne PAS utiliser un `MeshInstance3D` par bloc
- **Un seul `MeshInstance3D` par chunk** : le chunk entier = un seul mesh renderé en un draw call
- **Texture Atlas** : toutes les textures de blocs dans un seul atlas → un seul material/shader par chunk → un seul draw call
- **LOD (Level of Detail)** : chunks distants rendus avec un mesh simplifié ou remplacés par des impostors
- **Frustum Culling** : ne pas rebuilder/render les chunks hors champ de la caméra (Godot le fait partiellement, mais aider avec un culling custom basé sur l'AABB des chunks)

### Multithreading — Obligatoire pour les voxels

- **Génération de chunks en threads séparés** : utiliser `System.Threading.Tasks` ou `Task.Run()` pour la génération de terrain et le meshing. Ne JAMAIS bloquer le main thread avec la génération
- **Thread-safe data** : les données de blocs du chunk (tableau `byte[]` ou `ushort[]`) sont manipulées hors main thread. Le mesh final est appliqué sur le main thread via `CallDeferred()`
- **Job Queue** : file de priorité pour les chunks à générer/remesher (priorité = distance au joueur)
- **Chunk Loading Budget** : limiter le nombre de chunks meshés par frame (ex: max 2-3 par frame) pour éviter les stutters
- **`CancellationToken`** : annuler les jobs de chunks qui ne sont plus nécessaires (joueur s'est déplacé)

### Mémoire & Allocations

- **Object Pooling partout** : particles, projectiles, entités, UI elements. Utiliser `ObjectPool<T>` custom ou `ArrayPool<byte>`
- **Éviter les allocations GC dans les boucles hot** : pas de `new`, pas de LINQ, pas de closures/lambdas dans `_Process()` et `_PhysicsProcess()`
- **`Span<T>` et `stackalloc`** : pour les buffers temporaires dans le meshing et la génération
- **Struct over Class** : pour les données de bloc, les positions de chunk, les données de vertex — utiliser des `struct` value types quand la taille est petite et la copie est cheap
- **Chunk data = flat array** : `byte[CHUNK_SIZE_X * CHUNK_SIZE_Y * CHUNK_SIZE_Z]` pas un tableau 3D, pas de `Dictionary<Vector3I, Block>`. Accès via index = `x + z * SIZE_X + y * SIZE_X * SIZE_Z`

### Physics

- **Ne PAS utiliser un `CollisionShape3D` par bloc** : construire un `ConcavePolygonShape3D` ou un `HeightMapShape3D` par chunk
- **Ou mieux** : utiliser le raycasting custom pour l'interaction blocs (pas besoin du moteur physique pour savoir quel bloc le joueur regarde) et réserver la physique Godot aux entités mobiles uniquement
- **Physics layers** : séparer les layers (terrain, entités, projectiles, triggers) pour que le broadphase soit efficace
- **`_PhysicsProcess` minimaliste** : seule la logique qui DOIT tourner à tick fixe y va. Le reste dans `_Process()`

### Rendu général

- **Shader custom pour les blocs** : un shader unique qui gère l'atlas UV, l'ambient occlusion par vertex, et éventuellement le vent sur la végétation
- **Ambient Occlusion par vertex (AO)** : calculée au moment du meshing, pas en post-process. Beaucoup moins cher
- **Instancing** : pour la végétation, les petits props (herbe, fleurs, cailloux) utiliser `MultiMeshInstance3D`
- **Draw call budget** : viser < 500 draw calls pour le terrain. Monitorer via le Profiler Godot
- **Occlusion Culling** : activer le système d'occlusion Godot si le monde a beaucoup de grottes/intérieurs

### Profiling & Monitoring

- Utiliser le **Profiler Godot** intégré (Debugger → Profiler) régulièrement
- Monitorer : FPS, draw calls, vertices, physics ticks, mémoire
- Ajouter un **overlay debug in-game** toggleable (F3 style Minecraft) : FPS, chunks loaded, entities count, memory
- **Benchmarking** : script de benchmark automatique (charger X chunks, spawner Y entités, mesurer les frames)

---

## Exigences architecturales strictes

### Principes fondamentaux

- **SRP (Single Responsibility Principle)** : une classe = une responsabilité, pas de God Class
- **Composition over Inheritance** : privilégier les composants attachés aux entités plutôt que de longues chaînes d'héritage
- **Data-Driven Design** : zéro hardcoding de données. Toutes les stats, recettes, loots, blocs, biomes, quêtes, dialogues sont dans des fichiers externes (Godot Resources `.tres/.res`, JSON, ou base SQLite)
- **Open/Closed Principle** : ajouter un bloc, un mob, un skill, un biome ne doit jamais nécessiter de modifier le code existant
- **Separation of Concerns** : logique métier, rendu, réseau et UI sont strictement séparés
- **Event-Driven / Observer** : communication inter-systèmes via signaux C# typés ou EventBus, jamais de références croisées directes entre systèmes
- **Dependency Injection** : les systèmes reçoivent leurs dépendances, ils ne les cherchent pas eux-mêmes (pas de `GetNode("/root/...")` sauvage)

### Patterns à utiliser

- **Component System** : chaque entité (joueur, mob, PNJ, bloc interactif) est un assemblage de composants (`HealthComponent`, `InventoryComponent`, `AIComponent`, `CombatComponent`…)
- **State Machine / Pushdown Automata** : pour les états du joueur (idle, mining, combat, dialogue, menu) et l'IA des mobs
- **Command Pattern** : pour les actions joueur (permet undo, replay, remapping input)
- **Factory / Registry** : création d'entités, blocs, items via des registres centraux data-driven
- **Object Pooling** : pour les particules, projectiles, chunks, entités fréquemment instanciées/détruites
- **Strategy Pattern** : pour les comportements interchangeables (types de dégâts, algorithmes de génération, comportements IA)
- **Observer / Mediator** : EventBus global typé pour la communication inter-systèmes sans couplage

---

## Systèmes à concevoir

### 1. Monde & Chunks

- Génération procédurale par chunks (taille configurable, ex: 16×16×256)
- Système de LOD pour les chunks distants
- Streaming async de chunks (chargement/déchargement en threads séparés autour du joueur)
- Sérialisation/désérialisation binaire des chunks (pas JSON pour les chunks — trop lent et trop gros)
- Seed mondial + bruit Simplex multicouche via une lib C# performante (ex: `FastNoiseLite` intégré Godot ou lib custom)
- Système de biomes data-driven (température, humidité, altitude → biome)
- Structures générées (villages, donjons, ruines, grottes) via templates/blueprints

### 2. Système de blocs

- `BlockRegistry` : registre chargé au démarrage depuis des fichiers data
- Chaque type de bloc = une Resource/data entry : ID (ushort), nom, UV atlas coords, dureté, outil requis, loot table, flags (solide, transparent, liquide, émissif, interactif)
- Blocs interactifs (coffres, portes, fours, enchanteurs) via composants
- Système de lighting : propagation de lumière par bloc (BFS flood fill en thread séparé)
- Physique des liquides simplifiée (cellular automata)
- Données bloc stockées en flat array (`ushort[]`) — compact et cache-friendly

### 3. Entités — Joueur

- Assemblage de composants : Movement, Camera, Health, Stamina, Mana, Inventory, Equipment, Stats, Skills, QuestJournal, Interaction
- Contrôleur première/troisième personne avec toggle
- Input remappable via `InputMap` + Command Pattern
- Animations via `AnimationTree` (state machine)
- `CharacterBody3D` pour le mouvement (built-in Godot, optimisé)

### 4. Système RPG — Stats & Progression

- Attributs de base configurables via data (Force, Agilité, Intelligence, Constitution, Chance…)
- Système de niveaux avec courbe d'XP paramétrable
- Points de compétence à distribuer
- Classes/sous-classes définies en data (Guerrier, Mage, Ranger, Artisan…)
- Arbre de talents/compétences par classe, chargé depuis des fichiers
- Buffs/Debuffs en tant que composants temporaires (durée, stacking rules, icônes)
- Formules de calcul configurables, pas hardcodées

### 5. Système de combat

- Combat temps réel
- Types de dégâts multiples data-driven (physique, feu, glace, poison, arcane…)
- Formule de dégâts configurable (expression évaluée ou strategy pattern)
- Hitbox/Hurtbox via `Area3D` (léger, pas de physics simulation)
- Cooldowns, combos, skills actifs
- Aggro / threat system pour les mobs
- Effets de statut (stun, slow, burn, bleed…) en tant que composants empilables

### 6. Inventaire & Crafting

- Inventaire générique réutilisable (joueur, coffres, PNJ marchands)
- Slots avec filtres (type d'item, poids max)
- Drag & drop UI
- Crafting data-driven : recettes chargées depuis fichiers (inputs → output + conditions : station, skill min, classe…)
- Catégories de crafting (forge, alchimie, cuisine, enchantement…)
- File d'attente de crafting avec temps de fabrication

### 7. Système d'items & loot

- `ItemRegistry` : chaque item = data entry (ID, nom, icône, type, stack max, rareté, stats, effets)
- Rareté avec code couleur (Common → Legendary)
- Items procéduraux : stats aléatoires avec affixes/suffixes
- Loot tables data-driven par mob, coffre, bloc (poids, quantité min/max, conditions)
- Équipement avec slots (tête, torse, jambes, pieds, mains, accessoires)
- Durabilité et réparation

### 8. IA des mobs & PNJ

- Behavior Tree ou GOAP pour l'IA (implémenté en C# pur, pas de plugin GDScript)
- Composants IA interchangeables : Patrol, Chase, Flee, Attack, Idle, Wander
- Perception (vue, ouïe) via raycasts et zones `Area3D` — limiter la fréquence des checks (pas chaque frame, chaque 0.2-0.5s)
- Spawner system data-driven (conditions : biome, heure, altitude, lumière)
- PNJ : routines quotidiennes, dialogues, réputation
- Mobs définis en data
- **Budget IA** : limiter le nombre de mobs avec IA active (les mobs distants sont en sommeil)

### 9. Système de quêtes

- Quêtes définies en data : objectifs, récompenses, prérequis, dialogues liés
- Types d'objectifs : kill, collect, deliver, explore, craft, escort, interact
- Quêtes chaînées avec conditions de branchement
- Journal de quêtes avec tracking et marqueurs
- Quêtes procédurales (daily quests, bounties)

### 10. Dialogues & PNJ

- Système de dialogues branching chargé depuis des fichiers
- Conditions sur les nœuds (niveau, quête, réputation, items)
- Effets sur les nœuds (donner/retirer item, lancer quête, modifier réputation)
- Portraits et émotions

### 11. UI / HUD

- Séparation stricte UI / logique métier (MVVM ou Observer : le UI observe, ne modifie pas directement)
- **UI en Godot Control nodes** (pas de UI 3D sauf HUD in-world) — les Control nodes sont optimisés pour le 2D
- HUD : barres de vie/mana/stamina, hotbar, mini-map, buffs
- Menus : inventaire, crafting, carte, journal, talents, équipement, options
- Tooltips riches (stats comparées)
- Notifications / toasts
- **UI pooling** : pour les listes longues (inventaire, crafting), ne pas instancier un node par item visible — recycler les éléments UI

### 12. Audio

- AudioManager centralisé
- Musique adaptative par contexte (biome, combat, donjon, nuit)
- Sons 3D spatialisés (`AudioStreamPlayer3D`)
- Sound pools pour varier et éviter la saturation des bus audio
- **Limiter les `AudioStreamPlayer3D` simultanés** : budget audio (ex: max 32 sources 3D actives)

### 13. Sauvegarde & Persistance

- Sérialisation binaire pour les chunks (performance et taille)
- Format JSON ou MessagePack pour les données joueur/quêtes
- Autosave configurable
- Multi-slots
- Format versionné pour la rétrocompatibilité

### 14. Réseau / Multijoueur (préparation architecturale)

- Séparer dès le départ : logique autoritative (serveur) vs rendu (client)
- Solo = serveur local (même code path)
- Inputs prédictifs côté client
- Synchronisation d'état chunks, entités, inventaires
- API réseau abstraite (interface `INetworkTransport`) pour swapper ENet, WebSocket, Steam Networking
- **Bande passante** : ne synchroniser que les deltas (blocs modifiés, pas le chunk entier)

### 15. Cycle jour/nuit & Météo

- Horloge monde configurable
- `DirectionalLight3D` + `WorldEnvironment` pilotés par l'horloge
- Météo (pluie, neige, tempête, brouillard) avec effets gameplay
- Particules météo via `GPUParticles3D` (rendu GPU, pas de charge CPU)

### 16. Système de construction avancé

- Preview fantôme (mesh semi-transparent) avant placement
- Rotation de blocs
- Blueprints : sauvegarder/charger des structures
- Zones protégées (claim system pour le multijoueur)

### 17. Modding & Extensibilité

- Architecture pensée pour le modding dès le départ
- Chargement dynamique de data packs (nouveaux blocs, items, mobs, quêtes)
- API de modding documentée
- Système de hooks sur les événements du jeu

---

## Architecture multi-projets C# (Solution .sln)

### Philosophie

Le code est découpé en **plusieurs assemblies C# (`.csproj`)** au sein d'une même solution. Les projets "Core" et "métier" sont des **class libraries pures** sans aucune dépendance Godot. Seul le projet principal (`Game`) et les projets de bridge/adapters référencent Godot. Cela permet de :

- **Tester unitairement** toute la logique métier sans lancer Godot
- **Remplacer le moteur** si nécessaire (la logique ne dépend pas de Godot)
- **Compiler indépendamment** chaque module — un changement dans le crafting ne recompile pas le monde voxel
- **Forcer les frontières** : si un projet n'a pas la référence, il ne peut physiquement pas coupler
- **Faciliter le modding** : les moddeurs référencent les libs publiques sans toucher au code moteur

### Graphe de dépendances

```
┌─────────────────────────────────────────────────────────┐
│                    CraftRPG.Game                        │
│             (Godot project — point d'entrée)            │
│          Référence TOUS les projets ci-dessous          │
│     Contient : Scenes, bootstrapper, composition root   │
└──────────┬──────────┬──────────┬──────────┬─────────────┘
           │          │          │          │
     ┌─────▼───┐ ┌────▼────┐ ┌──▼───┐ ┌───▼────────┐
     │ .Godot  │ │ .Godot  │ │.Godot│ │  .Godot    │
     │ .World  │ │.Entities│ │ .UI  │ │  .Network  │
     │ Bridge  │ │ Bridge  │ │Bridge│ │  Bridge    │
     └────┬────┘ └────┬────┘ └──┬───┘ └─────┬──────┘
          │           │         │            │
   ┌──────▼──┐  ┌─────▼───┐    │     ┌──────▼──────┐
   │  .World │  │.Entities│    │     │  .Network   │
   │ (pure)  │  │ (pure)  │    │     │  (pure)     │
   └────┬────┘  └────┬────┘    │     └──────┬──────┘
        │            │         │            │
   ┌────▼────────────▼─────────▼────────────▼──┐
   │              .RPG  (pure)                  │
   │   Stats, Combat, Skills, Buffs, Loot       │
   └─────────────────┬────────────────────────-─┘
                     │
   ┌─────────────────▼─────────────────────────┐
   │             .Core  (pure)                  │
   │  EventBus, Interfaces, Registry<T>,        │
   │  ObjectPool, DataLoader, Math, Extensions  │
   └───────────────────────────────────────────┘
```

**Règle d'or :** les flèches pointent vers le bas uniquement. Un projet ne peut jamais référencer un projet au-dessus de lui.

---

### Détail de chaque projet

#### 1. `CraftRPG.Core` — Class Library (aucune dépendance externe)

Le socle partagé par tous les autres projets. Zéro dépendance, même pas Godot.

```
CraftRPG.Core/
├── CraftRPG.Core.csproj
├── Events/
│   ├── IEventBus.cs              # Interface du bus d'événements
│   ├── EventBus.cs               # Implémentation pub/sub typé
│   └── GameEvents.cs             # Catalogue de tous les événements (structs)
├── Registry/
│   ├── IRegistry.cs              # Interface générique Registry<TKey, TValue>
│   └── Registry.cs               # Implémentation avec chargement data-driven
├── DataLoading/
│   ├── IDataLoader.cs            # Interface chargement de données
│   ├── JsonDataLoader.cs         # Implémentation JSON
│   └── DataPath.cs               # Résolution de chemins data
├── Pooling/
│   ├── IObjectPool.cs
│   └── ObjectPool.cs
├── StateMachine/
│   ├── IState.cs
│   ├── IStateMachine.cs
│   └── StateMachine.cs
├── Command/
│   ├── ICommand.cs
│   └── CommandQueue.cs
├── Math/
│   ├── FastNoise.cs              # Bruit Simplex/Perlin standalone
│   ├── VoxelMath.cs              # Index conversions, directions
│   └── ChunkCoord.cs             # Struct position chunk
├── DI/
│   ├── IServiceLocator.cs
│   └── ServiceLocator.cs
├── Interfaces/
│   ├── ITickable.cs              # Update logique (pas lié à _Process)
│   ├── ISaveable.cs
│   └── IIdentifiable.cs
└── Extensions/
    ├── CollectionExtensions.cs
    └── MathExtensions.cs
```

#### 2. `CraftRPG.RPG` — Class Library → dépend de `Core`

Toute la logique RPG pure : stats, combat, crafting, items, quêtes, dialogues. Aucune notion de rendu, aucune notion de voxel.

```
CraftRPG.RPG/
├── CraftRPG.RPG.csproj
├── Stats/
│   ├── StatDefinition.cs         # Données : nom, min, max, formule
│   ├── StatContainer.cs          # Runtime : valeurs courantes + modifiers
│   ├── StatModifier.cs           # Flat, Percent, Multiplicative
│   └── AttributeSet.cs           # Force, Agi, Int, Con, Luck
├── Leveling/
│   ├── ExperienceCurve.cs        # Courbe XP configurable
│   ├── LevelSystem.cs
│   └── SkillPointAllocator.cs
├── Classes/
│   ├── ClassDefinition.cs        # Data : nom, stats de base, skills débloqués
│   ├── ClassRegistry.cs
│   └── TalentTree.cs
├── Combat/
│   ├── DamageType.cs             # Enum/data : physique, feu, glace, poison…
│   ├── DamageCalculator.cs       # Formule configurable via strategy
│   ├── IDamageFormula.cs
│   ├── HitResult.cs              # Struct résultat (dégâts, crit, type, source)
│   ├── StatusEffect.cs           # Burn, Slow, Stun… data-driven
│   ├── StatusEffectContainer.cs  # Gère stacking, durée, tick
│   └── ThreatTable.cs            # Aggro management
├── Skills/
│   ├── SkillDefinition.cs        # Data : coût mana, cooldown, effets, animation key
│   ├── SkillRegistry.cs
│   ├── Cooldown.cs
│   └── SkillExecutor.cs
├── Items/
│   ├── ItemDefinition.cs         # Data : ID, nom, type, stack, rareté, stats
│   ├── ItemRegistry.cs
│   ├── ItemInstance.cs            # Runtime : durabilité, affixes roulés
│   ├── ItemRarity.cs
│   ├── Affix.cs                  # Préfixes/suffixes procéduraux
│   ├── AffixPool.cs
│   ├── EquipmentSlot.cs
│   ├── EquipmentSet.cs           # Gère les slots équipés + bonus set
│   └── LootTable.cs              # Weighted random + conditions
├── Inventory/
│   ├── IInventory.cs
│   ├── Inventory.cs              # Générique : joueur, coffre, marchand
│   ├── InventorySlot.cs
│   └── SlotFilter.cs             # Filtres par type, poids…
├── Crafting/
│   ├── RecipeDefinition.cs       # Inputs, output, station, skill requis
│   ├── RecipeRegistry.cs
│   ├── CraftingCategory.cs       # Forge, alchimie, cuisine…
│   ├── CraftingQueue.cs          # File d'attente avec temps
│   └── CraftingValidator.cs      # Vérifie conditions
├── Quests/
│   ├── QuestDefinition.cs        # Objectifs, rewards, prérequis
│   ├── QuestObjective.cs         # Kill, collect, deliver, explore…
│   ├── QuestState.cs             # NotStarted, Active, Completed, Failed
│   ├── QuestJournal.cs           # Runtime : quêtes actives du joueur
│   ├── QuestRegistry.cs
│   └── ProceduralQuestGenerator.cs
├── Dialogue/
│   ├── DialogueTree.cs           # Arbre de nœuds chargé depuis data
│   ├── DialogueNode.cs           # Texte, choix, conditions, effets
│   ├── DialogueCondition.cs      # Level, quest, reputation, item check
│   └── DialogueEffect.cs         # Give item, start quest, change rep
├── Reputation/
│   ├── FactionDefinition.cs
│   ├── ReputationTracker.cs
│   └── FactionRegistry.cs
└── Buffs/
    ├── BuffDefinition.cs         # Data : durée, effet, stacking, icône
    ├── BuffInstance.cs            # Runtime avec timer
    └── BuffContainer.cs          # Gère les buffs actifs sur une entité
```

#### 3. `CraftRPG.World` — Class Library → dépend de `Core`

Tout ce qui concerne le monde voxel : données de chunks, génération, meshing, biomes, lighting. Aucune dépendance Godot — les meshes sont des tableaux de vertices/indices/UVs bruts.

```
CraftRPG.World/
├── CraftRPG.World.csproj
├── Blocks/
│   ├── BlockDefinition.cs        # Data : ID(ushort), flags, dureté, UV coords, loot ref
│   ├── BlockFlags.cs             # [Flags] enum : Solid, Transparent, Liquid, Emissive, Interactable
│   ├── BlockRegistry.cs
│   └── BlockInteraction.cs       # Résultat d'interaction (loot, transform, signal)
├── Chunks/
│   ├── ChunkData.cs              # ushort[] flat array + accessors x,y,z
│   ├── ChunkState.cs             # Enum : Unloaded, Loading, Loaded, Meshed, Active
│   ├── ChunkManager.cs           # Gère le pool de chunks, loading/unloading
│   └── ChunkSerializer.cs        # Sérialisation binaire + compression (RLE ou LZ4)
├── Generation/
│   ├── IWorldGenerator.cs
│   ├── WorldGenerator.cs         # Orchestrateur : biome → heightmap → blocs → structures
│   ├── IBiomeProvider.cs
│   ├── BiomeDefinition.cs        # Data : température, humidité, blocs surface/sous-sol, végétation
│   ├── BiomeRegistry.cs
│   ├── HeightmapGenerator.cs     # Bruit multicouche
│   ├── CaveCarver.cs
│   ├── OreDistributor.cs
│   └── StructurePlacer.cs        # Place les structures depuis des templates
├── Structures/
│   ├── StructureTemplate.cs      # Blueprint de structure (tableau de blocs relatifs)
│   ├── StructureRegistry.cs
│   └── StructureRule.cs          # Conditions de placement (biome, altitude…)
├── Meshing/
│   ├── IMeshBuilder.cs
│   ├── GreedyMeshBuilder.cs      # Greedy meshing → vertices, indices, UVs, AO
│   ├── MeshData.cs               # Struct : arrays de vertices, normals, UVs, indices, colors (pour AO)
│   └── MeshUtils.cs              # Face directions, UV lookup depuis atlas
├── Lighting/
│   ├── LightingEngine.cs         # BFS flood fill pour sunlight + block light
│   ├── LightData.cs              # Stockage compact (4 bits sun + 4 bits block par voxel)
│   └── LightPropagator.cs
├── Liquids/
│   ├── LiquidSimulator.cs        # Cellular automata
│   └── LiquidData.cs             # Niveau de liquide par bloc
└── Spatial/
    ├── WorldPosition.cs          # Struct : position absolue monde
    ├── ChunkPosition.cs          # Struct : position chunk (int x, z)
    └── LocalPosition.cs          # Struct : position locale dans un chunk (byte x, y, z)
```

#### 4. `CraftRPG.Entities` — Class Library → dépend de `Core`, `RPG`

Logique des entités (joueur, mobs, PNJ) sans aucune dépendance Godot. Définit les composants logiques et les systèmes d'IA.

```
CraftRPG.Entities/
├── CraftRPG.Entities.csproj
├── Components/
│   ├── HealthComponent.cs
│   ├── StaminaComponent.cs
│   ├── ManaComponent.cs
│   ├── MovementData.cs           # Vitesse, gravité, jump — données pures
│   ├── CombatComponent.cs        # Relie à DamageCalculator, weapon, skills
│   ├── InventoryComponent.cs     # Wrapper sur Inventory de RPG
│   ├── EquipmentComponent.cs
│   ├── StatsComponent.cs         # Wrapper sur StatContainer + AttributeSet
│   ├── QuestComponent.cs         # Wrapper sur QuestJournal
│   ├── BuffComponent.cs          # Wrapper sur BuffContainer
│   └── InteractionComponent.cs   # Range d'interaction, cible courante
├── AI/
│   ├── BehaviorTree/
│   │   ├── IBTNode.cs            # Interface nœud BT
│   │   ├── BTSelector.cs
│   │   ├── BTSequence.cs
│   │   ├── BTCondition.cs
│   │   ├── BTAction.cs
│   │   └── BTStatus.cs           # Running, Success, Failure
│   ├── Actions/
│   │   ├── PatrolAction.cs
│   │   ├── ChaseAction.cs
│   │   ├── FleeAction.cs
│   │   ├── AttackAction.cs
│   │   ├── WanderAction.cs
│   │   └── IdleAction.cs
│   ├── Perception/
│   │   ├── PerceptionData.cs     # Rayon vue, rayon ouïe, angle FOV
│   │   └── PerceptionResult.cs   # Liste des cibles perçues
│   └── Spawning/
│       ├── SpawnRule.cs           # Conditions : biome, heure, altitude, lumière
│       └── SpawnTable.cs         # Mob + poids + quantité
├── Definitions/
│   ├── MobDefinition.cs          # Data : stats, loot table, AI preset, model key
│   ├── MobRegistry.cs
│   ├── NPCDefinition.cs          # Data : dialogues, routine, faction
│   ├── NPCRegistry.cs
│   └── NPCSchedule.cs            # Routine quotidienne (heure → action + lieu)
└── Player/
    ├── PlayerData.cs             # Agrège tous les composants du joueur
    └── InputAction.cs            # Enum/struct des actions possibles (Command Pattern)
```

#### 5. `CraftRPG.Network` — Class Library → dépend de `Core`

Abstraction réseau pure. Aucune implémentation de transport ici, juste les interfaces et le protocole.

```
CraftRPG.Network/
├── CraftRPG.Network.csproj
├── INetworkTransport.cs          # Interface : Send, Receive, Connect, Disconnect
├── IPacket.cs
├── PacketRegistry.cs             # ID → type de packet
├── Packets/
│   ├── ChunkDataPacket.cs
│   ├── BlockChangePacket.cs
│   ├── EntityMovePacket.cs
│   ├── InventoryUpdatePacket.cs
│   └── ChatMessagePacket.cs
├── Serialization/
│   ├── PacketReader.cs           # Lecture binaire
│   └── PacketWriter.cs           # Écriture binaire
├── Authority/
│   ├── IServerAuthority.cs       # Valide les actions côté serveur
│   └── ClientPrediction.cs       # Logique de prédiction input
└── Sync/
    ├── DeltaCompressor.cs        # Ne sync que les changements
    └── InterpolationBuffer.cs    # Buffer pour smooth des entités réseau
```

#### 6. `CraftRPG.Godot.World` — Class Library Godot → dépend de `Core`, `World`

Bridge entre la logique monde pure et Godot. Transforme les `MeshData` en `ArrayMesh`, gère les `MeshInstance3D`, le threading Godot, les `StaticBody3D` des chunks.

```
CraftRPG.Godot.World/
├── CraftRPG.Godot.World.csproj   # Référence GodotSharp
├── ChunkNode.cs                  # Node3D : porte le MeshInstance3D + collision
├── ChunkMeshApplier.cs           # Convertit MeshData → ArrayMesh Godot
├── ChunkCollisionBuilder.cs      # Convertit faces solides → ConcavePolygonShape3D
├── WorldNode.cs                  # Node3D racine du monde, orchestre ChunkManager
├── ChunkLoadingScheduler.cs      # Budget par frame, priorité distance, CallDeferred
├── VoxelRaycast.cs               # Raycast custom blocs (pas de physics engine)
└── WorldEnvironmentController.cs # Jour/nuit, météo, DirectionalLight, sky
```

#### 7. `CraftRPG.Godot.Entities` — Class Library Godot → dépend de `Core`, `RPG`, `Entities`

Bridge entités. Transforme les composants logiques en nodes Godot (CharacterBody3D, AnimationTree, Area3D…).

```
CraftRPG.Godot.Entities/
├── CraftRPG.Godot.Entities.csproj
├── PlayerController.cs           # CharacterBody3D, input mapping, camera
├── PlayerAnimator.cs             # AnimationTree state machine
├── MobNode.cs                    # CharacterBody3D + AI bridge
├── NPCNode.cs                    # CharacterBody3D + interaction zone
├── HitboxNode.cs                 # Area3D pour les hitbox/hurtbox
├── EntitySpawnerNode.cs          # Gère le spawn/despawn avec pooling
├── PerceptionSensor.cs           # Area3D + raycasts pour la perception IA
└── EntityPool.cs                 # Object pool de nodes entité Godot
```

#### 8. `CraftRPG.Godot.UI` — Class Library Godot → dépend de `Core`, `RPG`

Toute l'UI Godot. Observe les données métier, ne les modifie jamais directement (MVVM / Observer).

```
CraftRPG.Godot.UI/
├── CraftRPG.Godot.UI.csproj
├── HUD/
│   ├── HUDController.cs          # Barre vie, mana, stamina, hotbar, buffs
│   ├── Hotbar.cs
│   ├── BuffBar.cs
│   ├── Minimap.cs
│   └── DebugOverlay.cs           # F3 : FPS, chunks, entities, memory
├── Menus/
│   ├── InventoryUI.cs            # Drag & drop, slots poolés
│   ├── CraftingUI.cs
│   ├── EquipmentUI.cs
│   ├── QuestJournalUI.cs
│   ├── TalentTreeUI.cs
│   ├── WorldMapUI.cs
│   ├── DialogueUI.cs
│   ├── PauseMenuUI.cs
│   └── OptionsUI.cs
├── Common/
│   ├── TooltipController.cs      # Tooltips riches, comparaison stats
│   ├── ToastNotification.cs      # Loot, level up, quête complétée
│   ├── UISlotPool.cs             # Recycling des éléments UI listes
│   └── DragDropManager.cs
└── ViewModels/
    ├── InventoryViewModel.cs     # Observe Inventory, expose pour l'UI
    ├── StatsViewModel.cs
    └── QuestViewModel.cs
```

#### 9. `CraftRPG.Godot.Network` — Class Library Godot → dépend de `Core`, `Network`

Implémentations concrètes des transports réseau via les API Godot.

```
CraftRPG.Godot.Network/
├── CraftRPG.Godot.Network.csproj
├── ENetTransport.cs              # INetworkTransport via ENet Godot
├── WebSocketTransport.cs         # INetworkTransport via WebSocket
└── NetworkManager.cs             # Node autoload, orchestre connexion/sync
```

#### 10. `CraftRPG.Game` — Godot Project (point d'entrée)

Le projet Godot principal. Contient les scènes, le bootstrapper, la composition root, les assets. Référence TOUS les autres projets.

```
CraftRPG.Game/
├── CraftRPG.Game.csproj          # Référence tous les .csproj ci-dessus
├── project.godot
├── .editorconfig
│
├── Bootstrap/
│   ├── GameBootstrapper.cs       # Autoload : initialise tous les systèmes, DI, registres
│   ├── CompositionRoot.cs        # Câble les dépendances (quel IDataLoader, quel INetworkTransport…)
│   └── GameConfig.cs             # Config globale chargée depuis fichier
│
├── Scenes/
│   ├── Main.tscn
│   ├── World/
│   ├── UI/
│   └── Entities/
│
├── Data/                         # Fichiers data-driven (JSON / .tres)
│   ├── Blocks/
│   ├── Items/
│   ├── Recipes/
│   ├── Mobs/
│   ├── NPCs/
│   ├── Biomes/
│   ├── Quests/
│   ├── Dialogues/
│   ├── Classes/
│   ├── Skills/
│   ├── Buffs/
│   ├── LootTables/
│   ├── Structures/
│   └── Factions/
│
├── Resources/
│   ├── Materials/
│   ├── Shaders/
│   │   ├── voxel_terrain.gdshader    # Atlas UV, vertex AO, vent végétation
│   │   └── liquid.gdshader
│   └── Themes/
│
├── Assets/
│   ├── Textures/
│   │   └── Atlas/
│   │       └── blocks_atlas.png      # Toutes les textures blocs en un seul atlas
│   ├── Models/
│   ├── Audio/
│   │   ├── Music/
│   │   └── SFX/
│   └── Fonts/
│
└── Addons/
```

#### 11. `CraftRPG.Tests` — xUnit Test Project → dépend de `Core`, `RPG`, `World`, `Entities`, `Network`

Tests unitaires et d'intégration sur toute la logique pure. Tourne sans Godot.

```
CraftRPG.Tests/
├── CraftRPG.Tests.csproj
├── Core/
│   ├── EventBusTests.cs
│   ├── RegistryTests.cs
│   ├── StateMachineTests.cs
│   └── ObjectPoolTests.cs
├── RPG/
│   ├── DamageCalculatorTests.cs
│   ├── InventoryTests.cs
│   ├── CraftingValidatorTests.cs
│   ├── LootTableTests.cs
│   ├── BuffStackingTests.cs
│   └── QuestJournalTests.cs
├── World/
│   ├── ChunkDataTests.cs
│   ├── GreedyMeshBuilderTests.cs
│   ├── BiomeSelectionTests.cs
│   ├── LightingEngineTests.cs
│   └── ChunkSerializerTests.cs
├── Entities/
│   ├── BehaviorTreeTests.cs
│   ├── PerceptionTests.cs
│   └── SpawnRuleTests.cs
└── Network/
    ├── PacketSerializationTests.cs
    └── DeltaCompressorTests.cs
```

---

### Fichier Solution (`.sln`)

```
CraftRPG.sln
│
├── src/
│   ├── CraftRPG.Core/                 # 0 dépendance
│   ├── CraftRPG.RPG/                  # → Core
│   ├── CraftRPG.World/                # → Core
│   ├── CraftRPG.Entities/             # → Core, RPG
│   ├── CraftRPG.Network/              # → Core
│   ├── CraftRPG.Godot.World/          # → Core, World           (+ GodotSharp)
│   ├── CraftRPG.Godot.Entities/       # → Core, RPG, Entities   (+ GodotSharp)
│   ├── CraftRPG.Godot.UI/             # → Core, RPG             (+ GodotSharp)
│   ├── CraftRPG.Godot.Network/        # → Core, Network         (+ GodotSharp)
│   └── CraftRPG.Game/                 # → Tout                  (+ GodotSharp)
│
└── tests/
    └── CraftRPG.Tests/                # → Core, RPG, World, Entities, Network
```

### Règles de dépendance inter-projets

| Projet | Peut référencer | Ne peut PAS référencer |
|---|---|---|
| `Core` | Rien | Tout le reste |
| `RPG` | `Core` | `World`, `Entities`, `Godot.*`, `Game` |
| `World` | `Core` | `RPG`, `Entities`, `Godot.*`, `Game` |
| `Entities` | `Core`, `RPG` | `World`, `Godot.*`, `Game` |
| `Network` | `Core` | `RPG`, `World`, `Entities`, `Godot.*`, `Game` |
| `Godot.World` | `Core`, `World` | `RPG`, `Entities`, autres `Godot.*` |
| `Godot.Entities` | `Core`, `RPG`, `Entities` | `World`, autres `Godot.*` |
| `Godot.UI` | `Core`, `RPG` | `World`, `Entities`, autres `Godot.*` |
| `Godot.Network` | `Core`, `Network` | `RPG`, `World`, `Entities`, autres `Godot.*` |
| `Game` | **Tous** | — |
| `Tests` | Tous sauf `Godot.*` et `Game` | `Godot.*`, `Game` |

### Contrainte Godot multi-csproj

Godot 4 .NET attend un seul `.csproj` dans le dossier `project.godot`. Pour le multi-projets :

- Seul `CraftRPG.Game.csproj` est à la racine du projet Godot (à côté de `project.godot`)
- Les autres `.csproj` vivent dans des dossiers `src/` en dehors du dossier Godot, ou dans des sous-dossiers avec des `ProjectReference`
- `CraftRPG.Game.csproj` contient tous les `<ProjectReference>` vers les autres projets
- Godot compilera la solution complète via le `.sln`
- Configurer Rider pour utiliser le `.sln` comme projet racine

---

## Contraintes de développement

- Toute feature doit être prototypée de façon isolée avant intégration
- Aucun singleton sauf cas strictement nécessaire (et documenté pourquoi)
- Le code doit être lisible sans explication orale
- Commits atomiques, une feature = une branche
- Profiler après chaque système majeur — ne pas attendre la fin pour optimiser
- Target minimum : **60 FPS stable** avec un render distance de 12 chunks et 50+ entités actives
- Tester sur une config modeste (pas uniquement sur une RTX 4090)

---

## Checklist performance par système

| Système | Metric cible | Technique clé |
|---|---|---|
| Chunk meshing | < 5ms par chunk | Greedy meshing + thread séparé |
| Chunk loading | 0 stutter main thread | Async + budget par frame |
| Rendu terrain | < 500 draw calls | Atlas + 1 mesh/chunk + LOD |
| Physics terrain | < 1ms/frame | Raycast custom, pas 1 collider/bloc |
| IA mobs | < 2ms total/frame | Budget IA, sleep distant, tick réduit |
| UI inventaire | 0 alloc en navigation | Pooling éléments UI |
| Lighting | < 3ms propagation | BFS en thread séparé |
| Sauvegarde chunk | < 10ms par chunk | Sérialisation binaire, compression |
