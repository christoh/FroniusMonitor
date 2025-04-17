namespace De.Hochstaetter.HomeAutomationClient.Adapters;

public abstract partial class DialogBase<TParameters, TResult, TBody> : ViewModelBase, IDialogBase where TBody : ContentControl, IDialogControl, new()
{
    protected CancellationTokenSource? TokenSource { get; private set; } = new();

    protected DialogBase(string title, TParameters parameters)
    {
        Title = title;
        Parameters = parameters;
        DialogBody = new TBody { DataContext = this, };

        var mainViewModel = IoC.GetRegistered<MainViewModel>();

        mainViewModel.DialogContent = DialogBody;
        mainViewModel.TitleText = title;
    }

    public TParameters? Parameters { get; protected set; }

    public TResult? Result { get; protected set; }

    public TBody DialogBody { get; }

    [ObservableProperty] public partial string Title { get; set; } = string.Empty;

    public virtual async Task<TResult?> ShowDialogAsync()
    {
        try
        {
            if (TokenSource == null)
            {
                throw new ObjectDisposedException($"{GetType().Name} was disposed");
            }

            var mainViewModel = IoC.GetRegistered<MainViewModel>();
            mainViewModel.IsDialogVisible = true;
            await Task.Delay(-1, TokenSource.Token).ConfigureAwait(false);
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
        var mainViewModel = IoC.GetRegistered<MainViewModel>();
        mainViewModel.IsDialogVisible = false;
        mainViewModel.DialogContent = null;
        mainViewModel.TitleText = null;
        TokenSource?.Cancel();
    }
}
