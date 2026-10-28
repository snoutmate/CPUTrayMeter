using System.Windows;

namespace CpuTrayMeter;

// settings window, can be opened by clicking at the app in system tray
public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        this.DataContext = App.Instance.Settings;
    }
}
