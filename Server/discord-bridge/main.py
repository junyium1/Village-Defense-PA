import os
import discord
from discord.ext import commands, tasks
import sqlite3
import random
import asyncio
import math
import time
from contextlib import closing
from fastapi import FastAPI, Request, HTTPException, Header
import uvicorn
import threading

# --- CONFIGURATION ---
DISCORD_TOKEN = os.getenv("DISCORD_TOKEN")
CHANNEL_ID = os.getenv("CHANNEL_ID")
API_SECRET_KEY = os.getenv("API_SECRET_KEY")
DB_PATH = "database.db"
COLOR_THEME = 0x172132
PORTAL_TTL_SECONDS = 900  # Durée de vie du portail ET du code PIN associé (15 min)

# --- INITIALISATION ---
app = FastAPI(title="Passerelle Unity-Discord")

intents = discord.Intents.default()
intents.message_content = True
intents.members = True 
bot = commands.Bot(command_prefix="!", intents=intents)

pending_links = {}

# --- BASE DE DONNÉES ET OUTILS ---
def init_db():
    with closing(sqlite3.connect(DB_PATH)) as conn:
        conn.execute("CREATE TABLE IF NOT EXISTS joueurs (discord_id TEXT PRIMARY KEY, mana INTEGER DEFAULT 0)")
        conn.execute("CREATE TABLE IF NOT EXISTS inventaire (discord_id TEXT, item_id TEXT, date_achat TIMESTAMP DEFAULT CURRENT_TIMESTAMP)")

        # Ajout des colonnes (sans écraser la DB existante)
        for ddl in (
            "ALTER TABLE joueurs ADD COLUMN notif_pref TEXT DEFAULT 'mp'",
            "ALTER TABLE joueurs ADD COLUMN fief_channel_id TEXT",
        ):
            try:
                conn.execute(ddl)
            except sqlite3.OperationalError:
                pass # La colonne existe déjà

        conn.commit()

def is_player_linked(discord_id: str) -> bool:
    """Vérifie rapidement si un joueur a lié son compte."""
    try:
        with closing(sqlite3.connect(DB_PATH)) as conn:
            c = conn.cursor()
            c.execute("SELECT 1 FROM joueurs WHERE discord_id = ?", (discord_id,))
            return c.fetchone() is not None
    except sqlite3.Error as e:
        print(f"❌ Erreur DB (is_player_linked) : {e}")
        return False

async def delete_channel_later(channel: discord.TextChannel, delay: int):
    await asyncio.sleep(delay)
    try:
        await channel.delete(reason="Le portail magique s'est refermé (15 min)")
    except discord.NotFound:
        pass

# --- UI : LE FIEF (Tableau de bord privé) ---
class NotifSelect(discord.ui.Select):
    def __init__(self):
        options = [
            discord.SelectOption(label="Message Privé (MP)", description="Alerte directe sur ton téléphone", value="mp", emoji="📱"),
            discord.SelectOption(label="Ping dans ce Fief", description="Notification classique sur le serveur", value="fief", emoji="🔔"),
            discord.SelectOption(label="Silencieux", description="Aucune notification hors-ligne", value="silence", emoji="🔕")
        ]
        super().__init__(placeholder="Choisis tes alertes hors-ligne...", min_values=1, max_values=1, options=options, custom_id="select_notif")

    async def callback(self, interaction: discord.Interaction):
        pref = self.values[0]
        discord_id = str(interaction.user.id)

        try:
            with closing(sqlite3.connect(DB_PATH)) as conn:
                conn.execute("UPDATE joueurs SET notif_pref = ? WHERE discord_id = ?", (pref, discord_id))
                conn.commit()
        except sqlite3.Error as e:
            print(f"❌ Erreur DB (notif_pref) : {e}")
            await interaction.response.send_message("❌ Erreur lors de la sauvegarde de ta préférence.", ephemeral=True)
            return

        noms = {"mp": "📱 Message Privé", "fief": "🔔 Ping dans le Fief", "silence": "🔕 Silencieux"}
        await interaction.response.send_message(f"✅ Préférence sauvegardée : En cas d'attaque, tu seras alerté en mode **{noms[pref]}**.", ephemeral=True)

