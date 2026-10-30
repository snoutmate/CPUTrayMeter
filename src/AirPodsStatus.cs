using System.Diagnostics;

namespace CpuTrayMeter;

public class AirPodsStatus
{
    public struct Pod
    {
        public int level;
        public bool isCharging;
        public bool isInEar;
    };

    public struct Case
    {
        public int level;
        public bool isCharging;
        public bool isLidOpen;
    };

    public Pod mLeft;
    public Pod mRight;
    public Case mCase;

    public int mModelD;
    public bool mPaired;

    public bool isValid = false;

    public AirPodsStatus(byte[] packetData)
    {
        // Airpods 
        // raw MD: 07-19-01-24-20-15-88-F9-... (rest is encrypted data)
        //         00 01 02 03 04 05 06 07 08 09 0a 0b 0c 0d 0e 0f 10 11 12 13 14 15 16 17 18 19 1a
        // 27 bytes

        // byte 0 - type - 0x07
        // byte 1 - length - 0x19
        // byte 2 - prefix - 0x1 mPaired, 0x0 unpaired
        // byte 3 - mModelD ID, high byte
        // byte 4 - mModelD ID, low byte
        //   - 0x0E20 - AirPods Pro(gen 1)
        //   - 0x1420 - AirPods Pro 2 (lightning)
        //   - 0x2420 - AirPods Pro 2 (USB-C)
        //   - 0x0220 - gen 1
        //   - 0x0F20 - gen 2
        //   - 0x1320 - gen 3
        //   - 0x0A20 - Max
        // byte 5 - status flags
        //   high nibble:
        //   - x000 - ?
        //   - 0x00 - ?
        //   - 00x0 - is flipped ? (flipped = primary is left earphone)
        //   - 000x - case lid open
        //   low nibble:
        //   - x000 - secondary in-ear
        //   - 0x00 - ?
        //   - 00x0 - primary in-ear
        //   - 000x - mic on primary earphone ?
        // byte 6 - charge*10%, 0xf = not available (disc/charging in closed case)
        //   high nibble: secondary pod charge
        //   low nibble: primary pod charge
        // byte 7 - charge cont., 
        //   high nibble:
        //   - x000 - ?
        //   - 0x00 - case is charging (via usb/tbolt)
        //   - 00x0 - secondary pod is charging
        //   - 000x - primary pod is charging
        //   low nibble - case charge*10% , 0xf = not available (pods not in case)

        mPaired = ((packetData[2] & 0x1) != 0);
        mModelD = packetData[3] << 8 | packetData[4];
        bool isFlipped = ((packetData[5] & 0x20) != 0);

        bool primInEar = ((packetData[5] & 0x02) != 0);
        bool secInEar = ((packetData[5] & 0x08) != 0);

        int primLevel = (packetData[6] & 0xf);
        int secLevel = ((packetData[6] >> 4) & 0xf);
        if (primLevel == 0xf)
            primLevel = -1;
        if (secLevel == 0xf)
            secLevel = -1;

        bool primIsCharging = ((packetData[7] & 0x10) > 0);
        bool secIsCharging = ((packetData[7] & 0x20) > 0);

        mCase.isCharging = ((packetData[7] & 0x40) > 0);
        mCase.level = (packetData[7] & 0xf);
        if (mCase.level == 0xf)
            mCase.level = -1;

        mCase.isLidOpen = ((packetData[5] & 0x10) > 0);

        if (isFlipped)
        {
            mLeft.level = primLevel;
            mLeft.isInEar = primInEar;
            mLeft.isCharging = primIsCharging;

            mRight.level = secLevel;
            mRight.isInEar = secInEar;
            mRight.isCharging = secIsCharging;
        }
        else
        {
            mLeft.level = secLevel;
            mLeft.isInEar = secInEar;
            mLeft.isCharging = secIsCharging;

            mRight.level = primLevel;
            mRight.isInEar = primInEar;
            mRight.isCharging = primIsCharging;
        }

        isValid = true;
    }

    public void PrintDebugInfo()
    {
        Trace.WriteLine("Airpods:");
        Trace.WriteLine($"model: {mModelD}");
        Trace.WriteLine($"left: {mLeft.level} {(mLeft.isCharging ? "[CHARGING]" : "")} {(mLeft.isInEar ? "[IN-EAR]" : "")}");
        Trace.WriteLine($"right: {mRight.level} {(mRight.isCharging ? "[CHARGING]" : "")} {(mRight.isInEar ? "[IN-EAR]" : "")}");
        Trace.WriteLine($"case: {mCase.level} {(mCase.isCharging ? "[CHARGING]" : "")}");
        Trace.WriteLine($"case lid is {(mCase.isLidOpen ? "OPEN" : "CLOSED")}");
    }

}
