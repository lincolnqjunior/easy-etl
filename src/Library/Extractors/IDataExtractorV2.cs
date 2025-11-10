using Library.Infra.EventArgs;
using Library.Infra.ZeroAlloc;

namespace Library.Extractors;

/// <summary>
/// Delegate for processing rows using zero-allocation EtlRecord.
/// </summary>
/// <param name="record">The record to process (passed by reference for zero-copy).</param>
public delegate void RecordAction(ref EtlRecord record);

/// <summary>
/// Interface for data extractors that use zero-allocation EtlRecord.
/// </summary>
public interface IDataExtractorV2
{
    event ReadNotification? OnRead;
    event ReadNotification? OnFinish;
    event EasyEtlErrorEventHandler OnError;

    long TotalLines { get; set; }
    int LineNumber { get; set; }
    long BytesRead { get; set; }
    double PercentRead { get; set; }
    long FileSize { get; set; }

    /// <summary>
    /// Gets the schema for the records produced by this extractor.
    /// </summary>
    FieldDescriptor[] Schema { get; }

    /// <summary>
    /// Extracts data and processes each record using the provided action.
    /// </summary>
    /// <param name="processRecord">Action to process each record.</param>
    void Extract(RecordAction processRecord);
}