class FiefView(discord.ui.View):
    def __init__(self):
        super().__init__(timeout=None) # Le menu est permanent
        self.add_item(NotifSelect())

async def create_fief_for_user(discord_id: int):
    """Crée le salon privé permanent (Le Fief) après la liaison du compte."""
    guild = bot.guilds[0] # Le bot prend le premier serveur où il est
    member = guild.get_member(discord_id)
    if not member: return

    channel_name = f"fief-{member.name.lower()}"
    existing = discord.utils.get(guild.text_channels, name=channel_name)
    if existing:
        # Le fief existe déjà : on s'assure que son ID est bien enregistré en DB
        # (rattrapage pour les fiefs créés avant l'introduction de fief_channel_id).
        try:
            with closing(sqlite3.connect(DB_PATH)) as conn:
                conn.execute("UPDATE joueurs SET fief_channel_id = ? WHERE discord_id = ?", (str(existing.id), str(discord_id)))
                conn.commit()
        except sqlite3.Error as e:
            print(f"❌ Erreur DB (rattrapage fief_channel_id) : {e}")
        return

    overwrites = {
        guild.default_role: discord.PermissionOverwrite(read_messages=False),
        member: discord.PermissionOverwrite(read_messages=True),
        guild.me: discord.PermissionOverwrite(read_messages=True)
    }

    channel = await guild.create_text_channel(channel_name, overwrites=overwrites)

    # On récupère le solde pour l'afficher et on enregistre l'ID du salon créé
    try:
        with closing(sqlite3.connect(DB_PATH)) as conn:
            c = conn.cursor()
            c.execute("SELECT mana FROM joueurs WHERE discord_id = ?", (str(discord_id),))
            row = c.fetchone()
            mana = row[0] if row else 0
            conn.execute("UPDATE joueurs SET fief_channel_id = ? WHERE discord_id = ?", (str(channel.id), str(discord_id)))
            conn.commit()
    except sqlite3.Error as e:
        print(f"❌ Erreur DB (create_fief_for_user) : {e}")
        mana = 0

    embed = discord.Embed(
        title=f"🏰 Le Fief de {member.display_name}",
        description=(
            "Bienvenue dans ton Quartier Général.\n\n"
            f"💰 **Solde actuel :** {mana} Mana\n\n"
            "Ici, tu recevras tes rapports de bataille. **Choisis ci-dessous** comment tu souhaites "
            "être alerté quand ton village est attaqué pendant que le jeu est fermé :"
        ),
        color=COLOR_THEME
    )
    await channel.send(content=member.mention, embed=embed, view=FiefView())

# --- UI : LIAISON DE COMPTE ---
class LiaisonView(discord.ui.View):
    def __init__(self):
        super().__init__(timeout=None)

    @discord.ui.button(label="Lier mon Grimoire (Jeu)", style=discord.ButtonStyle.blurple, custom_id="btn_lier")
    async def lier(self, interaction: discord.Interaction, button: discord.ui.Button):
        guild = interaction.guild
        code = random.randint(1000, 9999)
        pending_links[code] = (interaction.user.id, time.time() + PORTAL_TTL_SECONDS)

        overwrites = {
            guild.default_role: discord.PermissionOverwrite(read_messages=False),
            interaction.user: discord.PermissionOverwrite(read_messages=True),
            guild.me: discord.PermissionOverwrite(read_messages=True)
        }
        
        channel_name = f"portail-{interaction.user.name}"
        channel = await guild.create_text_channel(channel_name, overwrites=overwrites)
        
        embed = discord.Embed(
            title="🔮 Ton Sceau Magique Secret",
            description=(
                f"Chut... {interaction.user.mention}, fais vite.\n\n"
                f"Code de synchronisation : **{code}**\n\n"
                f"1️⃣ Ouvre le jeu Unity.\n"
                f"2️⃣ Rentre ce code pour lier ta progression.\n\n"
                f"⏳ *Ce portail s'autodétruira dans 15 minutes.*"
            ),
            color=COLOR_THEME
        )
        await channel.send(content=interaction.user.mention, embed=embed)
        await interaction.response.send_message(f"✨ Portail ouvert : {channel.mention}", ephemeral=True)
        asyncio.create_task(delete_channel_later(channel, PORTAL_TTL_SECONDS))

