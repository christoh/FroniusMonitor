using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.HomeAutomationClient;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public abstract partial class ViewModelBase : BindableBase
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsBusy))]
    public virtual partial string? BusyText { get; set; }

    public bool IsBusy => BusyText != null;

    public virtual Task Initialize()
    {
        return Task.CompletedTask;
    }
    
    protected async Task TaskExceptionHandler(Func<Task> task)
    {
        try
        {
            await task();
        }
        catch (Exception ex)
        {
            BusyText = null;
            await ex.Show().ConfigureAwait(false);
        }
        finally
        {
            BusyText = null;
        }
    }

    protected static async Task ShowHttpError<T>(ApiResult<T> result)
    {
        if (result.Status is HttpStatusCode.Forbidden)
        {
            await new MessageBox
            {
                Title = $"{Loc.HttpError} {(int)result.Status} ({result.Status})",
                Text = Loc.HttpForbidden,
                Buttons = [Loc.Ok],
                Icon = new ErrorIcon()
            }.Show();
        }
        else
        {
            await new MessageBox
            {
                Title = result.Title ?? $"{Loc.HttpError} {(int?)result.Status} ({result.Status})",
                Text = result.Errors?.Count > 1 ? result.Detail : null,
                ItemList = result.Errors?.Count < 1 ? null : result.Errors?.SelectMany(e => e.Value).ToList(),
                Icon = new ErrorIcon(),
                Buttons = [Loc.Ok],
            }.Show();
        }
    }
}