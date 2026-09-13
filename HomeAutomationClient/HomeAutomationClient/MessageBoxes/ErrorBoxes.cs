using De.Hochstaetter.Fronius.Models.HomeAutomationClient;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace De.Hochstaetter.HomeAutomationClient.MessageBoxes;

internal static class ErrorBoxes
{
    public static async ValueTask Show(this ProblemDetails? problemDetails)
    {
        if (problemDetails == null)
        {
            return;
        }

        await new MessageBox
        {
            Title = $"{Resources.HttpError} {problemDetails.Status} ({(int?)problemDetails.Status})",
            Text = problemDetails.Title,
            ItemList = problemDetails.Errors != null ? problemDetails.Errors.SelectMany(p => p.Value).ToList() : problemDetails.Detail == null ? null : [problemDetails.Detail],
            Icon = new ErrorIcon(),
        }.Show().ConfigureAwait(false);
    }

    /// <summary>
    /// What went wrong talking to a server, in words the user can act on.
    /// </summary>
    /// <param name="problemDetails">The failed result, or <see langword="null"/> for nothing to show.</param>
    /// <param name="serverAddress">Where the client was talking to, to name it in the message.</param>
    /// <remarks>
    /// Where the server answered at all, its own <c>ProblemDetails</c> is the better message and
    /// <see cref="Show(ProblemDetails?)"/> shows it - the server says "Incorrect username and/or password" in the
    /// user's language, and we have nothing to add. Where nothing answered, there is no message, only the
    /// exception of a socket: a wrong address, no network, or a server that is not running. That is the case this
    /// exists for, because "No such host is known" is not something an end user can do anything with.
    /// </remarks>
    public static async ValueTask ShowServerProblem(this ProblemDetails? problemDetails, string? serverAddress)
    {
        if (problemDetails == null)
        {
            return;
        }

        if (problemDetails.Status != null)
        {
            await problemDetails.Show().ConfigureAwait(false);
            return;
        }

        await new MessageBox
        {
            Title = Resources.CannotReachServer,
            Text = string.Format(CultureInfo.CurrentCulture, Resources.CannotReachServerDetails, serverAddress),
            ItemList = problemDetails.Detail is { } detail ? [detail] : null,
            Icon = new ErrorIcon(),
        }.Show().ConfigureAwait(false);
    }

    public static async ValueTask<MessageBoxResult?> Show(this MessageBox parameters)
    {
        return await new MessageBoxViewModel(parameters).ShowDialogAsync();
    }

    public static ValueTask<MessageBoxResult?> ShowHubError(this HubException ex)
    {
        var logger = IoC.GetRegistered<ILoggerFactory>().CreateLogger(typeof(ErrorBoxes));

        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning(ex, "A hub action failed");
        }

        return new MessageBox
        {
            Title = Resources.Error,
            Text = ex.Message,
            Buttons = [Resources.Ok],
            Icon = new ErrorIcon(),
        }.Show();
    }

    public static async ValueTask Show(this Exception ex)
    {
        if (!Design.IsDesignMode)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await new MessageBox
                {
                    Icon = new ErrorIcon(),
                    Title = $"{ex.GetType().Name}: {ex.Message}",
                    Text = ex.ToString(),
                }.Show().ConfigureAwait(false);
            });
        }
    }
}
