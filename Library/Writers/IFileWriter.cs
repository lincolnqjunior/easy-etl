using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Writers
{
    public interface IFileWriter
    {
        Task WriteAsync(string filePath, IAsyncEnumerable<Dictionary<string, object>> data);
    }
}
