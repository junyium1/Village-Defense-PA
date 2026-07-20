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

## 2026-07-19 — Phase A : affichage Mana + Inventaire

- **`Assets/Scripts/DiscordBridge/UI/InventoryRow.cs`** — *(créé)* Ligne d'inventaire (label unique « Nom xN [Type] »).
- **`Assets/Scripts/DiscordBridge/UI/InventoryScreen.cs`** — *(créé)* Vue pure : écoute `InventoryData.OnInventoryChanged`, instancie les lignes, boutons Rafraîchir/Fermer (Rafraîchir → `ProfileSyncController.SyncAsync`).
- **`Assets/Scripts/DiscordBridge/UI/InventoryMenuButton.cs`** — *(créé)* Ouvre `InventoryScreen` à la demande.
- **`Assets/Scriptable Objects/DiscordBridge/Items/*.asset`** — *(créés, VERSIONNÉS, pas de secret)* 6 `ItemDefinition` (skin_simple, skin_boss, shield_10m, boost_30m, freeze_5m, reinforce). Id = clé serveur `ITEMS`.
- **`Assets/Scriptable Objects/DiscordBridge/ItemDatabase.asset`** — *(créé)* référence les 6.
- **`Assets/Scriptable Objects/DiscordBridge/PlayerProfileData.asset` + `InventoryData.asset`** — *(créés)* SO runtime partagés.
- **`Assets/Scenes/MainMenuScene.unity`** — *(modifié)* Ajout `ProfileSyncController` sur `DiscordBridgeBootstrap` (câblé aux 3 SO) ; `LinkAccountController.profileSyncController` câblé ; `ManaHUD` (haut-droit) ; `InventoryScreen_Panel` (plein écran opaque, caché, dernier sibling) ; bouton `InventoryButton` (haut-gauche).

### À FAIRE — déco / attention
- `DisplayName` des `ItemDefinition` stockés SANS accents (glyphe TMP) → ré-accentuer à la passe déco.
- Pas de `ScrollRect` (simple `VerticalLayoutGroup`) : si > ~8 objets la liste déborde → ajouter un ScrollView en déco.
- Positions boutons (Inventaire / Compte Discord / Mana) en gray-box → à harmoniser.
- Icônes d'objets non fournies (champ `Icon` vide) → placeholders.

## 2026-07-19 — Phase B : logique complète (consommation, vagues, webhook, animations)

