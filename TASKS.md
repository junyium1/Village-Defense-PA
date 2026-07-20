# TASKS.md — Tickets du projet (mode solo OpenCode Go)

**Convention :**
- `[ ]` à faire · `[x]` fait · `[~]` en cours
- Statut : `ready` (prêt), `blocked` (attend qqch), `review` (fait, à valider)
- Un ticket = un commit atomique côté OpenCode

---

## T-01 (ready) : Retirer les routes /debug/* du backend
**Zone :** Backend Python — `/home/taqkt/docker/discord-bridge/main.py`
**Contexte :** Routes `/debug/generate-pin` (+ 1 autre) ajoutées pour tests de liaison. Doivent partir avant prod.
**Fichiers :** `main.py` (chercher `@app.` avec chemin `/debug`)
**Specs :**
- Supprimer TOUTES les routes préfixées `/debug/`
- Vérifier qu'aucun autre fichier ne les référence (`rg "/debug/" .`)
- Ajouter dans le CHANGELOG : "Retrait routes debug pré-prod"
**Acceptance :** `rg "/debug/" .` retourne 0 résultat. `curl -H "x-api-key:XXX" .../debug/generate-pin` → 404.

---

## T-02 [x] (review) : Ajout SendBattleReportAsync au DiscordAPIBridge
**Zone :** Unity C# — `Assets/Scripts/DiscordBridge/Networking/DiscordAPIBridge.cs`
**Contexte :** Prompt 4 backend en attente. Prépare le client pour qu'il n'y ait qu'à câbler quand le serveur suit.
**Fichiers :**
- `Assets/Scripts/DiscordBridge/DTOs/BattleReportDTO.cs` (nouveau, namespace `DiscordBridge.DTOs`, ne PAS créer le .meta — Unity le générera)
- `Assets/Scripts/DiscordBridge/Networking/DiscordAPIBridge.cs` (nouvelle méthode)
**Specs :**
- DTO : `{ discord_id: string, wave_reached: int, kills: int, victory: bool, duration_seconds: int }` — attributs `[JsonProperty]` Newtonsoft
- Méthode `Awaitable<ApiResult> SendBattleReportAsync(BattleReportRequest dto)`
- Endpoint `POST /api/battle-report`, header `x-api-key`, corps Newtonsoft
- Pas de HMAC (route non-signée pour l'instant)
- Suivre le pattern exact de `SendGameEventAsync` (mêmes error handlers, même `ApiResult`)
**Acceptance :** Compile 0 erreur. Méthode appelable, retourne `ApiResult.Failure` si serveur 404 (normal, route pas encore côté serveur). Aucun fichier `.meta`/`.unity`/`.prefab` dans le diff.

---

## T-03 (ready) : Backend Prompt 3 — anti-farm et boucle d'attaque
**Zone :** Backend Python — `/home/taqkt/docker/discord-bridge/main.py` (copie repo : `Server/discord-bridge/main.py`)
**Contexte :** GDD : les ratons rapportent trop (farm infini) et les attaques Discord n'ont pas de conséquence si le joueur ignore l'alerte.
**Fichiers :** `main.py` (+ schéma SQLite si besoin de colonnes)
**Specs :**
- `/ratons` : cooldown 1h par `discord_id` (persisté SQLite, pas en mémoire) ; réponse d'erreur explicite avec temps restant si rappelé trop tôt.
- DefenseView : fenêtre de réponse 300 s ; à expiration sans clic → pénalité -25 mana (plancher 0) + message de résultat édité.
- Raids serveur aléatoires : tâche périodique qui déclenche une attaque sur un joueur lié actif (fréquence raisonnable, ex. 1 tirage/h, probabilité faible) réutilisant le flux DefenseView existant.
- Bouclier auto-défense : si `shield_10m` actif (`effets_actifs` non expiré) au moment d'une attaque → attaque annulée, message « bouclier a absorbé l'attaque », pas de pénalité.
**Acceptance :** `python -m py_compile main.py` OK. Cooldown vérifiable par 2 appels `/ratons` successifs (2e → erreur). Aucune régression sur `ITEM_DURATIONS`, `/api/consume-item`, `/api/add-mana` (anti-triche intact).

---

## T-04 (blocked — attend Prompt 4 serveur) : Câblage battle report + tests effets en jeu
**Zone :** Unity scènes + Play mode — OpenCode via MCP (mode solo, plus de restriction)
**Contexte :** Après T-02 (client) et le futur Prompt 4 serveur : remplacer l'alerte « défends-toi » de `GameplayBridgeHooks` par un rapport de fin de partie ; tester gel/bouclier/renforts en GameScene.
**Statut :** Partie « tests effets en jeu » faisable dès que le MCP est validé. Partie « battle report » bloquée côté serveur.

---

## T-05 (blocked — MCP à valider après restart) : Mise au propre de l'affichage de tous les menus
**Zone :** Unity UI — toutes scènes (directive utilisateur 2026-07-19, session 2)
**Contexte :** L'utilisateur veut une passe déco/propreté globale sur les menus du jeu.
**Inventaire à faire via MCP :** `MainMenuScene` (StartMenuCanvas, LinkAccountScreen_Panel, InventoryScreen_Panel, ManaHUD, boutons) + `GameScene` (pause menu, HUD) + autres scènes éventuelles.
**Specs :** Plan-avant-code obligatoire (lister étapes + fichiers/scènes impactés, attendre validation). Pièges connus : alpha 1.0 strict fonds opaques, coroutines dt ≤ 0.05s, accents TMP (vérifier glyphes), panels plein écran opaques en dernier sibling.
**Acceptance :** Tous les menus cohérents visuellement (palette, boutons arrondis, animations), vérifiés en Play, 0 erreur console.
> **Note (session 3) :** le **menu principal 3D « signpost »** est sorti en tickets dédiés **T-06 → T-10** (séquencés, dépendants). T-05 = reste des menus 2D (inventaire / discord / pause).

---

## T-06 [x] (review) : Menu 3D — assainir + hiérarchiser le signpost racine
**Zone :** Unity scène `MainMenuScene` via MCP (objets importés des FBX du menu).
**Contexte :** Voir `.claude_memory.md` § « 🪧 MENU 3D SIGNPOST ». Décor + 3 panneaux importés, **couleurs déjà OK** (matériaux `MM_*` remappés). Problème : objets à coords monde Blender (~378,84,190), scale minuscule, `sign1_0.006` Scale 100 / rot -90° ; point de transform ≠ rendu réel.
**Fichiers/objets :** `MainMenuScene` ; objets issus de `Assets/Art/MainMenu/titlescreen.fbx` (planches `Scene_-_Root`, `sign1_*`, `Cube.002/004`, chaînes). **Ne PAS éditer les `.meta` ni les `.fbx` — MCP uniquement.**
**Specs (révisées, validées par l'utilisateur) :**
- ⚠️ **Préserver le placement manuel de l'utilisateur** : fait — rendu strictement identique (bounds avant/après égales au 1/1000e, captures `t06_avant`/`t06_apres` identiques).
- Critère « 0 scale 100/rot -90 » **remplacé** par « wrappers neufs propres (scale 1/rot 0) » : les transforms d'import (scale 100/rot 270, pivots cuits dans les vertices) ne se corrigent pas sans re-baker les meshes → on emballe, on ne retouche pas.
- ⚠️ **PIÈGE RÉSOLU** : `menu_signs` était une **instance de prefab modèle FBX** → `SetParent` refusé (« resides in a Prefab instance »). Fix : `PrefabUtility.UnpackPrefabInstance(..., Completely, ...)` puis reparentage.
**Résultat (fait 2026-07-19, session 4) :**
- `menu_signs` renommé **`MENU_3D`** (dépacké) ; structure :
  - `MENU_3D/Signpost_Root` (pivot = centre poteau, scale 1) : `Pole`, `Board_Title`, `Plank_LevelSelect`, `Plank_Options`, `Plank_Wipe`, `Plank_Quit`
  - `MENU_3D/LevelSelectSign_Root` : `Pole`, `Board`
  - `MENU_3D/ChainedSign_Root` : `Board`, `ChainL`, `ChainR` (extraits des sous-arbres Sketchfab)
  - `MENU_3D/Decor` (760 enfants : Tree×323, Cube×420, Plane×2, sign*, Sketchfab husks…) / `BlenderRefs` : `Camera` + `Light` Blender, **composants désactivés** (doublons Main Camera / Directional Light). La caméra Blender sert de réf de cadrage pour T-07.
- Scène sauvegardée. 0 nouvelle erreur console.
**Acceptance :** ✅ rendu identique (captures + bounds), hiérarchie propre, pivots wrappers propres. À valider visuellement par l'utilisateur.

---

## T-07 [x] (review) : Menu 3D — cadrage caméra + éclairage
**Zone :** `MainMenuScene` via MCP. **Dépend de T-06.**
**Résultat (fait 2026-07-19, session 4) :**
- Décision utilisateur : sous-menus **100% 3D** (pas de panels 2D). `LevelSelectSign_Root` et `ChainedSign_Root` **réalignés sur le point focal du signpost titre** puis désactivés (swap à la navigation, FX plus tard). Placement final ajusté à la main par l'utilisateur : LS=(378.945,84.583,198.294) · Chained=(378.942,84.510,198.294) — **le préserver**.
- **Cadrage caméra fait par l'utilisateur** : Main Camera pos=(378.975,84.625,198.939) rot=(6.8,178.9,0.2), near clip 0.05. **Ne pas écraser.**
- Éclairage existant validé (Directional Light chaude rot (50,330,0), ombres douces, skybox).
- **`MM_Dirt.mat`** (marron clair 0.75,0.58,0.40) créé et assigné à `Plane.001` (chemin de terre) — le « dirt ground » FBX était fautif à l'export. Assignation directe sur le renderer de scène (pas de remap FBX).
**Acceptance :** ✅ captures validées par l'utilisateur. Commit `f43576b`.

---

## T-08 [x] (review) : Menu 3D — textes TextMeshPro 3D sur les planches
**Zone :** `MainMenuScene` via MCP. **Dépend de T-06.**
**Résultat (fait 2026-07-19, session 4) :**
- 7 `TextMeshPro` 3D créés (enfants des planches, **pas de Canvas**) : `TMP_Title` (« MAGICIANS UNDER ATTACK! », wrap) sur `Board_Title` ; `TMP_Niveaux`/`TMP_Options`/`TMP_Reinitialiser`/`TMP_Quitter` sur les 4 planches (libellé Wipe = **« Réinitialiser »**, choix utilisateur) ; `TMP_TitleLS` (« Niveaux ») et `TMP_TitleOpt` (« Options ») sur les panneaux secondaires (inactifs — auto-size se calculera à l'activation).
- **Technique** : texte enfant de la planche, `localScale = 1/lossyScale` parent (neutralise le scale 100 d'import), positionné au centre de la face (+2 mm), `sizeDelta` = bounds planche ×0.95/0.9, auto-sizing ON (min 0.002 / max 0.035-0.06), couleur crème (0.96,0.92,0.82), aligné centre/milieu.
- **⚠️ PIÈGE TMP 3D** : la face lisible d'un `TextMeshPro` 3D (rot identity) regarde le **−z** → les textes étaient en miroir pour la caméra (qui regarde −z depuis +z). Fix : **rotation Y=180°** sur tous les textes. Règle : orienter le texte pour que sa face lise vers la caméra.
- Accents OK (« Réinitialiser » rendu). Captures `t08_textes_v1/v2`. Commit après validation.
**Acceptance :** ✅ textes lisibles en vue caméra (capture v2 validée).

---

## T-09 (ready) : Menu 3D — script hover planches `SignPlankHover.cs`
**Zone :** Unity C# `Assets/Scripts/Menus/SignPlankHover.cs` (nouveau) + câblage scène MCP. **Dépend de T-06.**
**Contexte :** Feedback survol (réf : contour blanc + agrandissement + inclinaison).
**Fichiers :** `Assets/Scripts/Menus/SignPlankHover.cs` (namespace `Menus`). Chaque `Plank_*` doit avoir un `Collider`.
**Specs :**
- `MonoBehaviour` ; `OnMouseEnter` → lerp scale ×1.05 + inclinaison ~6° autour d'un pivot « clou » (haut de planche) + activer contour blanc ; `OnMouseExit` → retour exact à l'état initial.
- Animation par **coroutine**, `deltaTime` plafonné **0.05**. Mémoriser scale/rotation de départ (ne pas cumuler).
- Contour blanc : soit toggle d'un enfant outline (mesh inversé inverse-hull), soit `MaterialPropertyBlock` (emission/couleur) — **aucune fuite sur les planches voisines**.
- Exposer `UnityEvent onHoverEnter` / `onHoverExit` pour câblage optionnel.
- Code complet, compilable, aucun placeholder.
**Acceptance :** Compile 0 erreur (`read_console`). En Play : survol d'une planche → grossit + s'incline + contour ; sortie → revient exactement à l'état initial. Aucune planche voisine affectée.

---

## T-10 (ready) : Menu 3D — pirouette + swap menu `SignpostRotator.cs` + câblage
**Zone :** Unity C# `Assets/Scripts/Menus/SignpostRotator.cs` (nouveau) + câblage scène MCP. **Dépend de T-06, T-08 ; utilise T-09.**
**Contexte :** Transition entre menus : clic planche → le signpost fait une pirouette et change de contenu. Options/discord = pancarte enchaînée qui tombe.
**Fichiers :** `Assets/Scripts/Menus/SignpostRotator.cs` (namespace `Menus`). Panneaux level-select / options(chaînes) présents en scène.
**Specs :**
- Coroutine : rotation ~180° de `Signpost_Root` ; **à mi-rotation (90°)** activer le panneau cible et désactiver le précédent (swap invisible dos-à-dos) ; `deltaTime` plafonné **0.05**.
- Panneaux enchaînés (options/discord) : anim « drop » (Y lerp descendant + léger rebond) au moment de l'affichage.
- **Câbler les clics des planches → `StartMenuManager`** (déjà codé, `namespace Menus`) : `Plank_LevelSelect`→`GoToLevelSelect()`, `Plank_Options`→`OpenOptions()`, `Plank_Wipe`→`WipeSave()`, `Plank_Quit`→`QuitGame()` (via `UnityEvent` / `OnMouseUpAsButton`). Retour = `GoToMainMenu()` / `CloseOptions()`.
- Code complet, compilable, aucun placeholder.
**Acceptance :** En Play : clic « Niveaux » → pirouette → panneau level select ; clic « Options » → pirouette + pancarte enchaînée qui tombe ; retour revient au menu principal ; `WipeSave`/`QuitGame` appelés correctement. 0 erreur console.

---

## T-11 [x] (review) : Menu 3D — élaboration du menu réglages (pancarte enchaînée)
**Zone :** `MainMenuScene` via MCP + scripts `Assets/Scripts/Menus/`. **Dépend de T-10.**
**Contexte :** Le panneau options = `ChainedSign_Root` (pancarte enchaînée : `Board`, `ChainL`, `ChainR`, `TMP_TitleOpt` « Options »), co-localisé sous `FlipPivot`, inactif au repos, swappé par pirouette. Demande utilisateur 2026-07-20 : élaborer le contenu du menu réglages.
**Étape 1/2 — transition dédiée « bousculade » : ✅ VALIDÉE EN PLAY (2026-07-20, « c'est bon »).** Choix utilisateur : la pancarte options arrive **en pendule** (pas de balayage latéral) et percute le titre qui **tombe en arrière** ; retour = miroir. `SignpostPushSwap.cs` (nouveau, **auto-câblé** par `Menu3DController.Awake` → zéro modif de scène, zéro MCP) : pendule pivot = haut des chaînes (bounds max Y), chute pivot = base poteau (bounds min Y), contact décalé de `contactLeadDeg` (anti-chevauchement), oscillation amortie, chute ease-in + rebond, relèvement easeOutBack. Poses restaurées exactement (pas de dérive). `IsBusy` static gèle l'input (`Menu3DInput` gaté sur les 2 busies). Pirouette conservée pour Niveaux ; garde-fou `SnapHomeSignpost()` si le titre est réaffiché via pirouette. **Le titre reste VISIBLE couché au sol** derrière la pancarte (demande 2026-07-20) — ses BoxColliders sont coupés tant qu'il est à terre. Réglage fin au ressenti Play : poser le composant à la main sur le GO `Menu3D` pour tweaker les angles/durées (publics). **⚠️ À vérifier en Play : pendule ne clippe pas la caméra (sinon baisser `swingStartDeg`).**
**Étape 2/2 — contenu des options : ✅ FAIT (2026-07-20, OpenCode).** 5 planches réglages + persistance PlayerPrefs : `Musique : ON/OFF` (toggle), `Volume musique : X %` (cycle 0/25/50/75/100), `Effets : ON/OFF` (toggle), `Volume effets : X %` (cycle), `Qualité : PC⇄Mobile` (cycle) — + `Plank_Back` inchangé. **`MainMixer.mixer`** (Resources : Music/SFX, params `MusicVolume`/`SfxVolume` en dB) créé (YAML manuel — non scriptable Unity 6) ; 4 AudioSources routées (menu→Music, PlacementSystem×2→SFX, prefab→Music). `SettingsStore.cs` (statique PlayerPrefs, event `Changed`) + `SettingsApplier.cs` (boot différé 0,05/0,3 s — ⚠️ le snapshot mixer est appliqué APRÈS les RuntimeInitializeOnLoadMethod, SetFloat immédiat écrasé). `SignOptionToggle` refactoré (générique MusicOn/SfxOn) + `SignOptionCycle` (nouveau). `QualitySettings.asset` : exclusion Standalone retirée du niveau Mobile (sinon names filtré = 1 seul niveau sur PC). **Vérifié en Play** : mixer −2,5 dB au lancement, toggles/cycles/persistance OK, labels OK, 0 erreur. ⚠️ Positions des planches **provisoires** (pile sous le board) → **placement final par l'utilisateur**. Détails + pièges : `cleanup_log.md` (2026-07-20 T-11 2/2).
**Existant (vérifié) :**
- `SignOptionToggle.cs` : planche toggle Musique ON/OFF via `AudioListener.volume` (0 / 0.5), label TMP « Musique : ON|OFF » — **aucune persistance** (réglage perdu au relancement).
- `MusicVolumeController.cs` : legacy UI 2D (Toggle+Slider) avec `// TODO implement saving settings` et `// TODO handle different audio sources (music VS SFX)` — non réutilisé en 3D.
- Anim « drop » de la pancarte (chaînes) : **jamais implémentée** (la spec T-10 la mentionnait, la pirouette seule a été faite).
**Specs :**
- **Plan-avant-code obligatoire** : définir avec l'utilisateur la liste des réglages exposés (candidats : Musique ON/OFF — déjà codé, volume musique, volume SFX — TODO legacy, qualité, plein écran…) et la persistance (PlayerPrefs via clés dédiées).
- Une planche par réglage sur la pancarte enchaînée : TMP 3D (règles T-08 : rotation Y=180°, `localScale = 1/lossyScale`), `BoxCollider` refit sur `mesh.bounds`, `SignPlankHover` (glow), `SignOptionToggle` / nouveau(s) script(s) dédiés.
- Persistance des réglages au clic + relecture à l'ouverture de scène.
- Anim « drop » (descente Y + rebond léger, coroutine, dt plafonné 0.05) à l'affichage de la pancarte — si validée dans le plan.
- Retour : planche dédiée → `Menu3DController.ShowMain()` (pirouette) ; `StartMenuManager.CloseOptions()` reste la référence logique.
- ⚠️ Pièges : edits scène MCP **hors Play** uniquement ; `Collider.bounds` lit (0,0,0) en mode Édition après refit (cache PhysX — donnée fiable = `size` locale) ; caméra X=377.95 à ne pas toucher ; hover glow via `_BaseColor`/`_EmissionColor`.
**Acceptance :** En Play : clic « Options » → pirouette → pancarte affichée (drop si retenu) ; chaque réglage fonctionne, persiste après relancement, et se relit correctement ; retour menu principal OK ; 0 erreur console.

---

## T-12 (ready) : Menu 3D — cel shading + éclairage sunset (assigné OpenCode)
**Zone :** `Assets/Shaders/` (fichiers neufs) + `Assets/Art/MainMenu/Materials/` + rig lumières de `MainMenuScene` (via prefab `MenuLighting` + une insertion scène unique).
**Contexte :** Demande utilisateur 2026-07-20 : OpenCode prend les shaders/lights du menu pendant que Claude Code continue le reste. **Cohabitation : zones de fichiers disjointes** (OpenCode = shaders/matériaux/lights menu ; Claude = le reste) ; touches scène en créneaux courts ou par prefab ; coordination via ce fichier + `.claude_memory.md`. **Plan détaillé à revalider avec l'utilisateur au démarrage du ticket.**
**Specs (proposition) :**
- Shader toon URP (Shader Graph + Custom Function HLSL wrappant `GetMainLight()`/`GetAdditionalLight()`) : NdotL quantifié 2-3 paliers (`smoothstep` serrés), rim light fresnel, ambiant SH teinté. Propriétés **obligatoires** : `_BaseMap`, `_BumpMap`, `_BaseColor`, `_EmissionColor` (compat hover glow `SignPlankHover` via MaterialPropertyBlock).
- Matériaux `MM_*Toon` dupliqués (originaux `MM_*` conservés), swap par assignation scène directe (méthode éprouvée T-07), créneau scène court.
- Éclairage sunset : Directional chaude angle bas ~15°, 2-3 spots d'accent sur le signpost via **Light Layers** (le décor fait 760 objets), brouillard léger + skybox teintée. Lights d'accent **quantifiées** dans la boucle additional lights du shader (sinon elles cassent l'effet).
- Pas d'outline mesh (inverted-hull déjà écarté — `PlankOutline.shader` inutilisé) ; contour = rim/fresnel.
- ⚠️ Vérifier le hover glow après swap des matériaux ; attention lisibilité TMP crème sur bois sombre éclairé sunset.
**Acceptance :** Rendu cel shading cohérent sur tout le menu (décor + signpost), hover glow fonctionnel, TMP lisibles, captures MCP validées par l'utilisateur, 0 régression nav/hover/pirouette.

---

## Notes pour OpenCode
Écris ici si un ticket est ambigu ou touche une zone interdite. Ne bloque pas la file — passe au suivant.

### Avancement menu 3D (2026-07-20)
Kimi K3 a fait la nav + grille niveaux + toggle musique (T-08/T-10 partiels), **arrêté crédit épuisé**. Claude (MCP) a fini via une structure différente des tickets d'origine :
- **T-06** (hiérarchie/assainir) : ✅ `Signpost_Root`/`LevelSelectSign_Root`/`ChainedSign_Root` + `FlipPivot` (co-localisation).
- **T-07** (caméra) : ✅ caméra fixe X=377.95.
- **T-08** (textes TMP) : ✅ (Kimi) — titres + libellés, lisibles.
- **T-09** (hover) : ✅ scale ×1.05 + tilt 6° + **glow blanc** (MaterialPropertyBlock ; l'inverted-hull rendait mal sur planche plate).
- **T-10** (pirouette + câblage) : ✅ `SignpostRotator` + `Menu3DController` → `StartMenuManager`, 3 transitions vérifiées Play.
- Bonus : grille Niveaux recadrée (scale 0.60) pour que « Retour » soit dans le cadre.
Détails complets : `.claude_memory.md` § MENU 3D SIGNPOST + `cleanup_log.md` (2026-07-20).