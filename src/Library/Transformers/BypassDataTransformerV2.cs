using Library.Infra.EventArgs;
using Library.Infra.ZeroAlloc;
using System.Diagnostics;

namespace Library.Transformers;

/// <summary>
/// A zero-allocation data transformer that bypasses the transformation process,
/// directly passing through the input data to the output without any modifications.
/// </summary>
public class BypassDataTransformerV2 : IDataTransformerV2
{
    private readonly int _notifyAfter;

    public event TransformNotificationHandler? OnTransform;
    public event TransformNotificationHandler? OnFinish;
    public event EasyEtlErrorEventHandler? OnError;

    public long IngestedLines { get; set; }
    public long TransformedLines { get; set; }
    public long ExcludedByFilter { get; set; }
    public double PercentDone { get; set; }
    public long TotalLines { get; set; }
    public double Speed { get; private set; }

    private readonly Stopwatch _timer = new();
    private readonly FieldDescriptor[] _schema;

    /// <summary>
    /// Gets the input schema (same as output for bypass).
    /// </summary>
    public FieldDescriptor[] InputSchema => _schema;

    /// <summary>
    /// Gets the output schema (same as input for bypass).
    /// </summary>
    public FieldDescriptor[] OutputSchema => _schema;

    /// <summary>
    /// Initializes a new instance of the <see cref="BypassDataTransformerV2"/> class.
    /// </summary>
    /// <param name="schema">The schema for the records (input and output are the same).</param>
    /// <param name="notifyAfter">Notify progress after this many records.</param>
    public BypassDataTransformerV2(FieldDescriptor[] schema, int notifyAfter = 1000)
    {
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        _notifyAfter = notifyAfter;
    }

    /// <summary>
    /// Passes through the record without modification.
    /// </summary>
    /// <param name="input">The input record.</param>
    /// <param name="pool">Pool for buffer allocation (not used in bypass).</param>
    /// <param name="outputCallback">Callback to emit the output record.</param>
    public void Transform(ref EtlRecord input, EtlRecordPool pool, RecordOutputCallback outputCallback)
    {
        if (!_timer.IsRunning)
        {
            _timer.Start();
        }

        IngestedLines++;
        TransformedLines++;

        // Pass through without modification
        outputCallback(ref input);

        NotifyProgress();
    }

    /// <summary>
    /// Stops the timer and notifies finish.
    /// </summary>
    public void Complete()
    {
        _timer.Stop();
        NotifyFinish();
    }

    /// <summary>
    /// Notifies subscribers about the progress after processing each batch of items.
    /// </summary>
    private void NotifyProgress()
    {
        if (TransformedLines % _notifyAfter == 0) 
        {
            if (TotalLines == 0) TotalLines = TransformedLines;
            PercentDone = (double)TransformedLines / IngestedLines * 100;
            Speed = IngestedLines / _timer.Elapsed.TotalSeconds;
            OnTransform?.Invoke(new TransformNotificationEventArgs(
                TotalLines, 
                IngestedLines, 
                TransformedLines, 
                ExcludedByFilter, 
                PercentDone, 
                Speed));
        }
    }

    /// <summary>
    /// Notifies subscribers that the transformation process has finished.
    /// </summary>
    private void NotifyFinish()
    {
        if (TotalLines < TransformedLines) TotalLines = TransformedLines;
        PercentDone = 100;
        Speed = TransformedLines / _timer.Elapsed.TotalSeconds;
        OnFinish?.Invoke(new TransformNotificationEventArgs(
            TotalLines, 
            IngestedLines, 
            TransformedLines, 
            ExcludedByFilter, 
            PercentDone, 
            Speed));
    }
}
