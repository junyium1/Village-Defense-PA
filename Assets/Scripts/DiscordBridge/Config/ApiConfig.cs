using UnityEngine;

namespace DiscordBridge.Config
{
    // Toute la configuration réseau du bridge, éditable sans recompiler (Inspector).
    // Crée l'asset via clic droit > Create > Discord Bridge > Api Config.
    [CreateAssetMenu(fileName = "ApiConfig", menuName = "Discord Bridge/Api Config")]
    public class ApiConfig : ScriptableObject
    {
        [Header("Serveur")]
        [Tooltip("URL de base du serveur FastAPI (tunnel Cloudflare), sans slash final. Ex: https://bridge.mondomaine.com")]
        [SerializeField] string baseUrl = "https://localhost:8000";

        [Header("Sécurité")]
        [Tooltip("Doit être IDENTIQUE à API_SECRET_KEY côté serveur (.env). Sert à la fois de " +
                 "header x-api-key ET de clé HMAC pour signer /api/add-mana (voir DiscordAPIBridge). " +
                 "Ce secret est embarqué dans le build client : limite connue et acceptée côté serveur " +
                 "(cf. ROADMAP.md), pas un oubli. Ne pas commiter une vraie clé de prod dans un asset versionné.")]
        [SerializeField] string apiSecretKey;

        [Header("Réseau")]
        [Tooltip("Délai avant abandon d'une requête, en secondes.")]
        [SerializeField] int timeoutSeconds = 10;

        public string BaseUrl => baseUrl.TrimEnd('/');
        public string ApiSecretKey => apiSecretKey;
        public int TimeoutSeconds => timeoutSeconds;
    }
}
