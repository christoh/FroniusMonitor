using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.HomeAutomationClient;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public abstract partial class ViewModelBase : BindableBase
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsBusy))]
    public virtual partial string? BusyText { get; set; }

    public bool IsBusy => BusyText != null;

    /// <summary>
    /// Whether the user can see the view this model drives: it is in a window that is shown and not minimized,
    /// and the app is not in the background. The view sets it, through <c>ViewVisibility.Follow</c>; a view model
    /// only reads it, and mostly through <see cref="WhenShown"/>. True until a view says otherwise, so a model
    /// without such a view behaves as it always did.
    /// </summary>
    [ObservableProperty]
    public partial bool IsShown { get; set; } = true;

    /// <summary>What <see cref="WhenShown"/> was asked to do while the view could not be seen, to be done once it can.</summary>
    private Action? whenShown;

    /// <summary>
    /// Does <paramref name="work"/> now if the view is shown, or once, when it is shown again, if it is not. For
    /// the reaction to an update from the service: a page nobody can see has no reason to rebuild itself on every
    /// report, and one rebuild when it comes back brings it up to date. Only the last piece of work asked for is
    /// kept, which is right for work that reads the current state and wrong for anything that must not be lost.
    /// </summary>
    /// <remarks>
    /// The work may be asked for on the hub's thread while <see cref="IsShown"/> flips on the UI thread. Neither
    /// order loses it: if the view is shown by the time the work is stored, the second look runs it.
    /// </remarks>
    protected void WhenShown(Action work)
    {
        if (IsShown)
        {
            work();
            return;
        }

        whenShown = work;

        if (IsShown && Interlocked.Exchange(ref whenShown, null) is { } pending)
        {
            pending();
        }
    }

    partial void OnIsShownChanged(bool value)
    {
        if (value && Interlocked.Exchange(ref whenShown, null) is { } pending)
        {
            pending();
        }
    }

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