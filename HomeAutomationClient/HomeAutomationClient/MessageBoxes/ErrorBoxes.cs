using De.Hochstaetter.Fronius.Models.HomeAutomationClient;

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

    public static async ValueTask Show(this MessageBox parameters)
    {
        await new MessageBoxViewModel(parameters).ShowDialogAsync();
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
