using Avalonia.Layout;
using Avalonia.VisualTree;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

/// <summary>
/// Lets the user resize an element by dragging a grip (typically the bottom right corner of a dialog). The
/// counterpart of <see cref="DragMove"/>, and there for the same reason: the client also runs in the browser,
/// where real windows do not exist, so a dialog is a plain control inside the main view and there is no window
/// manager to supply either.
/// </summary>
/// <remarks>
/// <para>
/// <b>It sizes the body of the dialog, not the frame around it.</b> The body is what decides how big a dialog is:
/// <c>Gen24SettingsDialogView</c> asks for a <c>MaxWidth</c> of 1024, and that is why the dialog is 1024 wide,
/// because its event log wants far more. Sizing the frame alone would leave the body at its cap with empty space
/// beside it.
/// </para>
/// <para>
/// <b>A dialog can be dragged to fill the whole of the overlay it lives in, from wherever it happens to sit.</b>
/// That took the most getting right, so the three things it rests on are worth stating:
/// </para>
/// <list type="bullet">
/// <item>
/// The frame is <b>pinned</b> while the drag lasts, or growing it would move its top and left edges outwards by
/// half of what it grew - the user drags the bottom right corner and the whole dialog creeps up and to the left.
/// </item>
/// <item>
/// The pin <b>folds in the render transform</b> that <see cref="DragMove"/> may have given the frame, and clears
/// it. Moving is done with a transform, which the layout knows nothing about, so a dialog dragged to the left
/// edge still occupies its old, centred layout rectangle - and the room to grow into is measured from that
/// rectangle. Measured: a dialog centred 188 from the left of a 1400 wide overlay stopped at 1212, and moving it
/// out of the way first did not help, because moving is exactly what the layout does not see.
/// </item>
/// <item>
/// The pinned corner <b>gives way once there is nothing left to grow into</b>, and only then. So the dialog keeps
/// its position while that costs nothing, and still reaches the full size of the overlay when dragged far enough.
/// </item>
/// </list>
/// <para>
/// <b>An explicit size is what makes it grow at all.</b> A maximum only binds content that wants more than it,
/// and a tab generally wants what it wants - raising the maximum then moves nothing. The size is therefore
/// explicit, and clamped so that it always fits: an element with an explicit size that does not fit is
/// <b>centred</b> in the space it did get, so it hangs over on both sides at once and the dialog loses its tab
/// headers off the top, its buttons off the bottom and its first column off the left. Because that failure is so
/// unpleasant the target is also aligned to the top left while it is sized, which turns any mistake in the
/// arithmetic into empty space at the bottom right instead of a dialog with its edges cut off.
/// </para>
/// <para>
/// The size is dropped again whenever <see cref="ResetTriggerProperty"/> changes, so the next dialog opens at the
/// size it asks for rather than at whatever the previous one was dragged to.
/// </para>
/// <para>
/// Pointer capture is a pure framework concern, so this lives in the view layer and not in a view model.
/// </para>
/// </remarks>
public sealed class DragResize : AvaloniaObject
{
    /// <summary>Nothing is ever this small on purpose, and a dialog dragged to nothing cannot be dragged back.</summary>
    private const double MinimumSize = 120d;

    private DragResize() { }

    /// <summary>Set on the grip: enables or disables resizing.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<DragResize, Control, bool>("IsEnabled");

    /// <summary>Set on the grip: the element that is actually resized. Defaults to the grip itself.</summary>
    public static readonly AttachedProperty<Control?> TargetProperty =
        AvaloniaProperty.RegisterAttached<DragResize, Control, Control?>("Target");

    /// <summary>
    /// Set on the grip: the frame around the target, which is held in place while the target is resized and is
    /// what the room to grow into is measured from. Optional; without it the target is its own frame.
    /// </summary>
    public static readonly AttachedProperty<Control?> AnchorProperty =
        AvaloniaProperty.RegisterAttached<DragResize, Control, Control?>("Anchor");

    /// <summary>
    /// Set on the grip: whenever this value changes, the target goes back to the size it asks for. Bind it to the
    /// object that defines what is currently shown, so that a new dialog does not inherit the size of the last.
    /// </summary>
    public static readonly AttachedProperty<object?> ResetTriggerProperty =
        AvaloniaProperty.RegisterAttached<DragResize, Control, object?>("ResetTrigger");

