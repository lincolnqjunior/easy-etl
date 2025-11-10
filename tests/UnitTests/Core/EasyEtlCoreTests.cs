using Library;
using Library.Extractors;
using Library.Infra;
using Library.Infra.EventArgs;
using Library.Loaders;
using Library.Transformers;
using Xunit;

namespace Tests.Core
{
    public class EasyEtlCoreTests
    {
        [Fact]
        public void Constructor_WithAllComponents_ShouldInitializeCorrectly()
        {
            // Arrange
            var extractor = new MockExtractor();
            var transformer = new BypassDataTransformer();
            var loader = new MockLoader();

            // Act
            var etl = new EasyEtl(extractor, transformer, loader);

            // Assert
            Assert.NotNull(etl.Extractor);
            Assert.NotNull(etl.Transformer);
            Assert.NotNull(etl.Loader);
            Assert.Same(extractor, etl.Extractor);
            Assert.Same(transformer, etl.Transformer);
            Assert.Same(loader, etl.Loader);
        }

        [Fact]
        public void Constructor_WithoutTransformer_ShouldUseBypassTransformer()
        {
            // Arrange
            var extractor = new MockExtractor();
            var loader = new MockLoader();

            // Act
            var etl = new EasyEtl(extractor, loader);

            // Assert
            Assert.NotNull(etl.Transformer);
            Assert.IsType<BypassDataTransformer>(etl.Transformer);
        }

        [Fact]
        public void Constructor_WithBoundedChannel_ShouldNotThrow()
        {
            // Arrange
            var extractor = new MockExtractor();
            var loader = new MockLoader();

            // Act
            var etl = new EasyEtl(extractor, loader, channelSize: 100);

            // Assert
            Assert.NotNull(etl);
        }

        [Fact]
        public void Constructor_WithNullExtractor_ShouldThrowArgumentNullException()
        {
            // Arrange
            var loader = new MockLoader();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new EasyEtl(null!, loader));
        }

