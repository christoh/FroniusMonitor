using Avalonia.VisualTree;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

/// <summary>
/// Scales whatever is put in it, for a user who wants the gauges bigger or more of them on screen at once. It is a
/// <b>layout</b> transform and not a render transform: what is inside is laid out at the scaled size, so a wrap
/// panel re-wraps, a scroll viewer around it knows how much there is to scroll, and nothing is clipped.
/// </summary>
/// <remarks>
/// <para>
/// A view that wants zooming puts one of these around the part that should zoom, and marks the element the input
/// should be taken on - its own root, or the window - with <see cref="IsScopeProperty"/>:
/// </para>
/// <code>
/// &lt;ContentPage c:ZoomBox.IsScope="True"&gt;
///     &lt;ScrollViewer&gt;
///         &lt;c:ZoomBox&gt;&lt;WrapPanel …/&gt;&lt;/c:ZoomBox&gt;
///     &lt;/ScrollViewer&gt;
/// &lt;/ContentPage&gt;
/// </code>
/// <para>
/// The two are separate on purpose: the pointer is hardly ever over the gauges themselves - it is over a scroll
/// viewer, a group box or the empty space beside one - so the scope takes the wheel for a whole view or window and
/// hands it to every <see cref="ZoomBox"/> below it. A scope with none of them leaves the event alone, which is
/// what lets a dialog window carry the same scope and still scroll normally.
/// </para>
/// </remarks>
public class ZoomBox : LayoutTransformControl
{
    /// <summary>What <see cref="Reset"/> goes back to, and what a view starts at.</summary>
    public const double DefaultScale = 1;

    /// <summary>What one notch of the wheel and one press of Ctrl+plus are worth unless a view says otherwise.</summary>
    public const double DefaultStep = 1.1;

    public static readonly StyledProperty<double> WheelStepProperty =
        AvaloniaProperty.Register<ZoomBox, double>(nameof(WheelStep), DefaultStep, coerce: (_, step) => Step(step));

    public static readonly StyledProperty<double> KeyStepProperty =
        AvaloniaProperty.Register<ZoomBox, double>(nameof(KeyStep), DefaultStep, coerce: (_, step) => Step(step));

    public static readonly StyledProperty<double> ScaleProperty =
        AvaloniaProperty.Register<ZoomBox, double>(nameof(Scale), DefaultScale, coerce: (box, scale) => ((ZoomBox)box).Limit(scale));

    public static readonly StyledProperty<double> MinimumScaleProperty =
        AvaloniaProperty.Register<ZoomBox, double>(nameof(MinimumScale), 0.25);

    public static readonly StyledProperty<double> MaximumScaleProperty =
        AvaloniaProperty.Register<ZoomBox, double>(nameof(MaximumScale), 4);

    /// <summary>
    /// Marks the element the zoom gestures are taken on: a view's own root, or a window. Everything below it that
    /// is a <see cref="ZoomBox"/> follows, wherever the pointer happens to be.
    /// </summary>
    public static readonly AttachedProperty<bool> IsScopeProperty =
        AvaloniaProperty.RegisterAttached<ZoomBox, InputElement, bool>("IsScope");

    public static bool GetIsScope(InputElement element) => element.GetValue(IsScopeProperty);

    public static void SetIsScope(InputElement element, bool value) => element.SetValue(IsScopeProperty, value);

    static ZoomBox()
    {
        IsScopeProperty.Changed.AddClassHandler<InputElement, bool>(OnIsScopeChanged);
    }

    /// <summary>
    /// The scale the running pinch started from, or null while no fingers are pinching. A pinch reports how far
    /// apart the fingers are **relative to where they started**, so the scale has to be built from that baseline
    /// rather than multiplied up event by event, which would square the gesture.
    /// </summary>
    private double? scaleWhenPinchStarted;

