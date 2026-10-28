using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace CpuTrayMeter;

using static InteropFunctions;

// the main tray window
public partial class TrayWindow : Window
{
    private IntPtr mHwnd;

    private IntPtr mTaskbarHwnd;
    private IntPtr mTaskbarNotifyAreaHwnd;

    private DipRect mTaskbarRect;
    private DipRect mTaskbarNotifyAreaRect;

    // how fast the window updates
    // limited by rendering speed and query PerformanceCounter speed
    private readonly DispatcherTimer mTrayTimer = new() { Interval = TimeSpan.FromMilliseconds(33) };

    private readonly CpuUsage mCpuUsage;

    private bool mSettingsChanged = false;

    // TODO: settable minimum width
    private readonly double cMinWidth = 100;

    DpiScale mDPI;

    public TrayWindow()
    {
        InitializeComponent();

        mCpuUsage = new();
        mTrayTimer.Tick += OnTrayTimer;

        App.Instance.Settings.SubscribeToChanges((s, e) => mSettingsChanged = true);

        MouseLeftButtonDown += Window_MouseLeftButtonDown;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // show/hide settings window when clicking on tray
        App.Instance.ToggleSettingsWindow();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        DebugPrint("-------- OnLoaded -------");

        Activate();
        mTrayTimer.Start();
    }

    private void OnContentRendered(object sender, EventArgs e)
    {
        DebugPrint("-------- OnContentRendered -------");
        SetWindowPositionAndSize();
    }

    private void OnClosed(object sender, RoutedEventArgs e)
    {
        mTrayTimer.Stop();
    }

    private void OnTrayTimer(object? sender, EventArgs e)
    {
        if (TaskbarChanged())
        {
            DebugPrint("Taskbar changed");
            SetWindowPositionAndSize();
        }
        else if (mSettingsChanged)
        {
            DebugPrint("Settings changed");
            mSettingsChanged = false;
            cpuMetersVisual.ReCalaculateWidth();
            SetWindowPositionAndSize();
            UpdateText();
        }

        RenderCPUBars();
        RenderText();
    }

    private void RenderCPUBars()
    {
        var values = mCpuUsage.GetFilteredUsage();
        cpuMetersVisual.UpdateVisuals(values);
    }

    private void UpdateText()
    {
        text.Fill = new SolidColorBrush(App.Instance.Settings.OverlayText.Color);
        text.Stroke = new SolidColorBrush(App.Instance.Settings.OverlayText.OutlineColor);
        text.FontSize = App.Instance.Settings.OverlayText.FontSize;
        text.StrokeThickness = App.Instance.Settings.OverlayText.OutlineThickness;
    }

    private void RenderText()
    {
        if (!App.Instance.Settings.OverlayText.Enabled)
        {
            text.Text = "";
            return;
        }

        // RAM usage
        (ulong avail_ram, ulong total_ram) = InteropFunctions.GetRamUsage();
        ulong used_ram = total_ram - avail_ram;
        double gb_divider = 1024 * 1024 * 1024;

        var template = App.Instance.Settings.OverlayText.Text;
        template = template.Replace("%umem", $"{(used_ram / gb_divider):F1}");
        template = template.Replace("%tmem", $"{(total_ram / gb_divider):F1}");

        text.Text = template;
    }

    public void GetTaskbarHandles()
    {
        mTaskbarHwnd = FindTaskbarHandle();
        mTaskbarNotifyAreaHwnd = FindTaskbarNotifyAreaHandle();
    }

    private void GetTaskbarArea()
    {
        mTaskbarRect = GetWindowRectInDips(mTaskbarHwnd, mDPI);
        mTaskbarNotifyAreaRect = GetWindowRectInDips(mTaskbarNotifyAreaHwnd, mDPI);
    }

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        DebugPrint($"Window_SourceInitialized");

        var wih = new WindowInteropHelper(this);
        mHwnd = wih.Handle;

        // prevent window from appearing in alt+tab menu
        _ = ModifyWindowFlags(mHwnd, GWL_EXSTYLE, addFlags: WS_EX_TOOLWINDOW);

        mDPI = VisualTreeHelper.GetDpi(this);
        GetTaskbarHandles();
        GetTaskbarArea();

        // parent window to task bar, size needs to be reset later in OnContentRendered
        bool cannot_insert_to_task_bar = (SetParent(mHwnd, mTaskbarHwnd) == 0);
        if (cannot_insert_to_task_bar)
            DebugPrint("ERROR: Can't parent tray window to taskbar");
    }

    // Moves window to needed position and resizes if needed
    // - since we're parented to taskbar, anything outside taskbar area won't be shown
    // - this needs to be called in ContentRendered and every time taskbar/notify area changes (position, dpi, or size)
    private void SetWindowPositionAndSize()
    {
        // base width on cpu bars
        Width = Math.Max(cpuMetersVisual.FullWidth, cMinWidth);

        // tray window should take 2/3 of taskbar vertically
        double trayWinHeightRatio = (2.0 / 3.0);
        Height = mTaskbarRect.Height() * trayWinHeightRatio;

        // because we're parenting our (WPF) window to taskbar using p/invoke winapi (SetParent), 
        // the positioning in WPF (Left/Top) doesnt' match, and to align our window such that 0,0 is top left of the taskbar
        // we need to *subtract* the taskbar position from our own (which is 0,0 at start)
        double top = -mTaskbarRect.Top;
        double left = -mTaskbarRect.Left;

        // center vertically on the taskbar
        top += (mTaskbarRect.Height() * (1.0 - trayWinHeightRatio)) / 2.0;

        // position just left of the tray notify area (icons) with adjustable padding
        left += mTaskbarNotifyAreaRect.Left;
        left -= App.Instance.Settings.TrayNotifyPadding;
        left -= ActualWidth;

        Left = left;
        Top = top;
    }

    // has taskbar or taskbar notify (size or position) changed?
    private bool TaskbarChanged()
    {
        var lastTaskbarRect = mTaskbarRect;
        var lastTaskbarNotifyAreaRect = mTaskbarNotifyAreaRect;

        GetTaskbarArea();

        return mTaskbarRect != lastTaskbarRect ||
                mTaskbarNotifyAreaRect != lastTaskbarNotifyAreaRect;
    }

    // handle to taskbar
    private static IntPtr FindTaskbarHandle() => FindWindowW("Shell_TrayWnd", null);

    // handle to taskbar notify area, this includes the fold-out arrow
    private IntPtr FindTaskbarNotifyAreaHandle() => FindWindowExW(mTaskbarHwnd, IntPtr.Zero, "TrayNotifyWnd", null);

    private void DebugPrint(string message)
    {
        Trace.WriteLine($"TrayWindow [hWnd 0x{mHwnd:x}] {message}");
    }
}
