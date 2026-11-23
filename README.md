# About

A simple 3D open‑world cooperative game created for research purposes to test LLM NPCs. The project is under active development.

![](Docs/Screenshots/Menu/1.jpg)
![](Docs/Screenshots/Game/1.jpg)
![](Docs/Screenshots/Game/2.jpg)
![](Docs/Screenshots/Game/3.jpg)
![](Docs/Screenshots/Game/4.jpg)
![](Docs/Screenshots/Game/5.jpg)
![](Docs/Screenshots/Game/6.jpg)
![](Docs/Screenshots/Game/7.jpg)

# Features
## Core
* Multiplayer
* Save system
* LLM NPCs
## Gameplay
* Day-night cycle system
* Health, hunger, endurance, intoxication, inventory
* Shooting
* Crafting system
* NPCs & mutants
* Artifacts

# Multiplayer
The multiplayer uses a self-hosted model with accounts based on asymmetric encryption. Upon the first entry into the game, the program will generate an RSA key that serves as your account. To play with friends, start the game as the host and click the “Invite Players” button. The system will ask you to specify your external IP address and external port. After that, an invitation code will be generated. Your friends will only need to click the “Join Game” button and copy this code. You will likely need to forward the port. The connection will be securely encrypted.

# LLMs
## Providers
When hosting a new game or in the server settings, you can configure one of the supported LLM providers.
### Supported providers
* [Polza](https://polza.ai/). Supports payment with Russian cards
* [Claude](https://platform.claude.com)
* [Ollama (local)](https://ollama.com/). Please note that when running on a single computer, rendering and LLM will compete for resources, which will result in low FPS on many personal computers and slow token generation. For a comfortable gaming experience, it is recommended to move Ollama server to a separate computer. To do this, set the `OLLAMA_HOST` environment variable to `0.0.0.0:11434` on that computer, restart Ollama, and enter its address and port in the server settings. Please also note that the connection to the remote Ollama server is not encrypted, so it is strongly recommended to use it only on trusted local networks.

At the time of writing this file, `anthropic/claude-haiku-5.5`, being cheaper, and `google/gemini-3.8-flash`, being more expensive, seem to be the most optimal choice among cloud models.
## NPCs capabilities
* Speech (including interactions between NPCs)
* Vision
* Persistent memory
* Movement
* Transfer and receipt of items, including trade
* Using items and interacting with the world
* An individual attitude toward each other character
* Dynamic character development

# Graphics
The game uses HDRP with support for texture streaming, reflections, APV, upscalers, speed tree, etc.
## RT
There is experimental support for almost all RT technologies, but in the author’s opinion, in this project it doesn’t justify itself — with the possible exception of the SSR + mixed RT reflections combination.
There is also experimental path tracing, but in its current implementation it doesn’t look very good.
![](Docs/Screenshots/PathTracing/1.jpg)
![](Docs/Screenshots/PathTracing/2.jpg)
## DLSS
The game supports DLSS 4. Support for DLSS 5, which at the time of writing this file is distributed mainly unofficially, is not built into this repository. But you can easily patch the build yourself, for example, using this [utility](https://github.com/faisalkindi/DLSS5oneclick).
![](Docs/Screenshots/DLSS5/1.jpg)
![](Docs/Screenshots/DLSS5/2.jpg)

# Development
## Tech Stack
* Unity
* NGO
* Bouncy Castle

The navmesh and the baked lighting are not stored in the repository. After cloning, open the Map scene and bake both locally:
* Tools → Shooter → World → Bake NavMesh
* Tools → Shooter → World → Bake GI