# --- UI : ÉVÉNEMENTS DE LA TAVERNE ---
class BoutiqueSelect(discord.ui.Select):
    def __init__(self):
        options = [
            discord.SelectOption(label="Skin : Fantôme Classique", description="10 Mana - Petit prix", value="skin_simple", emoji="👻"),
            discord.SelectOption(label="Skin : Office Worker Maudit", description="250 Mana - Skin légendaire", value="skin_boss", emoji="👔"),
            discord.SelectOption(label="Bouclier Électrique (10m)", description="40 Mana - Protège une tour", value="shield_10m", emoji="⚡"),
            discord.SelectOption(label="Boost de récolte (30m)", description="60 Mana - Mana x2", value="boost_30m", emoji="📈"),
            discord.SelectOption(label="Gel temporel (5m)", description="100 Mana - Stoppe les ennemis", value="freeze_5m", emoji="❄️"),
            discord.SelectOption(label="Appel des renforts", description="150 Mana - Spawn auto", value="reinforce", emoji="⚔️")
        ]
        super().__init__(placeholder="Que désires-tu acquérir ?", min_values=1, max_values=1, options=options)

    async def callback(self, interaction: discord.Interaction):
        item_value = self.values[0]
        prix = {
            "skin_simple": 10, "skin_boss": 250, 
            "shield_10m": 40, "boost_30m": 60, 
            "freeze_5m": 100, "reinforce": 150
        }
        noms = {
            "skin_simple": "Skin : Fantôme Classique", "skin_boss": "Skin : Office Worker Maudit",
            "shield_10m": "Bouclier Électrique", "boost_30m": "Boost de récolte",
            "freeze_5m": "Gel temporel", "reinforce": "Appel des renforts"
        }

        discord_id = str(interaction.user.id)
        try:
            with closing(sqlite3.connect(DB_PATH)) as conn:
                c = conn.cursor()
                c.execute("SELECT mana FROM joueurs WHERE discord_id = ?", (discord_id,))
                row = c.fetchone()
                
                if row is None:
                    await interaction.response.send_message("❌ Ton Grimoire n'est pas lié !", ephemeral=True)
                    return
                
                solde = row[0]
                if solde < prix[item_value]:
                    await interaction.response.send_message(f"❌ Pas assez de Mana ! (Il te faut {prix[item_value]} $)", ephemeral=True)
                else:
                    nouveau_solde = solde - prix[item_value]
                    c.execute("UPDATE joueurs SET mana = ? WHERE discord_id = ?", (nouveau_solde, discord_id))
                    c.execute("INSERT INTO inventaire (discord_id, item_id) VALUES (?, ?)", (discord_id, item_value))
                    conn.commit()
                    await interaction.response.send_message(f"✅ Transaction réussie ! Tu as acheté **{noms[item_value]}**.\n💳 *Mana restant : {nouveau_solde} $*", ephemeral=True)
        except sqlite3.Error as e:
            print(f"❌ Erreur DB (boutique) : {e}")
            await interaction.response.send_message("❌ Erreur lors de la transaction.", ephemeral=True)

class BoutiqueView(discord.ui.View):
    def __init__(self):
        super().__init__(timeout=180)
        self.add_item(BoutiqueSelect())

class MarchandView(discord.ui.View):
    def __init__(self):
        super().__init__(timeout=1800)

    @discord.ui.button(label="Interagir avec le marchand", style=discord.ButtonStyle.green, emoji="🎒")
    async def trade(self, interaction: discord.Interaction, button: discord.ui.Button):
        # Filtre anti-intrus
        if not is_player_linked(str(interaction.user.id)):
            await interaction.response.send_message("❌ Le marchand t'ignore. Tu dois d'abord lier ton compte dans #liaison-compte.", ephemeral=True)
            return

        embed = discord.Embed(
            title="🎒 Boutique Clandestine", 
            description="Le marchand t'ouvre son manteau discrètement. Que veux-tu acheter avec ton Mana ?", 
            color=0xD4AF37
        )
        await interaction.response.send_message(embed=embed, view=BoutiqueView(), ephemeral=True)

