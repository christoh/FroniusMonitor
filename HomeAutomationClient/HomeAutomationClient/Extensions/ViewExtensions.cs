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

    extension(Control view)
    {
        /// <summary>
        /// Calls <paramref name="onThemeChanged"/> whenever the application switches between light and dark, for as long
        /// as the view is loaded - for what the theme does not reach by itself, such as a converter that reads the
        /// theme's brushes once when it runs.
        /// </summary>
        /// <remarks>
        /// Subscribes on <c>Loaded</c> and unsubscribes on <c>Unloaded</c>, and that pairing is the point of this method:
        /// a page is kept and shown again, so a subscription made in the constructor, or one never removed, would add the
        /// handler once more on every navigation and run it N times per theme change for as long as the app runs.
        /// </remarks>
        public void FollowApplicationTheme(EventHandler onThemeChanged)
        {
            view.Loaded += (_, _) =>
            {
                if (Application.Current is { } application)
                {
                    application.ActualThemeVariantChanged += onThemeChanged;
                }
            };

            view.Unloaded += (_, _) =>
            {
                if (Application.Current is { } application)
                {
                    application.ActualThemeVariantChanged -= onThemeChanged;
                }
            };
        }
    }
}
