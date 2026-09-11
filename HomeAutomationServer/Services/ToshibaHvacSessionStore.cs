using De.Hochstaetter.Fronius.Models.ToshibaAc;

namespace De.Hochstaetter.HomeAutomationServer.Services;

/// <summary>
///     The server's <see cref="IToshibaHvacSessionStore" />: the bearer token lives in <see cref="Settings" /> and is
///     written to <c>Settings.xml</c> the moment a login has succeeded, so a restart does not log in again.
/// </summary>
public sealed class ToshibaHvacSessionStore(Settings settings, ILogger<ToshibaHvacSessionStore> logger) : IToshibaHvacSessionStore
{
    public ToshibaHvacSession? Session => settings.ToshibaHvac?.Session;

    public async Task SaveSessionAsync(ToshibaHvacSession session)
    {
        settings.ToshibaHvac ??= new ToshibaHvacSettings();
        settings.ToshibaHvac.Session = session;
        settings.ToshibaHvac.SessionTime = DateTime.UtcNow;
        await settings.SaveAsync().ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Stored the Toshiba HVAC session for consumer {ConsumerId} in {FileName}", session.ConsumerId, Settings.SettingsFileName);
        }
    }
}
