using System.Diagnostics;

namespace Library.Infra
{
    public static class FileStreamHelper
    {
        public static long CountLines(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            long lineCount = 0;
            byte[] buffer = new byte[1024 * 1024];
            int bytesRead;

            do
            {
                bytesRead = fs.Read(buffer, 0, buffer.Length);
                for (int i = 0; i < bytesRead; i++)
                    if (buffer[i] == '\n')
                        lineCount++;
            }
            while (bytesRead > 0);
            return lineCount;
        }

        public static long CountLinesParallel(string path)
        {
            const int blockSize = 1024 * 1024 * 10; // 10MB per block
            long totalLineCount = 0;

            FileInfo fileInfo = new(path);
            long fileLength = fileInfo.Length;
            int numberOfBlocks = (int)Math.Ceiling((double)fileLength / blockSize);

            Parallel.For(0, numberOfBlocks, (blockNumber) =>
            {
                long blockStart = blockNumber * blockSize;
                long blockEnd = Math.Min(blockStart + blockSize, fileLength);

                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                fs.Seek(blockStart, SeekOrigin.Begin);

                // If it is not the first block, advance to the next line to avoid splitting a line in half                    
                if (blockNumber != 0)
                {
                    ReadTillNextLine(fs);
                }

                long lineCount = 0;
                byte[] buffer = new byte[1024 * 1024]; // 1MB buffer
                int bytesRead;
                bool lastCharWasNewLine = false;
                while (fs.Position < blockEnd && (bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < bytesRead; i++)
                    {
                        if (buffer[i] == '\n')
                        {
                            // If the last character read was a newline, don't count the next empty line.                            
                            if (!lastCharWasNewLine)
                            {
                                lineCount++;
                            }

                            lastCharWasNewLine = true;
                        }
                        else
                        {
                            lastCharWasNewLine = false;
                        }
                    }
                }

                // If the last character read was a newline, don't count the next empty line.                
                if (blockNumber == numberOfBlocks - 1 && lastCharWasNewLine)
                {
                    lineCount--;
                }

                // Sync access to the total line counter                
                lock (fileInfo)
                {
                    totalLineCount += lineCount;
                }
            });

            return totalLineCount;
        }

        private static void ReadTillNextLine(FileStream fs)
        {
            // Lê byte por byte até encontrar uma nova linha, evitando dividir uma linha entre blocos
            byte[] singleByte = new byte[1];
            while (fs.Read(singleByte, 0, 1) > 0)
            {
                if (singleByte[0] == '\n') break;
            }
        }
    }
}
