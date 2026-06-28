using System.Windows.Data;

namespace De.Hochstaetter.FroniusMonitor.Wpf.Localization;

/// <summary>
/// Observable source for localized strings. Bindings created by <see cref="Resource"/> target its
/// indexer; changing <see cref="Culture"/> makes every live binding re-read for the new UI language.
/// </summary>
public sealed class LocalizationSource : BindableBase
{
    public static LocalizationSource Instance { get; } = new();

    private LocalizationSource() { }

    /// <summary>
    /// The culture used to resolve all <c>{l:Resource}</c> values. Tracked explicitly (rather than reading
    /// the ambient <see cref="CultureInfo.CurrentUICulture"/>) so newly created windows and already-open
    /// windows always agree, independent of thread/ExecutionContext culture propagation. Setting it
    /// re-evaluates every live binding.
    /// </summary>
    public CultureInfo Culture
    {
        get;
        [SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
        set
        {
            field = value;
            NotifyOfPropertyChange("Item[]");
        }
    } = CultureInfo.CurrentUICulture;

    public string this[string key] => Loc.ResourceManager.GetString(key, Culture) ?? key;
}

/// <summary>
/// Localizes a string from the application resources by key. Unlike <c>{x:Static p:Resources.Key}</c>
/// (resolved once and cached by WPF), this binds to <see cref="LocalizationSource"/> so the text updates
/// live when the UI language changes at runtime. Use as <c>{l:Resource Key}</c>.
/// </summary>
public sealed class Resource(string key) : MarkupExtension
{
    public string Key { get; set; } = key;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        // Bind when the target is a dependency property (live updates). Inside a template the
        // TargetObject is a SharedDp placeholder rather than the real DependencyObject, so only the
        // TargetProperty is checked -- otherwise templated strings would fall back to a static literal
        // and never update on a language change. Binding.ProvideValue handles the template deferral
        // (it returns the binding itself for shared targets, re-binding per instantiated element).
        // Non-DP targets (e.g. nested inside another markup extension) still get the string literally.
        if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget { TargetProperty: DependencyProperty })
        {
            var binding = new Binding($"[{Key}]")
            {
                Source = LocalizationSource.Instance,
                Mode = BindingMode.OneWay,
            };

            return binding.ProvideValue(serviceProvider);
        }

        return LocalizationSource.Instance[Key];
    }
}
