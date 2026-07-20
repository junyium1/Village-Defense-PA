---
description: Délègue un ticket de TASKS.md à OpenCode Go (kimi-k3) et vérifie le diff
argument-hint: <ticket, ex. T-02>
allowed-tools: Bash(pwsh -File ./scripts/delegate.ps1:*), Bash(git diff:*), Bash(git status:*), Bash(git checkout:*)
---

Délègue le ticket **$ARGUMENTS** à OpenCode Go. Séquence stricte :

1. **Lire `TASKS.md`** et trouver le ticket $ARGUMENTS. S'il n'existe pas, est `[x]`, ou est `blocked` → refuser et expliquer pourquoi.
2. **Vérifier la zone** : le ticket ne doit toucher QUE `Assets/Scripts/**/*.cs`, le backend Python, des tests, de la doc ou `TASKS.md`. S'il implique `.unity`/`.prefab`/`.meta`/`.asset`/MCP Unity/Play mode → refuser, expliquer, proposer de le traiter soi-même.
3. **Construire une instruction AUTONOME** (OpenCode n'a aucune mémoire de nos conversations) : specs complètes du ticket + chemins de fichiers exacts + critères d'acceptance mesurables. Ne pas renvoyer vers « la conversation ».
4. **Lancer** : `pwsh -File ./scripts/delegate.ps1 $ARGUMENTS "<instruction>"` (le script logge dans `.delegate-logs/`).
5. **`git diff --stat` OBLIGATOIRE** après le run. Si un fichier `.unity`, `.prefab`, `.meta` ou `ApiConfig.asset` apparaît dans le diff (comparer aussi `git status` pour les fichiers créés) → `git checkout -- <fichier>` immédiat (ou suppression signalée si créé) + signaler la violation à l'utilisateur.
6. **Mettre à jour `TASKS.md`** : cocher `[x]` si l'acceptance est satisfaite (vérifier réellement, pas sur parole du log) ; sinon noter l'échec sous le ticket avec la cause, statut `review`.

Résume à la fin : ticket, fichiers modifiés, verdict acceptance, anomalies du diff.
