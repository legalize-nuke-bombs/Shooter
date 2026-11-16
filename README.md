# About

3D open-world cooperative survival game with LLM integration. The project is under active development.

![](Docs/s1.jpg)
![](Docs/s2.jpg)
![](Docs/s3.jpg)
![](Docs/s4.jpg)
![](Docs/s5.jpg)

# Features
## Core
* Multiplayer game with self-hosted servers
* Save system
* LLM NPCs
## Gameplay
* Day-night cycle system
* Health, hunger, endurance, intoxication, inventory
* Shooting
* Crafting system
* NPCs & mutants
* Artifacts
## Graphics
* HDRP
* Upscalers (STP, DLSS)
* Texture streaming
* APV
* Experimental partial RTX support
## Multiplayer
* Decentralized account model based on asymmetric encryption
## LLM NPСs capabilities
* Speech (including interactions between NPCs)
* Vision
* Persistent memory
* Movement
* Transfer and receipt of items, including trade
* Using items and interacting with the world
* An individual attitude toward each other character
* Dynamic character development

# Tech Stack
* Unity
* NGO
* Bouncy Castle

# Development
The navmesh and the baked lighting are not stored in the repository. After cloning, open the Map scene and bake both locally:
* Tools → Shooter → World → Bake NavMesh
* Tools → Shooter → World → Bake GI

Baking is heavy: the GI bake peaks at about 26 GB of RAM in the editor.
In the editor, play mode with GI on stutters because of APV disk streaming; builds are not affected.

# Supported LLM providers
* Polza
* Claude
* Ollama (a very good pc is required)