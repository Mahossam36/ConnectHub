using BFF.Models.Sessions;

namespace BFF.Services.Integration;

public interface IIntegrationClient
{
    Task<HttpResponseMessage> ForwardAsync(HttpRequest request, string? path, UserSession? session, CancellationToken cancellationToken = default);
}