class BossView(discord.ui.View):
    def __init__(self, required_clicks):
        super().__init__(timeout=3600)
        self.required_clicks = required_clicks
        self.current_clicks = set()

    def get_health_bar(self):
        total_blocks = 10
        if self.required_clicks == 0: return "[██████████] 100%"
        progress = len(self.current_clicks) / self.required_clicks
        filled = int(progress * total_blocks)
        empty = total_blocks - filled
        bar = "█" * filled + "░" * empty
        percent = int(progress * 100)
        return f"[{bar}] {percent}%"

    @discord.ui.button(label="Lancer un sort !", style=discord.ButtonStyle.red, emoji="⚔️")
    async def attack(self, interaction: discord.Interaction, button: discord.ui.Button):
        # Filtre anti-intrus
        if not is_player_linked(str(interaction.user.id)):
            await interaction.response.send_message("❌ Tu dois lier ton Grimoire dans #liaison-compte avant de combattre !", ephemeral=True)
            return

        if interaction.user.id in self.current_clicks:
            await interaction.response.send_message("❌ Tu as déjà attaqué !", ephemeral=True)
            return

        self.current_clicks.add(interaction.user.id)
        
        if len(self.current_clicks) >= self.required_clicks:
            for child in self.children:
                child.disabled = True
            
            # Récompense de groupe
            reward = 200
            try:
                with closing(sqlite3.connect(DB_PATH)) as conn:
                    c = conn.cursor()
                    for user_id in self.current_clicks:
                        c.execute("UPDATE joueurs SET mana = mana + ? WHERE discord_id = ?", (reward, str(user_id)))
                    conn.commit()
            except sqlite3.Error as e:
                print(f"❌ Erreur DB (récompense boss) : {e}")

            embed = interaction.message.embeds[0]
            embed.title = "🎉 L'Office Manager a été vaincu !"
            embed.color = 0x00FF00
            embed.description = f"Le village est sauvé ! Chaque combattant a reçu **{reward} $** de butin."
            await interaction.message.edit(embed=embed, view=self)
            await interaction.response.send_message("Coup de grâce ! Tu as reçu ton butin.", ephemeral=True)
        else:
            embed = interaction.message.embeds[0]
            embed.description = f"**Un Office Manager colossal attaque la taverne !**\n\nPV : {self.get_health_bar()}\n*(Nécessite {self.required_clicks} attaques différentes)*"
            await interaction.message.edit(embed=embed, view=self)
            await interaction.response.send_message("💥 Bim ! Ton sort touche la cible.", ephemeral=True)

