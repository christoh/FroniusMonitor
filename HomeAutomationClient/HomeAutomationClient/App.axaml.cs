using System.Diagnostics;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using De.Hochstaetter.HomeAutomationClient.Views;
using De.Hochstaetter.HomeAutomationClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace De.Hochstaetter.HomeAutomationClient
{
    public partial class App : Application
    {
        public static IServiceCollection? ServiceCollection { get; set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            ServiceCollection ??= new ServiceCollection();
            
            ServiceCollection
                .AddSingleton<MainView>()
                .AddSingleton<MainViewModel>()
                .AddTransient<GaugeTestView>()
                .AddTransient<GaugeTestViewModel>()
                .AddTransient<LinearGaugeTestView>()
                .AddTransient<LinearGaugeTestViewModel>()
                ;

            var serviceProvider= ServiceCollection.BuildServiceProvider();
            IoC.Update(serviceProvider);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();
                desktop.MainWindow = new MainWindow();
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                singleViewPlatform.MainView = IoC.Get<MainView>();
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Get an array of plugins to remove
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }
}