        [Fact]
        public void Constructor_WithNullLoader_ShouldThrowArgumentNullException()
        {
            // Arrange
            var extractor = new MockExtractor();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new EasyEtl(extractor, null!));
        }

        [Fact]
        public async Task Execute_WithSimpleData_ShouldProcessAllRows()
        {
            // Arrange
            var extractor = new MockExtractor(rowCount: 10);
            var loader = new MockLoader();
            var etl = new EasyEtl(extractor, loader);

            // Act
            await etl.Execute();

            // Assert
            Assert.Equal(10, loader.ProcessedRows.Count);
        }

        [Fact]
        public async Task Execute_ShouldTriggerOnCompleteEvent()
        {
            // Arrange
            var extractor = new MockExtractor(rowCount: 5);
            var loader = new MockLoader();
            var etl = new EasyEtl(extractor, loader);
            
            bool completed = false;
            etl.OnComplete += (args) => { completed = true; };

            // Act
            await etl.Execute();

            // Assert
            Assert.True(completed);
        }

        [Fact]
        public async Task Execute_ShouldTriggerOnChangeEvents()
        {
            // Arrange
            var extractor = new MockExtractor(rowCount: 10);
            var loader = new MockLoader();
            var etl = new EasyEtl(extractor, loader);
            
            int changeCount = 0;
            etl.OnChange += (args) => { changeCount++; };

            // Act
            await etl.Execute();

            // Assert
            Assert.True(changeCount > 0, "OnChange should be triggered at least once");
        }

        [Fact]
        public async Task Execute_WithTransformer_ShouldTransformData()
        {
            // Arrange
            var extractor = new MockExtractor(rowCount: 5);
            var transformer = new MockTransformer();
            var loader = new MockLoader();
            var etl = new EasyEtl(extractor, transformer, loader);

            // Act
            await etl.Execute();

            // Assert
            Assert.Equal(5, transformer.TransformedCount);
        }

        [Fact]
        public async Task Execute_WithError_ShouldTriggerOnErrorEvent()
        {
            // Arrange
            var extractor = new MockExtractorWithError();
            var loader = new MockLoader();
            var etl = new EasyEtl(extractor, loader);
            
            ErrorNotificationEventArgs? errorArgs = null;
            etl.OnError += (args) => { errorArgs = args; };

            // Act
            await etl.Execute();
            
            // Assert
            Assert.NotNull(errorArgs);
        }

        // Mock Classes
        private class MockExtractor : IDataExtractor
        {
            private readonly int _rowCount;
            public event ReadNotification? OnRead;
            public event ReadNotification? OnFinish;
            public event EasyEtlErrorEventHandler? OnError;

            public long TotalLines { get; set; }
            public int LineNumber { get; set; }
            public long BytesRead { get; set; }
            public double PercentRead { get; set; }
            public long FileSize { get; set; }

            public MockExtractor(int rowCount = 10)
            {
                _rowCount = rowCount;
                TotalLines = rowCount;
            }

            public void Extract(RowAction processRow)
            {
                for (int i = 0; i < _rowCount; i++)
                {
                    var row = new Dictionary<string, object?>
                    {
                        ["Id"] = i,
                        ["Name"] = $"Row_{i}"
                    };
                    processRow(ref row);
                    LineNumber++;
                }
                OnFinish?.Invoke(new ExtractNotificationEventArgs(TotalLines, LineNumber, FileSize, BytesRead, 100, 0));
            }
        }

        private class MockExtractorWithError : IDataExtractor
        {
            public event ReadNotification? OnRead;
            public event ReadNotification? OnFinish;
            public event EasyEtlErrorEventHandler? OnError;

            public long TotalLines { get; set; } = 10;
            public int LineNumber { get; set; }
            public long BytesRead { get; set; }
            public double PercentRead { get; set; }
            public long FileSize { get; set; }

            public void Extract(RowAction processRow)
            {
                throw new InvalidOperationException("Mock extraction error");
            }
        }

        private class MockLoader : IDataLoader
        {
            public event LoadNotificationHandler? OnWrite;
            public event LoadNotificationHandler? OnFinish;
            public event EasyEtlErrorEventHandler? OnError;

            public long CurrentLine { get; set; }
            public long TotalLines { get; set; }
            public double PercentWritten { get; set; }

            public List<Dictionary<string, object?>> ProcessedRows { get; } = new();

            public async Task Load(IAsyncEnumerable<Dictionary<string, object?>> data, CancellationToken cancellationToken)
            {
                await foreach (var row in data.WithCancellation(cancellationToken))
                {
                    ProcessedRows.Add(row);
                    CurrentLine++;
                }
                OnFinish?.Invoke(new LoadNotificationEventArgs(CurrentLine, TotalLines, 100, 0));
            }
        }

        private class MockTransformer : IDataTransformer
        {
            public event TransformNotificationHandler? OnTransform;
            public event TransformNotificationHandler? OnFinish;
            public event EasyEtlErrorEventHandler? OnError;

            public long IngestedLines { get; set; }
            public long TransformedLines { get; set; }
            public long ExcludedByFilter { get; set; }
            public double PercentDone { get; set; }
            public long TotalLines { get; set; }

            public int TransformedCount { get; private set; }

            public async IAsyncEnumerable<Dictionary<string, object?>> Transform(
                IAsyncEnumerable<Dictionary<string, object?>> data,
                [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await foreach (var item in data.WithCancellation(cancellationToken))
                {
                    TransformedCount++;
                    TransformedLines++;
                    yield return item;
                }
                OnFinish?.Invoke(new TransformNotificationEventArgs(TotalLines, IngestedLines, TransformedLines, ExcludedByFilter, PercentDone, 0));
            }

            public List<Dictionary<string, object?>> ApplyTransformations(Dictionary<string, object?> item)
            {
                return new List<Dictionary<string, object?>> { item };
            }
        }
    }
}
