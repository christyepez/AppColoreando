using System.Net.Http.Json;
using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Contracts;

namespace AppColoreando.Infrastructure.ContentGeneration;

public sealed class VisualProcessorClient(HttpClient http) : IVisualProcessorClient
{
    public async Task<VisualProcessorResult> ProcessAsync(
        VisualProcessorRequest request,
        CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync("process", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return new(false, null, null,
                $"HTTP_{(int)response.StatusCode}",
                await response.Content.ReadAsStringAsync(ct));
        }

        return await response.Content.ReadFromJsonAsync<VisualProcessorResult>(cancellationToken: ct)
            ?? new(false, null, null, "EMPTY_RESPONSE", "Visual processor returned an empty response.");
    }
}