    private static readonly AttachedProperty<Resizer?> ResizerProperty =
        AvaloniaProperty.RegisterAttached<DragResize, Control, Resizer?>("Resizer");

    /// <summary>
    /// Where the frame stood before it was pinned, kept on the frame itself.
    /// </summary>
    /// <remarks>
    /// Not a field of the <see cref="Resizer"/>, because the pin outlives it: the resizer is thrown away and
    /// built again every time resizing is switched on or off, which for the settings dialog is every time the
    /// user comes and goes from the event log tab, while the frame has to stay where the user put it until the
    /// dialog itself is gone. Kept here, the resizer that finds a pinned frame can undo the pin whoever made it.
    /// </remarks>
    private static readonly AttachedProperty<(HorizontalAlignment Horizontal, VerticalAlignment Vertical, Thickness Margin)?> PlaceBeforePinProperty =
        AvaloniaProperty.RegisterAttached<DragResize, Control, (HorizontalAlignment, VerticalAlignment, Thickness)?>("PlaceBeforePin");

    static DragResize()
    {
        IsEnabledProperty.Changed.AddClassHandler<Control>(OnIsEnabledChanged);
        ResetTriggerProperty.Changed.AddClassHandler<Control>((grip, _) => grip.GetValue(ResizerProperty)?.Reset());
    }

    public static bool GetIsEnabled(Control grip) => grip.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(Control grip, bool value) => grip.SetValue(IsEnabledProperty, value);

    public static Control? GetTarget(Control grip) => grip.GetValue(TargetProperty);

    public static void SetTarget(Control grip, Control? value) => grip.SetValue(TargetProperty, value);

    public static Control? GetAnchor(Control grip) => grip.GetValue(AnchorProperty);

    public static void SetAnchor(Control grip, Control? value) => grip.SetValue(AnchorProperty, value);

    public static object? GetResetTrigger(Control grip) => grip.GetValue(ResetTriggerProperty);

    public static void SetResetTrigger(Control grip, object? value) => grip.SetValue(ResetTriggerProperty, value);

    private static void OnIsEnabledChanged(Control grip, AvaloniaPropertyChangedEventArgs e)
    {
        grip.GetValue(ResizerProperty)?.Detach();
        grip.SetValue(ResizerProperty, null);

        if (e.NewValue is true)
        {
            var resizer = new Resizer(grip);
            grip.SetValue(ResizerProperty, resizer);
            resizer.Attach();
        }
    }

    /// <summary>Keeps the drag state of a single grip. One instance per grip lives in <see cref="ResizerProperty"/>.</summary>
    private sealed class Resizer(Control grip)
    {
        private Point dragOrigin;
        private Size sizeAtDragStart;
        private bool isDragging;

        private Control? resized;
        private double widthBefore, heightBefore, maxWidthBefore, maxHeightBefore;
        private HorizontalAlignment targetHorizontalBefore;
        private VerticalAlignment targetVerticalBefore;

        private Control? pinned;

        /// <summary>What the frame has around the target - its title bar. Measured once; see <see cref="Pin"/>.</summary>
        private Size chrome;

        private Visual? watched;

        public void Attach()
        {
            grip.PointerPressed += OnPointerPressed;
            grip.PointerMoved += OnPointerMoved;
            grip.PointerReleased += OnPointerReleased;
            grip.PointerCaptureLost += OnPointerCaptureLost;
            grip.Cursor = new Cursor(StandardCursorType.BottomRightCorner);
        }

        public void Detach()
        {
            grip.PointerPressed -= OnPointerPressed;
            grip.PointerMoved -= OnPointerMoved;
            grip.PointerReleased -= OnPointerReleased;
            grip.PointerCaptureLost -= OnPointerCaptureLost;
            grip.ClearValue(InputElement.CursorProperty);
            isDragging = false;

            // The size goes, the place stays. The settings dialog switches resizing off when the user leaves the
            // event log tab, and a size is a size whichever tab is showing: one left behind by a table of a few
            // hundred rows would stop the forms on the other tabs sizing themselves to what is on them. Where the
            // user put the dialog is not like that and is kept - the frame stays pinned until the dialog is gone.
            ReleaseSize();
        }

