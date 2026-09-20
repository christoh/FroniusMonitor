namespace De.Hochstaetter.HomeAutomationClient.ViewModels.Adapters;

public abstract partial class DialogBase<TParameters, TResult, TBody>(TParameters parameters) : ViewModelBase, IDialogBase
    where TBody : ContentControl, IDialogControl, new()
    where TParameters : DialogParameters
{
    private readonly IDialogPresenter presenter = IoC.GetRegistered<IDialogPresenter>();

    /// <summary>
    /// Where this dialog is on screen: in the dialog frame of the main view, or in a window of its own. Null
    /// until it is shown, and kept afterwards so that a busy text set on the way out still has somewhere to go.
    /// </summary>
    private IDialogPresentation? presentation;

    protected CancellationTokenSource? TokenSource { get; private set; }

    public TParameters Parameters { get; protected set; } = parameters;

    public TResult? Result { get; protected set; }

    /// <summary>
    /// What the busy animation over this dialog says. It belongs to wherever the dialog is - the main view has
    /// one animation for the dialog it hosts, a dialog in a window of its own has one in that window - so this
    /// is the presentation's to hold rather than this view model's.
    /// </summary>
    public override string? BusyText
    {
        get => presentation?.BusyText;
        set
        {
            if (presentation is { } shown)
            {
                shown.BusyText = value;
            }

            // The generated setter this overrides would announce both; a binding to IsBusy - the row of controls a
            // dialog disables while it loads - otherwise keeps whatever it read first. Found 2026-09-20 with the
            // Solar.web chart, whose Initialize sets the busy text before the first binding is read.
            OnPropertyChanged(nameof(BusyText));
            OnPropertyChanged(nameof(IsBusy));
        }
    }

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    public virtual async Task<TResult?> ShowDialogAsync()
    {
        try
        {
            var isShown = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                TokenSource = new();

                // The presentation comes first, because creating the body assigns its DataContext, which starts
                // Initialize, and a dialog that sets a busy text there is on the UI thread and gets that far
                // synchronously. Null means an equivalent dialog is up already and was brought to the front.
                presentation = presenter.Create(this, Parameters);

                if (presentation is not { } shown)
                {
                    return false;
                }

                shown.SetBody(new TBody { DataContext = this, });
                shown.Open();
                return true;
            });

            if (!isShown)
            {
                return Result;
            }

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
        Dispatcher.UIThread.Invoke(() => presentation?.Close());
        TokenSource?.Cancel();
    }
}
