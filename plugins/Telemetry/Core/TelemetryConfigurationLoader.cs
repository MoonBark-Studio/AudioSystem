using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MoonBark.Telemetry.Core;

/// <summary>
/// Loads <see cref="TelemetryConfiguration"/> and <see cref="GameStatDefinition"/> from YAML.
/// Uses YamlDotNet for parsing. Path resolution is delegated to the caller
/// so callers can use their own content-root conventions.
/// </summary>
public static class TelemetryConfigurationLoader
{
    /// <summary>Loads the telemetry configuration from a YAML file.</summary>
    public static TelemetryConfiguration Load(string path)
    {
        if (!File.Exists(path))
            return new TelemetryConfiguration();

        string yaml = File.ReadAllText(path);
        return LoadFromYaml(yaml);
    }

    /// <summary>Loads configuration from a YAML string (useful for embedded configs).</summary>
    public static TelemetryConfiguration LoadFromYaml(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
            return new TelemetryConfiguration();

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var config = deserializer.Deserialize<TelemetryConfigurationYaml>(yaml);
        return config?.ToCore() ?? new TelemetryConfiguration();
    }

    /// <summary>Saves configuration to a YAML file.</summary>
    public static void Save(TelemetryConfiguration config, string path)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var yaml = serializer.Serialize(new TelemetryConfigurationYaml(config));
        File.WriteAllText(path, yaml);
    }

    // Internal YAML shape — fields use underscored names matching YAML conventions.
    // Not public: callers use TelemetryConfiguration directly.
    private sealed class TelemetryConfigurationYaml
    {
        public string? toggle_key { get; set; }
        public int history_size { get; set; } = 60;
        public double critical_threshold_ms { get; set; } = 4.0;
        public bool auto_show_on_critical { get; set; } = true;
        public int panel_offset_x { get; set; } = 10;
        public int panel_offset_y { get; set; } = 10;
        public int panel_width { get; set; } = 400;
        public List<GameStatDefinitionYaml>? stats { get; set; }

        public TelemetryConfigurationYaml() { }

        public TelemetryConfigurationYaml(TelemetryConfiguration config)
        {
            toggle_key = config.ToggleKey;
            history_size = config.HistorySize;
            critical_threshold_ms = config.CriticalThresholdMs;
            auto_show_on_critical = config.AutoShowOnCritical;
            panel_offset_x = config.PanelOffsetX;
            panel_offset_y = config.PanelOffsetY;
            panel_width = config.PanelWidth;
            stats = config.Stats.Select(s => new GameStatDefinitionYaml(s)).ToList();
        }

        public TelemetryConfiguration ToCore()
        {
            return new TelemetryConfiguration
            {
                ToggleKey = toggle_key ?? "F3",
                HistorySize = history_size,
                CriticalThresholdMs = critical_threshold_ms,
                AutoShowOnCritical = auto_show_on_critical,
                PanelOffsetX = panel_offset_x,
                PanelOffsetY = panel_offset_y,
                PanelWidth = panel_width,
                Stats = stats?.Select(s => s.ToCore()).ToList() ?? new List<GameStatDefinition>()
            };
        }
    }

    private sealed class GameStatDefinitionYaml
    {
        public string? id { get; set; }
        public string? display_name { get; set; }
        public string? category { get; set; } = "general";
        public bool show_in_panel { get; set; } = true;
        public bool dev_only { get; set; } = false;
        public string? value_format { get; set; } = "{0}";

        public GameStatDefinitionYaml() { }

        public GameStatDefinitionYaml(GameStatDefinition stat)
        {
            id = stat.Id;
            display_name = stat.DisplayName;
            category = stat.Category;
            show_in_panel = stat.ShowInPanel;
            dev_only = stat.DevOnly;
            value_format = stat.ValueFormat;
        }

        public GameStatDefinition ToCore()
        {
            return new GameStatDefinition
            {
                Id = id ?? string.Empty,
                DisplayName = display_name ?? string.Empty,
                Category = category ?? "general",
                ShowInPanel = show_in_panel,
                DevOnly = dev_only,
                ValueFormat = value_format ?? "{0}"
            };
        }
    }
}
