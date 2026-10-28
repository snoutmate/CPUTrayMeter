using System.Windows;
using System.Windows.Media;

namespace CpuTrayMeter;

// visual element that renders CPU usage bars
public class CpuMetersVisual : FrameworkElement
{
    private readonly VisualCollection mChildren;
    private List<float> mBarValues;
    private double mFullWidth = 0;
    // used to determine the tray window size
    public double FullWidth => mFullWidth;

    public CpuMetersVisual()
    {
        mBarValues = [];
        // fill in some mValues for xaml Designer view only
        // will get replaced by proper # of cores when the window loads
        for (int i = 0; i < 24; ++i)
            mBarValues.Add(i / 24.0f * 100.0f);

        ReCalaculateWidth();
        mChildren = new VisualCollection(this);
        UpdateVisuals(mBarValues);
    }

    public void UpdateVisuals(List<float> values)
    {
        mBarValues = values;
        mChildren.Clear();
        mChildren.Add(DrawBars());
    }

    // width based on number of bars(logical cores) and size/spacing
    public void ReCalaculateWidth()
    {
        var settings = App.Instance.Settings;
        int bar_width = settings.CpuBars.BarWidth;
        int space_width = settings.CpuBars.GapWidth;

        if (mBarValues.Count > 0)
            mFullWidth = mBarValues.Count * bar_width + ((mBarValues.Count - 1) * space_width);
        else
            mFullWidth = 0;
    }

    // called every frame
    private DrawingVisual DrawBars()
    {
        var settings = App.Instance.Settings;

        DrawingVisual drawingVisual = new();

        DrawingContext drawingContext = drawingVisual.RenderOpen();

        // TODO: peak tracing ?
        Brush base_color = new SolidColorBrush(settings.CpuBars.FloorLineColor);
        Brush color = new SolidColorBrush(settings.CpuBars.BarColor);

        int bar_width = settings.CpuBars.BarWidth;
        int space_width = settings.CpuBars.GapWidth;
        double max_height = ActualHeight;

        if (settings.CpuBars.BarsSorted)
            mBarValues.Sort();

        double top = 0;
        double x = 0;
        foreach (float pct in mBarValues)
        {
            float unit_value = pct / 100.0f;

            // exponential scaling gives less visual noise when computer is
            //   under very light load with multiple processes firing on and off
            double value = settings.CpuBars.BarsScaling switch
            {
                AppSettingsCpuBars.BarsScalingMode.Linear => unit_value,
                AppSettingsCpuBars.BarsScalingMode.SoftExponential => Math.Pow(unit_value, 1.33),
                AppSettingsCpuBars.BarsScalingMode.HardExponential => Math.Pow(unit_value, 2.0),
                _ => throw new NotImplementedException()
            };

            // the bar itself
            int height = (int)(max_height * value);
            var rect = new Rect(new System.Windows.Point(x, top + max_height - height),
                new System.Windows.Size(bar_width, height));
            drawingContext.DrawRectangle(color, null, rect);

            // base lines
            if (settings.CpuBars.FloorLines)
            {
                var rect_base = new Rect(new System.Windows.Point(x, top + max_height - 1),
                    new System.Windows.Size(bar_width, 1));
                drawingContext.DrawRectangle(base_color, null, rect_base);
            }

            x += bar_width + space_width;
        }

        // persist the drawing content.
        drawingContext.Close();

        return drawingVisual;
    }


    // Provide a required override for the VisualChildrenCount property.
    protected override int VisualChildrenCount
    {
        get { return mChildren.Count; }
    }

    // Provide a required override for the GetVisualChild method.
    protected override Visual GetVisualChild(int index)
    {
        if (index < 0 || index >= mChildren.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return mChildren[index];
    }

}
