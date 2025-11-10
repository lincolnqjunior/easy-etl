using Library.Infra.EventArgs;
using Library.Infra.ZeroAlloc;

namespace Library.Loaders;

/// <summary>
/// Interface for data loaders that use zero-allocation EtlRecord.
/// </summary>
public interface IDataLoaderV2
{
    event LoadNotificationHandler? OnWrite;
    event LoadNotificationHandler? OnFinish;
    event EasyEtlErrorEventHandler OnError;

    long CurrentLine { get; set; }
    long TotalLines { get; set; }
    double PercentWritten { get; set; }

    /// <summary>
    /// Gets the schema expected by this loader.
    /// </summary>
    FieldDescriptor[] Schema { get; }

    /// <summary>
    /// Loads a record synchronously (ref struct limitation).
    /// </summary>
    /// <param name="record">Record to load.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    void Load(ref EtlRecord record, CancellationToken cancellationToken);

    /// <summary>
    /// Completes the loading process (flushes buffers, closes connections, etc.).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task Complete(CancellationToken cancellationToken);
}
