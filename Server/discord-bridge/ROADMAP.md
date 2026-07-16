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
| `GET  /api/inventory/{id}` | Renvoie l'inventaire |

### 8. Infrastructure
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
- [ ] **Bug event-loop** sur `/webhook/game-event` : `await send_attack_alert(...)` direct
      s'exécute sur la boucle d'uvicorn au lieu de celle du bot → doit passer par
      `asyncio.run_coroutine_threadsafe(..., bot.loop)` (comme dans `/api/link-account`).
- [ ] Tests Discord de bout en bout (liaison réelle, boutique, boss à plusieurs, défense).

### 🟡 C. Points de design à clarifier
- [ ] **`CHANNEL_ID`** lu dans `.env` mais jamais utilisé → config morte.
- [ ] **Aucune route pour créditer du Mana depuis le gameplay Unity** (le Mana ne se gagne
      aujourd'hui que via Discord) → probablement un manque.
- [ ] **Inventaire jamais « consommé »** : pas de champ / route marquant un objet comme utilisé.
- [ ] Clé API comparée avec `!=` (pas constant-time) — mineur.
- [ ] SQLite : 2 threads (bot + API) écrivent dans la même DB → risque ponctuel de
      `database is locked`.