# --- UI : DÉFENSE (Mini-jeu lors d'attaque) ---
class DefenseView(discord.ui.View):
    def __init__(self, discord_id: str):
        super().__init__(timeout=180)
        self.discord_id = discord_id

    @discord.ui.button(label="Invoquer un Golem", style=discord.ButtonStyle.blurple, emoji="🗿")
    async def defend_golem(self, interaction: discord.Interaction, button: discord.ui.Button):
        await self._handle_defense(interaction, "golem")

    @discord.ui.button(label="Lancer un Éclair", style=discord.ButtonStyle.danger, emoji="⚡")
    async def defend_lightning(self, interaction: discord.Interaction, button: discord.ui.Button):
        await self._handle_defense(interaction, "lightning")

    @discord.ui.button(label="Invoquer la Brume", style=discord.ButtonStyle.secondary, emoji="🌫️")
    async def defend_mist(self, interaction: discord.Interaction, button: discord.ui.Button):
        await self._handle_defense(interaction, "mist")

    async def _handle_defense(self, interaction: discord.Interaction, defense_type: str):
        if str(interaction.user.id) != self.discord_id:
            await interaction.response.send_message("❌ Ce n'est pas ton attaque !", ephemeral=True)
            return

        outcomes = {
            "golem": {"success": 70, "mana_gain": 50, "text": "Le Golem a bloqué l'invasion ! 🛡️"},
            "lightning": {"success": 50, "mana_gain": 80, "text": "L'éclair a vaporisé les ennemis ! ⚡"},
            "mist": {"success": 30, "mana_gain": 150, "text": "La Brume les a tous confondus ! 🌫️"}
        }

        outcome = outcomes[defense_type]
        success = random.randint(1, 100) <= outcome["success"]
        
        if success:
            try:
                with closing(sqlite3.connect(DB_PATH)) as conn:
                    conn.execute("UPDATE joueurs SET mana = mana + ? WHERE discord_id = ?", (outcome["mana_gain"], self.discord_id))
                    conn.commit()
                embed_color = 0x00FF00
                embed_title = "✅ Défense réussie !"
                embed_desc = f"{outcome['text']}\n\n💰 Tu reçois **{outcome['mana_gain']} Mana** pour ton courage."
            except Exception as e:
                print(f"❌ Erreur DB lors de la défense : {e}")
                embed_color = 0xFF6600
                embed_title = "⚠️ Problème technique"
                embed_desc = "La défense s'est mal déroulée, mais tu garderas ton village."
        else:
            embed_color = 0xFF0000
            embed_title = "❌ Défense échouée..."
            embed_desc = "Malgré tes efforts, les ennemis ont pillé une partie de tes réserves.\n\n💔 Perte mineure : -10 Mana"
            try:
                with closing(sqlite3.connect(DB_PATH)) as conn:
                    conn.execute("UPDATE joueurs SET mana = MAX(0, mana - 10) WHERE discord_id = ?", (self.discord_id,))
                    conn.commit()
            except Exception as e:
                print(f"❌ Erreur DB lors de perte Mana : {e}")

        embed = discord.Embed(title=embed_title, description=embed_desc, color=embed_color)
        await interaction.response.send_message(embed=embed)
        for child in self.children:
            child.disabled = True
        await interaction.message.edit(view=self)

# --- BOUCLE D'ÉVÉNEMENTS ALÉATOIRES ---
@tasks.loop(minutes=30)
async def event_loop():
    for guild in bot.guilds:
        taverne = discord.utils.get(guild.text_channels, name="taverne")
        if not taverne:
            continue
        
        if random.random() <= 0.20:
            if random.random() < 0.5:
                embed = discord.Embed(
                    title="🎒 Un Marchand Ambulant s'installe !",
                    description="Il lève le camp dans 30 minutes.",
                    color=0xD4AF37
                )
                await taverne.send(embed=embed, view=MarchandView())
            else:
                humains = [m for m in guild.members if not m.bot]
                required = math.ceil(len(humains) * 0.5)
                if required < 1: required = 1                
                view = BossView(required)
                embed = discord.Embed(
                    title="🚨 ALERTE BOSS !",
                    description=f"**Un Office Manager colossal attaque la taverne !**\n\nPV : {view.get_health_bar()}\n*(Nécessite {required} attaques différentes)*",
                    color=0xFF0000
                )
                await taverne.send(embed=embed, view=view)

# --- COMMANDES ET ÉVÉNEMENTS DU BOT ---
@bot.event
async def on_ready():
    bot.add_view(LiaisonView())
    bot.add_view(FiefView()) # Rend le menu du fief permanent après un crash/reboot
    await bot.tree.sync()
    if not event_loop.is_running():
        event_loop.start()
    print(f"✅ Bot connecté en tant que {bot.user}")

@bot.event
async def on_member_join(member):
    guild = member.guild
    bienvenue_channel = discord.utils.get(guild.text_channels, name="bienvenue")
    liaison_channel = discord.utils.get(guild.text_channels, name="liaison-compte")

    if bienvenue_channel:
        lien = liaison_channel.mention if liaison_channel else "#liaison-compte"
        embed = discord.Embed(
            title="✨ Un nouveau Mage rejoint le bastion !",
            description=(
                f"Salutations {member.mention} ! L'ordre des Magiciens est honoré de t'accueillir.\n\n"
                f"👉 **Première mission :** Rends-toi immédiatement dans le sanctuaire {lien} pour synchroniser ton grimoire et commencer à accumuler du Mana !"
            ),
            color=COLOR_THEME
        )
        embed.set_thumbnail(url=member.display_avatar.url)
        await bienvenue_channel.send(content=member.mention, embed=embed)

