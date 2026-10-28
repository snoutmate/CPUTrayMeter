using System.Diagnostics;

namespace CpuTrayMeter;

// gather CPU per-core usage from PerformanceCounters in percent (0-100)
// when querying at short intervals (<1s), the output is usually too noisy to be usable,
// so we average the results over N samples and remove the lows

class CpuUsage
{
    private readonly List<PerformanceCounter> mPerfCounters = [];
    private readonly int mNumCores;

    // anything usage below this value(%) is just noise
    static readonly float cMinimumUsage = 10.0f;

    private Queue<List<float>> mValues;

    private static int QueueSize => App.Instance.Settings.CpuBars.AveragingSamples;

    public CpuUsage()
    {
        mValues = [];
        // logical cores
        mNumCores = Environment.ProcessorCount;
        for (int i = 0; i < mNumCores; ++i)
            mPerfCounters.Add(new PerformanceCounter("Processor", "% Processor Time", i.ToString()));

        InitializeQueue();
    }

    public void InitializeQueue()
    {
        mValues = [];
        for (int i = 0; i < QueueSize; ++i)
            mValues.Enqueue(GetUsage());
    }

    public List<float> GetUsage()
    {
        var usage = new List<float>();
        foreach (var pc in mPerfCounters)
            usage.Add(pc.NextValue());
        return usage;
    }

    // average last N samples
    public List<float> GetFilteredUsage()
    {
        if (mValues.Count != QueueSize)
        {
            // Queue size changed in settings
            InitializeQueue();
        }
        else
        {
            mValues.Dequeue();
            mValues.Enqueue(GetUsage());
        }

        var filteredUsage = new List<float>();
        var nMins = new List<int>();

        for (int i = 0; i < mNumCores; ++i)
        {
            filteredUsage.Add(0.0f);
            nMins.Add(0);
        }

        // average and filter out if most values are below threshold
        foreach (var v in mValues)
        {
            for (int i = 0; i < mNumCores; ++i)
            {
                if (v[i] < cMinimumUsage)
                    nMins[i] += 1;
                filteredUsage[i] += v[i];
            }
        }

        // compute average
        // if more than half the samples are below certain threshold, we force the output to 0.0
        for (int i = 0; i < mNumCores; ++i)
        {
            filteredUsage[i] /= QueueSize;
            if (nMins[i] >= (QueueSize / 2))
                filteredUsage[i] = 0.0f;
        }

        return filteredUsage;
    }
}
