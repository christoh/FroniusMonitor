using Avalonia.VisualTree;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

/// <summary>
/// Lets the user move an element by dragging a handle (typically a dialog title bar). The client also runs in the
/// browser, where real windows do not exist, so dialogs are plain controls inside the main view. This supplies the
/// moving that a window manager would otherwise provide.
/// </summary>
/// <remarks>
/// Pointer capture and render transforms are pure framework concerns, so this lives in the view layer and not in a
/// view model. The moved element keeps its layout position; only its <see cref="Visual.RenderTransform"/> changes,
/// and the offset is clamped so that the element can never be dragged out of its container.
/// </remarks>
public sealed class DragMove : AvaloniaObject
{
    private DragMove() { }

    /// <summary>
    /// Set on the drag handle: enables or disables moving.
    /// </summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<DragMove, Control, bool>("IsEnabled");

    /// <summary>
    /// Set on the drag handle: the element that is actually moved. Defaults to the handle itself.
    /// </summary>
    public static readonly AttachedProperty<Control?> TargetProperty =
        AvaloniaProperty.RegisterAttached<DragMove, Control, Control?>("Target");

    /// <summary>
    /// Set on the drag handle: whenever this value changes, the target snaps back to its layout position. Bind it to
    /// the object that defines what is currently shown, so that a new dialog does not inherit the previous position.
    /// </summary>
    public static readonly AttachedProperty<object?> ResetTriggerProperty =
        AvaloniaProperty.RegisterAttached<DragMove, Control, object?>("ResetTrigger");

    private static readonly AttachedProperty<Dragger?> DraggerProperty =
        AvaloniaProperty.RegisterAttached<DragMove, Control, Dragger?>("Dragger");

    static DragMove()
    {
        IsEnabledProperty.Changed.AddClassHandler<Control>(OnIsEnabledChanged);
        ResetTriggerProperty.Changed.AddClassHandler<Control>((handle, _) => handle.GetValue(DraggerProperty)?.Reset());
    }

    public static bool GetIsEnabled(Control handle) => handle.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(Control handle, bool value) => handle.SetValue(IsEnabledProperty, value);

    public static Control? GetTarget(Control handle) => handle.GetValue(TargetProperty);

    public static void SetTarget(Control handle, Control? value) => handle.SetValue(TargetProperty, value);

    public static object? GetResetTrigger(Control handle) => handle.GetValue(ResetTriggerProperty);

    public static void SetResetTrigger(Control handle, object? value) => handle.SetValue(ResetTriggerProperty, value);

    private static void OnIsEnabledChanged(Control handle, AvaloniaPropertyChangedEventArgs e)
    {
        handle.GetValue(DraggerProperty)?.Detach();
        handle.SetValue(DraggerProperty, null);

        if (e.NewValue is true)
        {
            var dragger = new Dragger(handle);
            handle.SetValue(DraggerProperty, dragger);
            dragger.Attach();
        }
    }

    /// <summary>
    /// Keeps the drag state of a single handle. One instance per handle lives in <see cref="DraggerProperty"/>.
    /// </summary>
    private sealed class Dragger(Control handle)
    {
        private Control? target;
        private Point dragOrigin;
        private Vector offsetAtDragStart;
        private bool isDragging;

        public void Attach()
        {
            handle.PointerPressed += OnPointerPressed;
            handle.PointerMoved += OnPointerMoved;
            handle.PointerReleased += OnPointerReleased;
            handle.PointerCaptureLost += OnPointerCaptureLost;
            handle.Cursor = new Cursor(StandardCursorType.SizeAll);
            ResolveTarget();
        }

        public void Detach()
        {
            handle.PointerPressed -= OnPointerPressed;
            handle.PointerMoved -= OnPointerMoved;
            handle.PointerReleased -= OnPointerReleased;
            handle.PointerCaptureLost -= OnPointerCaptureLost;
            handle.ClearValue(InputElement.CursorProperty);
            isDragging = false;
            Reset();

            if (target != null)
            {
                target.PropertyChanged -= OnTargetPropertyChanged;
                target = null;
            }
        }

        /// <summary>
        /// Moves the target back to the position that the layout gives it.
        /// </summary>
        public void Reset()
        {
            if (ResolveTarget().RenderTransform is TranslateTransform translate)
            {
                translate.X = 0;
                translate.Y = 0;
            }
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var moved = ResolveTarget();

            if (!e.GetCurrentPoint(handle).Properties.IsLeftButtonPressed || moved.GetVisualParent() is not { } container)
            {
                return;
            }

            dragOrigin = e.GetPosition(container);
            offsetAtDragStart = GetOffset(moved);
            isDragging = true;
            e.Pointer.Capture(handle);
            e.Handled = true;
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            var moved = ResolveTarget();

            if (!isDragging || !ReferenceEquals(e.Pointer.Captured, handle) || moved.GetVisualParent() is not { } container)
            {
                return;
            }

            SetOffset(moved, container, offsetAtDragStart + (e.GetPosition(container) - dragOrigin));
            e.Handled = true;
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (!isDragging)
            {
                return;
            }

            isDragging = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }

        private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e) => isDragging = false;

        /// <summary>
        /// A resized container (or a dialog that changed its size) can leave the target outside, so the current
        /// offset is clamped again whenever the target is re-arranged.
        /// </summary>
        private void OnTargetPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property != Visual.BoundsProperty || target is not { RenderTransform: TranslateTransform } || target.GetVisualParent() is not { } container)
            {
                return;
            }

            SetOffset(target, container, GetOffset(target));
        }

        private Control ResolveTarget()
        {
            var current = GetTarget(handle) ?? handle;

            if (!ReferenceEquals(current, target))
            {
                if (target != null)
                {
                    target.PropertyChanged -= OnTargetPropertyChanged;
                }

                target = current;
                target.PropertyChanged += OnTargetPropertyChanged;
            }

            return current;
        }

        private static Vector GetOffset(Control moved) => moved.RenderTransform is TranslateTransform translate ? new Vector(translate.X, translate.Y) : default;

        private static void SetOffset(Control moved, Visual container, Vector offset)
        {
            if (moved.RenderTransform is not TranslateTransform translate)
            {
                translate = new TranslateTransform();
                moved.RenderTransform = translate;
            }

            var bounds = moved.Bounds;
            translate.X = Clamp(offset.X, -bounds.X, container.Bounds.Width - bounds.Right);
            translate.Y = Clamp(offset.Y, -bounds.Y, container.Bounds.Height - bounds.Bottom);
        }

        /// <summary>
        /// Like <see cref="Math.Clamp(double,double,double)"/>, but tolerates a swapped range: a target bigger than
        /// its container has no position where it fits, and then either edge is a valid limit.
        /// </summary>
        private static double Clamp(double value, double min, double max) => min <= max ? Math.Clamp(value, min, max) : Math.Clamp(value, max, min);
    }
}
