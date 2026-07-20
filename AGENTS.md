# AGENTS.md — OpenCode Go (mode SOLO)

Tu es le **développeur principal** du projet (tu remplaces Claude Code, qui n'est plus dans la boucle). Tu travailles seul, de la planification à l'implémentation.

## DÉMARRAGE DE SESSION (obligatoire)
1. Lire `.claude_memory.md` (memory bank : état du projet, architecture, pièges) — c'est ta mémoire persistante.
2. Lire `TASKS.md` (tickets en cours).
3. Vérifier `git status`.
4. Si tu dois interagir avec Unity : vérifier le MCP (voir section MCP).

## FIN DE TÂCHE COMPLEXE (obligatoire)
- Mettre à jour `.claude_memory.md` pour documenter tes changements (ta future session reprendra de là).
- Cocher le ticket dans `TASKS.md`, commit atomique `T-XX: résumé` ou message clair.

## RÈGLES DE TRAVAIL (héritées de CLAUDE.md)
- **Plan avant code** : pour toute demande d'architecture (hors fix syntaxe/renommage), liste les étapes + fichiers impactés, attends la validation de l'utilisateur avant d'écrire le code.
- **Jamais de suppression de fichier sans autorisation.** Traçabilité : loguer toute suppression validée ou modif majeure dans `cleanup_log.md` (Chemin, Ligne, Action).
- Code 100% complet et compilable, aucun placeholder.
- Recherches ciblées (grep/glob sur mots-clés) avant de lire un fichier entier.
- Réponses denses et factuelles, en français.
- Terminer STRICTEMENT chaque réponse par la chaîne `O_O` (convention héritée de Claude Code, adoptée en mode solo).

## MCP UNITY
- Serveur configuré dans `opencode.json` (`unityMCP` via uvx `mcpforunityserver==10.1.0`, stdio, socket Unity 6400).
- Utiliser **exclusivement** les outils MCP pour inspecter/modifier les scènes et l'éditeur. Si le MCP timeout ou est indisponible : ne pas deviner la structure, patienter et relancer (ou demander à l'utilisateur de vérifier Window > MCP for Unity = 🟢 Running en stdio/6400, pas HTTP/8080).
- **Jamais d'édition manuelle du YAML** des `.unity` / `.prefab` — uniquement via MCP.
- YAML des `.asset` : édition manuelle tolérée uniquement si la tâche/le ticket le demande explicitement (précédent : réparation UnitData).
- `.meta` : ne jamais y toucher sauf instruction explicite.
- Avant capture MCP : éditeur sans focus = frames gelées → forcer alpha/scale avant screenshot.

## ZONES INTERDITES
- `Assets/Scriptable Objects/DiscordBridge/ApiConfig.asset` (secret API, gitignored — ne jamais committer ni logger la clé).
- `Library/`, `Temp/`, `Obj/`, `Logs/`, `Builds/`.
- Scènes/prefabs en édition directe de fichier (voir MCP).

## RÈGLES ANTI-RÉGRESSION (critique — ne jamais violer)

### Réseau / DTOs
- **Newtonsoft.Json** (pas `System.Text.Json`) pour tous les DTOs.
- **`discord_id`** : renvoyé en `long` par `/link-account`, en `string` par `/player` et `/inventory`. Toujours manipuler en **`string`** côté Unity.
- **HMAC `/api/add-mana`** : le timestamp C# DOIT avoir un `.0` ajouté à la main (ex: `"1721402400.0"`) pour matcher le float Python. Sinon signature invalide.
- **Header** : `x-api-key` (minuscule).
- **MVVM strict** : les Views/UI (`DiscordBridge.UI`) ne référencent JAMAIS `DiscordBridge.Networking` ni `DiscordBridge.DTOs`. Elles écoutent les ScriptableObjects de `DiscordBridge.Data` via events `Action`. Réseau sans exceptions : tout passe par `ApiResult`.
- **Effets consommables** : utiliser `remaining_seconds` (relatif), jamais `expires_at` (décalage d'horloge).

### Async Unity 6
- **`Awaitable`** (pas `Task`). Pas de `async void` sauf handler d'event UI.

### Rendu URP
- Alpha **1.0** strict sur les fonds opaques (0.985 fuit en espace linéaire).
- Coroutines d'anim UI : `deltaTime` plafonné à 0.05s (sinon état figé après hitch/perte de focus).

### ScriptableObjects
- Un SO = un fichier. Jamais de classe `[Serializable]` héritée de `ScriptableObject` DANS un autre fichier .cs (piège `m_Script: {fileID: 0}`).

### Backend Python (`/home/taqkt/docker/discord-bridge/main.py` via SSH `192.168.10.160`, copie repo `Server/discord-bridge/main.py`)
- `/api/consume-item` : UPSERT `MAX(ancien, now)+durée` → prolonge, jamais raccourcit.
- Barème serveur autoritaire : `ITEM_DURATIONS = {shield_10m:600, boost_30m:1800, freeze_5m:300, reinforce:0}`.
- L'achat boutique **n'active PAS** l'effet — consommation via Unity uniquement.
- `boost_30m` : ×2 appliqué CÔTÉ SERVEUR après le cap (`/api/add-mana`, `/ratons`, DefenseView ; pas BossView). Ne jamais dupliquer côté client.
- Anti-triche `/api/add-mana` : nonce unique, `last_wave` monotone, fenêtre timestamp, `X-Api-Key` — ne jamais retirer ni affaiblir, ne jamais logger la clé.
- Après toute modif : `python -m py_compile main.py` doit passer avant commit.

## ÉTAT DU PROJET
- Voir `.claude_memory.md` (source de vérité détaillée). Backend : FastAPI mono-process via Cloudflare Tunnel `https://api-towerdefence.taqkt.dev`. Frontend : Unity URP, MVVM, `Assets/Scripts/DiscordBridge/`.
- Serveur en attente (« plus tard ») : Prompt 1 (retrait `/debug/*` = ticket T-01), Prompt 3 (anti-farm = T-03), Prompt 4 (`/api/battle-report` = T-02 côté client).
