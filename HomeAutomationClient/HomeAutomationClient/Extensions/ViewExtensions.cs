namespace De.Hochstaetter.HomeAutomationClient.Extensions;

public static class ViewExtensions
{
    extension(StyledElement view)
    {
        /// <summary>
        /// Gives a view the view model the container holds for it and starts that view model's
        /// <see cref="ViewModelBase.Initialize"/> - what every page does in its constructor, so that an
        /// <c>Initialize</c> a view model gets later runs without anybody having to remember the view.
        /// </summary>
        /// <remarks>
        /// <para>
        /// From the constructor, after <c>InitializeComponent</c>. The view keeps its public parameterless constructor,
        /// which the XAML runtime loader and the previewer need (AVLN3001), and <c>TryGetRegistered</c> rather than
        /// <c>GetRegistered</c>, because the designer has no container: there the data context stays empty and the
        /// preview renders. See HomeAutomationClient.CodeBehindInjection.
        /// </para>
        /// <para>
        /// Not for a dialog body, which is handed its view model and has <c>DialogView</c> for this, and not for a page
        /// that starts its view model each time it is shown rather than once - <c>PowerFlowView</c>.
        /// </para>
        /// </remarks>
        /// <returns>The view model, or <see langword="null"/> in the designer.</returns>
        public T? UseViewModel<T>() where T : ViewModelBase
        {
            var viewModel = IoC.TryGetRegistered<T>();
            view.DataContext = viewModel;

            if (viewModel != null)
            {
                // Nobody can await it in a constructor; Initialize reports its own failures through
                // TaskExceptionHandler, and HandleTaskExceptions catches whatever escapes that all the same.
                ViewModelBase.HandleTaskExceptions(viewModel.Initialize);
            }

            return viewModel;
        }
    }
}