- **`Assets/Scripts/DiscordBridge/Data/ItemDefinition.cs`** — *(modifié)* Ajout champ `DurationMinutes` (durée d'effet, 0 = instantané/permanent).
- **`Assets/Scriptable Objects/DiscordBridge/Items/*.asset`** — *(modifiés À LA MAIN, YAML)* `DurationMinutes` injecté : shield_10m=10, boost_30m=30, freeze_5m=5, reinforce=0. Vérifier import Unity au retour du bridge.
- **`Assets/Scripts/DiscordBridge/Data/ActiveEffectsData.cs`** — *(créé)* SO runtime des buffs actifs côté client (`IsActive(id)`, `GetRemainingSeconds(id)`). ⚠️ Asset `.asset` PAS ENCORE créé (bridge down).
- **`Assets/Scripts/DiscordBridge/Controllers/ConsumeItemController.cs`** — *(créé)* `ConsumeAsync(itemId)` → POST /api/consume-item → active l'effet + re-sync inventaire. Events OnConsumeSucceeded/Failed.
- **`Assets/Scripts/Game/CombatManager.cs`** — *(modifié, fichier GAMEPLAY)* Ajout events `WaveStarted(int)` / `WaveRewardReady(int,int)` + compteur `_killsThisWave` (incrémenté dans OnEnemyDied) + `IsFinalWave`. Crédit de vague : au départ de la suivante, ou à la victoire. AUCUNE référence DiscordBridge introduite.
- **`Assets/Scripts/DiscordBridge/Controllers/GameplayBridgeHooks.cs`** — *(créé)* S'abonne à CombatManager → WaveRewardController (mana) + webhook game-event (alerte Discord ; par défaut : dernière vague uniquement).
- **`Assets/Scripts/DiscordBridge/Controllers/WaveRewardController.cs`** — *(modifié)* Garde null sur `DiscordAPIBridge.Instance` (GameScene lancée sans bootstrap).
- **`Assets/Scripts/DiscordBridge/Controllers/ProfileSyncController.cs`** — *(modifié)* Polling optionnel (`pollIntervalSeconds`, 0 = off) + verrou anti-synchros concurrentes `_syncInFlight`.
- **`Assets/Scripts/DiscordBridge/UI/InventoryRow.cs`** — *(réécrit)* Bouton « Utiliser » (consommables) remonté par callback ; `SetInteractable`.
- **`Assets/Scripts/DiscordBridge/UI/InventoryScreen.cs`** — *(réécrit)* Branche ConsumeItemController (statuts Utilisation/Erreur/Objet utilisé) + accroche UIPanelAnimator.
- **`Assets/Scripts/DiscordBridge/UI/LinkAccountScreen.cs`** — *(modifié)* Affiche le Mana dans l'état lié (via PlayerProfileData, SO) + accroche UIPanelAnimator.
- **`Assets/Scripts/DiscordBridge/UI/UIPanelAnimator.cs`** — *(créé)* Fondu+zoom ouverture/fermeture (CanvasGroup, temps non-scalé).
- **`Assets/Scripts/DiscordBridge/UI/UIButtonFX.cs`** — *(créé)* Survol/pression des boutons (échelle).

### ✅ Câblage scène FAIT (2026-07-19, après retour du bridge)
1. ✅ Compilé, 0 erreur. 2. ✅ `ActiveEffectsData.asset` créé (GUID `642835929486c0f43bc7f620dc4cdd24`). 3. ✅ MainMenuScene câblée (tout). 4. ✅ GameScene : `DiscordGameplayBridge` créé + câblé. 5. ✅ Passe déco appliquée.

## 2026-07-19 — Passe déco + correctifs de rendu (fin Phase B)

- **`Assets/Scenes/MainMenuScene.unity`** — *(modifié)* Palette violette (boutons arrondis via sprite builtin `UISprite.psd` Sliced) : primaire #6B4FBF, danger rouge (Délier), succès vert (Utiliser), neutre (Fermer). Titres en gras. `DiscordAccountButton` déplacé sous `InventoryButton` (haut-gauche, ne chevauche plus le titre). ManaHUD gras violet. Accents restaurés dans les textes de scène (Rafraîchir/Délier/Compte lié). ScrollRect (`ListScroll` + RectMask2D + ContentSizeFitter) sur la liste d'inventaire.
- **Correctif rendu (IMPORTANT, espace linéaire URP)** : un fond `alpha 0.985` laisse VISIBLEMENT transparaître le contenu derrière (fuite ~20% perçue après encodage gamma). Fonds des 2 panneaux forcés à **alpha 1.0** ; viewport liste = teinte violette opaque (pas de blanc semi-transparent). Règle : jamais d'alpha ~0.98 sur un fond censé être opaque.
- **`UIPanelAnimator.cs` + `UIButtonFX.cs`** — *(modifiés)* `dt` plafonné à 0.05s : après un hitch (perte de focus éditeur, GC), l'animation saute à la fin au lieu de rester figée semi-transparente.
- **Items** — DisplayName ré-accentués (Fantôme, Électrique, récolte) via SerializedObject (sérialisation Unity `\xC9` etc.).
- **`Assets/Screenshots/Assets_Screenshots_deco_*.png`** — *(créés, TEMPORAIRES ×8)* Captures de debug/validation. **Supprimables** (demander accord) — avec les 6 anciennes `link_menu_*`/`flow_*`.
- Vérifié en Play : inventaire live (Skin Fantôme x1, serveur), Mana 190 live, panneau compte « Compte lié — Mana : 190 ». Compte re-lié localement (PlayerPrefs).

## 2026-07-19 — Phase C : effets consommables réels + réparation UnitData

- **`Assets/Scripts/DiscordBridge/DTOs/PlayerDataDTO.cs`** — *(modifié)* + `ActiveEffectDto` et `PlayerDataResponse.ActiveEffects` (champ serveur `active_effects`).
- **`Assets/Scripts/DiscordBridge/DTOs/ConsumeItemDTO.cs`** — *(modifié)* + `DurationSeconds` / `ExpiresAt` (réponse enrichie serveur).
- **`Assets/Scripts/DiscordBridge/Data/ActiveEffectsData.cs`** — *(modifié)* + `ActivateForSeconds`, `SyncFromServer` (serveur autoritaire), `GetSnapshot`.
- **`Assets/Scripts/DiscordBridge/Controllers/ProfileSyncController.cs`** — *(modifié)* + champ `activeEffects` ; restauration des buffs à chaque sync.
- **`Assets/Scripts/DiscordBridge/Controllers/ConsumeItemController.cs`** — *(modifié)* durée serveur prioritaire ; durée 0 → réserve one-shot locale.
- **`Assets/Scripts/DiscordBridge/Session/PendingItemEffectsStore.cs`** — *(créé)* réserve PlayerPrefs des effets one-shot (reinforce) déjà décomptés serveur.
- **`Assets/Scripts/DiscordBridge/Controllers/GameplayEffectsController.cs`** — *(créé)* applique gel/bouclier/renforts en GameScene (scan 0.25s).
- **`Assets/Scripts/DiscordBridge/UI/InventoryScreen.cs`** — *(modifié)* ligne « Effets actifs » (TMP `ActiveBuffsText`, MAJ 1s).
- **`Assets/Scripts/Game/Health.cs`** — *(modifié, fichier gameplay)* + propriété `Invulnerable` (2 lignes, neutre).
- **`Assets/Scripts/Game/CombatManager.cs`** — *(modifié, fichier gameplay)* victoire = `NoEnemyAlive()` (faction Enemy seulement) au lieu de « plus aucune Unit » — sinon les renforts alliés bloquaient la victoire.
- **`Assets/Scripts/Game/Units/Unit.cs` → `UnitData.cs`** — *(RÉPARATION)* classe `UnitData` extraite dans son propre fichier `Assets/Scripts/Game/Units/UnitData.cs` (GUID `448a47e84f935c646a6d055dad562eb1`). Cause : classe ≠ nom de fichier → `GolemData.asset` et `GolbinData.asset` avaient `m_Script: {fileID: 0}` (assets NON chargeables par script). `m_Script` des 2 .asset réécrits vers le nouveau GUID → réparés (`Ally`/`Enemy` OK).
- **`Assets/Prefabs/Golem.prefab`** — *(modifié)* `Unit.data` recâblé sur `GolemData` (réf morte) + `MeleeAttack` ajouté (renfort fonctionnel).
- **`Assets/Scenes/MainMenuScene.unity`** — *(modifié)* + TMP `ActiveBuffsText` dans `InventoryScreen_Panel` ; câblages `ProfileSyncController.activeEffects`, `InventoryScreen.{activeEffects,itemDatabase,activeBuffsText}`.
- **`Assets/Scenes/GameScene.unity`** — *(modifié)* + `GameplayEffectsController` sur `DiscordGameplayBridge` (activeEffects + Golem prefab + GolemData).

### À FAIRE — attention
- `CombatManager.cs`, `Health.cs`, `Unit.cs`, `Golem.prefab` sont des fichiers gameplay partagés (équipe) : signaler l'extraction `UnitData.cs` et le nouveau critère de victoire lors du prochain rebase/merge.
- Effets gel/bouclier/renforts NON testés en Play (à faire).

## 2026-07-19 — Setup délégation OpenCode Go

- **`AGENTS.md`** — *(modifié, existait via test T-00)* + règles MVVM strict, `remaining_seconds` vs `expires_at`, boost ×2 serveur, anti-triche add-mana intouchable, `py_compile` obligatoire.
- **`TASKS.md`** — *(modifié)* T-02 : chemins corrigés (`Networking/DiscordAPIBridge.cs`, `DTOs/BattleReportDTO.cs`, interdiction .meta) ; + T-03 (Prompt 3 backend : cooldown ratons, timeout 300s/-25, raids, bouclier auto-défense) ; + T-04 (blocked, MCP/Play — réservé Claude Code).
- **`.gitignore`** — *(modifié)* `.claude/` → `.claude/*` + exceptions `!settings.json` `!commands/` (versionnage duo) ; + `.delegate-logs/`. Lignes ApiConfig intouchées.
- **`.claude/settings.json`** — *(créé, versionné)* allow : delegate.ps1, opencode models, git diff/status.
- **`.claude/commands/delegate.md`** — *(créé)* slash command `/delegate <ticket>` : lecture ticket → contrôle zone → instruction autonome → run → `git diff --stat` + rollback si fichier interdit → cocher/noter.
- **`CLAUDE.md`** — *(modifié)* section `# 🧠 ROUTAGE DE MODÈLE` remplacée par `# 🧠 ROUTAGE : DÉLÉGATION` (Faible/Moyen → délégation OpenCode au lieu de Haiku ; liste JAMAIS DÉLÉGUÉ ; diff post-délégation obligatoire).
- **`.claude_memory.md`** — *(modifié)* nouvelle section « Duo Claude Code / OpenCode Go ».

## 2026-07-19 — Rollback tentative design menus T-05 (autorisé par l'utilisateur, base propre avant reprise itérative)

- **`Assets/Scenes/MainMenuScene.unity`** — *(restauré via `git checkout`)* Retour à l'état HEAD (48+/48- annulés : références aux composants de la tentative échouée).
- **`Assets/Scripts/Menus/MenuBackdrop.cs` + `.meta`** — *(supprimés, non trackés)* Script de la tentative échouée. Aucune référence dans le code tracké (grep vérifié).
- **`Assets/Scripts/Menus/MenuTransitionAnimator.cs` + `.meta`** — *(supprimés, non trackés)* Idem.
- **`Assets/Textures/` (Rope.png, WoodPole.png, WoodSignButton.png, WoodSignPanel.png + metas) et `Assets/Textures.meta`** — *(supprimés, non trackés)* Assets visuels de la tentative échouée.
- Les scripts légitimes de `Assets/Scripts/Menus/` (MenuManager, PauseMenuManager, LevelSelectManager, etc.) sont **trackés et intacts**.
- État final : `git status` propre (0 modification, 0 non-tracké).

## 2026-07-19 — Purge des références IA du versionnage + merge main
- **`.gitignore`** — *(modifié)* section « Outillage IA » : CLAUDE.md, .claude/ (tout, exceptions retirées), .claudeignore, .claude_memory.md, .mcp.json, AGENTS.md, TASKS.md, cleanup_log.md, scripts/delegate.ps1, .delegate-logs/, .opencode/, opencode.json, Packages/com.coplaydev.unity-mcp/, packages-lock.json + Screenshots/_Recovery.
- **Untracking (git rm --cached, fichiers CONSERVÉS sur disque)** : .claude_memory.md, .claudeignore, .mcp.json, CLAUDE.md, cleanup_log.md, packages-lock.json, Packages/com.coplaydev.unity-mcp/ (716 fichiers).
- **`Packages/manifest.json`** — *(modifié)* retrait `com.coplaydev.unity-mcp` (URL git ; le package embarqué sur disque reste actif localement) et `com.unity.ai.assistant` (sera désinstallé au prochain refresh Unity — item en attente depuis Phase A, couvert par la demande de purge IA). `com.unity.ai.navigation` CONSERVÉ (NavMesh, dépendance gameplay).
- Commit `1322a5d` (Phase C + purge) ; `main` fast-forwardée sur `1322a5d` sans checkout.

## 2026-07-20 — Menu 3D signpost : nav (Kimi K3) + pirouette/hover (Claude, MCP)

- **Scripts nav (Kimi K3, `Assets/Scripts/Menus/`)** — *(créés)* `SignPlankBase.cs`, `Menu3DInput.cs`, `SignPlankAction.cs`, `SignLevelPlank.cs`, `SignOptionToggle.cs`, `Menu3DController.cs`. Raycast Input System → `OnClicked` → contrôleur ; grille 8 niveaux (LevelData réels) + toggle musique. Kimi arrêté (crédit épuisé) en plein T-10.
- **`Assets/Scripts/Menus/SignpostRotator.cs`** — *(créé, Claude)* pirouette 0°→90°→(−90°)→0°, swap du contenu à la tranche, caméra fixe, `IsBusy` gèle l'input, dt≤0.05.
- **`Assets/Scripts/Menus/SignPlankHover.cs`** — *(créé, Claude)* hover scale ×1.05 + tilt 6° AUTOUR du centre bounds (pivots importés ~80 u loin de la géométrie), base capturée au 1er survol, contour blanc.
- **`Assets/Art/MainMenu/PlankOutline.shader`** — *(créé, Claude)* contour inverted-hull (Cull Front, extrusion normale monde). ⚠️ partiel sur planche plate (rend surtout les tranches latérales).
- **`Assets/Scripts/Menus/Menu3DController.cs` + `Menu3DInput.cs`** — *(réécrits, Claude)* routage par la pirouette + hover/gating input.
- **`Assets/Scenes/MainMenuScene.unity`** — *(modifié via MCP)* `FlipPivot` (axe poteau) créé ; `Signpost_Root`/`LevelSelectSign_Root`/`ChainedSign_Root` **co-localisés** (boards alignés) + reparentés sous FlipPivot ; `SignpostRotator` + 15 `SignPlankHover` ajoutés ; **caméra fixe X=377.95** (⚠️ 378.975 = ancien cadrage sous-panneau → signpost hors-champ). Vérifié Play : 3 transitions pirouette OK, hover scale/tilt OK.

### À FAIRE — attention
- ✅ RÉSOLU (choix utilisateur) : contour → **glow blanc** (`SignPlankHover` éclaircit la planche vers le blanc via MaterialPropertyBlock `_BaseColor`/`_EmissionColor`, ×0.6). `PlankOutline.shader` **conservé mais inutilisé** (suppression = accord).
- ✅ RÉSOLU : panneau Niveaux recadré (`LevelSelectSign_Root` scale 0.80→**0.60** + board re-aligné) → 8 niveaux + « Retour » tiennent dans le cadre.
- Reste : doublon FBX `titlescreen`/`menu_signs` ; **test souris réel** (hover + clics) à faire par l'utilisateur.
- Doublon FBX `titlescreen.fbx` / `menu_signs.fbx` (identiques) toujours présent.
- **`Assets/Screenshots/reprise_*.png` + `play_*.png`** — *(créés, TEMPORAIRES)* captures de validation. Supprimables (accord requis).

## 2026-07-20 — Réactivation input menu 3D + textures bois du signpost

- **`Assets/Scenes/MainMenuScene.unity`** — *(modifié via MCP, sauvegardé)* `Menu3DInput` (GO `Menu3D`) était `enabled=false` (désactivé pour tests, jamais réactivé) → hover/clic morts. Remis `enabled=true` + champ `cam` assigné explicitement à `Main Camera` (avant : non-assigné, dépendait du fallback `Camera.main`). C'est le SEUL composant qui pilote hover ET clic.
- **`Assets/Art/MainMenu/textures/`** — *(dossier créé par l'utilisateur)* textures exportées de Blender. **Jeu bois (signpost)** = `Image_0.jpg` (base color atlas, sRGB), `Image_1.png` (metallic-roughness+occlusion, non utilisé), `Image_2.png` (normal). **Jeu `.001`** = décor rocheux (`Image_0.001.jpg` roche, etc. — PAS encore appliqué).
- **`Assets/Art/MainMenu/textures/Image_2.png`** — *(réimporté)* `textureType` → **NormalMap**.
- **`Assets/Art/MainMenu/Materials/MM_Wood.mat`** — *(modifié)* `_BaseMap`=Image_0, `_BaseColor`=blanc (avant : brun 0.54/0.35/0.18 qui teintait), `_BumpMap`=Image_2 + keyword `_NORMALMAP`, `_Smoothness`=0.2 (matte), `_Metallic`=0. Les UV des planches pointent déjà dans l'atlas → mapping correct. Vérifié par rendu caméra (`scratchpad/wood_check.png`).

### À FAIRE — attention
- `Image_1` (metallic-roughness glTF : G=rough, B=metal) NON appliqué (packing ≠ masque URP R=metal/A=smooth). Si besoin de variation de surface : générer un masque URP repacké. Apport visuel faible en low-poly.
- Décor rocheux (`Image_x.001`) non texturé (matériaux `MM_Dirt`/décor encore unis) — proposer une passe si voulu.
- Lisibilité des textes TMP (Niveaux/Réinitialiser) un peu faible sur bois sombre → à revoir avec l'éclairage coucher de soleil.