        /// <summary>Gives back whatever was resized the size and the place it had before it was, and forgets it.</summary>
        /// <remarks>
        /// The values are put back rather than cleared. A dialog states its own size - the settings dialog asks
        /// for a <c>MaxWidth</c> of 1024 in its XAML, which is a local value like the one a drag writes - so
        /// clearing it does not reveal the dialog's number, it reveals the framework's: no maximum at all, and the
        /// dialog reopens as wide as its widest tab wants. An unset size is <see cref="double.NaN"/> and an unset
        /// maximum is <see cref="double.PositiveInfinity"/>, so putting the old value back covers the case where
        /// there was none.
        /// </remarks>
        public void Reset()
        {
            ReleaseSize();
            ReleasePlace();
        }

        /// <summary>
        /// Gives the target back the size it had before the drag, so that whatever else the dialog shows can size
        /// itself again. The frame stays where it is.
        /// </summary>
        private void ReleaseSize()
        {
            Unwatch();

            if (resized is not { } target)
            {
                return;
            }

            target.Width = widthBefore;
            target.Height = heightBefore;
            target.MaxWidth = maxWidthBefore;
            target.MaxHeight = maxHeightBefore;
            target.HorizontalAlignment = targetHorizontalBefore;
            target.VerticalAlignment = targetVerticalBefore;
            resized = null;
        }

        /// <summary>
        /// Lets the frame be centred again. Only for a dialog that is going away: while one is on screen it stays
        /// where the user last put it, whether or not it is being resized at the moment.
        /// </summary>
        private void ReleasePlace()
        {
            var frame = pinned ?? GetAnchor(grip);
            pinned = null;

            if (frame?.GetValue(PlaceBeforePinProperty) is not { } place)
            {
                return;
            }

            frame.HorizontalAlignment = place.Horizontal;
            frame.VerticalAlignment = place.Vertical;
            frame.Margin = place.Margin;
            frame.ClearValue(PlaceBeforePinProperty);
        }

        private Control? Target => GetTarget(grip) ?? grip;

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(grip).Properties.IsLeftButtonPressed || Target is not { } target)
            {
                return;
            }

