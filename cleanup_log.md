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

## 2026-07-20 — T-11 étape 2/2 : contenu du menu options (réglages + persistance)

- **`Assets/Resources/MainMixer.mixer`** — *(créé, YAML édité à la main — tolérance AGENTS.md asset, plan validé)* AudioMixer `Master` › `Music` + `SFX`, paramètres exposés `MusicVolume`/`SfxVolume` (guid→m_Volume des groupes). Format : docs `!u!241/243/244/245` (AudioMixerController/GroupController/EffectController Attenuation/SnapshotController). ⚠️ Création groupes+params NON scriptable en Unity 6 (API publique absente) → d'où le YAML manuel. Doublon accidentel `Assets/NewAudioMixer 1.mixer` supprimé (créé par moi via menu Create, jamais utilisé).
- **`Assets/Scripts/Menus/SettingsStore.cs`** — *(créé)* statique, PlayerPrefs (`settings.musicOn/musicVol/sfxOn/sfxVol/quality`), `ApplyAll()` public, event `Changed`, dB = `20*Log10(v)` (0 → −80).
- **`Assets/Scripts/Menus/SettingsApplier.cs`** — *(créé)* ⚠️ **PIÈGE MIXER** : le snapshot par défaut est appliqué par le moteur audio **APRÈS** les `RuntimeInitializeOnLoadMethod` (même AfterSceneLoad) → SetFloat immédiat écrasé. Boot différé (0,05 s + 0,3 s) via `WaitForSecondsRealtime`, puis s'auto-détruit. ⚠️ Test MCP : Play gelé sans focus (frame=1) → `Application.runInBackground=true` en Play pour dégeler.
- **`Assets/Scripts/Menus/SignOptionToggle.cs`** — *(réécrit)* toggle générique (`Setting { MusicOn, SfxOn }`) via SettingsStore. Champ `onVolume` supprimé (valeur sérialisée scène ignorée sans erreur), `label`/`labelFormat` conservés → câblage scène intact.
- **`Assets/Scripts/Menus/SignOptionCycle.cs`** — *(créé)* clic = palier suivant (wrap) ; volumes 0/25/50/75/100 %, qualité = noms `QualitySettings`.
- **`ProjectSettings/QualitySettings.asset`** — *(modifié)* ⚠️ **PIÈGE QUALITÉ** : `QualitySettings.names` est **filtré par plateforme** — `Mobile` excluait Standalone → 1 seul niveau (« PC ») sur PC, cycle inutilisable. Retiré `excludedTargetPlatforms: [- Standalone]` du niveau Mobile → names=[Mobile, PC]. Défaut Standalone inchangé (PC, index 1).
- **`Assets/Scenes/MainMenuScene.unity`** — *(modifié via MCP, sauvegardé)* pancarte `ChainedSign_Root` : 4 planches clonées de `Plank_Music` (`Plank_MusicVol`, `Plank_Sfx`, `Plank_SfxVol`, `Plank_Quality`) + `Plank_Back` descendu en bas de pile ; positions **provisoires** empilées sous le board (translation monde — pivots cuits, jamais de set local) → **l'utilisateur replace**. AudioSource `MusicManager/MenuMusicSource` routée → groupe **Music** (⚠️ ce GO scène N'EST PAS une instance du prefab `MusicManager.prefab` malgré le nom — routing direct scène requis).
- **`Assets/Prefabs/Menus/MusicManager.prefab`** — *(modifié via API)* AudioSource → groupe Music (cohérence, même si l'instance scène est indépendante).
- **`Assets/Scenes/GameScene.unity`** — *(modifié via MCP additif, sauvegardé)* 2 AudioSources de `PlacementSystem` → groupe **SFX**.
- **Vérifié en Play** : mixer −2,5 dB (75 %) au lancement via SettingsApplier ; toggles → −80 dB live ; cycles volumes + qualité PC⇄Mobile ; PlayerPrefs persistés après relancement ; labels runtime corrects ; bousculade + pancarte OK ; 0 erreur console. Captures `t11_options_planches.png` / `t11_options_final.png` (gitignorées).

### À FAIRE — attention
- Positions des 6 planches options = **provisoires** (pile sous le board) → placement final par l'utilisateur (comme les niveaux).
- Convention audio : toute **nouvelle** AudioSource SFX doit être routée vers le groupe `MainMixer/SFX` (sinon elle sort sur Master = non affectée par le réglage Effets — reste audible, pas de régression, mais réglage sans effet sur elle).
- Le toggle legacy 2D `MusicVolumeController` (panel options 2D hors flux) n'est PAS branché sur SettingsStore (volontairement — hors scope ; si le panel 2D est réactivé un jour, le brancher).

## 2026-07-20 — Fix fuite IsBusy statique (pause → menu titre injouable)

- **`Assets/Scripts/Menus/SignpostPushSwap.cs` + `SignpostRotator.cs`** — *(modifiés)* ajout `public static void ResetBusy() => IsBusy = false;`.
- **`Assets/Scripts/Menus/Menu3DInput.cs`** — *(modifié)* `Awake()` appelle `SignpostRotator.ResetBusy()` + `SignpostPushSwap.ResetBusy()`. Cause : `QuitToMainMenu` lançait `SwingOut` (IsBusy=true + coroutine) PUIS `LoadScene` synchrone → coroutine tuée avant `IsBusy=false` → flag statique survivait → `Menu3DInput` gelait hover/clic dans le menu titre. 3 fichiers, 0 modif scène.

## 2026-07-20 — T-12 : cel shading (BotW/TotK) + éclairage sunset du menu titre (Claude/MCP)

⚠️ **T-12 était « assigné OpenCode » dans la memory → réassigné à Claude sur demande explicite de l'utilisateur.** Périmètre : **MainMenuScene seule**, outline **PC uniquement** (Mobile = cel sans outline).

- **`Assets/Settings/MM_MenuVolume.asset`** — *(créé)* VolumeProfile post-process : Bloom (tint chaud), Tonemapping Neutral, ColorAdjustments (chaud, contraste), WhiteBalance (+12 temp), Vignette.
- **`Assets/Art/MainMenu/MM_SunsetSky.mat`** — *(créé)* Skybox/Procedural réglé golden-hour (atmosphere 1.15, exposure 1.2).
- **`Assets/Settings/PC_RPAsset.asset`** — *(modifié)* `m_SupportsHDR=true` (bloom propre).
- **`Assets/Scenes/MainMenuScene.unity`** — *(modifié, sauvegardé)* Directional Light `Directional Light` réglée sunset (rot 17,320 / couleur 1,0.84,0.62 / I=1.45 ; **anciennes valeurs : rot 50,330 / 1,0.957,0.839 / I=1**). Fog ON (ExponentialSquared, chaud 0.93,0.84,0.70, densité 0.0085 ; **avant : OFF**). Skybox → MM_SunsetSky, ambient 1.2. GO `Global Volume (Menu)` (profil MM_MenuVolume). Main Camera : `renderPostProcessing=true` + volumeLayerMask Everything (**transform INTOUCHÉE**, X=377.95 préservé). 421 renderers décor réassignés aux matériaux cel (voir ci-dessous).
- **`Assets/Art/MainMenu/Shaders/MM_CelLit.shader`** — *(créé)* URP HLSL, 4 passes (ForwardLit cel + ShadowCaster + DepthOnly + **DepthNormals** requis pour l'outline). Diffus bandé (`_Steps`), rim light chaud, `_BaseMap`/`_BaseColor`/`_BumpMap`/`_EmissionColor` (**hover `SignPlankHover` intact**), SH ambient. Main light seule (menu = 1 directionnelle).
- **`Assets/Art/MainMenu/Materials/MM_{Wood,Bark,Leaves,Dirt,Iron,Chain}.mat`** — *(modifiés)* shader → MM/CelLit (couleurs/textures préservées ; MM_Wood garde `_NORMALMAP`).
- **`Assets/Art/MainMenu/Materials/Decor/MM_{Grass,DecorWood,Bush}.mat`** — *(créés)* extraits du FBX puis convertis cel. ⚠️ **PIÈGE** : `AssetDatabase.ExtractAsset` sur un FBX + scène DÉPACKÉE = refs bakées cassées (421 slots null). Récup : retirer les remaps FBX (revient embarqué, refs restaurées) puis **réassigner in-scene par nom** (Material.001→MM_Grass ×418, Wood→MM_DecorWood ×2, Material.002→MM_Bush ×1). Les 3 matériaux embarqués du FBX restent (inutilisés).
- **`Assets/Art/MainMenu/Shaders/MM_OutlineFullscreen.shader`** — *(créé)* edge-detection plein écran (Roberts cross profondeur relative + normales), tunables `_OutlineColor/_OutlineThickness/_DepthThreshold/_NormalThreshold/_OutlineOpacity`. ⚠️ include Blit = `com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl` (URP 17, PAS le chemin universal).
- **`Assets/Settings/MM_Outline.mat`** — *(créé)* matériau outline (normalThreshold 1.5 / depthThreshold 0.10 / thickness 1.1 / opacity 0.9 — réglés pour ne garder que les silhouettes, pas les facettes du sol).
- **`Assets/Settings/PC_Renderer.asset`** — *(modifié)* + `FullScreenPassRendererFeature` « MM_Outline » (injection AfterRenderingPostProcessing, requirements Depth|Normal). ⚠️ **PIÈGE** : ajouter à `m_RendererFeatures` par code laisse `m_RendererFeatureMap` désync (URP ignore la feature) → reconstruire la map (localFileId de chaque feature via `TryGetGUIDAndLocalFileIdentifier`).
- **Vérifié en Play** : sunset + cel + outline OK, UI 2D (Inventaire/Discord/Mana) non-outlinée (overlay au-dessus), titre affiché par le controller, hover glow rend à travers le cel shader. Captures `Assets/Screenshots/mm_*.png` (gitignorées).

### À FAIRE — attention
- **`PlankOutline.shader`** (inverted-hull, déjà inutilisé) reste sans usage — l'outline est désormais le post-process plein écran.
- Résidu faible : quelques lignes de facettes au sol en distance moyenne (acceptable, stylisé). Monter `_NormalThreshold` si l'utilisateur veut un sol plus lisse.
- **Test souris réel** (hover/clic sur planches sous le nouveau rendu) à faire par l'utilisateur.
- Mobile : outline absent (choix), cel shading présent ; HDR non forcé sur Mobile_RPAsset (bloom plus faible).
- UI 2D `StartMenuCanvas` (Inventaire/Discord/ManaHUD) non restylée (hors scope cel — sprites peints) ; proposer une passe si voulu.

## 2026-07-21 — Bascule setup multi-agent OpenCode (Qwen Max chef + Qwen Plus assistant), retrait ancien système

Contexte : passage au duo natif OpenCode (agent `build` = Qwen3.7 Max = chef, subagent `assistant` = Qwen3.7 Plus) via `opencode.json`. L'ancien système de délégation par sous-processus (kimi-k3) et le prompt Claude Code deviennent obsolètes. Suppressions **validées par l'utilisateur** (2026-07-21).

- **`scripts/delegate.ps1`** — *(SUPPRIMÉ)* Ancien script de délégation `opencode run --model opencode-go/kimi-k3`. Remplacé par les subagents natifs OpenCode (outil `task` : chef → assistant). Jamais utilisé par l'utilisateur.
- **`.claude/commands/delegate.md`** — *(SUPPRIMÉ)* Slash-command `/delegate` associé au script ci-dessus. Obsolète.
- **`CLAUDE.md`** — *(SUPPRIMÉ)* Prompt Claude Code (routage périmé Haiku/Sonnet/Opus, délégation vers OpenCode Go). Lu uniquement par Claude Code (ignoré par OpenCode quand `AGENTS.md` existe). L'utilisateur n'utilise plus Claude Code sur ce projet. Toutes les règles anti-régression utiles sont déjà dans `AGENTS.md`.
- **`SKILL.md`** — *(SUPPRIMÉ)* Fichier vide (0 octet), sans usage.
- **`.claude/settings.json`** — *(modifié)* Retrait de la permission `Bash(pwsh -File ./scripts/delegate.ps1:*)` (script supprimé).
- **`.mcp.json`** — *(à réviser)* Config MCP au format Claude Code (`mcpServers`), désormais inutile côté OpenCode (qui lit `opencode.json`). Conservé pour l'instant (n'interfère pas). À retirer si Claude Code est définitivement abandonné.

## 2026-07-22 — Nettoyage grille de placement (GameScene)

- **`Level/Plateformes`** — *(SUPPRIMÉ, 1020 enfants)* Ancien système de grille (Nodes inactifs, parent `activeSelf=false`). Remplacé par `BuildingSystem/GridVisualization` + `GridBlinker`.
- **`BuildingSurface`** — *(MeshRenderer éteint)* Plan 200×200 `Grass.mat` qui doublonnait visuellement avec `Sol`. MeshCollider conservé pour les raycasts de placement.
- **`Assets/Scripts/BuildingSystem/GridBlinker.cs`** — *(créé)* Pulse sinusoïdal de l'alpha de la grille via MaterialPropertyBlock (`_Color`), unscaledTime (survit à la pause).
- **`BuildingSystem/GridVisualization`** — *(activé + GridBlinker attaché)* Grille visible en permanence avec clignotement pendant la partie.

## 2026-07-23 — Cohérence map ↔ grille (GameScene) — zone fixe, sol herbe plat, grille build-only

Contexte : demande utilisateur « finir l'intégration de la map avec la grille ». Diagnostic : le niveau est un bloc autonome (`LevelZone` à l'origine + tous ses enfants), la clairière du décor est un **flanc de colline** (~18 u de dénivelé sur l'emprise 200×200) → la dalle plate se faisait traverser par le relief. Choix utilisateur : zone **fixe** ce niveau, rendu **herbe** (plus de marron), grille **invisible sauf en mode build**.

- **`Assets/Scripts/Game/GameManager.cs`** — *(modifié, Start)* Branche `zonePlacer == null` : auto-`LevelZone.Instance.Confirm()` avant `EnterPlacement()`, pour qu'une zone fixe (posée en éditeur) soit constructible sans phase de placement runtime. Vérifié en Play : `IsPlaced=true` au démarrage, NavMesh baké, chemin spawn→objectif `PathComplete`.
- **`GameScene › GameManager.zonePlacer`** — *(scène, mis à None)* Court-circuite la phase `ZonePlacement` (drag de la zone à la souris) — non voulue pour un niveau fixe. `LevelZonePlacer` conservé sur `GameController` pour l'éditeur / autres niveaux.
- **`GameScene › LevelZone`** — *(scène)* `position.y` 0 → **7.5** : monte tout le niveau (grille, sol, spawn, village, objectif, cameraBounds) au-dessus du terrain (max +7.05 sous l'emprise). Plan de pose des bâtiments (`Grid.CellToWorld`) → Y=7.5 uniforme.
- **`GameScene › LevelZone/Sol`** — *(scène)* Cube transformé en **dalle herbe** : `localScale` (200,0.2,200) → (210,**18.95**,210), `localPosition` (0.5,0.35,5.1) → (0,**-9.025**,0) ; top monde ≈ 7.95 (au-dessus du relief), bottom ≈ -11 (sous le terrain → pas de flottement au bord aval). Matériau `Sol plane.mat` → **`Grass.mat`**. Remplace la terre brune par de l'herbe plate constructible.
- **`GameScene › LevelZone/GridParent/GridVisualization`** — *(scène)* `localPosition.y` 0 → **0.5** (monde 8.0) : grille pile au-dessus de l'herbe. Reste **masquée hors build** (PlacementSystem show/hide) car la zone est désormais auto-validée (`ZoneNotReady=false`). ⚠️ Supersède l'entrée 2026-07-22 « grille visible en permanence + GridBlinker » : elle n'était permanente que parce que la zone n'était jamais validée (phase ZonePlacement). `GridBlinker` toujours attaché → pulse pendant le mode build uniquement.
- **`GameScene › BuildingSurface`** — *(désactivé, `SetActive(false)`)* Plan 200×200 orphelin (layer Default, hors LevelZone, renderer déjà éteint). Non utilisé pour le placement (les raycasts de pose visent le layer **Placement** = `GridVisualization`, pas Default). Réversible ; supprimable définitivement sur accord.

### À FAIRE / à surveiller
- **`LevelZone.Instance` ressort `null` en Play après le démarrage** (le composant est bien vivant, `IsPlaced=true`) — anomalie pré-existante de `LevelZone`, sans impact ici (toutes les refs runtime sont assignées en scène, pas de fallback `Instance`). À durcir si un futur code dépend de `Instance`.
- Léger écart de teinte entre la dalle `Grass.mat` (vert plat) et l'herbe texturée de la map ; bords masqués par les arbres. Option : essayer `Assets/Art/map/Materials/Terrain_Baked.mat` sur la dalle pour un raccord plus fin.
- Décroché (mesa) jusqu'à ~18 u au coin aval de la clairière, masqué par la ligne d'arbres. Si gênant : option « aplatir le terrain » (édition mesh) écartée pour l'instant.

## 2026-07-24 — Skin « pancarte » écran Discord + réparation bouton Retour des Touches (T-05)

Contexte : reprise du travail perdu la veille (construit en Play mode via MCP → annulé à la sortie, rien sur disque). MCP Unity hors service cette session (voir `opencode.json` ci-dessous) → **décision chef : relooking 100 % runtime en C#** (même pattern que `KeybindsScreen` qui construit son UI par code, zéro édition de scène, réversible).

- **`Assets/Scripts/Menus/KeybindsScreen.cs`** — *(modifié)* **Fix bouton Retour** (+ Par défaut + les 6 boutons de touche, tous morts). Cause racine : le `KeybindsCanvas` avait été construit dans l'éditeur (ContextMenu) puis **sauvegardé dans MainMenuScene** → les `onClick.AddListener` de `Build()` sont runtime-only, **jamais sérialisés** → au chargement `TryLinkExistingRoot()` réutilisait le canvas sauvegardé, `Build()` n'était jamais appelé → zéro listener (seul Echap répondait, géré dans `Update`). Ajout de **`RewireButtons()`** : recâble par chemin `Panel/Board/Row_*/Key` → `BeginRebind(i)`, `Footer/Defaults` → reset, `Footer/Back` → `Close`. Appelé depuis `TryLinkExistingRoot()` (une seule fois garantie : early-return si `_root` déjà lié).
- **`Assets/Scripts/DiscordBridge/UI/LinkAccountScreen.cs`** — *(modifié)* **`ApplyPancarteSkin()`** appelé dans `Awake` (ne tourne qu'à la 1re activation du panel, avant `OnEnable`/`Refresh` → le bon groupe est affiché dès la 1re frame). Recette du handoff 2026-07-23 appliquée telle quelle : voile dim α0.55 sur le panel (sprite null), enfant `Plank` (sprite `Resources/UI/pencarte`, preserveAspect, 1240×675.86, raycastTarget false), enfant `Board` (ancres 0.243/0.168 → 0.757/0.727 ≈ 637×378, `VerticalLayoutGroup` spacing 8 padding (0,0,4,4) MiddleCenter), migration `Title`/`NotLinkedGroup`/`LinkedGroup`/`StatusText`/`CloseButton` sous Board, `LayoutElement`s (620×46 / 620×168 / 620×116 / 620×34 / 220×44), nouveau **`Hint`** dans NotLinkedGroup (index 0 → masqué automatiquement quand le compte est lié), ColorBlocks bois `#3A281C`/`#5C3F2A`, rouge bois Délier `#5A2320`/`#7A322C`, input `#2A1C12`/`#3C2A1C`, images boutons passées en blanc (teintage multiplicatif), titre retitré **« COMPTE DISCORD »** (style TOUCHES — à valider). Champs inspecteur `plankSprite` + `pancarteSkin` (bool, défaut true) pour couper le skin sans toucher au code. MVVM intact : la vue ne restyle qu'elle-même, aucune réf Networking/DTO.
- **`opencode.json`** — *(gitignoré)* Chemin `uvx.exe` corrigé : `C:\Users\yanis\.local\bin\uvx.exe` (disparu — uv réinstallé via Python) → `C:\Users\yanis\AppData\Local\Programs\Python\Python311\Scripts\uvx.exe`. Spawn manuel du serveur testé OK (FastMCP 3.4.4, `mcpforunityserver==10.1.0`). **Redémarrage d'OpenCode requis** + côté éditeur vérifier Window > MCP for Unity (transport stdio, port 6400, 🟢 Running — le socket 6400 était DOWN).

### À FAIRE / à surveiller
- **Tester en Play** : Options → Touches → Retour / Par défaut / rebind (tous recâblés) ; menu → Compte Discord → pancarte, états lié/non-lié, Fermer. 0 erreur console attendue.
- Titre « COMPTE DISCORD » : à valider (revenir à « Lier mon compte Discord » = 1 ligne dans `ApplyPancarteSkin`).
- Reste T-05 : même traitement pancarte sur `InventoryScreen_Panel` — décision à trancher avec l'utilisateur : la liste défilante dépasse la planche de bois (élargir la pancarte / réduire le ScrollRect / assumer le débord).

## 2026-07-24 — Minimap schématique temps réel (T-13 — tâche 2 utilisateur, délégué assistant + revue chef)

Contexte : demande utilisateur 2026-07-24 — minimap calquée sur la grille de jeu (32×32), bas-gauche de l'écran ; rond rouge = ennemi, carré vert = bâtiment, rond vert = allié. Choix validés : bâtiments = tourelles + pièges ; affichage avec **toggle M**. MCP toujours down → build **100 % code**, zéro édition de scène.

- **`Assets/Scripts/Game/Units/Unit.cs`** — *(modifié)* registre statique `public static readonly List<Unit> All` maintenu par `OnEnable`/`OnDisable` (les unités détruites en sortent). Diff minimal, zéro changement de comportement.
- **`Assets/Scripts/Game/Defenses/TurretManager.cs`** + **`TrapManager.cs`** — *(modifiés)* même registre `All` (tourelles / pièges posés).
- **`Assets/Scripts/Game/MinimapUI.cs`** — *(créé)* auto-spawn via `SceneManager.sceneLoaded` si une `LevelZone` existe (fallback `FindAnyObjectByType` — anomalie connue `Instance` null ; rien créé au menu). Canvas overlay `sortingOrder 100` (sous les écrans 500), panel 208 px ancré bas-gauche (marge 24 px, réf. 1920×1080), fond α 0,65. Grille des cellules + bordure dessinées sur `Texture2D` 256×256 procédurale (Bilinear, HideAndDontSave, **cache statique anti-fuite**). Icônes poolées (160 max) : cercle 32×32 procédural à bord fondu ; carré = `Image` sans sprite (quad plein). Refresh 0,15 s **unscaled** (vivante en pause). Projection `transform.InverseTransformPoint` / `WorldSize` → gère le yaw de la zone, hors-zone ignorés. Empilage : bâtiments < alliés < ennemis (menaces au-dessus). Toggle **M** (`Keyboard.current`). Build différé si la zone n'est pas prête.

### À FAIRE / à surveiller
- **Tester en jeu** : icônes suivent ennemis/tourelles/pièges en temps réel, M affiche/masque, rien au menu principal, 0 erreur console.
- Si un futur niveau a une taille de grille différente : le sprite de grille se régénère automatiquement (cache par `SizeInCells`).
- Pool de 160 icônes : au-delà, les entités excédentaires sont simplement ignorées (à augmenter si des vagues dépassent ~150 unités).

## 2026-07-24 — Polish minimap + système de succès (T-14 — délégué assistant + revue chef)

Contexte : MCP revenu. Demandes utilisateur : ① polish léger minimap (pack léger + boss violet), ② système de succès affiché **uniquement au menu principal**, ouverture **Echap**, fond pancarte (même style que Discord/Touches), 4 succès : lier Discord / finir niveau 1 / finir le 1ᵉʳ boss / finir tous les niveaux.

**Polish minimap (chef, commit `5c56ceb`)** — `MinimapUI.cs` : panel 208→240 px, fond α 0,72, bordure crème 3 px, titre « CARTE » au-dessus du panneau ; en niveau boss (`LevelSelectManager.SelectedLevel.isBoss` — il n'existe pas d'unité boss distincte, le boss est un flag de NIVEAU) les ennemis = gros ronds violets ×1,8 (taille réappliquée à chaque placement car le pool réutilise les icônes).

**Succès (assistant, revue + accents par le chef)** :
- **`Game/Achievements/AchievementStore.cs`** *(créé)* : `AchievementDef` (classe simple) + 4 defs ; flags PlayerPrefs `Ach.<id>` **monotones** ; `EvaluateAll()` null-safe appelé à chaque ouverture de l'écran ; `ResetAll()` pour le wipe.
- **`UI/PancarteStyle.cs`** *(créé)* : palette bois centralisée (copiée de LinkAccountScreen) + `LoadPlank`/`ApplyVeil`/`ApplyPlank`/`StyleButton`. Duplication assumée avec `LinkAccountScreen.ApplyPancarteSkin` (non refactoré pour ne pas casser un écran validé — à factoriser plus tard).
- **`Menus/AchievementsScreen.cs`** *(créé)* : auto-spawn si scène == `MainMenuScene` (idiome MinimapUI, zéro édition de scène), canvas 500, voile + planche 1240×675.86, titre « SUCCÈS », 4 lignes (titre/description/état DÉBLOQUÉ vert / VERROUILLÉ gris), bouton FERMER, Echap toggle. Gardes anti-conflit : n'ouvre pas si `KeybindsScreen.IsOpen` / `LinkAccountScreen.IsOpen` / `InventoryScreen.IsOpen`.
- **`LevelButtonUI.cs`** *(+3 l.)* : `public LevelData Data => levelData;`.
- **`LevelSelectManager.cs`** *(+22 l.)* : `GetOrderedLevels()` (tri par levelID).
- **`MenuManager.cs`** *(+2 l.)* : `WipeSave()` appelle aussi `AchievementStore.ResetAll()`.
- **`LinkAccountScreen.cs` / `InventoryScreen.cs`** *(+14 l. chacun)* : `static bool IsOpen` (true dans Open, false dans les 2 chemins de Close).

### À FAIRE / à surveiller
- **Tester au menu** : Echap ouvre/ferme les succès, les états passent en vert après avoir lié Discord / terminé des niveaux, Wipe save réinitialise.
- Erreurs console **pré-existantes** (sans rapport, repérées au reload 2026-07-24) : 3× « referenced script missing » dans la scène ouverte (séquelles du merge ?), `UniversalRenderPipelineGlobalSettings` missing types (dérive URP), NRE `CameraSystem.cs:71` au OnEnable. À investiguer dans un ticket dédié si gênant.
- Toast en jeu au déblocage d'un succès : non fait (store a tout ce qu'il faut via `EvaluateAll`).

## 2026-07-24 — Bouton Discord (T-19) + drop zone ennemis (T-20) + vérif succès (T-17 partiel)

**T-19 (chef, commit `4113aaf`)** — **`LinkAccountScreen.cs`** *(modifié)* : const `DiscordInviteUrl` (`https://discord.gg/qAtH7XuHc`) + bouton « REJOINDRE LE DISCORD » créé dans `ApplyPancarteSkin` (runtime, listener vivant), inséré au-dessus de FERMER (`SetSiblingIndex` sur l'index de `closeButton`), stylé `RestyleButton(WoodNormal/WoodHover)`, `Application.OpenURL`. Visible dans les 2 états (choix utilisateur). **`AchievementsScreen.cs`** : `Open()` passé `public` (hook test MCP).

**T-20 (assistant, revue + fix compile chef, commit `dd0941f`)** — **`Game/Units/EnemyVisuals.cs`** *(créé)* : statique ; `Apply(enemyRoot, bossLevel)` — `Resources.Load<GameObject>("Enemies/enemy_default"|"enemy_boss")` (cache statique, null mémorisé), enfant « Visual » instancié, `MeshRenderer` du pion **masqué** (jamais détruit), auto-scale `localScale *= pionHeight / modelHeight` (bounds monde) ; no-op si rien déposé. **`CombatManager.cs`** *(+3 l.)* : appel dans `SpawnEnemy()` avec flag `Menus.LevelSelectManager.SelectedLevel.isBoss` (⚠️ pas de `using Menus;` dans ce fichier → nom qualifié). **`Assets/Resources/Enemies/LISEZ-MOI.txt`** *(créé)* : notice drop zone.

**T-17 partiel (chef, MCP)** — Play MainMenuScene, `execute_code` → `AchievementsScreen.Open()` forcé : rendu pancarte OK, 4 lignes OK, « Connecté » DÉBLOQUÉ (compte effectivement lié), autres VERROUILLÉ. Capture `Assets/Screenshots/T17_succes_force_open.png`.

### À FAIRE / à surveiller
- **Tester en Play** : clic « REJOINDRE LE DISCORD » (2 états), Echap succès, déblocage par complétion, Wipe.
- T-15 (assets) : il suffira de glisser `enemy_default.fbx` (+ `enemy_boss.fbx` optionnel) dans `Assets/Resources/Enemies/` — vérifier alors orientation/échelle et matériaux (remap éventuel dans l'importeur FBX, pas dans le code). FBX = LFS (quota).
- `cache` statique d'`EnemyVisuals` : avec domain reload standard OK ; si « Enter Play Mode Options » sans reload un jour → prévoir un reset (comme les autres registres statiques du projet).
