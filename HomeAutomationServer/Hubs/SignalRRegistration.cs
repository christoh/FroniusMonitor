using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;

namespace De.Hochstaetter.HomeAutomationServer.Hubs;

public static class SignalRRegistration
{
    /// <summary>
    /// Registers SignalR the way <see cref="HomeAutomationHub"/> needs it: the payload serialization the clients
    /// expect, and <see cref="ClientToServerOnlyHubFilter"/>, which keeps a client from sending to other clients.
    /// </summary>
    /// <remarks>
    /// This lives in one place so the tests can set up a host with exactly the registration the server uses
    /// instead of a copy of it that could drift.
    /// </remarks>
    public static ISignalRServerBuilder AddHomeAutomationSignalR(this IServiceCollection services)
    {
        services.AddSingleton<ClientToServerOnlyHubFilter>();

        return services
            .AddSignalR(o => o.AddFilter<ClientToServerOnlyHubFilter>())
            .AddJsonProtocol(o =>
            {
                o.PayloadSerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
                o.PayloadSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                o.PayloadSerializerOptions.IgnoreReadOnlyProperties = true;
                o.PayloadSerializerOptions.IgnoreReadOnlyFields = true;
                o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });
    }
}
