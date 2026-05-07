using System.Net.Http.Headers;

namespace Sextant.Services;

public class AuthHandler(TokenService tokenService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpRequestMessage? retryRequest = null;
        if (request.Options.TryGetValue(EsiRequestOptions.CharacterId, out var characterId))
        {
            retryRequest = await CloneRequest(request, cancellationToken);
            var accessToken = await tokenService.GetAccessToken(characterId);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized ||
            retryRequest is null ||
            !retryRequest.Options.TryGetValue(EsiRequestOptions.CharacterId, out characterId))
        {
            retryRequest?.Dispose();
            return response;
        }

        response.Dispose();
        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await tokenService.RefreshAccessToken(characterId));
        return await base.SendAsync(retryRequest, cancellationToken);
    }

    private static async Task<HttpRequestMessage> CloneRequest(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri) { Version = request.Version };

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (request.Options.TryGetValue(EsiRequestOptions.CharacterId, out var characterId))
            clone.Options.Set(EsiRequestOptions.CharacterId, characterId);

        if (request.Content is null) return clone;

        var body = await request.Content.ReadAsByteArrayAsync(cancellationToken);
        clone.Content = new ByteArrayContent(body);
        foreach (var header in request.Content.Headers)
            clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return clone;
    }
}
