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
