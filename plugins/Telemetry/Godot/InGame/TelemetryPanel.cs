using Godot;
using MoonBark.Framework.Core;
using MoonBark.Telemetry.Core;

namespace MoonBark.Telemetry.Godot.InGame;

/// <summary>
/// In-game telemetry panel — displays ECS perf metrics and game stat summaries during play.
/// Toggled with a key (default F3). Reads IPerformanceData + TelemetryConfiguration.
/// </summary>
public partial class TelemetryPanel : Control
{
    public enum SystemSortMode
    {
        Performance,
        Name,
    }

    private IPerformanceData? _perfData;
    private IReadOnlyWorldView? _worldView;
    private GameStatDatabase? _statDatabase;
    private TelemetryConfiguration _config = new();
    private bool _visible;
    private bool _devMode;
    private string _toggleKey = "F3";
    private SystemSortMode _sortMode = SystemSortMode.Performance;

    private PanelContainer? _panelRoot;
    private Label? _summaryLabel;
    private Label? _hotspotLabel;
    private Button? _sortByPerformanceButton;
    private Button? _sortByNameButton;
    private Action? _sortByPerformanceHandler;
    private Action? _sortByNameHandler;
    private VBoxContainer? _systemList;
    private VBoxContainer? _statsList;
    private ScrollContainer? _scrollContainer;
    private int _lastRenderedTick = -1;
    private bool _refreshRequested = true;

    /// <summary>
    /// Initialize the panel with performance data and configuration.
    /// </summary>
    /// <param name="perfData">ECS performance data source.</param>
    /// <param name="worldView">Optional game simulation data source.</param>
    /// <param name="config">Panel configuration from YAML.</param>
    public void Initialize(IPerformanceData perfData, IReadOnlyWorldView? worldView, TelemetryConfiguration config)
    {
        _perfData = perfData;
        _worldView = worldView;
        _config = config;
        _toggleKey = config.ToggleKey;
        _statDatabase = new GameStatDatabase(
            config.Stats,
            perfData,
            worldView
        );

        if (GodotObject.IsInstanceValid(_panelRoot))
            _panelRoot.CustomMinimumSize = new Vector2(_config.PanelWidth, 420);

        _refreshRequested = true;
    }

