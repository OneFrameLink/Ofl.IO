using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Ofl.IO
{
    public static class TextReaderExtensions
    {
        public static IEnumerable<string> GetEnumerable(this TextReader textReader)
        {
            // Validate parameters.
            if (textReader == null) throw new ArgumentNullException(nameof(textReader));

            // The implementation.
            IEnumerable<string> Implementation() {
                // The line.
                string line;

                // Cycle through readline.  While there's a value, yield.
                while ((line = textReader.ReadLine()) != null)
                    yield return line;
            }

            // Call the implementation.
            return Implementation();
        }

        public static async IAsyncEnumerable<string> GetAsyncEnumerable(
            this TextReader reader,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            // Validate parameters.
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            // The line.
            string line;

            // While the line is not null.
            while (!cancellationToken.IsCancellationRequested &&
                (line = await reader.ReadLineAsync().ConfigureAwait(false)) != null
            )
                // Yield
                yield return line;
        }
    }
}
