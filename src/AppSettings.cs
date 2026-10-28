using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Windows.Media;

namespace CpuTrayMeter;

// app settings and defaults
public partial class AppSettingsCpuBars : ObservableObject
{
    public enum BarsScalingMode
    {
        Linear,
        SoftExponential,
        HardExponential
    }
    public Array BarsScalingModes { get; } = Enum.GetValues<BarsScalingMode>();

    [ObservableProperty] public partial bool BarsSorted { get; set; } = false;
    [ObservableProperty] public partial bool FloorLines { get; set; } = true;

    [ObservableProperty] public partial BarsScalingMode BarsScaling { get; set; } = BarsScalingMode.Linear;
    [ObservableProperty] public partial int AveragingSamples { get; set; } = 16;

    [ObservableProperty] public partial int BarWidth { get; set; } = 5;
    [ObservableProperty] public partial int GapWidth { get; set; } = 2;
    [ObservableProperty] public partial Color BarColor { get; set; } = Colors.DeepSkyBlue;
    [ObservableProperty] public partial Color FloorLineColor { get; set; } = Colors.Gray;
}

public partial class AppSettingsOverlayText : ObservableObject
{
    [ObservableProperty] public partial bool Enabled { get; set; } = true;
    [ObservableProperty] public partial Color Color { get; set; } = Colors.Azure;
    [ObservableProperty] public partial Color OutlineColor { get; set; } = Colors.Black;
    [ObservableProperty] public partial int FontSize { get; set; } = 12;
    [ObservableProperty] public partial int OutlineThickness { get; set; } = 3;
    [ObservableProperty] public partial string Text { get; set; } = "%umem/%tmem GB";
}

public partial class AppSettings : ObservableObject
{
    [ObservableProperty] public partial double TrayNotifyPadding { get; set; } = 20;
    [ObservableProperty] public partial bool RunAtStartup { get; set; } = false;

    public AppSettingsCpuBars CpuBars { get; set; } = new();
    public AppSettingsOverlayText OverlayText { get; set; } = new();

    public void SubscribeToChanges(PropertyChangedEventHandler onChangedCallback)
    {
        // root
        this.PropertyChanged += onChangedCallback;
        // children
        this.CpuBars.PropertyChanged += onChangedCallback;
        this.OverlayText.PropertyChanged += onChangedCallback;
    }
}

