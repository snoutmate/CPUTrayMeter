using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace CpuTrayMeter;

using static InteropFunctions;

// main application window
// invisible, and only used to open tray window before it gets reparented to taskbar
public partial class MainWindow : Window
{
    private IntPtr mHwnd;

    // open tray window 1 sec after program start
    private readonly DispatcherTimer mMainTimer = new() { Interval = TimeSpan.FromMilliseconds(1000) };

    private bool mFirstStart = true;
    private TrayWindow? mTrayWindow = null;

    public MainWindow()
    {
        // no hWND yet in contructor, but controls exists
        InitializeComponent();
        mMainTimer.Tick += OnMainTimer;
    }

    private void OnMainTimer(object? sender, EventArgs e)
    {
        if (mFirstStart) // this runs one second after the program starts
        {
            //the logic for setting the settings window to "always on top" is placed here to resolve potential issues
            //where the setting might fail to take effect if placed within the initialization function
            DisableMousePenetrate();
            Hide();

            if (mTrayWindow == null)
                OpenTrayWindow();

            mFirstStart = false;
        }
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        // SourceInitialized is ran after constructor, window still not visible yet, but we can now use Win32 interop, SetWindowLong etc.
        mHwnd = new WindowInteropHelper(this).Handle;

        // WS_POPUP
        // returns 0 on fail, use GetLastError()
        IntPtr res = ModifyWindowFlags(mHwnd, GWL_STYLE, addFlags: WS_POPUP);

        // - WS_EX_APPWINDOW, + WS_EX_TOOLWINDOW
        res = ModifyWindowFlags(mHwnd, GWL_EXSTYLE, addFlags: WS_EX_TOOLWINDOW, removeFlags: WS_EX_APPWINDOW);

        // If the program hides the main window upon startup, or if the window is located in the top left corner, first set its opacity to 0.
        Opacity = 0.0;
    }

    // disable click-through for the entire window (needed ?)
    private void DisableMousePenetrate()
    {
        long res = ModifyWindowFlags(mHwnd, GWL_EXSTYLE, removeFlags: WS_EX_TRANSPARENT);
    }

    private void CloseTrayWindow()
    {
        if (mTrayWindow != null)
        {
            if (IsTrayWindowValid())
                mTrayWindow.Close();
            mTrayWindow = null;
        }
    }

    private void OpenTrayWindow()
    {
        mTrayWindow = new TrayWindow();
        mTrayWindow.ShowDialog();
    }

    private bool IsTrayWindowValid()
    {
        return mTrayWindow is not null;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // bring the window to foreground and activate it
        Activate();
        mMainTimer.Start();
    }

    private void OnClosed(object sender, RoutedEventArgs e)
    {
        mMainTimer.Stop();
    }
}
