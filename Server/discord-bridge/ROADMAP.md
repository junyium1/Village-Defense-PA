# 🛡️ Passerelle Discord / FastAPI — État des lieux & Roadmap

> Document de suivi du serveur (`main.py`). Basé sur l'audit du code de la branche
> `feature/discord-bridge`. Dernière mise à jour : 2026-07-16.

---

## ✅ FAIT & FONCTIONNEL (côté serveur)

### 1. Base de données SQLite — `init_db()`
- Table **`joueurs`** : `discord_id` (PK), `mana`, `notif_pref` (défaut `mp`), `fief_channel_id`.
- Table **`inventaire`** : `discord_id`, `item_id`, `date_achat`.
- Migrations automatiques via `ALTER TABLE` (n'écrase pas une DB existante).

### 2. Liaison de compte par code PIN
- Bouton **« Lier mon Grimoire »** → code à 4 chiffres, salon privé `portail-<user>`,
  auto-destruction en 15 min.
- Codes stockés dans `pending_links` avec expiration, **usage unique**.
- Route **`POST /api/link-account`** : Unity envoie le code → validation → création
  du joueur → **création automatique du Fief**.

### 3. Le Fief (QG privé permanent) — `create_fief_for_user()`
- Salon privé `fief-<user>` visible seulement par le joueur.
- Affiche le **solde de Mana** + menu de préférences : 📱 MP / 🔔 Ping Fief / 🔕 Silencieux.
- Menu **persistant** après reboot (`add_view` dans `on_ready`).

### 4. La Taverne — événements aléatoires — `event_loop()`
- Boucle toutes les **30 min**, **20 %** de déclenchement, puis **50/50 Marchand ou Boss**.
- **🎒 Marchand + Boutique** : filtre « compte lié », 6 objets, débit Mana + ajout inventaire.
- **🚨 Boss coopératif** : ≈ 50 % des membres humains doivent attaquer, barre de vie,
  1 clic/joueur, **200 Mana** par participant.

### 5. Alertes d'attaque + mini-jeu Défense
- **`send_attack_alert()`** : route MP / Fief / Silence, avec repli MP → Fief si MP bloqués.
- **`DefenseView`** : 🗿 Golem (70 %, +50), ⚡ Éclair (50 %, +80), 🌫️ Brume (30 %, +150),
  échec = −10 Mana.

### 6. Commandes
- **`/ratons`** (slash) : +15 à 40 Mana.
- **Admin** : `!init_village`, `!force_event <marchand|boss>`, `!force_link`, `!force_attack`.

### 7. API REST (protégée par header `x-api-key`)
| Route | Rôle |
|---|---|
| `POST /webhook/game-event` | Unity signale une attaque → alerte routée |
| `POST /api/link-account`   | Valide le code PIN |
| `GET  /api/player/{id}`    | Renvoie mana + notif_pref |
| `GET  /api/inventory/{id}` | Renvoie l'inventaire ACTIF (non consommé) enrichi (nom, type) |
| `POST /api/add-mana`       | Crédite du Mana (montant calculé serveur, signé HMAC, anti-rejeu) |
| `POST /api/consume-item`   | Retire un objet consommable de l'inventaire (activé en jeu) |

### 8. Sécurité anti-triche (gain de Mana)
- Le client **n'envoie jamais** un montant : il déclare `wave` + `enemies_killed`, le
  **serveur calcule** la récompense (`MANA_REWARD_PER_WAVE` + kills, plafonnée).
- **Signature HMAC-SHA256** de `discord_id:wave:enemies_killed:nonce:timestamp` (clé =
  `API_SECRET_KEY`), fenêtre de fraîcheur de 120 s.
- **Anti-rejeu** : table `used_nonces` (un nonce = un usage).
- **Idempotence + anti-inflation** : `last_wave` monotone (vague déjà réclamée refusée,
  saut de vague > 3 refusé), kills plafonnés à 100.
- ⚠️ **Limite honnête** : le secret étant embarqué dans le client Unity, un reverse-engineer
  déterminé peut forger des signatures. Ces protections **bornent** le gain au maximum d'un
  joueur légitime rapide ; elles ne le rendent pas impossible sans simulation serveur.

### 9. Infrastructure
- Dockerfile (Python 3.11-slim) + docker-compose avec **tunnel Cloudflare** (`cloudflared`).
- Aucun port exposé localement : tout passe par le tunnel.
- `.env` : `DISCORD_TOKEN`, `CHANNEL_ID`, `API_SECRET_KEY`, `TUNNEL_TOKEN`.

---

## ⏳ À FAIRE

### 🔴 A. Côté client Unity — 100 % à faire
- [ ] Écran de saisie du code PIN → `POST /api/link-account`, stocker le `discord_id` (PlayerPrefs).
- [ ] Affichage Mana / inventaire → `GET /api/player/{id}` et `GET /api/inventory/{id}`.
- [ ] Déclenchement des attaques → `POST /webhook/game-event`.
- [ ] Application des effets des objets achetés (bouclier, boost, gel…).
- [ ] Wrapper HTTP C# (`UnityWebRequest`) ajoutant le header `x-api-key`.

### 🟠 B. Corrections serveur
- [x] **Bug event-loop** sur `/webhook/game-event` → corrigé (`run_coroutine_threadsafe` + `wrap_future`).
- [x] Clé API en temps constant (`hmac.compare_digest`).
- [x] SQLite WAL (2 threads bot + API).
- [x] Route `/api/add-mana` sécurisée (anti-triche) + tests logiques 10/10.
- [x] Consommation d'inventaire (`is_consumed` + `/api/consume-item`).
- [ ] Tests Discord de bout en bout (liaison réelle, boutique, boss à plusieurs, défense).

### 🟡 C. Reste mineur / à décider plus tard
- [ ] **`CHANNEL_ID`** lu dans `.env` mais jamais utilisé → config morte (à supprimer ou brancher).
- [ ] Réglage fin des constantes de récompense (`MANA_REWARD_*`) selon l'économie voulue.
- [ ] (Option) Expiration temporelle serveur des boosts via `expires_at` si besoin d'un timer
      autoritatif côté serveur (aujourd'hui le timer du buff tourne côté Unity).
