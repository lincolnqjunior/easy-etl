namespace Library.Readers
{
    public interface IFileReader
    {
        IAsyncEnumerable<Dictionary<string, object>> Read(string filePath);
    }
}