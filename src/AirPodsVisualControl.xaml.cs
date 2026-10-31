using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Windows.Storage.Streams;

namespace CpuTrayMeter;

/// <summary>
/// Interaction logic for AirPodsVisualControl.xaml
/// </summary>
public partial class AirPodsVisualControl : UserControl
{
    private AirPodsStatus mStatus;

    private readonly DispatcherTimer mTimer;

    public AirPodsVisualControl()
    {
        InitializeComponent();
        mStatus = new();
        mTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
        mTimer.Start();
        mTimer.Tick += OnTimer;
    }

    private void OnTimer(object? sender, EventArgs e)
    {
        UpdateVisuals();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        App.Instance.SubscribeToBTAdvert((sender, args) =>
        {
            var adv = args.Advertisement;

            // AD type 0xFF
            foreach (var md in adv.ManufacturerData)
            {
                if (md.CompanyId == 0x4C && // Apple Inc. Company ID
                    md.Data.Length == 27) // airpods data packet length
                {
                    byte[] data = ReadBuffer(md.Data);

                    Trace.WriteLine($"Manufacturer ID: 0x{md.CompanyId:X4}");
                    Trace.WriteLine($"Manufacturer Data: {BitConverter.ToString(data)}");

                    AirPodsDataReceived(data);
                }
            }
        });
    }

    private void AirPodsDataReceived(byte[] data)
    {
        var packet = new AirPodsStatus(data);
        if (!packet.IsValid)
            return;

        packet.PrintDebugInfo();

        // we need to execute UI calls from UI thread via Dispatcher.Invoke()
        // (because this callback is called from non-UI thread)
        // alternatively we can just update in Timer
        this.Dispatcher.Invoke(() => 
        {
            mStatus = packet;
            UpdateVisuals();
        });
    }

    private void UpdateVisuals()
    {
        if (mStatus.IsValid && mStatus.IsRecent)
        {
            leftPodChargeText.Content = $"{(mStatus.mLeft.level < 0 ? "" : mStatus.mLeft.level * 10)}";
            rightPodChargeText.Content = $"{(mStatus.mRight.level < 0 ? "" : mStatus.mRight.level * 10)}";

            if (mStatus.mLeft.level<0)
                leftPod.Stroke = new SolidColorBrush(Colors.Black);
            else
                leftPod.Stroke = new SolidColorBrush(mStatus.mLeft.isCharging ? Colors.Blue : Colors.White);


            if (mStatus.mRight.level < 0)
                rightPod.Stroke = new SolidColorBrush(Colors.Black);
            else
                rightPod.Stroke = new SolidColorBrush(mStatus.mRight.isCharging ? Colors.Blue : Colors.White);

            caseChargeText.Content = $"{(mStatus.mCase.level < 0 ? "" : mStatus.mRight.level * 10)}";
        }
        else
        {
            leftPodChargeText.Content = "";
            rightPodChargeText.Content = "";
            leftPod.Stroke = new SolidColorBrush(Colors.Black);
            rightPod.Stroke = new SolidColorBrush(Colors.Black);
        }

    }

    private static byte[] ReadBuffer(IBuffer buffer)
    {
        var data = new byte[buffer.Length];
        using var reader = DataReader.FromBuffer(buffer);
        reader.ReadBytes(data);
        return data;
    }

}
