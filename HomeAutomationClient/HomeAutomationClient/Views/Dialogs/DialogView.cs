namespace De.Hochstaetter.HomeAutomationClient.Views.Dialogs;

/// <summary>
/// The base of every dialog body: a <see cref="UserControl"/> the dialog system can host, and which starts the
/// <see cref="ViewModelBase.Initialize"/> of the view model it is handed. Until 2026-10-24 each body did that in a
/// code behind of its own, eight times the same lines.
/// </summary>
/// <remarks>
/// <para>
/// <c>DataContextChanged</c> is raised first and <c>Initialize</c> started after it, so a body that subscribes to its
/// view model in a handler of that event - the two chart dialogs - has done so before the view model starts loading.
/// </para>
/// <para>
/// It can run more than once for one body: in the dialog frame a nested dialog takes the body out of the tree and
/// puts it back, see DialogSystem.Lifecycle. A view model that loads something guards its <c>Initialize</c> against
/// that; for one that does not override it, the call does nothing.
/// </para>
/// </remarks>
public abstract class DialogView : UserControl, IDialogControl
{
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // Nobody can await it here; Initialize reports its own failures through TaskExceptionHandler, and
        // HandleTaskExceptions catches whatever escapes that all the same.
        if (DataContext is ViewModelBase viewModel)
        {
            ViewModelBase.HandleTaskExceptions(viewModel.Initialize);
        }
    }
}