            Remember(target);
            dragOrigin = e.GetPosition(null);
            sizeAtDragStart = target.Bounds.Size;
            isDragging = true;
            e.Pointer.Capture(grip);
            e.Handled = true;
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!isDragging || !ReferenceEquals(e.Pointer.Captured, grip) || Target is not { } target)
            {
                return;
            }

            var delta = e.GetPosition(null) - dragOrigin;
            Resize(target, new Size(sizeAtDragStart.Width + delta.X, sizeAtDragStart.Height + delta.Y));
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

        /// <summary>Notes what the target looked like before this drag, so that <see cref="Reset"/> can undo it.</summary>
        private void Remember(Control target)
        {
            if (ReferenceEquals(resized, target))
            {
                return;
            }

            // A different element than the one last resized: put that one back before adopting this one.
            Reset();

            resized = target;
            widthBefore = target.Width;
            heightBefore = target.Height;
            maxWidthBefore = target.MaxWidth;
            maxHeightBefore = target.MaxHeight;
            targetHorizontalBefore = target.HorizontalAlignment;
            targetVerticalBefore = target.VerticalAlignment;

            // Any mistake in the arithmetic below now shows as empty space at the bottom right rather than as a
            // dialog whose tab headers, buttons and first column are cut off - see the remarks on the class.
            target.HorizontalAlignment = HorizontalAlignment.Left;
            target.VerticalAlignment = VerticalAlignment.Top;

            Pin(target);
        }

        /// <summary>Holds the frame of the dialog where it stands, and notes what it has around the target.</summary>
        /// <remarks>
        /// The frame is centred, so its layout position is half of whatever room is left over and moves as soon as
        /// the size changes. Left and top alignment with the position it is <b>seen</b> at as a margin puts it
        /// exactly where it already was, and from there it stays put. The render transform that
        /// <see cref="DragMove"/> may have given it is part of that seen position, so it is folded in and cleared:
        /// see the remarks on the class for why nothing works until it is.
        /// </remarks>
        private void Pin(Control target)
        {
            if (GetAnchor(grip) is not { } frame)
            {
                return;
            }

            pinned = frame;

            // Only the first pin of this dialog notes where it stood; the ones after it would note the pinned
            // place and the dialog would never be centred again.
            if (frame.GetValue(PlaceBeforePinProperty) is null)
            {
                frame.SetValue(PlaceBeforePinProperty, (frame.HorizontalAlignment, frame.VerticalAlignment, frame.Margin));
            }

            // Measured now, while the layout is settled, and not again: taken live it is the difference between
            // two bounds that different layout passes have written, and mid-drag it goes negative.
            chrome = new Size
            (
                Math.Max(0, frame.Bounds.Width - target.Bounds.Width),
                Math.Max(0, frame.Bounds.Height - target.Bounds.Height)
            );

            var corner = Corner(frame);

            // Never pinned outside the overlay: a dialog already bigger than what it sits in is seen at a negative
            // position, and pinning it there would hold it clipped at the very moment the user is trying to make
            // it fit.
            frame.Margin = new Thickness(Math.Max(0, corner.X), Math.Max(0, corner.Y), 0, 0);
            frame.HorizontalAlignment = HorizontalAlignment.Left;
            frame.VerticalAlignment = VerticalAlignment.Top;

            if (frame.RenderTransform is TranslateTransform moved)
            {
                moved.X = 0;
                moved.Y = 0;
            }

            Watch(frame);
        }

        /// <summary>Where the frame is seen, within the container it may fill.</summary>
        private static Point Corner(Control frame) =>
            frame.GetVisualParent() is Visual container
                ? frame.TranslatePoint(new Point(0, 0), container) ?? new Point(frame.Bounds.X, frame.Bounds.Y)
                : new Point(frame.Bounds.X, frame.Bounds.Y);

        /// <summary>
        /// Gives the target the size the drag has reached, moving the pinned corner out of the way where that is
        /// the only way to reach it, and never letting the frame grow past the container it sits in.
        /// </summary>
        private void Resize(Control target, Size wanted)
        {
            var width = Math.Max(MinimumSize, wanted.Width);
            var height = Math.Max(MinimumSize, wanted.Height);

            // The declared maximum of the dialog is what it asks for when nobody has said otherwise, and a user
            // dragging the corner has said otherwise.
            target.MaxWidth = double.PositiveInfinity;
            target.MaxHeight = double.PositiveInfinity;

            if (pinned is { } frame && frame.GetVisualParent() is Visual container)
            {
                var margin = frame.Margin;

                // Only as far as it has to: the corner stays put while there is anything left to grow into and
                // gives way after that, so the dialog can be dragged to the full size of the overlay from wherever
                // it happens to sit rather than only as far as the edge.
                var left = Math.Clamp(container.Bounds.Width - chrome.Width - width, 0, Math.Max(0, margin.Left));
                var top = Math.Clamp(container.Bounds.Height - chrome.Height - height, 0, Math.Max(0, margin.Top));

                if (Math.Abs(left - margin.Left) > 0.5 || Math.Abs(top - margin.Top) > 0.5)
                {
                    frame.Margin = new Thickness(left, top, 0, 0);
                }

                width = Math.Min(width, container.Bounds.Width - chrome.Width - left);
                height = Math.Min(height, container.Bounds.Height - chrome.Height - top);
            }

            target.Width = Math.Max(MinimumSize, width);
            target.Height = Math.Max(MinimumSize, height);
        }

        /// <summary>
        /// Follows the container of the dialog, so that a size the drag has given is measured again when the room
        /// for it changes - the window resized, or a phone turned on its side.
        /// </summary>
        /// <remarks>
        /// Without this a dialog dragged large stays large when the window is made small: the size is an explicit
        /// one and nothing re-examines it, so the dialog overflows and is clipped. <see cref="DragMove"/>
        /// re-clamps its offset for the same reason.
        /// </remarks>
        private void Watch(Control frame)
        {
            var container = frame.GetVisualParent() as Visual;

            if (ReferenceEquals(container, watched))
            {
                return;
            }

            Unwatch();
            watched = container;

            if (watched is not null)
            {
                watched.PropertyChanged += OnContainerPropertyChanged;
            }
        }

        private void Unwatch()
        {
            if (watched is not null)
            {
                watched.PropertyChanged -= OnContainerPropertyChanged;
                watched = null;
            }
        }

        private void OnContainerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property != Visual.BoundsProperty || resized is not { } target || double.IsNaN(target.Width))
            {
                return;
            }

            Resize(target, new Size(target.Width, target.Height));
        }
    }
}