    public override void _Ready()
    {
        _devMode = OS.HasFeature("editor") || !OS.HasFeature("standalone");
        _visible = _devMode;
        BuildUi();
        RefreshUi();
        SetPanelVisible(_visible);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed)
        {
            if (key.Keycode == Key.F3 || (_toggleKey == "F3" && key.Keycode == Key.F3))
            {
                _visible = !_visible;
                SetPanelVisible(_visible);
            }
        }
    }

    public override void _Process(double delta)
    {
        if (!_visible || _perfData == null)
            return;

        RefreshUi();
    }

    private static readonly NamePerformanceComparer NamePerformanceComparerInstance = new();
    private static readonly PerformanceNameComparer PerformanceNameComparerInstance = new();

    public static List<SystemMetrics> SortSystems(IEnumerable<SystemMetrics> systems, SystemSortMode sortMode)
    {
        var list = systems as List<SystemMetrics> ?? new List<SystemMetrics>(systems);
        list.Sort(sortMode == SystemSortMode.Name ? NamePerformanceComparerInstance : PerformanceNameComparerInstance);
        return list;
    }

    private sealed class NamePerformanceComparer : IComparer<SystemMetrics>
    {
        public int Compare(SystemMetrics a, SystemMetrics b)
        {
            int nameCompare = string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            if (nameCompare != 0) return nameCompare;
            return b.LatestMs.CompareTo(a.LatestMs);
        }
    }

    private sealed class PerformanceNameComparer : IComparer<SystemMetrics>
    {
        public int Compare(SystemMetrics a, SystemMetrics b)
        {
            int perfCompare = b.LatestMs.CompareTo(a.LatestMs);
            if (perfCompare != 0) return perfCompare;
            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void BuildUi()
    {
        if (GodotObject.IsInstanceValid(_panelRoot))
            return;

        _panelRoot = new PanelContainer
        {
            Name = "TelemetryPanelRoot",
            Position = new Vector2(_config.PanelOffsetX, _config.PanelOffsetY),
            CustomMinimumSize = new Vector2(_config.PanelWidth, 420),
            Size = new Vector2(_config.PanelWidth, 420),
            Visible = false,
        };
        AddChild(_panelRoot);

        ScrollContainer scrollContainer = new()
        {
            Name = "TelemetryScroll",
            OffsetLeft = 8f,
            OffsetTop = 8f,
            OffsetRight = -8f,
            OffsetBottom = -8f,
        };
        _scrollContainer = scrollContainer;
        _panelRoot.AddChild(scrollContainer);

        VBoxContainer content = new()
        {
            Name = "TelemetryContent",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        scrollContainer.AddChild(content);

        Label titleLabel = new()
        {
            Name = "TelemetryTitle",
            Text = "MoonBark Telemetry",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 18);
        content.AddChild(titleLabel);

        content.AddChild(new HSeparator());

        _summaryLabel = new Label
        {
            Name = "TelemetrySummary",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        content.AddChild(_summaryLabel);

        HBoxContainer controlRow = new()
        {
            Name = "TelemetrySortControls",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        content.AddChild(controlRow);

        _sortByPerformanceButton = new Button
        {
            Name = "SortByPerformanceButton",
            Text = "Sort: Performance",
            ToggleMode = true,
        };
        _sortByPerformanceHandler = () => SetSortMode(SystemSortMode.Performance);
        _sortByPerformanceButton.Pressed += _sortByPerformanceHandler;
        controlRow.AddChild(_sortByPerformanceButton);

        _sortByNameButton = new Button
        {
            Name = "SortByNameButton",
            Text = "Sort: Name",
            ToggleMode = true,
        };
        _sortByNameHandler = () => SetSortMode(SystemSortMode.Name);
        _sortByNameButton.Pressed += _sortByNameHandler;
        controlRow.AddChild(_sortByNameButton);

        _hotspotLabel = new Label
        {
            Name = "TelemetryHotspot",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        content.AddChild(_hotspotLabel);

        content.AddChild(new HSeparator());

        Label systemHeader = new()
        {
            Name = "TelemetrySystemHeader",
            Text = "ECS Systems",
        };
        systemHeader.AddThemeFontSizeOverride("font_size", 16);
        content.AddChild(systemHeader);

        _systemList = new VBoxContainer
        {
            Name = "TelemetrySystemList",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        content.AddChild(_systemList);

        content.AddChild(new HSeparator());

        Label statsHeader = new()
        {
            Name = "TelemetryStatsHeader",
            Text = "Game Stats",
        };
        statsHeader.AddThemeFontSizeOverride("font_size", 16);
        content.AddChild(statsHeader);

        _statsList = new VBoxContainer
        {
            Name = "TelemetryStatsList",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        content.AddChild(_statsList);
    }

    private void RefreshUi(bool force = false)
    {
        if (_perfData == null || !GodotObject.IsInstanceValid(_summaryLabel) || !GodotObject.IsInstanceValid(_hotspotLabel) || !GodotObject.IsInstanceValid(_systemList) || !GodotObject.IsInstanceValid(_statsList))
            return;

        int currentTick = _worldView?.TickCount ?? -1;
        if (!force && !_refreshRequested && currentTick == _lastRenderedTick)
            return;

        double scrollValue = _scrollContainer?.GetVScrollBar()?.Value ?? 0;

        _summaryLabel.Text =
            $"Entities: {_perfData.EntityCount:N0}  Components: {_perfData.ComponentCount:N0}  GC Gen-0: {_perfData.GcGen0Collections:N0}";

        UpdateSortButtons();

        IReadOnlyList<SystemMetrics> systems = SortSystems(_perfData.Systems, _sortMode);
        RefreshSystemList(systems);
        RefreshHotspotSummary(systems);
        RefreshStatsList();

        if (GodotObject.IsInstanceValid(_scrollContainer))
            _scrollContainer.GetVScrollBar().Value = scrollValue;

        _lastRenderedTick = currentTick;
        _refreshRequested = false;
    }

    private void RefreshHotspotSummary(IReadOnlyList<SystemMetrics> systems)
    {
        if (!GodotObject.IsInstanceValid(_hotspotLabel))
            return;

        if (systems.Count == 0)
        {
            _hotspotLabel.Text = "Hotspot: no ECS systems recorded yet.";
            return;
        }

        SystemMetrics hottestSystem = systems[0];
        _hotspotLabel.Text =
            $"Hotspot: {hottestSystem.Name}  {hottestSystem.LatestMs:F2}ms latest / {hottestSystem.AvgMs:F2}ms avg / {hottestSystem.PeakMs:F2}ms peak";
    }

    private void RefreshSystemList(IReadOnlyList<SystemMetrics> systems)
    {
        if (!GodotObject.IsInstanceValid(_systemList))
            return;

        ClearChildren(_systemList);

        _systemList.AddChild(CreateSystemHeaderRow());

        if (systems.Count == 0)
        {
            _systemList.AddChild(CreateEmptyRow("No ECS systems recorded yet."));
            return;
        }

        foreach (SystemMetrics system in systems)
        {
            _systemList.AddChild(CreateSystemRow(system));
        }
    }

    private void RefreshStatsList()
    {
        if (!GodotObject.IsInstanceValid(_statsList))
            return;

        ClearChildren(_statsList);

        if (_statDatabase == null)
        {
            _statsList.AddChild(CreateEmptyRow("No stats database loaded."));
            return;
        }

        bool showDev = _devMode;
        foreach (GameStatValue stat in _statDatabase.GetStats(showInPanel: true))
        {
            if (stat.Definition.DevOnly && !showDev)
                continue;

            string formatted = string.Format(stat.Definition.ValueFormat, stat.Value);
            _statsList.AddChild(CreateStatRow(stat.Definition.DisplayName, formatted));
        }
    }

    private void UpdateSortButtons()
    {
        if (GodotObject.IsInstanceValid(_sortByPerformanceButton))
            _sortByPerformanceButton.ButtonPressed = _sortMode == SystemSortMode.Performance;

        if (GodotObject.IsInstanceValid(_sortByNameButton))
            _sortByNameButton.ButtonPressed = _sortMode == SystemSortMode.Name;
    }

    private void SetSortMode(SystemSortMode sortMode)
    {
        if (_sortMode == sortMode)
            return;

        _sortMode = sortMode;
        _refreshRequested = true;
        RefreshUi(force: true);
    }

    private static HBoxContainer CreateSystemHeaderRow()
    {
        HBoxContainer row = new()
        {
            Name = "TelemetrySystemHeaderRow",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };

        row.AddChild(CreateCellLabel("System", true));
        row.AddChild(CreateCellLabel("Latest", false));
        row.AddChild(CreateCellLabel("Avg", false));
        row.AddChild(CreateCellLabel("Peak", false));
        return row;
    }

    private static HBoxContainer CreateSystemRow(SystemMetrics system)
    {
        HBoxContainer row = new()
        {
            Name = $"TelemetrySystemRow_{SanitizeNodeName(system.Name)}",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };

        row.AddChild(CreateCellLabel(system.Name, true));
        row.AddChild(CreateCellLabel($"{system.LatestMs:F2}ms", false));
        row.AddChild(CreateCellLabel($"{system.AvgMs:F2}ms", false));
        row.AddChild(CreateCellLabel($"{system.PeakMs:F2}ms", false));
        return row;
    }

    private static HBoxContainer CreateStatRow(string label, string value)
    {
        HBoxContainer row = new()
        {
            Name = $"TelemetryStatRow_{SanitizeNodeName(label)}",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };

        row.AddChild(CreateCellLabel(label, true));
        row.AddChild(CreateCellLabel(value, false));
        return row;
    }

    private static Label CreateEmptyRow(string message)
    {
        return new Label
        {
            Text = message,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
    }

    private static Label CreateCellLabel(string text, bool expand)
    {
        Label label = new()
        {
            Text = text,
            SizeFlagsHorizontal = expand ? Control.SizeFlags.ExpandFill : Control.SizeFlags.ShrinkBegin,
        };

        if (!expand)
            label.HorizontalAlignment = HorizontalAlignment.Right;

        return label;
    }

    private static void ClearChildren(Node parent)
    {
        foreach (Node child in parent.GetChildren())
        {
            if (!child.IsQueuedForDeletion())
                child.QueueFree();
        }
    }

    private void SetPanelVisible(bool visible)
    {
        Visible = visible;
        if (GodotObject.IsInstanceValid(_panelRoot))
            _panelRoot.Visible = visible;
    }

    private static string SanitizeNodeName(string value)
    {
        char[] chars = value
            .Select(character => char.IsLetterOrDigit(character) ? character : '_')
            .ToArray();
        return new string(chars);
    }

    public override void _ExitTree()
    {
        // Unsubscribe from all button events to prevent memory leaks
        if (GodotObject.IsInstanceValid(_sortByPerformanceButton) && _sortByPerformanceHandler != null)
        {
            _sortByPerformanceButton.Pressed -= _sortByPerformanceHandler;
            _sortByPerformanceHandler = null;
        }

        if (GodotObject.IsInstanceValid(_sortByNameButton) && _sortByNameHandler != null)
        {
            _sortByNameButton.Pressed -= _sortByNameHandler;
            _sortByNameHandler = null;
        }

        base._ExitTree();
    }
}