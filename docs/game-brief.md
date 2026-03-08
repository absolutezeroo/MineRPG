---
stepsCompleted: [1, 2, 3, 4, 5]
inputDocuments:
  - PROJECTS.md
  - ARCHITECTURE.md
date: 2026-03-08
author: Clayton
---

# Game Brief: MineRPG

## Resume Executif

MineRPG est un jeu en monde ouvert voxel 3D qui fusionne la liberte totale d'un sandbox a la Minecraft avec la profondeur d'un RPG complet. Le joueur retrouve cette sensation magique du "je peux tout faire, je deviens qui je veux" tout en beneficiant d'un systeme de progression multi-branche, d'un combat action-RPG exigeant, d'un artisanat strategique lie aux runes, et d'un monde genere procedurallement dont la beaute et la diversite donnent une vraie envie d'explorer. Le style visuel reprend l'esthetique voxel classique sublimee par des shaders modernes, tout en maintenant des performances solides.

---

## Vision

### Constat : Ce qui manque aux jeux voxel actuels

Les jeux voxel sandbox actuels souffrent de lacunes majeures qui limitent l'experience joueur sur le long terme :

- **Minecraft** offre une liberte de construction inegalee mais son combat est rudimentaire (clic spam), sa progression quasi inexistante (pas de classes, pas de skills, pas de build), ses donjons sont des structures basiques sans veritable defi, et ses biomes manquent de vegetation, de decorations et de structures qui embellissent le paysage
- **Terraria** propose une excellente progression RPG en 2D mais ne procure pas l'immersion d'un monde 3D explorable librement
- **Hytale** promet beaucoup mais n'est toujours pas disponible et reste une inconnue
- **Cube World** avait le bon concept mais a decu par un systeme de progression qui se reinitialise entre les zones

Le resultat : aucun jeu ne combine reellement **liberte sandbox totale** + **profondeur RPG complete** + **monde visuellement epoustouflant** dans un package coherent et performant.

### Solution proposee

MineRPG comble ce vide en proposant :

- **Progression multi-branche sans restriction** — Le joueur developpe librement ses competences en magie, combat, artisanat et exploration en parallele. Pas de choix de classe force : tu construis ton propre build hybride
- **Combat action-RPG** — Parades, roulades, esquives, attaques chargees. Chaque affrontement demande du skill, pas du clic spam
- **Systeme de runes et artisanat strategique** — Les runes se trouvent en loot, se craftent et se combinent. Un joueur specialise artisan produit de meilleures runes avec un taux d'echec reduit, creant une synergie entre progression et crafting
- **Difficulte dynamique** — Les monstres deviennent plus forts au fil des jours, maintenant le challenge meme pour les joueurs experimentes. Les batisseurs et joueurs pacifiques sont proteges par des mecaniques adaptees
- **Vrais donjons** — Des donjons generes avec une hierarchie complete : mini-boss, semi-boss, boss. Une experience de donjon digne d'un RPG, pas les structures basiques de Minecraft
- **Monde vivant et decoratif** — Vegetation riche, fleurs, structures decoratives "inutiles mais jolies", diversite de biomes qui donne un vrai sentiment d'exploration
- **Visuel Minecraft + shaders** — L'esthetique voxel classique que les joueurs aiment, sublimee par des shaders modernes, sans sacrifier les performances

### Differenciateurs cles

| Differenciateur | MineRPG | Concurrence |
|---|---|---|
| Systeme de build | Multi-branche libre (guerrier + mage + artisan) | Minecraft : aucun / Terraria : lineaire |
| Combat | Action-RPG (parade, esquive, charge) | Minecraft : clic spam |
| Runes & Crafting | Lie aux competences, combinaison, taux d'echec | Minecraft : enchantement aleatoire |
| Donjons | Mini-boss → Semi-boss → Boss | Minecraft : coffres dans des couloirs |
| Difficulte | Scaling dynamique temporel | Minecraft : statique |
| Monde | Vegetation riche, structures decoratives, diversite | Minecraft : biomes plats et vides |
| Visuel | Voxel classique + shaders performants | Minecraft : necessite mods |

---

## Joueurs Cibles

### Public principal

**Tranche d'age :** 7 ans et plus — accessible aux jeunes joueurs tout en offrant une profondeur qui retient les adultes

**Joueurs vises :** Les fans de sandbox voxel (Minecraft, Terraria), les amateurs de jeux de construction et gestion (city builders), et les joueurs RPG qui cherchent une experience action dans un monde ouvert

### Profils de joueurs