@bot.tree.command(name="ratons", description="Envoie tes ratons laveurs voler les armes des ennemis !")
async def ratons(interaction: discord.Interaction):
    discord_id = str(interaction.user.id)
    if not is_player_linked(discord_id):
        await interaction.response.send_message("❌ Tes ratons laveurs sont perdus. Tu dois d'abord lier ton compte dans #liaison-compte.", ephemeral=True)
        return

    gain = random.randint(15, 40)
    try:
        with closing(sqlite3.connect(DB_PATH)) as conn:
            c = conn.cursor()
            c.execute("UPDATE joueurs SET mana = mana + ? WHERE discord_id = ?", (gain, discord_id))
            c.execute("SELECT mana FROM joueurs WHERE discord_id = ?", (discord_id,))
            total_mana = c.fetchone()[0]
            conn.commit()
    except sqlite3.Error as e:
        print(f"❌ Erreur DB (ratons) : {e}")
        await interaction.response.send_message("❌ Erreur technique, réessaie plus tard.", ephemeral=True)
        return

    embed = discord.Embed(
        title="🦝 Retour de mission !",
        description=f"Tes ratons laveurs ont volé la mallette d'un Office Worker !\n\n💧 **Mana récolté :** {gain} $\n🏦 **Total en banque :** {total_mana} $",
        color=COLOR_THEME
    )
    await interaction.response.send_message(embed=embed)

@bot.command()
@commands.has_permissions(administrator=True)
async def init_village(ctx):
    guild = ctx.guild
    await ctx.message.delete()

    for nom in ["bienvenue", "liaison-compte", "taverne"]:
        salon = discord.utils.get(guild.text_channels, name=nom)
        if not salon:
            salon = await guild.create_text_channel(nom)
            await ctx.send(f"✅ Salon {salon.mention} créé.", delete_after=5)

    liaison_channel = discord.utils.get(guild.text_channels, name="liaison-compte")
    await liaison_channel.purge(limit=10)
    
    embed = discord.Embed(title="📜 Le Registre des Mages", description="Clique ci-dessous pour générer ton sceau magique unique.", color=COLOR_THEME)
    await liaison_channel.send(embed=embed, view=LiaisonView())

@bot.command()
@commands.has_permissions(administrator=True)
async def force_event(ctx, event_type: str):
    await ctx.message.delete()
    if ctx.channel.name != "taverne":
        msg = await ctx.send("❌ À utiliser dans le salon #taverne.")
        await asyncio.sleep(3)
        await msg.delete()
        return

    if event_type == "marchand":
        embed = discord.Embed(title="🎒 Un Marchand Ambulant s'installe !", description="Il lève le camp dans 30 minutes.", color=0xD4AF37)
        await ctx.send(embed=embed, view=MarchandView())
    elif event_type == "boss":
        humains = [m for m in ctx.guild.members if not m.bot]
        required = math.ceil(len(humains) * 0.5)
        if required < 1: required = 1
        view = BossView(required)
        embed = discord.Embed(
            title="🚨 ALERTE BOSS !",
            description=f"**Un Office Manager colossal attaque la taverne !**\n\nPV : {view.get_health_bar()}\n*(Nécessite {required} attaques)*",
            color=0xFF0000
        )
        await ctx.send(embed=embed, view=view)

