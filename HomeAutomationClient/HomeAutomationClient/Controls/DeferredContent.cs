using System.Diagnostics;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

/// <summary>
/// Content that is built a frame or two after it is attached, a few at a time in the order they were attached, so
/// that a page with many of them comes up before the last of them exists. The dashboard puts one around each
/// consumer card: the page shows its inverters, and the busy spinner keeps turning while the cards arrive below.
/// Built in the one layout pass that put the page on screen, as the data templates of an <c>ItemsControl</c> are,
/// the twenty-two cards of a house held the UI thread for seconds with nothing drawn until the last was done.
/// </summary>
/// <remarks>
/// One queue for all of them, worked off in turns posted at <see cref="DispatcherPriority.Background"/>: below
/// input, layout and rendering, so that between two turns whatever is built gets drawn and the user can act. A
/// turn builds for <see cref="TurnBudget"/> and then posts the next one. Something detached before its turn - the
/// page was left - is skipped, and enqueues itself again when it comes back.
/// </remarks>
public sealed class DeferredContent : TemplatedDecorator
{
    /// <summary>
    /// How long one turn may build before it lets the dispatcher draw. Long enough for a handful of simple cards,
    /// short enough for the busy spinner to keep turning.
    /// </summary>
    private static readonly TimeSpan TurnBudget = TimeSpan.FromMilliseconds(25);

    private static readonly Queue<DeferredContent> waiting = new();
    private static bool isTurnPosted;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (IsBuilt)
        {
            return;
        }

        waiting.Enqueue(this);
        PostTurn();
    }

    private static void PostTurn()
    {
        if (isTurnPosted)
        {
            return;
        }

        isTurnPosted = true;
        Dispatcher.UIThread.Post(Turn, DispatcherPriority.Background);
    }

    private static void Turn()
    {
        isTurnPosted = false;
        var stopwatch = Stopwatch.StartNew();

        while (waiting.TryDequeue(out var next))
        {
            if (TopLevel.GetTopLevel(next) != null)
            {
                next.BuildChild();
            }

            if (waiting.Count > 0 && stopwatch.Elapsed >= TurnBudget)
            {
                PostTurn();
                return;
            }
        }
    }
}
