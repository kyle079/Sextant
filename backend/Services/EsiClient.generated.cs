// AUTO-GENERATED ESI client facade placeholder until NSwag can be run in the build environment.
// Regenerate from CCP's swagger.json when the .NET/NSwag toolchain is available.
using System.Text.Json;

namespace Sextant.Services;

public class EsiClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<JsonDocument> GetStatus(CancellationToken cancellationToken = default) =>
        await GetJson("status/", cancellationToken);

    public async Task<JsonDocument> GetUniverseType(int typeId, CancellationToken cancellationToken = default) =>
        await GetJson($"universe/types/{typeId}/", cancellationToken);

    public async Task<JsonDocument> SearchInventoryType(string search, bool strict = true, CancellationToken cancellationToken = default) =>
        await GetJson($"search/?categories=inventory_type&strict={strict.ToString().ToLowerInvariant()}&search={Uri.EscapeDataString(search)}", cancellationToken);

    private async Task<JsonDocument> GetJson(string path, CancellationToken cancellationToken)
    {
        await using var stream = await httpClient.GetStreamAsync(path, cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }
}
