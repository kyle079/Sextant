namespace Sextant.Services;

public class RateLimitHandler(ILogger<RateLimitHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.Headers.TryGetValues("X-Ratelimit-Remaining", out var remainingValues) &&
            int.TryParse(remainingValues.FirstOrDefault(), out var remaining) && remaining < 10)
        {
            var resetSeconds = 1;
            if (response.Headers.TryGetValues("X-Ratelimit-Reset", out var resetValues))
                int.TryParse(resetValues.FirstOrDefault(), out resetSeconds);

            logger.LogWarning("ESI rate limit remaining is {Remaining}; delaying future requests for {Seconds}s", remaining, resetSeconds);
            await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(resetSeconds, 1, 30)), cancellationToken);
        }

        return response;
    }
}
