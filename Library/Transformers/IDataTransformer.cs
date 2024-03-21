using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Transformers
{
    public interface IDataTransformer
    {
        IAsyncEnumerable<Dictionary<string, object>> Transform(IAsyncEnumerable<Dictionary<string, object>> row);
    }
}
