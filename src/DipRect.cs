namespace CpuTrayMeter;

// represents window position and size in device-independent coordinates
public struct DipRect(double left = 0, double top = 0, double right = 0, double bottom = 0)
{
    public double Left = left, Top = top, Right = right, Bottom = bottom;

    public readonly double Height() => Bottom - Top;
    public readonly double Width() => Right - Left;

    // round to nearest full pixel for all comparisons
    public readonly bool Equals(DipRect other) =>
        (int)Left == (int)other.Left &&
        (int)Right == (int)other.Right &&
        (int)Top == (int)other.Top &&
        (int)Bottom == (int)other.Bottom;

    public readonly override bool Equals(object? obj) => obj is DipRect other && Equals(other);

    public override readonly int GetHashCode() => HashCode.Combine((int)Left, (int)Right, (int)Top, (int)Bottom);

    public static bool operator ==(DipRect l, DipRect r) => l.Equals(r);
    public static bool operator !=(DipRect l, DipRect r) => !l.Equals(r);
}
