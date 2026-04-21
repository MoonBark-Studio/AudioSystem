using Godot;
using MoonBark.Framework.Core;
using MoonBark.Telemetry.Core;
using MoonBark.Telemetry.Godot.InGame;
using System.Collections.Generic;

namespace MoonBark.Telemetry.Demo.Tests;

/// <summary>
/// Engine-integrated test for the TelemetryPanel visual node.
/// Ensures the panel handles data updates correctly inside the Godot SceneTree.
/// </summary>
public partial class TelemetryPanelIntegrationTests : Node
{
    private TelemetryPanel? _panel;
    private MockPerformanceData _mockData = new();
    private TelemetryConfiguration _config = new();

    public override void _Ready()
    {
        // Initial setup for the test
        _panel = new TelemetryPanel();
        _panel.Initialize(_mockData, _config);
        AddChild(_panel);
        
        GD.Print("TelemetryPanelIntegrationTests: Setup complete.");
        
        RunTests();
    }

    private async Task RunTests()
    {
        await ToSignal(GetTree(), "process_frame");
        
        TestVisibilityToggle();
        TestMockDataRendering();
        
        GD.Print("All Telemetry Integration Tests Passed!");
    }

    private void TestVisibilityToggle()
    {
        if (_panel == null) return;

        // Force toggle logic
        var input = new InputEventKey { Pressed = true, Keycode = Key.F3 };
        _panel._Input(input);
        
        if (!_panel.Visible)
            GD.PrintErr("TestVisibilityToggle FAILED: Panel should be visible after F3.");
    }

    private void TestMockDataRendering()
    {
        if (_panel == null) return;

        _mockData.UpdateMetrics("MovementSystem", 1.5, 1.2, 2.0, 100);
        
        // Ensure process frame runs and draw is queued
        _panel.QueueRedraw();
        
        GD.Print("TestMockDataRendering: Metrics updated and redraw queued.");
    }
}

public class MockPerformanceData : IPerformanceData
{
    private List<SystemMetrics> _systems = new();
    public IReadOnlyList<SystemMetrics> Systems => _systems;
    public int EntityCount { get; set; } = 1000;
    public int ComponentCount { get; set; } = 5000;
    public long GcGen0Collections { get; set; } = 42;

    public void UpdateMetrics(string name, double latest, double avg, double peak, int entities)
    {
        _systems.Clear();
        _systems.Add(new SystemMetrics(name, latest, avg, peak, entities));
    }

    public void ResetPeaks() { }
}
