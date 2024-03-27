using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace Library.Infra
{
    public sealed class AsyncEnumerableDataReader(IAsyncEnumerable<Dictionary<string, object?>> enumerable) : IDataReader
    {
        private readonly IAsyncEnumerator<Dictionary<string, object?>> _enumerator = enumerable.GetAsyncEnumerator();
        private Dictionary<string, object?> _currentRecord = [];
        private Dictionary<string, int> _columnOrdinalMappings = [];
        private bool _isClosed = false;
        private Type[]? _columnTypes;

        private void InitializeColumnTypes()
        {
            _columnTypes = new Type[_currentRecord!.Count];
            int index = 0;
            foreach (var kvp in _currentRecord)
            {
                // Assume that all values can be nullable.
                var type = kvp.Value?.GetType();
                if (type != null)
                {
                    // If the type is a value type and the value is not null, consider Nullable<T>
                    if (Nullable.GetUnderlyingType(type) == null && type.IsValueType)
                    {
                        _columnTypes[index] = typeof(Nullable<>).MakeGenericType(type);
                    }
                    else
                    {
                        _columnTypes[index] = type;
                    }
                }
                else
                {
                    _columnTypes[index] = typeof(DBNull);
                }
                index++;
            }
        }

        public async Task<bool> ReadAsync()
        {
            bool hasData = await _enumerator.MoveNextAsync();
            if (hasData)
            {
                _currentRecord = _enumerator.Current;

                if (_columnTypes == null) { InitializeColumnTypes(); }

                _columnOrdinalMappings = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                int ordinal = 0;
                foreach (var key in _currentRecord.Keys)
                {
                    _columnOrdinalMappings[key] = ordinal++;
                }
            }
            else
            {
                _currentRecord = [];
                _columnOrdinalMappings = [];
            }
            return hasData;
        }

        // IDataReader Members
        public bool Read()
        {
            // CAUTION: This can cause deadlocks in certain contexts            
            // Try to read synchronously
            try
            {
                // Calls the async method and waits for the result
                return ReadAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {   
                throw new InvalidOperationException("Error reading data synchronously", ex);
            }
        }

        public int FieldCount => _currentRecord?.Count ?? 0;

        public object this[string name] => _currentRecord?[name] ?? throw new KeyNotFoundException($"The key '{name}' was not found.");

        public object this[int i] => _currentRecord?[GetFieldName(i)] ?? DBNull.Value;

        public bool IsDBNull(int i) => this[i] is DBNull || this[i] == null;

        public string GetName(int i)
        {
            if (_currentRecord == null) throw new InvalidOperationException("No data available.");
            foreach (var kvp in _currentRecord)
            {
                if (--i < 0) return kvp.Key;
            }
            throw new KeyNotFoundException();
        }

        private string GetFieldName(int index)
        {
            if (_currentRecord == null) throw new InvalidOperationException("No data available.");
            int currentIndex = 0;
            foreach (var key in _currentRecord.Keys)
            {
                if (currentIndex == index) return key;
                currentIndex++;
            }
            throw new KeyNotFoundException($"Field index {index} is out of range.");
        }        

        public void Close() => Dispose();

        public bool NextResult()
        {
            // Assuming no nested results sets, so always returns false.
            return false;
        }

        public bool GetBoolean(int i) => Convert.ToBoolean(this[i]);

        public byte GetByte(int i) => Convert.ToByte(this[i]);

        public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length)
        {
            // Verifica se o buffer é nulo
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer), "Buffer cannot be null.");
            }

            // Get the value in the specified column and check if it is a byte[]
            var value = this[i];
            if (value is not byte[] data)
            {
                throw new InvalidOperationException($"The value in column index {i} is not a byte array.");
            }

            // Verify if the offsets are valid
            if (fieldOffset < 0 || fieldOffset >= data.Length)
            {
                // Retorna 0 se não há mais dados a serem copiados
                return 0;
            }

            if (bufferoffset < 0 || bufferoffset >= buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferoffset), "Buffer offset is out of range.");
            }

            // Calculating the number of bytes available to copy
            int bytesAvailable = data.Length - (int)fieldOffset;
            int bytesToCopy = Math.Min(length, bytesAvailable);
            bytesToCopy = Math.Min(bytesToCopy, buffer.Length - bufferoffset); // Ensure buffer has enough space

            // Check if there are bytes to copy
            if (bytesToCopy <= 0) { return 0; }

            // Copy bytes to the buffer
            Buffer.BlockCopy(data, (int)fieldOffset, buffer, bufferoffset, bytesToCopy);

            return bytesToCopy;
        }

        public char GetChar(int i) => Convert.ToChar(this[i]);

        public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length)
        {
            // Similar ao GetBytes, adaptado para caracteres.
            if (buffer == null) return 0;

            var data = ((string)this[i]).ToCharArray();
            int charsToCopy = Math.Min(length, data.Length - (int)fieldoffset);
            Array.Copy(data, (int)fieldoffset, buffer, bufferoffset, charsToCopy);
            return charsToCopy;
        }

        public IDataReader GetData(int i)
        {
            // Normalmente usado para obter um DataReader de uma coluna específica que é ela mesma um conjunto de dados.
            // Não aplicável na maioria dos cenários de Dictionary.
            throw new NotSupportedException("Getting IDataReader from a field is not supported.");
        }

        public string GetDataTypeName(int i)
        {
            return this[i]?.GetType().Name ?? "DBNull";
        }

        public DateTime GetDateTime(int i) => Convert.ToDateTime(this[i]);

        public decimal GetDecimal(int i) => Convert.ToDecimal(this[i]);

        public double GetDouble(int i) => Convert.ToDouble(this[i]);

        public Type GetFieldType(int i)
        {
            if (_columnTypes == null || i < 0 || i >= _columnTypes.Length)
            {
                throw new KeyNotFoundException($"Column index {i} is out of range.");
            }
            return _columnTypes[i];
        }

        public float GetFloat(int i) => Convert.ToSingle(this[i]);

        public Guid GetGuid(int i) => Guid.Parse(this[i].ToString() ?? string.Empty);

        public short GetInt16(int i) => Convert.ToInt16(this[i]);

        public int GetInt32(int i) => Convert.ToInt32(this[i]);

        public long GetInt64(int i) => Convert.ToInt64(this[i]);

        public int GetOrdinal(string name)
        {
            if (_columnOrdinalMappings != null && _columnOrdinalMappings.TryGetValue(name, out int ordinal))
            {
                return ordinal;
            }
            throw new KeyNotFoundException($"Column {name} not found.");
        }

        public string GetString(int i) => Convert.ToString(this[i]) ?? string.Empty;

        public object GetValue(int i) => this[i];

        public int GetValues(object[] values)
        {
            int i = 0;
            foreach (var val in _currentRecord.Values)
            {
                if (i < values.Length)
                {
                    values[i] = val ?? DBNull.Value;
                    i++;
                }
            }
            return i; // Number of array elements populated.
        }

        // Assuming no nested results sets, so Depth is always 0.
        public int Depth => 0;

        // RecordsAffected is relevant for update/delete/insert operations, not applicable here.
        public int RecordsAffected => -1;

        public bool IsClosed => _isClosed;

        public DataTable GetSchemaTable()
        {
            throw new NotImplementedException("GetSchemaTable is not implemented.");
        }

        #region IDisposable Support
        public async ValueTask DisposeAsync()
        {
            if (!_isClosed)
            {
                await _enumerator.DisposeAsync();
                _isClosed = true;
            }
        }

        public void Dispose()
        {
            // Synchronously dispose the enumerator
            DisposeAsync().AsTask().GetAwaiter().GetResult();
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