# --- UTILITAIRE : ALERTES ET ROUTAGE ---
async def send_attack_alert(discord_id: str, action: str, attacker_name: str = "Inconnu"):
    try:
        with closing(sqlite3.connect(DB_PATH)) as conn:
            c = conn.cursor()
            c.execute("SELECT notif_pref, fief_channel_id FROM joueurs WHERE discord_id = ?", (discord_id,))
            row = c.fetchone()
            notif_pref, fief_channel_id = row if row else ("mp", None)
    except Exception as e:
        print(f"❌ Erreur DB (notif_pref) : {e}")
        return False

    if notif_pref == "silence":
        print(f"ℹ️ Joueur {discord_id} en mode silence, alerte ignorée.")
        return True

    try:
        guild = bot.guilds[0]
        member = guild.get_member(int(discord_id))
        if not member:
            print(f"❌ Membre {discord_id} introuvable dans la guilde.")
            return False
    except Exception as e:
        print(f"❌ Erreur lors de récupération du membre : {e}")
        return False

    embed = discord.Embed(
        title="🚨 ALERTE ATTAQUE !",
        description=f"**{attacker_name}** essaie d'envahir ton village !\n\n{action}\n\nRéponds vite pour défendre ton territoire !",
        color=0xFF0000
    )

    if notif_pref == "mp":
        try:
            await member.send(embed=embed, view=DefenseView(discord_id))
            print(f"✅ Alerte envoyée en MP à {member.name}")
            return True
        except discord.Forbidden:
            print(f"⚠️ MP bloqué par {member.name}, basculage vers le Fief...")
            notif_pref = "fief"
        except Exception as e:
            print(f"❌ Erreur lors d'envoi DM : {e}")
            return False

    if notif_pref == "fief":
        try:
            fief_channel = guild.get_channel(int(fief_channel_id)) if fief_channel_id else None
            if not fief_channel:
                # Repli pour les comptes liés avant l'introduction de fief_channel_id
                fief_channel = discord.utils.get(guild.text_channels, name=f"fief-{member.name.lower()}")
            if not fief_channel:
                print(f"❌ Fief introuvable pour {member.name}")
                return False
            await fief_channel.send(content=member.mention, embed=embed, view=DefenseView(discord_id))
            print(f"✅ Alerte envoyée dans le Fief de {member.name}")
            return True
        except Exception as e:
            print(f"❌ Erreur lors d'envoi au Fief : {e}")
            return False

    return False

# --- ROUTES API (UNITY -> DISCORD) ---
@app.post("/webhook/game-event")
async def receive_unity_event(request: Request, x_api_key: str = Header(None)):
    if x_api_key != API_SECRET_KEY:
        raise HTTPException(status_code=401, detail="Accès refusé")
    
    try:
        data = await request.json()
    except Exception as e:
        raise HTTPException(status_code=400, detail=f"Payload invalide : {e}")

    discord_id = data.get("discord_id")
    joueur = data.get("joueur", "Inconnu")
    action = data.get("action", "Une invasion a lieu !")

    if not discord_id:
        raise HTTPException(status_code=400, detail="discord_id manquant")

    success = await send_attack_alert(discord_id, action, joueur)
    
    if success:
        return {"status": "success", "message": "Alerte d'attaque routée avec succès"}
    else:
        raise HTTPException(status_code=500, detail="Impossible d'envoyer l'alerte")

@app.post("/api/link-account")
async def link_account(data: dict, x_api_key: str = Header(None)):
    if x_api_key != API_SECRET_KEY:
        raise HTTPException(status_code=401, detail="Accès refusé")

    try:
        code = int(data.get("code"))
    except (TypeError, ValueError):
        raise HTTPException(status_code=400, detail="Code invalide")

    entry = pending_links.pop(code, None)  # à usage unique : retiré qu'il soit valide ou non
    if not entry:
        raise HTTPException(status_code=400, detail="Code invalide")

    discord_id, expires_at = entry
    if time.time() > expires_at:
        raise HTTPException(status_code=400, detail="Code expiré")

    try:
        with closing(sqlite3.connect(DB_PATH)) as conn:
            # INSERT OR IGNORE : si le joueur est déjà lié (ex: reconnexion), on ne touche
            # pas à son solde ni à ses préférences existantes.
            conn.execute("INSERT OR IGNORE INTO joueurs (discord_id, mana) VALUES (?, 0)", (str(discord_id),))
            conn.commit()
    except sqlite3.Error as e:
        raise HTTPException(status_code=500, detail=f"Erreur DB : {e}")

    asyncio.run_coroutine_threadsafe(create_fief_for_user(int(discord_id)), bot.loop)

    return {"status": "success", "discord_id": discord_id}


