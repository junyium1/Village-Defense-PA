# 🚨 RÈGLES ABSOLUES (Mode Expert)
- Zéro blabla : aucune salutation, introduction ou conclusion.
- Sois factuel et dense. Ne justifie tes choix que si je te demande "Pourquoi ?".
- Si un concept critique nécessite mon attention, demande-moi l'autorisation avant d'en faire l'explication.
- Code complet : fournis des scripts 100% fonctionnels et remplaçables (aucun placeholder type `// reste du code`).
- Bugs : donne uniquement le bloc ou la ligne corrigée.
- Termine STRICTEMENT chacune de tes réponses par cette chaîne de caractères : O_O

# 💾 MÉMOIRE PERSISTANTE (Memory Bank)
- À ta toute première action dans une nouvelle session, tu DOIS lire le fichier `.claude_memory.md` situé à la racine.
- Ce fichier contient l'état actuel du projet, l'arborescence vitale, et les règles d'architecture.
- À la fin de chaque tâche complexe (ajout de feature, refactoring), tu DOIS mettre à jour `.claude_memory.md` de manière autonome pour documenter tes propres changements, afin que ta "future version" dans une autre session puisse reprendre le relais.

# 🧠 CYCLE DE DÉVELOPPEMENT : PLAN Aclaude.mdVANT CODE
Pour toute demande d'architecture (hors correction de syntaxe ou renommage) :
1. Liste les étapes de modification et les fichiers impactés (C# et Python).
2. Attends MA validation avant d'écrire le moindre bloc de code.

# 🗑️ GESTION DES FICHIERS ET SÉCURITÉ
- NE SUPPRIME JAMAIS un fichier sans mon autorisation.
- Traçabilité : Maintiens `cleanup_log.md` à la racine. Pour toute suppression validée ou modification majeure, loggue l'action (Chemin, Ligne, Action) pour nettoyage ultérieur.

# 🎮 CONTEXTE : LAST MAGICIANS
- Projet hybride : Frontend Unity (C#) et Backend Python.
- Architecture : Utilisation massive de ScriptableObjects pour la data (Ennemis, Unités, Défenses).
- Priorité : Optimisation des performances et propreté de l'arborescence.

# ⚡ OPTIMISATION DES TOKENS ET OUTILS
- Interdiction des recherches globales type `ls -R`.
- Utilise `grep` / `rg` pour cibler des mots-clés avant de lire un fichier entier (limite d'ouverture : 200 lignes max).
- Utilise EXCLUSIVEMENT le pont MCP Unity pour inspecter la scène.
- Si le MCP est indisponible (erreur ou timeout lors d'une compilation), ne devine pas. Patiente et relance ta requête.

# 🧠 ROUTAGE : DÉLÉGATION
Avant d'exécuter une tâche, évalue le niveau d'effort requis :
- Niveau "Faible / Moyen" (boilerplate, DTOs, backend Python, doc, tests, renommage) -> **DÉLÈGUE** à OpenCode Go : rédige/complète le ticket dans `TASKS.md` puis `/delegate T-XX`. Ne suggère plus Haiku.
- Niveau "Élevé" (logique standard, dev quotidien) -> Périmètre de Sonnet. Exécute directement.
- Niveau "Ultra Code" (architecture lourde, croisement de +4 fichiers DiscordBridge, MCP) -> Arrête-toi et réponds UNIQUEMENT : "[SUGGESTION : Effort Ultra Code] Tape `/model` et passe sur Opus."
- **JAMAIS DÉLÉGUÉ** : MCP Unity, scènes/prefabs/meta, logique HMAC, typage `discord_id`, `.claude_memory.md`.
- Après CHAQUE délégation : `git diff --stat` obligatoire ; fichier interdit dans le diff → `git checkout` immédiat + signalement.

# 🤝 DUO AVEC OPENCODE GO
- OpenCode Go (GLM-5.1 / Kimi K2.6) est l'exécutant. Il lit `AGENTS.md` et `TASKS.md`.
- Quand tu planifies (Plan avant code), tu peux **déléguer les tickets simples** en les écrivant dans `TASKS.md` avec statut `ready` — spécifications complètes, fichiers listés, critères d'acceptance mesurables.
- Tu gardes pour toi : MCP Unity, refactors multi-fichiers touchant DiscordBridge, tout ce qui manipule scènes/prefabs, mise à jour de `.claude_memory.md`.
- Format ticket dans `TASKS.md` : voir la structure existante (`T-XX (statut) : titre` + Zone/Contexte/Fichiers/Specs/Acceptance).
- Si OpenCode a ajouté une note sous un ticket, la traiter avant de replanifier.