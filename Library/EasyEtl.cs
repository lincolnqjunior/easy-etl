using Library.Readers;
using Library.Transformers;
using Library.Writers;
using System.Threading.Channels;

namespace Library
{
    public class EasyEtl(IFileReader reader, IDataTransformer transformer, IDataWriter writer, int channelSize = 1000)
    {
        private readonly IFileReader _reader = reader;
        private readonly IDataTransformer _transformer = transformer;
        private readonly IDataWriter _writer = writer;
        private readonly Channel<Dictionary<string, object>> extractChannel = Channel.CreateBounded<Dictionary<string, object>>(channelSize);
        private readonly Channel<Dictionary<string, object>> transformChannel = Channel.CreateBounded<Dictionary<string, object>>(channelSize);
        private long linesToWrite = 0;


        public void Init(string filePath)
        {
            Task.WhenAll(Extract(filePath), Transform(), Load()).Wait();
        }

        private Task Extract(string filePath)
        {
            return Task.Run(() =>
            {
                try
                {
                    _reader.Read(filePath, (ref Dictionary<string, object> row) =>
                    {
                        var buffer = new Dictionary<string, object>(row);
                        var result = extractChannel.Writer.TryWrite(buffer);

                        if (!result)
                        {
                            var exMsg = $"Could not write to extractChannel. {Environment.NewLine}Line: {_reader.LineNumber} {Environment.NewLine}Row Values: {buffer.Values}";
                            throw new ChannelClosedException(exMsg);
                        }
                    });

                    extractChannel.Writer.Complete();
                }
                catch (Exception ex)
                {
                    extractChannel.Writer.Complete(ex);
                }
            });
        }

        private Task Transform()
        {
            return Task.Run(async () =>
            {
                var transformedRows = _transformer.Transform(extractChannel.Reader.ReadAllAsync());
                await foreach (var transformedRow in transformedRows)
                {
                    transformChannel.Writer.TryWrite(transformedRow);
                    Interlocked.Increment(ref linesToWrite);
                    _writer.TotalLines = linesToWrite;
                }

                transformChannel.Writer.Complete();
            });
        }

        private Task Load()
        {   
            return Task.Run(async () =>
            {
                await _writer.Write(transformChannel.Reader.ReadAllAsync());
            });
        }
    }
}