**Le Combattant**
Joueur type Terraria / Dark Souls lite. Il cherche le defi, les donjons, le loot rare et les boss. Il investit dans ses competences de combat, perfectionne ses parades et esquives, et chasse les runes les plus puissantes. Le scaling dynamique de difficulte le garde engage sur le long terme — le monde ne devient jamais trivial.

**Le Batisseur**
Joueur type Minecraft creatif. Il construit des structures ambitieuses, decore ses creations, et veut que le monde soit beau. Le mode pacifique lui permet de jouer sans la pression des mobs. Les structures decoratives et la vegetation riche du monde l'inspirent. En multijoueur, il est le coeur du village partage.

**L'Explorateur**
Joueur pousse par la curiosite. Il veut voir ce qu'il y a derriere la prochaine montagne, decouvrir un nouveau biome, tomber sur une ruine inattendue. La diversite des biomes, la vegetation, les structures decoratives du monde genere sont la pour lui. Chaque session est une decouverte.

**L'Artisan**
Joueur type gestion / optimisation. Il maitrise le systeme de crafting, specialise ses competences d'artisanat pour produire les meilleures runes avec un faible taux d'echec. Il fournit le groupe en equipement optimise. Son expertise est valorisee en multijoueur ou il devient indispensable.

**Le Chef de Village**
Joueur type city builder / gestion. En multijoueur, il organise le village partage avec ses amis, gere les ressources, recrute et coordonne. Il trouve son plaisir dans la construction collective et le developpement de la communaute.

### Modes de jeu

| Mode | Description |
|---|---|
| Solo | Experience complete — progression, donjons, construction, exploration |
| Co-op / Multi PvE | Village partage entre amis, donjons en groupe, gestion collective |
| PvP | Affrontements entre joueurs avec le systeme de combat action-RPG |
| Pacifique | Construction et exploration sans agression des mobs |

### Parcours joueur

- **Decouverte** — Le joueur lance le jeu, decouvre un monde voxel visuellement riche et ressent la liberte totale
- **Premiers pas** — Il mine, craft ses premiers outils, decouvre les bases du combat (parade, esquive)
- **Specialisation** — Il commence a investir dans ses branches de competences preferees et trouve ses premieres runes
- **Moment "aha!"** — Il combine des runes, vainc son premier mini-boss en donjon, ou construit sa premiere base decoree
- **Long terme** — Le scaling de difficulte maintient le challenge, le village multi grandit, les donjons de haut niveau et les boss motivent la progression

---

## Metriques de Succes

### Philosophie

MineRPG est un projet passion. Le succes ne se mesure pas en revenus mais en **joueurs qui reviennent chaque jour**, en **contenu partage par la communaute**, et en **mods qui font vivre le jeu** au-dela de sa version de base.

### Succes joueur

| Metrique | Objectif | Signal positif |
|---|---|---|
| Retention quotidienne | Le joueur revient chaque jour pour 30 min+ | Plus valorise qu'une session de 4h hebdomadaire |
| Diversite d'activites | Le joueur alterne entre combat, construction, exploration, crafting | Il ne s'ennuie pas, chaque session offre quelque chose |
| Progression ressentie | Chaque session apporte un sentiment d'avancement | Nouvelle rune, nouveau donjon, nouveau biome decouvert |
| Partage communautaire | Les joueurs partagent leurs creations, builds et moments | Videos, screenshots, guides, streams |

### Succes communaute

| Metrique | Objectif | Signal positif |
|---|---|---|
| Modding actif | Une communaute de moddeurs cree du contenu | Nouveaux blocs, mobs, donjons, biomes par la communaute |
| Serveurs multijoueur | Des villages actifs avec des groupes d'amis | Villages qui grandissent, joueurs qui collaborent |
| Contenu cree par les joueurs | Guides, videos, fan art, streams | Le jeu inspire les gens a creer au-dela du jeu |
| Bouche a oreille | Les joueurs recommandent le jeu a leurs amis | Croissance organique de la communaute |

### Succes technique

| Metrique | Objectif | Seuil |
|---|---|---|
| FPS stable | 60 FPS constants | Sur un PC moyen (GPU milieu de gamme, 16 Go RAM) |
| Pas de stutters | Chargement de chunks sans saccades | Budget frame respecte, async obligatoire |
| Temps de chargement | Entree en jeu rapide | < 30 secondes du lancement au gameplay |
| Stabilite | Pas de crashes | Sessions de 30 min+ sans plantage |

### Succes game design

