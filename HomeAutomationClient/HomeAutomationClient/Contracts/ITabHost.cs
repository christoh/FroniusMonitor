namespace De.Hochstaetter.HomeAutomationClient.Contracts;

/// <summary>
/// What a tab of a dialog needs from the dialog around it: somewhere to say that it is busy, and somewhere to
/// put a toast. Both belong to the dialog, which is where there is room for them - see
/// <c>Gen24SettingsDialogViewModel</c>.
/// </summary>
/// <remarks>
/// An interface rather than the dialog view model itself, so that a tab can be built and tested without one. The
/// dialog view model derives from <c>DialogBase</c>, which reaches into the static injector for
/// <c>MainViewModel</c>, which wants the whole client behind it; none of that has anything to do with what a tab
/// does with the numbers on it.
/// </remarks>
public interface ITabHost
{
    string? BusyText { get; set; }

    string? ToastText { get; set; }
}