    public ZoomBox()
    {
        LayoutTransform = new ScaleTransform(Scale, Scale);

        // Unlike the wheel and the keyboard, this is on the box and not on the window: a pinch is done *to*
        // something, with the fingers on it, so it zooms what is under them and nothing else. A pinch anywhere
        // else in the window is left alone - it may well be a scroll gesture.
        GestureRecognizers.Add(new PinchGestureRecognizer());
        AddHandler(PinchEvent, OnPinch);
        AddHandler(PinchEndedEvent, OnPinchEnded);
    }

    /// <summary>How much bigger than its natural size the content is drawn and laid out. 1 is unzoomed.</summary>
    public double Scale
    {
        get => GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    public double MinimumScale
    {
        get => GetValue(MinimumScaleProperty);
        set => SetValue(MinimumScaleProperty, value);
    }

    public double MaximumScale
    {
        get => GetValue(MaximumScaleProperty);
        set => SetValue(MaximumScaleProperty, value);
    }

    /// <summary>
    /// What one notch of the wheel multiplies the scale by. Geometric rather than additive, so a notch back undoes
    /// a notch forward exactly, wherever the scale happens to be.
    /// </summary>
    public double WheelStep
    {
        get => GetValue(WheelStepProperty);
        set => SetValue(WheelStepProperty, value);
    }

    /// <summary>The same for one press of Ctrl with plus or minus.</summary>
    public double KeyStep
    {
        get => GetValue(KeyStepProperty);
        set => SetValue(KeyStepProperty, value);
    }

    /// <summary>Multiplies the current scale, which is how both the wheel and the keyboard change it.</summary>
    public void ZoomBy(double factor) => Scale *= factor;

    public void Reset() => Scale = DefaultScale;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        switch (change.Property.Name)
        {
            case nameof(Scale):
                LayoutTransform = new ScaleTransform(Scale, Scale);
                break;

            // A narrower range has to bring a scale outside it back in, or the content stays where the old limits
            // allowed it and the new ones are a promise the control does not keep.
            case nameof(MinimumScale):
            case nameof(MaximumScale):
                CoerceValue(ScaleProperty);
                break;
        }
    }

    private void OnPinch(object? sender, PinchEventArgs e)
    {
        scaleWhenPinchStarted ??= Scale;
        Scale = scaleWhenPinchStarted.Value * e.Scale;

        // So that a scroll viewer around the gauges does not pan them while they are being pinched.
        e.Handled = true;
    }

    private void OnPinchEnded(object? sender, PinchEndedEventArgs e)
    {
        scaleWhenPinchStarted = null;
        e.Handled = true;
    }

    private double Limit(double scale) => double.IsFinite(scale) ? Math.Clamp(scale, MinimumScale, MaximumScale) : DefaultScale;

    /// <summary>
    /// A step is multiplied and divided by, so it has to be a finite positive number; anything else - a zero from
    /// a style that forgot the value - would zoom to infinity or invert the direction rather than do nothing.
    /// </summary>
    private static double Step(double step) => double.IsFinite(step) && step > 0 ? step : DefaultStep;

    /// <summary>
    /// The top level a scope has its handlers on, so that they can be taken off again. Null while the scope is
    /// not in a visual tree.
    /// </summary>
    private static readonly AttachedProperty<TopLevel?> HookedTopLevelProperty =
        AvaloniaProperty.RegisterAttached<ZoomBox, InputElement, TopLevel?>("HookedTopLevel");

    /// <summary>How many scopes have hooked one top level, so two of them do not zoom everything twice.</summary>
    private static readonly AttachedProperty<int> ScopeCountProperty =
        AvaloniaProperty.RegisterAttached<ZoomBox, TopLevel, int>("ScopeCount");

    private static void OnIsScopeChanged(InputElement element, AvaloniaPropertyChangedEventArgs<bool> change)
    {
        element.AttachedToVisualTree -= OnScopeAttached;
        element.DetachedFromVisualTree -= OnScopeDetached;
        Unhook(element);

        if (!change.NewValue.Value)
        {
            return;
        }

        // A scope may be marked before it is in a tree - a view built by the container, a window not yet shown -
        // and may be moved between trees, which is what a page shown in the main view of a browser does.
        element.AttachedToVisualTree += OnScopeAttached;
        element.DetachedFromVisualTree += OnScopeDetached;
        Hook(element);
    }

