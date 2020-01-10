using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Ofl.IO
{
    public static class StreamExtensions
    {
        public static readonly int DefaultToAsyncEnumerableBufferSize = 4096;

        public static IAsyncEnumerable<byte> ToAsyncEnumerable(this Stream stream) =>
            stream.ToAsyncEnumerable(DefaultToAsyncEnumerableBufferSize, ArrayPool<byte>.Shared);

        public static IAsyncEnumerable<byte> ToAsyncEnumerable(
            this Stream stream, 
            int bufferSize,
            ArrayPool<byte> arrayPool,
            CancellationToken cancellationToken
        )
        {
            // Validate parameters.
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), 
                    $"The { nameof(bufferSize) } parameter must be a positive value.");
            if (arrayPool == null) throw new ArgumentNullException(nameof(arrayPool));

            // The implementation.
            async IAsyncEnumerable<byte> Implementation() {
                // The buffer.
                byte[] buffer = null;

                // Wrap in a try/finalize.
                try
                {
                    // Allocate.
                    buffer = arrayPool.Rent(bufferSize);

                    // The bytes read.
                    int read;

                    // Keep reading from the stream in the allocated amounts.
                    while ((read = await stream.ReadAsync(buffer, 0, bufferSize, cancellationToken)
                        .ConfigureAwait(false)) != 0)
                    {
                        // Cycle through the bytes and yield.
                        for (int index = 0; index < read; ++index)
                            yield return buffer[index];
                    }
                }
                finally
                {
                    // Return.
                    if (buffer != null)
                        arrayPool.Return(buffer);
                }
            }

            // Return the implementation.
            return Implementation();
        }
    }
}