@app.get("/api/player/{discord_id}")
async def get_player_data(discord_id: str, x_api_key: str = Header(None)):
    if x_api_key != API_SECRET_KEY:
        raise HTTPException(status_code=401, detail="Accès refusé")
    try:
        with closing(sqlite3.connect(DB_PATH)) as conn:
            c = conn.cursor()
            c.execute("SELECT mana, notif_pref FROM joueurs WHERE discord_id = ?", (discord_id,))
            row = c.fetchone()
            if not row:
                raise HTTPException(status_code=404, detail="Joueur introuvable")
            mana, notif_pref = row
            return {"discord_id": discord_id, "mana": mana, "notif_pref": notif_pref}
    except sqlite3.Error as e:
        raise HTTPException(status_code=500, detail=f"Erreur DB : {e}")


@app.get("/api/inventory/{discord_id}")
async def get_inventory(discord_id: str, x_api_key: str = Header(None)):
    if x_api_key != API_SECRET_KEY:
        raise HTTPException(status_code=401, detail="Accès refusé")
    try:
        with closing(sqlite3.connect(DB_PATH)) as conn:
            c = conn.cursor()
            c.execute("SELECT item_id, date_achat FROM inventaire WHERE discord_id = ? ORDER BY date_achat DESC", (discord_id,))
            rows = c.fetchall()
            inventory = [{"item_id": row[0], "date_achat": row[1]} for row in rows]
            return {"discord_id": discord_id, "inventory": inventory}
    except sqlite3.Error as e:
        raise HTTPException(status_code=500, detail=f"Erreur DB : {e}")

@bot.command()
@commands.has_permissions(administrator=True)
async def force_link(ctx):
    """Commande admin pour forcer la liaison, se donner de l'argent et générer le Fief."""
    await ctx.message.delete()
    try:
        with closing(sqlite3.connect(DB_PATH)) as conn:
            conn.execute("INSERT OR REPLACE INTO joueurs (discord_id, mana) VALUES (?, 1000)", (str(ctx.author.id),))
            conn.commit()
    except sqlite3.Error as e:
        print(f"❌ Erreur DB (force_link) : {e}")
        await ctx.send("❌ Erreur technique lors de la liaison forcée.", delete_after=5)
        return

    # Création du fief directement après l'inscription
    await create_fief_for_user(ctx.author.id)
    
    msg = await ctx.send("✅ Tricherie activée : Ton compte est lié et ton Fief a été créé !")
    await asyncio.sleep(5)
    await msg.delete()

@bot.command()
@commands.has_permissions(administrator=True)
async def force_attack(ctx, action: str = "Attaque test"):
    """Commande admin pour tester le routage d'alertes d'attaque."""
    await ctx.message.delete()
    discord_id = str(ctx.author.id)
    
    if not is_player_linked(discord_id):
        msg = await ctx.send("❌ Tu dois d'abord lier ton compte avec `!force_link`.")
        await asyncio.sleep(3)
        await msg.delete()
        return

    success = await send_attack_alert(discord_id, action, "Admin (Test)")
    
    if success:
        msg = await ctx.send("✅ Alerte d'attaque envoyée selon ta préférence de notification !")
    else:
        msg = await ctx.send("❌ Erreur lors de l'envoi de l'alerte.")
    
    await asyncio.sleep(3)
    await msg.delete()

# --- LANCEMENT MULTI-THREAD ---
if __name__ == "__main__":
    if not DISCORD_TOKEN:
        raise RuntimeError("DISCORD_TOKEN manquant : vérifie ton fichier .env")
    if not API_SECRET_KEY:
        raise RuntimeError("API_SECRET_KEY manquant : vérifie ton fichier .env")

    init_db()
    threading.Thread(target=lambda: bot.run(DISCORD_TOKEN)).start()
    uvicorn.run(app, host="0.0.0.0", port=8000)