| Metrique | Signal positif | Signal d'alerte |
|---|---|---|
| Equilibre combat | Les parades/esquives sont utiles et satisfaisantes | Le joueur peut tout tanker sans esquiver |
| Scaling difficulte | Le monde reste un defi au fil des jours | Le joueur one-shot tout apres quelques heures |
| Systeme de runes | Les joueurs experimentent des combinaisons | Tout le monde utilise la meme build optimal |
| Donjons | Les joueurs refont les donjons avec plaisir | Les donjons se ressemblent tous |
| Diversite des builds | Plusieurs builds viables (mage, guerrier, hybride, artisan) | Un seul build domine |

---

## Scope MVP

### Philosophie MVP

Le MVP de MineRPG doit repondre a une question simple : **"Est-ce que le monde donne envie d'explorer, et est-ce que le combat donne envie de se battre ?"** Si le joueur est emerveille par le monde et accroche par les donjons, le reste (competences, runes, multi) viendra naturellement par-dessus.

### Fonctionnalites essentielles — Par priorite

**Priorite 1 — Monde vivant et decoratif**
- Biomes diversifies avec vegetation riche (fleurs, herbes, arbustes, arbres varies)
- Structures decoratives generees (ruines, arches, formations rocheuses, petits autels)
- Decorations de paysage qui rendent chaque biome unique et memorable
- Sentiment d'exploration : chaque direction offre quelque chose de nouveau
- *Deja en place :* terrain procedural 38 biomes, generation de base, greedy meshing

**Priorite 2 — Donjons avec hierarchie de boss**
- Generation procedurale de donjons avec salles, couloirs, pieges
- Hierarchie de boss : mini-boss, semi-boss, boss
- Loot de donjon interessant (equipement, ressources rares)
- Difficulte progressive au sein d'un meme donjon
- Variete de donjons selon les biomes

**Priorite 3 — Combat action-RPG**
- Systeme de parade avec timing et feedback
- Roulade d'esquive avec i-frames
- Attaques chargees avec des degats accrus
- Mobs avec des patterns d'attaque lisibles et varies
- Feedback visuel et sonore satisfaisant (hit, parade reussie, esquive)

**Priorite 4 — Shaders et polish visuel**
- Shader voxel avec ambient occlusion par vertex
- Eclairage dynamique (cycle jour/nuit)
- Effets d'eau et de vegetation (vent sur les feuilles)
- Post-processing leger (bloom, tonemap) sans impacter les performances
- Cible : esthetique Minecraft sublimee, 60 FPS sur PC moyen

**Priorite 5 — Fondations modding**
- Architecture data-driven des le depart (blocs, items, mobs en JSON)
- API de modding documentee pour ajouter du contenu
- Chargement dynamique de data packs
- Systeme de hooks sur les evenements du jeu
- *Deja en place :* registres data-driven, chargement JSON

### Hors scope MVP — Pour plus tard

| Systeme | Raison du report | Quand |
|---|---|---|
| Competences multi-branche | Necessite un systeme de combat stable d'abord | Post-MVP v2 |
| Systeme de runes | Depend des competences et du crafting avance | Post-MVP v2 |
| Multijoueur (co-op, PvP) | Architecture solo d'abord, reseau ensuite | Post-MVP v3 |
| Gestion de village | Necessite le multijoueur | Post-MVP v3 |
| Mode pacifique | Simple a ajouter une fois le combat en place | Post-MVP v2 |
| Scaling dynamique difficulte | Necessite un equilibrage combat solide | Post-MVP v2 |
| Quetes et dialogues | Contenu narratif apres les systemes de base | Post-MVP v3 |

### Criteres de succes MVP

- Le joueur explore le monde pendant 30 min sans s'ennuyer visuellement
- Les donjons sont rejouables et offrent un defi satisfaisant
- Le combat donne une sensation de maitrise (pas du clic spam)
- 60 FPS stable sur un PC milieu de gamme
- Un moddeur peut ajouter un nouveau bloc via un fichier JSON sans toucher au code

### Vision post-MVP

| Phase | Contenu | Objectif |
|---|---|---|
| **v2 — Profondeur RPG** | Competences multi-branche, runes, crafting avance, mode pacifique, scaling difficulte | Le joueur construit son build unique |
| **v3 — Communaute** | Multijoueur co-op/PvP, villages partages, gestion collective | Jouer ensemble |
| **v4 — Contenu narratif** | Quetes, dialogues, PNJ, reputation, factions | Le monde raconte une histoire |
| **v5 — Ecosysteme** | Modding complet, serveurs communautaires, outils de creation | La communaute fait vivre le jeu |