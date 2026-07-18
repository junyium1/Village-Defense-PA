using Newtonsoft.Json;

namespace DiscordBridge.DTOs
{
    // Forme des erreurs FastAPI (HTTPException) : { "detail": "..." }
    public class ApiErrorResponse
    {
        [JsonProperty("detail")] public string Detail;
    }
}
