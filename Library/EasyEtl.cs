using Library.Readers;
using Library.Transformers;
using System.Threading.Channels;

namespace Library
{
    public class EasyEtl(IFileReader reader, IDataTransformer transformer, int channelSize = 1000)
    {
        private readonly IFileReader _reader = reader;
        private readonly IDataTransformer _transformer = transformer;
        private readonly Channel<Dictionary<string, object>> extractChannel = Channel.CreateBounded<Dictionary<string, object>>(channelSize);
        private readonly Channel<Dictionary<string, object>> transformChannel = Channel.CreateBounded<Dictionary<string, object>>(channelSize);

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
                }

                transformChannel.Writer.Complete();
            });
        }

        private Task Load()
        {
            var a = 0;
            return Task.Run(async () =>
            {
                await foreach (var row in transformChannel.Reader.ReadAllAsync())
                {
                    a++;
                }
            });
        }
    }
}
