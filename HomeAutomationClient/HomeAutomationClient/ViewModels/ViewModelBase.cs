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

    /// <summary>
    /// The same guard as <see cref="TaskExceptionHandler"/> for work that nobody can await: an event handler, a
    /// window that is closing. The exception is shown rather than lost, which is what matters here - an exception
    /// that escapes takes the whole app down, and a fire and forget task that fails has nobody to report to.
    /// </summary>
    /// <remarks>
    /// Everything is inside the <c>try</c>, the call that starts the work included: an <see langword="async"/>
    /// <see langword="void"/> method that throws before its first <see langword="await"/> throws on the caller's
    /// stack, which is the one place there is nobody to catch it.
    /// </remarks>
    public static async void HandleTaskExceptions(Func<Task> task)
    {
        try
        {
            await task().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await ex.Show().ConfigureAwait(false);
        }
    }

    public static async Task ShowHttpError<T>(ApiResult<T> result)
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