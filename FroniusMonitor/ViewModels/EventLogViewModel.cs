namespace De.Hochstaetter.FroniusMonitor.ViewModels;

public class EventLogViewModel(IGen24Service gen24Service) : ViewModelBase
{
    public IOrderedEnumerable<Gen24Event>? Events
    {
        get;
        set => Set(ref field, value);
    }

    public string Title
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    internal override async Task OnInitialize()
    {
        try
        {
            Title = await gen24Service.GetUiString("EVENTLOG.TITLE").ConfigureAwait(false);
            await base.OnInitialize().ConfigureAwait(false);
            var inverterBaseSettings = await gen24Service.ReadGen24Entity<Gen24InverterSettings>("config/common").ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(inverterBaseSettings.SystemName))
            {
                Title += $" - {inverterBaseSettings.SystemName}";
            }

            Events = await gen24Service.GetFroniusEvents().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ShowBox
            (
                string.Format(Resources.InverterCommReadError, ex is TaskCanceledException ? Loc.InverterTimeout : ex.Message),
                    ex.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error
            );

            Close();
        }
    }
}
