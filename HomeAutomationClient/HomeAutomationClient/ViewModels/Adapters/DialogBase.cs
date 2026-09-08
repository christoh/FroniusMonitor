namespace De.Hochstaetter.HomeAutomationClient.ViewModels.Adapters;

public abstract partial class DialogBase<TParameters, TResult, TBody>(TParameters parameters) : ViewModelBase, IDialogBase
    where TBody : ContentControl, IDialogControl, new()
    where TParameters : DialogParameters
{
    private readonly MainViewModel mainViewModel = IoC.GetRegistered<MainViewModel>();

    protected CancellationTokenSource? TokenSource { get; private set; }

    public TParameters Parameters { get; protected set; } = parameters;

    public TResult? Result { get; protected set; }

    public override string? BusyText
    {
        get => mainViewModel.DialogBusyText;
        set
        {
            mainViewModel.DialogBusyText = value;
        }
    }

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    public virtual async Task<TResult?> ShowDialogAsync()
    {
        try
        {
            mainViewModel.DialogQueue.Push(mainViewModel.CurrentDialog);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                TokenSource = new();

                // Take the busy text of whatever was on screen and clear it *before* the body is created. Creating
                // the body assigns its DataContext, which starts Initialize, and a dialog that sets a busy text
                // there is on the UI thread and gets that far synchronously. With the two lines the other way
                // round the argument list read that new busy text into the item and the clear then wiped it, so
                // the dialog came up with no busy animation and the item held the wrong text to restore.
                var busyTextBelow = BusyText;
                BusyText = null;

                var dialogItem = new DialogQueueItem(Parameters.Title, new TBody { DataContext = this, }, Parameters, busyTextBelow);
                mainViewModel.CurrentDialog = dialogItem;
            });

            await Task.Delay(-1, TokenSource!.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Ignore the cancellation exception
        }
        finally
        {
            TokenSource = new CancellationTokenSource();
        }

        return Result;
    }

    public void Dispose()
    {
        TokenSource?.Dispose();
        TokenSource = null;
        GC.SuppressFinalize(this);
    }

    public abstract Task AbortAsync();

    protected void Close()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            mainViewModel.CurrentDialog = mainViewModel.DialogQueue.TryPop(out var previousDialog) ? previousDialog : null;
            BusyText = mainViewModel.CurrentDialog?.BusyText;
        });
        
        TokenSource?.Cancel();
    }
}
