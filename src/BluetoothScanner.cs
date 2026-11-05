global using BLEAdvertHandler = Windows.Foundation.TypedEventHandler<
    Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementWatcher,
    Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementReceivedEventArgs>;

using Windows.Devices.Bluetooth.Advertisement;

namespace CpuTrayMeter;

public class BluetoothScanner
{
    private readonly BluetoothLEAdvertisementWatcher mWatcher;

    public BluetoothScanner()
    {
        mWatcher = new BluetoothLEAdvertisementWatcher
        {
            // Active scanning also requests scan responses
            //ScanningMode = BluetoothLEScanningMode.Active
            ScanningMode = BluetoothLEScanningMode.Passive
        };

        // at least 1 callback function is needed before calling Start()
        mWatcher.Received += (s, args) => { };
        mWatcher.Start();
    }

    public void SubscribeToAdvert(BLEAdvertHandler handler)
    {
        mWatcher.Received += handler;
    }

    public void Stop() => mWatcher.Stop();

    // TODO: unsubscribe
};

