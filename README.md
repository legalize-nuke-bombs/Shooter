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
When hosting a new game or in the server settings, you can configure one of the supported LLM providers. The selected model must support a context window of 256K tokens or more.
### Supported cloud providers
* [Polza](https://polza.ai/). Supports payment with Russian cards
* [Claude](https://platform.claude.com)
At the time of writing this file, `xiaomi/mimo-v2.6-flash-mopd` seems to be the solid choice among cloud models.
### Local providers
Any OpenAI-compatible local server ([llama.cpp](https://github.com/ggml-org/llama.cpp), [Ollama](https://ollama.com/), [LM Studio](https://lmstudio.ai/), ...) is supported.
<br><br>
When running on a single computer, rendering and LLM will compete for resources, which will result in low FPS on many personal computers and slow token generation. For a comfortable gaming experience, it is recommended to move the LLM server to a separate computer. To do this, make the server listen on all interfaces on that computer (`--host 0.0.0.0` for `llama-server`, the `OLLAMA_HOST` environment variable set to `0.0.0.0:11434` for Ollama, then restart it) and enter its address and port in the server settings. Set the context length of the LLM server to 256K (`-c 262144` for `llama-server`, the context length setting for Ollama).
<br><br>
Connection to the remote server is not encrypted, so it is strongly recommended to forward port via SSH or use it only on trusted local networks.
<br><br>
Local llm server will need to serve requests from completely different NPCs with different histories. This requires storing caches of many different contexts at the same time. Most standard configurations for raising a local llm server store such caches entirely in RAM, which is inefficient on home computers with a small number of RAM. Therefore, it is recommended to use a bundle of [llama.cpp](https://github.com/ggml-org/llama.cpp) + [this very simple llama proxy](https://github.com/legalize-nuke-bombs/LlamaProxy) that automatically streams caches from disk.

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
