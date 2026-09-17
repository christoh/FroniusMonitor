using Avalonia.Controls.Shapes;
using Avalonia.VisualTree;
using De.Hochstaetter.HomeAutomationClient.Converters;

namespace De.Hochstaetter.HomeAutomationClient.Views;

/// <summary>
/// The power flow page. The cards are XAML and bindings; what is here is the wiring between them, which needs the
/// framework twice over: it is geometry read off the laid out cards, and it moves.
/// </summary>
/// <remarks>
/// <para>
/// <b>The wires follow the cards, not the other way round.</b> Every card is laid out by ordinary panels, and
/// after each layout pass <see cref="Route"/> reads where each one ended up and draws the wires to fit: a trunk
/// between the sources and the house, a spine with one rail per row of consumers, and a stub down to each card.
/// That is what makes twenty consumers twenty short stubs instead of twenty lines across the picture, and what
/// lets the same code serve two inverters or five without a layout of its own.
/// </para>
/// <para>
/// <b>Motion carries the number.</b> An active wire is a dashed line over a faint trace, and its dashes move at
/// a speed that follows the watts on a log scale, so that a fridge visibly crawls and a charging car does not blur.
/// The direction follows the sign: a battery's dashes run into the inverter while it discharges and out of it
/// while it charges. One animation frame callback advances every wire; there is no timer per wire.
/// </para>
/// <para>
/// <b>Two things are diffed, on purpose.</b> A new snapshot arrives with every reading and is folded into stable
/// items on the UI thread (<see cref="PowerFlowViewModel.Apply"/>), so the cards are never rebuilt. And
/// <see cref="Route"/> runs on every layout pass but writes to a wire only what changed, because writing a path's
/// geometry invalidates layout and an unconditional write would loop.
/// </para>
/// <para>
/// A page, shown by the <c>IPagePresenter</c> like a detail page. The view model follows the devices while the
/// page is loaded and lets go of them when it is unloaded - on a head with one view at a time that is every trip
/// back to the dashboard, and the same page comes back with the same cards.
/// </para>
/// </remarks>
public partial class PowerFlowView : ContentPage
{
    /// <summary>What the page presenter tells this page by; there is one of it, not one per device.</summary>
    public const string PageKey = "power-flow";

    private const double StrokeThickness = 3;
    private const double ThickStrokeThickness = 4;
    private const double DashLength = 10;
    private const double GapLength = 14;

    /// <summary>The DC taps on the inverter's left edge are this far apart, centred on its middle.</summary>
    private const double TapSpacing = 14;

    /// <summary>A rail runs this far above the top of the consumer cards of its row; the cards leave a margin for it.</summary>
    private const double RailOffset = 20;

    /// <summary>The spine stands this far right of the house; the consumers' left margin leaves room for it.</summary>
    private const double SpineOffset = 34;

    /// <summary>Beyond this many watts a wire is drawn thicker: the trunk, the house, an inverter at full tilt.</summary>
    private const double ThickThreshold = 2000;

    private readonly Dictionary<string, Wire> wires = [];

    /// <summary>For the tests: the wires by key, each as its path, whether its dashes move and whether they run against the way it is drawn.</summary>
    internal IReadOnlyDictionary<string, (string Path, bool Moves, bool Reversed)> WireStates => wires.ToDictionary(pair => pair.Key, pair => (pair.Value.PathData, pair.Value.Moves, pair.Value.Reversed));
    private PowerFlowViewModel? viewModel;
    private bool isRunning;
    private int isRefreshPending;
    private TimeSpan? lastFrame;

    public PowerFlowView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;

        // Parameterless, so the XAML runtime loader and the previewer can create the view (AVLN3001). The container
        // hands out the view model it would have injected; Try..., because the designer has no container. After the
        // handler is on, or the view would never hear about the view model it was just given.
        DataContext = IoC.TryGetRegistered<PowerFlowViewModel>();

        Stage.LayoutUpdated += (_, _) => Route();
        ActualThemeVariantChanged += (_, _) => Recolor();

        Loaded += (_, _) =>
        {
            if (viewModel is { } model)
            {
                ViewModelBase.HandleTaskExceptions(model.Initialize);
            }

            RequestRefresh();
            StartFrames();
        };

        Unloaded += (_, _) =>
        {
            isRunning = false;
            viewModel?.Stop();
        };
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (viewModel != null)
        {
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        viewModel = DataContext as PowerFlowViewModel;

        if (viewModel == null)
        {
            return;
        }

        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        RequestRefresh();
    }

