using System.Data;
using System.Diagnostics;
using System.Windows;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;

namespace CpuTrayMeter;

public class BluetoothScanner
{
    private readonly BluetoothLEAdvertisementWatcher mWatcher;

    public BluetoothScanner()
    {
        mWatcher = new BluetoothLEAdvertisementWatcher
        {
            // Active scanning also requests scan responses
            ScanningMode = BluetoothLEScanningMode.Active
        };

        mWatcher.Received += OnAdvertisementReceived;
        mWatcher.Start();
    }

    private static void OnAdvertisementReceived(
    BluetoothLEAdvertisementWatcher sender,
    BluetoothLEAdvertisementReceivedEventArgs args)
    {
        var adv = args.Advertisement;
//        Trace.Write(".");

        // AD type 0xFF
        foreach (var md in adv.ManufacturerData)
        {
            if (md.CompanyId == 0x4C && // Apple Inc. Company ID
                md.Data.Length == 27) // airpods data packet length
            {
                byte[] data = ReadBuffer(md.Data);

                Trace.WriteLine($"Manufacturer ID: 0x{md.CompanyId:X4}");
                Trace.WriteLine($"Manufacturer Data: {BitConverter.ToString(data)}");

                var packet = new AirPodsStatus(data);
                packet.PrintDebugInfo();

            }
        }
    }

    private static byte[] ReadBuffer(IBuffer buffer)
    {
        var data = new byte[buffer.Length];
        using var reader = DataReader.FromBuffer(buffer);
        reader.ReadBytes(data);
        return data;
    }
};

