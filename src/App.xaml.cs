using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace CpuTrayMeter;

// application setup and entry point
public partial class App : Application
{
    public MainWindow? mMainWindow = null;
    public SettingsWindow? mSettingsWindow = null;
    private bool mSettingsWindowShown = false;

    public SettingsService SettingsService { get; } = new(App.AppName);
    public AppSettings Settings { get; private set; } = new();

    public static App Instance => (App)Current;

    public static string Version
    {
        get
        {
            var version = Assembly
                .GetEntryAssembly()?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;
            return version ?? "?.?.?";
        }
    }

    public static string AppName => Process.GetCurrentProcess().ProcessName;
    public static string AppNameWithVersion => AppName + " v" + Version;

    private static void SetRunAtWindowsStart(bool run)
    {
        var exePath = Environment.ProcessPath;
        var regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        if (exePath != null && regKey != null)
        {
            if (run)
                regKey.SetValue(AppName, exePath);
            else
                regKey.DeleteValue(AppName, false);
        }
    }

    private void OnSettingsChange(object? s, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "RunAtStartup")
            SetRunAtWindowsStart(Settings.RunAtStartup);

        // save settings on any change
        SettingsService.Save(Settings);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Settings = SettingsService.Load();
        Settings.SubscribeToChanges(OnSettingsChange);

        mMainWindow = new MainWindow();
        bool? result = mMainWindow.ShowDialog(); // blocking
        if (result == true)
        {
            // OK
        }
        else
        {
            // Dialog creation failed
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SettingsService.Save(Settings);
        base.OnExit(e);
    }

    public void ShowSettingsWindow()
    {
        mSettingsWindow = new SettingsWindow();
        mSettingsWindow.Closed += (sender, e) =>
        {
            mSettingsWindow = null;
            mSettingsWindowShown = false;
        };

        mSettingsWindow.Show();
        mSettingsWindowShown = true;
    }
    public void HideSettinsWindow()
    {
        mSettingsWindow?.Close();
    }

    public void ToggleSettingsWindow()
    {
        if (mSettingsWindowShown)
            HideSettinsWindow();
        else
            ShowSettingsWindow();
    }
}
