namespace De.Hochstaetter.HomeAutomationClient.Adapters;

public abstract partial class DialogBase<TParameters, TResult, TBody>(TParameters parameters) : ViewModelBase, IDialogBase
    where TBody : ContentControl, IDialogControl, new()
    where TParameters : DialogParameters
{
    private readonly MainViewModel mainViewModel = IoC.GetRegistered<MainViewModel>();

    protected CancellationTokenSource? TokenSource { get; private set; }

    public TParameters Parameters { get; protected set; } = parameters;

    public TResult? Result { get; protected set; }

    [ObservableProperty] public partial string Title { get; set; } = string.Empty;

    public virtual async Task<TResult?> ShowDialogAsync()
    {
        try
        {
            mainViewModel.DialogQueue.Push(mainViewModel.CurrentDialog);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                TokenSource = new();
                var dialogItem = new DialogQueueItem(Parameters.Title, new TBody { DataContext = this, }, Parameters.ShowCloseBox);
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
        Dispatcher.UIThread.Invoke(() => { mainViewModel.CurrentDialog = mainViewModel.DialogQueue.TryPop(out var previousDialog) ? previousDialog : null; });
        TokenSource?.Cancel();
    }
}
