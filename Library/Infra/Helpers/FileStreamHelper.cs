using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("Tests")]
namespace Library.Infra.Helpers
{
    public static class FileInfoHelper
    {
        private const char CR = '\r';
        private const char LF = '\n';
        private const char NULL = (char)0;

        public static async Task<long> CountLines(this FileInfo fileInfo, Encoding? encoding = default)
        {
            using var stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, true);

            var lineCount = 0L;
            var byteBuffer = new byte[1024 * 1024];
            var detectedEOL = NULL;
            var currentChar = NULL;
            int bytesRead;

            if (encoding is null || Equals(encoding, Encoding.ASCII) || Equals(encoding, Encoding.UTF8))
            {
                while ((bytesRead = await stream.ReadAsync(byteBuffer)) > 0)
                {
                    for (var i = 0; i < bytesRead; i++)
                    {
                        currentChar = (char)byteBuffer[i];

                        if (detectedEOL != NULL)
                        {
                            if (currentChar == detectedEOL)
                            {
                                lineCount++;
                            }
                        }
                        else if (currentChar == LF || currentChar == CR)
                        {
                            detectedEOL = currentChar;
                            lineCount++;
                        }
                    }
                }
            }
            else
            {
                var charBuffer = new char[byteBuffer.Length];

                while ((bytesRead = await stream.ReadAsync(byteBuffer)) > 0)
                {
                    var charCount = encoding.GetChars(byteBuffer, 0, bytesRead, charBuffer, 0);

                    for (var i = 0; i < charCount; i++)
                    {
                        currentChar = charBuffer[i];

                        if (detectedEOL != NULL)
                        {
                            if (currentChar == detectedEOL)
                            {
                                lineCount++;
                            }
                        }
                        else if (currentChar == LF || currentChar == CR)
                        {
                            detectedEOL = currentChar;
                            lineCount++;
                        }
                    }
                }
            }

            if ((currentChar != LF && currentChar != CR && currentChar != NULL) && lineCount != 0)
            {
                lineCount++;
            }

            return lineCount;
        }
    }
}
