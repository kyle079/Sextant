using Microsoft.Extensions.Caching.Memory;

namespace Sextant.Services;

public class CacheHandler(IMemoryCache cache) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Get)
            return await base.SendAsync(request, cancellationToken);

        var cacheKey = $"esi:{request.RequestUri}";
        if (cache.TryGetValue<CachedEsiResponse>(cacheKey, out var cached))
            return cached!.ToHttpResponseMessage(request);

        var response = await base.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return response;

        var expires = response.Content.Headers.Expires ?? response.Headers.Expires;
        if (!expires.HasValue || expires.Value <= DateTimeOffset.UtcNow)
            return response;

        var body = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var cachedResponse = new CachedEsiResponse(
            response.StatusCode,
            response.ReasonPhrase,
            body,
            response.Content.Headers.ContentType?.ToString());

        cache.Set(cacheKey, cachedResponse, expires.Value);
        return cachedResponse.ToHttpResponseMessage(request);
    }

    private sealed record CachedEsiResponse(
        System.Net.HttpStatusCode StatusCode,
        string? ReasonPhrase,
        byte[] Body,
        string? ContentType)
    {
        public HttpResponseMessage ToHttpResponseMessage(HttpRequestMessage request)
        {
            var message = new HttpResponseMessage(StatusCode)
            {
                ReasonPhrase = ReasonPhrase,
                RequestMessage = request,
                Content = new ByteArrayContent(Body),
            };

            if (!string.IsNullOrWhiteSpace(ContentType))
                message.Content.Headers.TryAddWithoutValidation("Content-Type", ContentType);

            message.Headers.Add("X-Sextant-Cache", "HIT");
            return message;
        }
    }
}