    private static void OnScopeAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is InputElement element)
        {
            Hook(element);
        }
    }

    private static void OnScopeDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is InputElement element)
        {
            Unhook(element);
        }
    }

    /// <summary>
    /// Puts the handlers on the scope's <b>top level</b> rather than on the scope itself. A routed event only
    /// reaches the elements on its route, and neither route goes where this has to work: a key tunnels from the
    /// top level to whatever has the focus, so a view inside the window is skipped when the focus is elsewhere or
    /// nowhere at all, and the wheel goes to what is under the pointer, which is nothing where the pointer is over
    /// a panel with no background. The top level is on both routes, always.
    /// </summary>
    /// <remarks>
    /// Tunnelling as well, which is Avalonia's equivalent of WPF's <c>OnPreviewKeyDown</c>: the gesture has to be
    /// seen before the scroll viewer under the pointer turns the wheel into scrolling and before whatever has the
    /// focus turns the key into text.
    /// </remarks>
    private static void Hook(InputElement element)
    {
        if (TopLevel.GetTopLevel(element) is not { } topLevel || ReferenceEquals(element.GetValue(HookedTopLevelProperty), topLevel))
        {
            return;
        }

        Unhook(element);
        var scopeCount = topLevel.GetValue(ScopeCountProperty);

        if (scopeCount == 0)
        {
            topLevel.AddHandler(InputElement.PointerWheelChangedEvent, OnScopeWheelChanged, RoutingStrategies.Tunnel);
            topLevel.AddHandler(InputElement.KeyDownEvent, OnScopeKeyDown, RoutingStrategies.Tunnel);
        }

        topLevel.SetValue(ScopeCountProperty, scopeCount + 1);
        element.SetValue(HookedTopLevelProperty, topLevel);
    }

    private static void Unhook(InputElement element)
    {
        if (element.GetValue(HookedTopLevelProperty) is not { } topLevel)
        {
            return;
        }

        var scopeCount = topLevel.GetValue(ScopeCountProperty) - 1;
        topLevel.SetValue(ScopeCountProperty, scopeCount);

        if (scopeCount <= 0)
        {
            topLevel.RemoveHandler(InputElement.PointerWheelChangedEvent, OnScopeWheelChanged);
            topLevel.RemoveHandler(InputElement.KeyDownEvent, OnScopeKeyDown);
        }

        element.SetValue(HookedTopLevelProperty, null);
    }

    private static void OnScopeWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.Delta.Y == 0)
        {
            return;
        }

        e.Handled = Apply(sender, box => box.ZoomBy(e.Delta.Y > 0 ? box.WheelStep : 1 / box.WheelStep));
    }

    private static void OnScopeKeyDown(object? sender, KeyEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            return;
        }

        // Both the number row and the number pad, because a keyboard has plus and minus on both and the layouts
        // of the languages this app speaks do not agree on which key carries them.
        Action<ZoomBox>? zoom = e.Key switch
        {
            Key.Add or Key.OemPlus => box => box.ZoomBy(box.KeyStep),
            Key.Subtract or Key.OemMinus => box => box.ZoomBy(1 / box.KeyStep),
            Key.D0 or Key.NumPad0 => box => box.Reset(),
            _ => null,
        };

        if (zoom is not null)
        {
            e.Handled = Apply(sender, zoom);
        }
    }

    /// <summary>
    /// Does something to every <see cref="ZoomBox"/> of this scope, and says whether there was one. A scope
    /// without any leaves the event to whatever else wants it.
    /// </summary>
    private static bool Apply(object? scope, Action<ZoomBox> zoom)
    {
        if (scope is not Visual visual)
        {
            return false;
        }

        var found = false;

        foreach (var box in visual.GetVisualDescendants().OfType<ZoomBox>())
        {
            zoom(box);
            found = true;
        }

        return found;
    }
}
