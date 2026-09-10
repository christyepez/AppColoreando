using AppColoreando.Application.Contracts;

namespace AppColoreando.Application.Abstractions;

public interface IVisualProcessorClient
{
    Task<VisualProcessorResult> ProcessAsync(
        VisualProcessorRequest request,
        CancellationToken ct);
}
