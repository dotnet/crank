// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;

namespace Microsoft.Crank.Jobs.PipeliningClient
{
    public static class SequenceReaderExtensions
    {
        public static bool TryReadTo<T>(this ref SequenceReader<T> sequenceReader, out ReadOnlySpan<T> span, ReadOnlySpan<T> delimiter, bool advancePastDelimiter = true) where T : unmanaged, IEquatable<T>
        {
            if (sequenceReader.TryReadTo(out ReadOnlySequence<T> sequence, delimiter, advancePastDelimiter))
            {
                span = sequence.IsSingleSegment ? sequence.FirstSpan : sequence.ToArray();
                return true;
            }
            span = default;
            return false;
        }
    }
}
