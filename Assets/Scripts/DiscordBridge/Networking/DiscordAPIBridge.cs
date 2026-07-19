using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using DiscordBridge.Config;
using DiscordBridge.DTOs;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace DiscordBridge.Networking
{
    // Point d'entrée HTTP unique vers la passerelle FastAPI/Discord. Chaque appel est
    // asynchrone (Awaitable, natif Unity 6) et retourne toujours un ApiResult<T> : jamais
    // d'exception pour un échec réseau/HTTP, pour forcer l'appelant à gérer l'échec au lieu
    // d'un try/catch oublié.
    public class DiscordAPIBridge : MonoBehaviour
    {
        public static DiscordAPIBridge Instance { get; private set; }

        [SerializeField] ApiConfig config;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // --- API PUBLIQUE ---

        public Awaitable<ApiResult<LinkAccountResponse>> LinkAccountAsync(int code, CancellationToken cancellationToken = default)
        {
            var body = new LinkAccountRequest { Code = code };
            return SendAsync<LinkAccountResponse>(UnityWebRequest.kHttpVerbPOST, "/api/link-account", body, cancellationToken);
        }

        public Awaitable<ApiResult<PlayerDataResponse>> GetPlayerDataAsync(string discordId, CancellationToken cancellationToken = default)
        {
            string path = $"/api/player/{UnityWebRequest.EscapeURL(discordId)}";
            return SendAsync<PlayerDataResponse>(UnityWebRequest.kHttpVerbGET, path, null, cancellationToken);
        }

        public Awaitable<ApiResult<InventoryResponse>> GetInventoryAsync(string discordId, CancellationToken cancellationToken = default)
        {
            string path = $"/api/inventory/{UnityWebRequest.EscapeURL(discordId)}";
            return SendAsync<InventoryResponse>(UnityWebRequest.kHttpVerbGET, path, null, cancellationToken);
        }

        public Awaitable<ApiResult<GameEventResponse>> SendGameEventAsync(string discordId, string joueur, string action, CancellationToken cancellationToken = default)
        {
            var body = new GameEventRequest { DiscordId = discordId, Joueur = joueur, Action = action };
            return SendAsync<GameEventResponse>(UnityWebRequest.kHttpVerbPOST, "/webhook/game-event", body, cancellationToken);
        }

        public Awaitable<ApiResult<BattleReportResponse>> SendBattleReportAsync(BattleReportRequest dto, CancellationToken cancellationToken = default)
        {
            return SendAsync<BattleReportResponse>(UnityWebRequest.kHttpVerbPOST, "/api/battle-report", dto, cancellationToken);
        }

        public Awaitable<ApiResult<ConsumeItemResponse>> ConsumeItemAsync(string discordId, string itemId, CancellationToken cancellationToken = default)
        {
            var body = new ConsumeItemRequest { DiscordId = discordId, ItemId = itemId };
            return SendAsync<ConsumeItemResponse>(UnityWebRequest.kHttpVerbPOST, "/api/consume-item", body, cancellationToken);
        }

        // Le montant de Mana n'est jamais choisi par le client : on déclare "vague X terminée,
        // Y ennemis tués", signé en HMAC, et le serveur calcule + plafonne la récompense
        // (verify_mana_signature côté Python). Voir BuildManaSignature pour le format exact.
        public Awaitable<ApiResult<AddManaResponse>> AddManaAsync(string discordId, int wave, int enemiesKilled, CancellationToken cancellationToken = default)
        {
            long unixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string nonce = Guid.NewGuid().ToString("N");
            string signature = BuildManaSignature(discordId, wave, enemiesKilled, nonce, unixSeconds);

            var body = new AddManaRequest
            {
                DiscordId = discordId,
                Wave = wave,
                EnemiesKilled = enemiesKilled,
                Nonce = nonce,
                Timestamp = unixSeconds, // Json.NET sérialise un double entier en "X.0", voir plus bas
                Signature = signature
            };

            return SendAsync<AddManaResponse>(UnityWebRequest.kHttpVerbPOST, "/api/add-mana", body, cancellationToken);
        }

        // --- SIGNATURE HMAC (miroir exact de verify_mana_signature côté serveur) ---

        // Le serveur recalcule : hmac_sha256(API_SECRET_KEY, f"{discord_id}:{wave}:{enemies_killed}:{nonce}:{timestamp}")
        // où `timestamp` est un float Python re-stringifié par un f-string. Un float Python à
        // valeur entière (ex: 1752633600.0) se formate TOUJOURS avec un ".0" final — contrairement
        // à un simple ToString() en C# qui donnerait "1752633600". On envoie donc volontairement
        // un timestamp en secondes entières et on reconstruit ce ".0" à la main pour que la base
        // signée soit un match caractère pour caractère avec ce que le serveur recalcule.
        // Si un jour la précision du timestamp change côté client, verify_mana_signature côté
        // serveur doit être mis à jour EN MÊME TEMPS, sinon toutes les signatures échoueront.
        string BuildManaSignature(string discordId, int wave, int enemiesKilled, string nonce, long unixSeconds)
        {
            string timestampAsPythonFloat = unixSeconds.ToString(CultureInfo.InvariantCulture) + ".0";
            string signatureBase = $"{discordId}:{wave}:{enemiesKilled}:{nonce}:{timestampAsPythonFloat}";

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(config.ApiSecretKey));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureBase));

            var hex = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
                hex.Append(b.ToString("x2"));
            return hex.ToString();
        }

        // --- TRANSPORT GÉNÉRIQUE ---

        async Awaitable<ApiResult<TResponse>> SendAsync<TResponse>(string method, string path, object jsonBody, CancellationToken cancellationToken)
        {
            string url = config.BaseUrl + path;

            using var request = new UnityWebRequest(url, method);
            request.downloadHandler = new DownloadHandlerBuffer();

            if (jsonBody != null)
            {
                byte[] payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(jsonBody));
                request.uploadHandler = new UploadHandlerRaw(payload);
                request.SetRequestHeader("Content-Type", "application/json");
            }

            request.SetRequestHeader("x-api-key", config.ApiSecretKey);
            request.timeout = config.TimeoutSeconds;

            try
            {
                // Awaitable.FromAsyncOperation reprend TOUJOURS sur le thread principal Unity :
                // sûr d'accéder à des objets de scène juste après un `await`, contrairement à Task.
                await Awaitable.FromAsyncOperation(request.SendWebRequest(), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return ApiResult<TResponse>.Fail(0, "Requête annulée");
            }

            if (request.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.DataProcessingError)
                return ApiResult<TResponse>.Fail(0, request.error);

            long statusCode = request.responseCode;
            string rawBody = request.downloadHandler.text;

            if (request.result == UnityWebRequest.Result.ProtocolError)
                return ApiResult<TResponse>.Fail(statusCode, TryExtractErrorDetail(rawBody));

            try
            {
                TResponse data = JsonConvert.DeserializeObject<TResponse>(rawBody);
                return ApiResult<TResponse>.Ok(data, statusCode);
            }
            catch (JsonException e)
            {
                return ApiResult<TResponse>.Fail(statusCode, $"Réponse JSON invalide : {e.Message}");
            }
        }

        static string TryExtractErrorDetail(string rawBody)
        {
            try
            {
                var error = JsonConvert.DeserializeObject<ApiErrorResponse>(rawBody);
                return error?.Detail ?? rawBody;
            }
            catch (JsonException)
            {
                return rawBody;
            }
        }
    }
}