    /// <summary>A snapshot may arrive from the hub's thread; the items are folded on the UI thread, once per burst.</summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PowerFlowViewModel.Snapshot))
        {
            RequestRefresh();
        }
    }

    private void RequestRefresh()
    {
        if (Interlocked.Exchange(ref isRefreshPending, 1) == 0)
        {
            Dispatcher.UIThread.Post(Refresh);
        }
    }

    private void Refresh()
    {
        isRefreshPending = 0;

        if (viewModel?.Apply() ?? false)
        {
            // A card came or went: the layout that follows brings the new geometry, Route only has to know that
            // the wires it has may no longer be the wires it needs.
            wires.Values.ToList().ForEach(wire => wire.Detach(Wires));
            wires.Clear();
        }

        Route();
    }

    private void Recolor()
    {
        foreach (var wire in wires.Values)
        {
            wire.Recolor(this);
        }
    }

    private void StartFrames()
    {
        if (isRunning)
        {
            return;
        }

        isRunning = true;
        lastFrame = null;
        TopLevel.GetTopLevel(this)?.RequestAnimationFrame(OnFrame);
    }

    /// <summary>One step of every moving wire. Re-requests itself for as long as the page is on screen.</summary>
    private void OnFrame(TimeSpan time)
    {
        if (!isRunning)
        {
            return;
        }

        // Capped: a frame after the window was hidden for a while would otherwise jump the dashes a long way.
        var seconds = lastFrame is { } last ? Math.Min((time - last).TotalSeconds, 0.1) : 0;
        lastFrame = time;

        foreach (var wire in wires.Values)
        {
            wire.Advance(seconds);
        }

        TopLevel.GetTopLevel(this)?.RequestAnimationFrame(OnFrame);
    }

    /// <summary>
    /// Draws every wire to where the cards are now. Called after every layout pass, so it changes a wire only
    /// where its geometry, its figure or its colour actually differ - see the class remarks.
    /// </summary>
    private void Route()
    {
        if (viewModel is not { } model)
        {
            return;
        }

        var cards = Stage.GetVisualDescendants()
            .OfType<Border>()
            .Where(border => border.Classes.Contains("Card") && border.DataContext is PowerFlowNodeItem)
            .Select(border => (Item: (PowerFlowNodeItem)border.DataContext!, Rect: RectOf(border)))
            .Where(card => card.Rect is { })
            .GroupBy(card => card.Item.Key)
            .ToDictionary(group => group.Key, group => (group.First().Item, Rect: group.First().Rect!.Value));

        if (!cards.TryGetValue(PowerFlowSnapshot.HouseKey, out var house))
        {
            return;
        }

        var needed = new HashSet<string>();
        var items = model.Items;

        // ----- Sources onto the trunk -----
        // A tap is where a wire meets the trunk, with what it feeds in there: the grid's import, an inverter's AC
        // output, and the house's draw as a negative. What crosses the trunk between two taps is the sum of the
        // taps on one side.
        var trunkTaps = new List<(double Y, double Injection)>();
        var sourcesRight = double.NegativeInfinity;

        if (items.Grid is { } grid && cards.TryGetValue(grid.Key, out var gridCard))
        {
            trunkTaps.Add((gridCard.Rect.Center.Y, grid.Node.Power ?? 0));
            sourcesRight = Math.Max(sourcesRight, gridCard.Rect.Right);
        }

        foreach (var cluster in items.Inverters)
        {
            if (!cards.TryGetValue(cluster.Inverter.Key, out var inverter))
            {
                continue;
            }

            sourcesRight = Math.Max(sourcesRight, inverter.Rect.Right);
            trunkTaps.Add((inverter.Rect.Center.Y, inverter.Item.Node.Power ?? 0));

            // The DC cards, each into its own tap on the inverter's left edge; the taps are spread around the middle.
            var dcSources = cluster.DcSources.Select(item => cards.TryGetValue(item.Key, out var card) ? card : default).Where(card => card.Item is { }).ToList();

            for (var i = 0; i < dcSources.Count; i++)
            {
                var (item, rect) = dcSources[i];
                var tapY = inverter.Rect.Center.Y + (i - (dcSources.Count - 1) / 2.0) * TapSpacing;
                var midX = rect.Right + (inverter.Rect.Left - rect.Right) / 2;
                var path = Path(new Point(rect.Right, rect.Center.Y), new Point(midX, rect.Center.Y), new Point(midX, tapY), new Point(inverter.Rect.Left, tapY));
                Set(needed, $"dc:{item.Key}", item.Node, path, labelAt: new Point(midX, rect.Center.Y - 12), signed: item.Node.Kind == PowerFlowNodeKind.Battery);
            }
        }

        if (double.IsNegativeInfinity(sourcesRight))
        {
            // No source yet. Only the house and its consumers then; the trunk waits for the first inverter.
            RouteConsumers(needed, items, cards, house);
            Prune(needed);
            return;
        }

        var trunkX = sourcesRight + (house.Rect.Left - sourcesRight) / 2;

        if (items.Grid is { } gridNode && cards.TryGetValue(gridNode.Key, out var gridRect))
        {
            var y = gridRect.Rect.Center.Y;
            Set(needed, "grid", gridRect.Item.Node, Path(new Point(gridRect.Rect.Right, y), new Point(trunkX, y)), labelAt: new Point(gridRect.Rect.Right + (trunkX - gridRect.Rect.Right) / 2, y - 12), signed: true, dots: [new Point(trunkX, y)]);
        }

        foreach (var cluster in items.Inverters)
        {
            if (!cards.TryGetValue(cluster.Inverter.Key, out var inverter))
            {
                continue;
            }

            var y = inverter.Rect.Center.Y;
            Set(needed, $"ac:{cluster.Key}", inverter.Item.Node, Path(new Point(inverter.Rect.Right, y), new Point(trunkX, y)), labelAt: new Point(inverter.Rect.Right + (trunkX - inverter.Rect.Right) / 2, y - 12), signed: false, dots: [new Point(trunkX, y)]);
        }

        // The trunk, one segment per gap between two taps, each carrying the net of everything above it: drawn
        // downwards, so a negative net runs upwards. With one inverter producing, the grid above it and the house
        // below, that is a few watts up to the grid and the rest down to the house, and nothing below the house. As
        // one wire with the house's figure it ran from the top tap to the bottom one, past the house.
        var houseY = house.Rect.Center.Y;
        trunkTaps.Add((houseY, -(house.Item.Node.Power ?? 0)));
        var taps = trunkTaps.OrderBy(tap => tap.Y).ToList();
        var crossing = 0d;

        for (var i = 0; i < taps.Count - 1; i++)
        {
            crossing += taps[i].Injection;
            Set(needed, $"trunk:{i}", new PowerFlowNode($"trunk:{i}", PowerFlowNodeKind.House, string.Empty, crossing), Path(new Point(trunkX, taps[i].Y), new Point(trunkX, taps[i + 1].Y)), labelAt: null, signed: false);
        }

        // The last step into the house carries what the house draws, with its dot on the trunk like every tap.
        Set(needed, "house", house.Item.Node, Path(new Point(trunkX, houseY), new Point(house.Rect.Left, houseY)), labelAt: null, signed: false, dots: [new Point(trunkX, houseY)]);

        RouteConsumers(needed, items, cards, house);
        Prune(needed);
    }

    /// <summary>The spine right of the house, a rail above every row of consumer cards, and a stub down to each card.</summary>
    private void RouteConsumers(HashSet<string> needed, PowerFlowViewModelItems items, Dictionary<string, (PowerFlowNodeItem Item, Rect Rect)> cards, (PowerFlowNodeItem Item, Rect Rect) house)
    {
        var consumers = items.Consumers
            .Select(item => cards.TryGetValue(item.Key, out var card) ? card : default)
            .Where(card => card.Item is { })
            .ToList();

        if (consumers.Count == 0)
        {
            return;
        }

        var houseY = house.Rect.Center.Y;
        var spineX = house.Rect.Right + SpineOffset;

        // A row is every card whose top is the same, within the rounding a layout pass leaves. A rail carries what
        // its row draws and is a consumer for the idle rule: two lamps at 5 W are on, and so is the rail to them.
        var rows = consumers
            .GroupBy(card => Math.Round(card.Rect.Top))
            .OrderBy(group => group.Key)
            .Select((group, i) => new ConsumerRow(group.ToList(), group.Key - RailOffset, new PowerFlowNode($"rail:{i}", PowerFlowNodeKind.Consumer, string.Empty, group.Sum(card => card.Item.Node.Power ?? 0))))
            .ToList();

        // The house feeds the spine at the junction; from there the spine runs up and down as two wires, each
        // starting at the junction, so that the dashes run away from it on both. One path that went up and came back
        // down passed the upper part twice, with the two runs of dashes crossing over each other. The spine carries
        // what the rails carry, under the consumers' idle rule like them.
        Set(needed, "spine", SpineNode("spine", rows), Path(new Point(house.Rect.Right, houseY), new Point(spineX, houseY)), labelAt: null, signed: false, dots: [new Point(spineX, houseY)]);
        SetSpineRun(needed, "spine:up", spineX, houseY, rows.Where(row => row.RailY < houseY).ToList(), Math.Min);
        SetSpineRun(needed, "spine:down", spineX, houseY, rows.Where(row => row.RailY > houseY).ToList(), Math.Max);

        foreach (var (row, railY, rowNode) in rows)
        {
            Set(needed, rowNode.Key, rowNode, Path(new Point(spineX, railY), new Point(row.Max(card => card.Rect.Center.X), railY)), labelAt: null, signed: false, dots: [new Point(spineX, railY)]);

            foreach (var card in row)
            {
                var x = card.Rect.Center.X;
                Set(needed, $"stub:{card.Item.Key}", card.Item.Node, Path(new Point(x, railY), new Point(x, card.Rect.Top)), labelAt: null, signed: false, dots: [new Point(x, railY)]);
            }
        }
    }

    /// <summary>One run of the spine, from the junction with the house to the farthest rail on that side, carrying what those rails draw. No rails there, no run.</summary>
    private void SetSpineRun(HashSet<string> needed, string key, double spineX, double houseY, IReadOnlyList<ConsumerRow> rows, Func<double, double, double> farthest)
    {
        if (rows.Count == 0)
        {
            return;
        }

        Set(needed, key, SpineNode(key, rows), Path(new Point(spineX, houseY), new Point(spineX, rows.Select(row => row.RailY).Aggregate(farthest))), labelAt: null, signed: false);
    }

    private static PowerFlowNode SpineNode(string key, IReadOnlyList<ConsumerRow> rows) => new(key, PowerFlowNodeKind.Consumer, string.Empty, rows.Sum(row => row.Node.Power ?? 0));

    /// <summary>A row of consumer cards, the rail above it and the node that rail carries.</summary>
    private sealed record ConsumerRow(List<(PowerFlowNodeItem Item, Rect Rect)> Cards, double RailY, PowerFlowNode Node);

    private static string Path(params Point[] points) => string.Join(' ', points.Select((point, i) => $"{(i == 0 ? 'M' : 'L')}{point.X.ToString("F1", CultureInfo.InvariantCulture)},{point.Y.ToString("F1", CultureInfo.InvariantCulture)}"));

    private void Set(HashSet<string> needed, string key, PowerFlowNode node, string path, Point? labelAt, bool signed, IReadOnlyList<Point>? dots = null)
    {
        needed.Add(key);

        if (!wires.TryGetValue(key, out var wire))
        {
            wire = new Wire(labelAt is { });
            wire.Attach(Wires);
            wires[key] = wire;
        }

        wire.Update(this, node, path, labelAt, signed, dots ?? []);
    }

    private void Prune(HashSet<string> needed)
    {
        foreach (var (key, wire) in wires.Where(pair => !needed.Contains(pair.Key)).ToList())
        {
            wire.Detach(Wires);
            wires.Remove(key);
        }
    }

    /// <summary>Where a card is in the stage's coordinates, or null while it has not been laid out.</summary>
    private Rect? RectOf(Control card)
    {
        if (card.Bounds.Width <= 0 || card.Bounds.Height <= 0 || card.TranslatePoint(new Point(0, 0), Stage) is not { } origin)
        {
            return null;
        }

        return new Rect(origin, card.Bounds.Size);
    }

    private IBrush Brush(string key) => this.TryFindResource(key, ActualThemeVariant, out var resource) && resource is IBrush brush ? brush : Brushes.Gray;

    /// <summary>
    /// One connection: a faint trace, the moving dashes over it while power flows, the junction dots where it
    /// meets a bus, and for the sources a label with the figure.
    /// </summary>
    private sealed class Wire(bool hasLabel)
    {
        private readonly Path trace = new() { StrokeLineCap = PenLineCap.Round, StrokeJoin = PenLineJoin.Round };
        private readonly Path flow = new() { StrokeLineCap = PenLineCap.Round, StrokeJoin = PenLineJoin.Round };
        private readonly List<Ellipse> dots = [];
        private readonly Border? label = hasLabel ? new Border { CornerRadius = new CornerRadius(4), Padding = new Thickness(5, 1), ZIndex = LabelZIndex } : null;
        private readonly TextBlock? labelText = hasLabel ? new TextBlock { FontSize = 10.5, FontWeight = FontWeight.SemiBold } : null;

        private string? path;
        private string? dotsAt;

        /// <summary>
        /// Above every wire, whichever was drawn last: the canvas adds a wire's paths and dots as it is routed, so
        /// without this the trunk, routed after the taps on it, painted over their dots, and the spine over the rails'.
        /// </summary>
        private const int DotZIndex = 1;

        private const int LabelZIndex = 2;

        /// <summary>Null until the first update, so that the first update styles the wire whatever it is - an idle one included.</summary>
        private PowerFlowKind? kind;

        private bool thick;
        private double speed;

        public string PathData => path ?? string.Empty;

        public bool Moves => speed > 0;

        public bool Reversed => reversed;
        private double period;
        private bool reversed;

        public void Attach(Canvas canvas)
        {
            canvas.Children.Add(trace);
            canvas.Children.Add(flow);

            if (label is { } pill)
            {
                pill.Child = labelText;
                canvas.Children.Add(pill);
            }
        }

        public void Detach(Canvas canvas)
        {
            canvas.Children.Remove(trace);
            canvas.Children.Remove(flow);
            dots.ForEach(dot => canvas.Children.Remove(dot));

            if (label is { } pill)
            {
                canvas.Children.Remove(pill);
            }
        }

        public void Update(PowerFlowView view, PowerFlowNode node, string newPath, Point? labelAt, bool signed, IReadOnlyList<Point> dotsAt)
        {
            var power = node.Power ?? 0;
            var newThick = Math.Abs(power) >= ThickThreshold;
            var newKind = node.FlowKind;

            if (newPath != path)
            {
                path = newPath;
                trace.Data = flow.Data = Geometry.Parse(newPath);
            }

            if (newThick != thick || newKind != kind)
            {
                thick = newThick;
                kind = newKind;
                var thickness = thick ? ThickStrokeThickness : StrokeThickness;
                trace.StrokeThickness = flow.StrokeThickness = thickness;
                // In multiples of the stroke thickness, which is how a dash array is measured.
                flow.StrokeDashArray = [DashLength / thickness, GapLength / thickness];
                period = (DashLength + GapLength) / thickness;
                flow.IsVisible = kind != PowerFlowKind.Idle;
                Recolor(view);
            }

            // Log scale: 20 W crawls, 7 kW is brisk, and neither is a blur or a standstill.
            var absolute = Math.Abs(power);
            var secondsPerPeriod = node.IsIdle ? 0 : Math.Max(0.45, 3.2 - Math.Log10(Math.Max(absolute, 1)) * 0.72);
            speed = secondsPerPeriod > 0 ? period / secondsPerPeriod : 0;
            reversed = node.IsReversed;

            var newDots = string.Join(';', dotsAt.Select(point => $"{point.X:F1},{point.Y:F1}"));

            if (newDots != this.dotsAt)
            {
                this.dotsAt = newDots;
                var canvas = (Canvas)trace.Parent!;
                dots.ForEach(dot => canvas.Children.Remove(dot));
                dots.Clear();

                foreach (var point in dotsAt)
                {
                    var dot = new Ellipse { Width = 7, Height = 7, Fill = view.Brush("FlowJunction"), ZIndex = DotZIndex };
                    Canvas.SetLeft(dot, point.X - 3.5);
                    Canvas.SetTop(dot, point.Y - 3.5);
                    canvas.Children.Add(dot);
                    dots.Add(dot);
                }
            }

            if (label is { } pill && labelText is { } text && labelAt is { } at)
            {
                var newText = PowerText.Format(node.Power, signed, CultureInfo.CurrentCulture);

                if (text.Text != newText)
                {
                    text.Text = newText;
                }

                pill.Measure(Size.Infinity);
                Canvas.SetLeft(pill, at.X - pill.DesiredSize.Width / 2);
                Canvas.SetTop(pill, at.Y - pill.DesiredSize.Height / 2);
            }
        }

        public void Recolor(PowerFlowView view)
        {
            trace.Stroke = view.Brush("FlowIdle");
            flow.Stroke = view.Brush(kind switch
            {
                PowerFlowKind.Solar => "FlowSolar",
                PowerFlowKind.Battery => "FlowBattery",
                PowerFlowKind.Grid => "FlowGrid",
                _ => "FlowHouse",
            });

            dots.ForEach(dot => dot.Fill = view.Brush("FlowJunction"));

            if (label is { } pill && labelText is { } text)
            {
                pill.Background = view.Brush("FlowPageBackground");
                text.Foreground = view.Brush("FlowLabel");
            }
        }

        /// <summary>Moves the dashes on: a smaller offset shifts the pattern towards the end of the path, which is the way the wire is drawn.</summary>
        public void Advance(double seconds)
        {
            if (speed <= 0 || period <= 0)
            {
                return;
            }

            var offset = flow.StrokeDashOffset + (reversed ? speed : -speed) * seconds;
            flow.StrokeDashOffset = ((offset % period) + period) % period;
        }
    }
}
