# Cleanup Log

Suivi des suppressions / modifications majeures à nettoyer en fin de projet.
Format : Fichier / Ligne / Action.

## 2026-07-18 — Bascule MCP officiel (payant) → MCP CoplayDev (gratuit)

- **`.mcp.json`** — *(modifié)* Remplacé le serveur `unity-mcp` (relais officiel `relay_win.exe`) par `unityMCP` (serveur CoplayDev gratuit via `uvx` / package PyPI `mcpforunityserver==10.1.0`).
  Raison : le relais officiel (`com.unity.ai.assistant`) exige une licence Unity AI payante → connexion `revoked` (0 entitlement).

### À FAIRE — validation requise avant suppression
- **`Packages/manifest.json` ligne 4** — retirer `"com.unity.ai.assistant": "2.15.0-pre.2"` (package officiel devenu inutile). NE PAS supprimer sans accord explicite.
- **`ProjectSettings/Packages/com.unity.ai.assistant/`** — dossier de config du package officiel ; à retirer si le package est désinstallé.
- **`~/.unity/relay/relay_win.exe`** (~100 Mo) — binaire relais officiel inutilisé. Hors dépôt (dossier utilisateur), suppression optionnelle. NE PAS supprimer sans accord.

## 2026-07-18 — Injection config réseau + smoke test (Incrément 1 « Assemblage Éditeur »)

- **`.gitignore`** — *(modifié)* Ajout de 2 lignes ignorant `ApiConfig.asset` + `.meta`. Raison : l'asset contient la **clé API de prod** → ne doit jamais partir sur GitHub.
- **`Assets/Scriptable Objects/DiscordBridge/`** — *(créé)* Dossier cible des assets du bridge (convention projet `Assets/Scriptable Objects/`).
- **`Assets/Scriptable Objects/DiscordBridge/ApiConfig.asset`** — *(créé, NON versionné)* GUID `09c1506c3b1a60249b587e95cae359e6`. Champs : `baseUrl=https://api-towerdefence.taqkt.dev`, `apiSecretKey` (64c), `timeoutSeconds=10`. Smoke test `GET /api/inventory/test` → HTTP 200 OK.

### À FAIRE — attention
- Passage en équipe → prévoir un `ApiConfig.template.asset` à clé vide committé (l'asset réel reste gitignored).
- Ne JAMAIS retirer les 2 lignes `.gitignore` sans re-sécuriser la clé.

## 2026-07-18 — Menu « Lier mon compte » fonctionnel (Incrément 2 « Assemblage Éditeur »)

- **`Assets/Scenes/MainMenuScene.unity`** — *(modifié)* Ajout de 2 objets : `DiscordBridgeBootstrap` (racine ; `DiscordAPIBridge` + `LinkAccountController`, config câblée) et `LinkAccountScreen_Panel` (sous `StartMenuCanvas` ; Titre + `CodeInput` + `SubmitButton` + `StatusText`, script `LinkAccountScreen` câblé). Réutilise l'`EventSystem` et le `Canvas` existants.
- **`Assets/Screenshots/link_menu_preview.png` + `link_menu_play.png`** — *(créés, TEMPORAIRES)* Captures de contrôle. **Supprimables** (demander accord avant suppression).

### À FAIRE — attention
- Le panneau de liaison est un gray-box qui se superpose au menu existant. Étape déco : rendu opaque + affichage conditionnel (bouton « Lier mon compte »).

## 2026-07-18 — Auto-masquage si déjà lié + panneau opaque

- **`Assets/Scripts/DiscordBridge/Controllers/LinkAccountController.cs`** — *(modifié)* Ajout `public bool IsLinked => SessionStore.IsLinked;` (expose l'état de liaison à la vue).
- **`Assets/Scripts/DiscordBridge/UI/LinkAccountScreen.cs`** — *(modifié)* `OnEnable` masque l'écran (`gameObject.SetActive(false)`) si `controller.IsLinked` → un joueur déjà lié ne voit plus le menu.
- **`Assets/Scenes/MainMenuScene.unity`** — *(modifié)* `LinkAccountScreen_Panel` passé en plein écran + Image opaque (RGBA 0.1,0.12,0.16) → cache le menu du jeu derrière.
- **`Assets/Screenshots/link_menu_opaque.png`** — *(créé, TEMPORAIRE)* Capture de contrôle. Supprimable (avec les 2 autres `link_menu_*.png`).

## 2026-07-18 — Flux Lier/Délier à la demande (Incrément 3 « flux »)

- **`Assets/Scripts/DiscordBridge/UI/LinkAccountMenuButton.cs`** — *(créé)* Bouton de menu qui ouvre `LinkAccountScreen` à la demande.
- **`Assets/Scripts/DiscordBridge/UI/LinkAccountScreen.cs`** — *(réécrit)* 2 états (lié/non-lié) via `Refresh()`, `Open()`/`Close()`, gestion Délier ; ne se masque plus tout seul (panneau caché par défaut, ouvert via bouton).
- **`Assets/Scripts/DiscordBridge/Controllers/LinkAccountController.cs`** — *(modifié)* Ajout `Unlink()` + event `OnUnlinked`.
- **`Assets/Scenes/MainMenuScene.unity`** — *(modifié)* `LinkAccountScreen_Panel` restructuré (NotLinkedGroup / LinkedGroup / CloseButton), placé en dernier sibling, caché par défaut. Ajout du bouton `DiscordAccountButton` dans `StartMenuCanvas`.
- **`Assets/Screenshots/flow_menu.png` + `flow_linked.png` + `flow_unlinked.png`** — *(créés, TEMPORAIRES)* Captures de démo. Supprimables.
