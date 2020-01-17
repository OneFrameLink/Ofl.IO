using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Ofl.IO
{
    // NOTE: When https://github.com/dotnet/roslyn/issues/40893 is resolved, then we can go back to using
    // the internal iterator pattern to pre-check properly.
    public static class StreamExtensions
    {
        public static readonly int DefaultToAsyncEnumerableBufferSize = 4096;

        public static async IAsyncEnumerable<byte> ToAsyncEnumerable(
            this Stream stream,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            // Validate parameters.
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            // Iterate through everything.
            await foreach (byte b in stream
                .ToAsyncEnumerable(DefaultToAsyncEnumerableBufferSize, cancellationToken))
                yield return b;
        }

        public static async IAsyncEnumerable<byte> ToAsyncEnumerable(
            this Stream stream,
            int bufferSize,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            // Validate parameters.
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            // Iterate through everything.
            await foreach (byte b in stream.ToAsyncEnumerable(ArrayPool<byte>.Shared, bufferSize, cancellationToken))
                yield return b;
        }

        public static async IAsyncEnumerable<byte> ToAsyncEnumerable(
            this Stream stream,
            ArrayPool<byte> bufferPool,
            int bufferSize,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            // Validate parameters.
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize),
                    $"The {nameof(bufferSize)} parameter must be a positive value.");

            // The buffer.
            byte[] buffer = null;

            // Wrap in a try/finally.
            try
            {
                // Rent the array
                buffer = bufferPool.Rent(bufferSize);

                // Iterate through everything.
                await foreach (byte b in stream.ToAsyncEnumerable(buffer, cancellationToken))
                    yield return b;
            }
            finally
            {
                // Free the pool.
                bufferPool.Return(buffer);
            }

        }

        public static async IAsyncEnumerable<byte> ToAsyncEnumerable(
            this Stream stream,
            ArraySegment<byte> buffer,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            // Validate parameters.
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            // Iterate through everything.
            await foreach (byte b in stream.ToAsyncEnumerable(buffer.AsMemory(), cancellationToken))
                yield return b;
        }

        public static async IAsyncEnumerable<byte> ToAsyncEnumerable(
            this Stream stream, 
            Memory<byte> buffer,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            // Validate parameters.
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (buffer.Length == 0) throw new ArgumentException(
                $"The {nameof(buffer)} parameter must have a positive length.", nameof(buffer));

            // The bytes read.
            int read;

            // Keep reading from the stream in the allocated amounts.
            while ((read = await stream.ReadAsync(buffer, cancellationToken)
                .ConfigureAwait(false)) != 0)
            {
                // Cycle through the bytes and yield.
                for (int index = 0; index < read; ++index)
                    yield return buffer.Span[index];
            }
        }
    }
}
