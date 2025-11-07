using Library.Infra.EventArgs;
using Library.Infra.ZeroAlloc;

namespace Library.Transformers;

/// <summary>
/// Callback delegate for emitting transformed records.
/// </summary>
/// <param name="record">The output record.</param>
public delegate void RecordOutputCallback(ref EtlRecord record);

/// <summary>
/// Interface for data transformers that use zero-allocation EtlRecord.
/// </summary>
public interface IDataTransformerV2
{
    event TransformNotificationHandler OnTransform;
    event TransformNotificationHandler OnFinish;
    event EasyEtlErrorEventHandler OnError;

    long IngestedLines { get; set; }
    long TransformedLines { get; set; }
    long ExcludedByFilter { get; set; }
    double PercentDone { get; set; }
    long TotalLines { get; set; }

    /// <summary>
    /// Gets the input schema expected by this transformer.
    /// </summary>
    FieldDescriptor[] InputSchema { get; }

    /// <summary>
    /// Gets the output schema produced by this transformer.
    /// </summary>
    FieldDescriptor[] OutputSchema { get; }

    /// <summary>
    /// Transforms a record in-place or produces new records via callback.
    /// </summary>
    /// <param name="input">Input record to transform.</param>
    /// <param name="pool">Pool for allocating output buffers if needed.</param>
    /// <param name="outputCallback">Callback to emit output records.</param>
    void Transform(ref EtlRecord input, EtlRecordPool pool, RecordOutputCallback outputCallback);
}
