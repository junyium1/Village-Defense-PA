namespace DiscordBridge.Networking
{
    // Enveloppe de résultat : un appel réseau échoué (timeout, HTTP 4xx/5xx, JSON invalide)
    // ne lève jamais d'exception. L'appelant doit lire Success plutôt que catcher.
    public readonly struct ApiResult<T>
    {
        public bool Success { get; }
        public T Data { get; }
        public long StatusCode { get; }
        public string ErrorMessage { get; }

        ApiResult(bool success, T data, long statusCode, string errorMessage)
        {
            Success = success;
            Data = data;
            StatusCode = statusCode;
            ErrorMessage = errorMessage;
        }

        public static ApiResult<T> Ok(T data, long statusCode) => new(true, data, statusCode, null);
        public static ApiResult<T> Fail(long statusCode, string errorMessage) => new(false, default, statusCode, errorMessage);
    }
}
