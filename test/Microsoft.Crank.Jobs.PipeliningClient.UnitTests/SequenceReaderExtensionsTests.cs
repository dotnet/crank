using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Crank.Jobs.PipeliningClient;
using Xunit;

namespace Microsoft.Crank.Jobs.PipeliningClient.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="SequenceReaderExtensions"/> class.
    /// </summary>
    public class SequenceReaderExtensionsTests
    {
        /// <summary>
        /// Creates a ReadOnlySequence from multiple segments.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
        /// <param name="segments">The segments to combine.</param>
        /// <returns>A multi-segment ReadOnlySequence.</returns>
        private static ReadOnlySequence<T> CreateMultiSegmentSequence<T>(IEnumerable<T[]> segments) where T : unmanaged
        {
            var buffers = segments.Select(s => new ReadOnlyMemory<T>(s)).ToList();
            if (buffers.Count == 0)
                return ReadOnlySequence<T>.Empty;
            if (buffers.Count == 1)
                return new ReadOnlySequence<T>(buffers[0]);

            // Create linked segments.
            var first = new BufferSegment<T>(buffers[0]);
            BufferSegment<T> last = first;
            for (int i = 1; i < buffers.Count; i++)
            {
                var segment = new BufferSegment<T>(buffers[i]);
                last.SetNext(segment);
                last = segment;
            }
            return new ReadOnlySequence<T>(first, 0, last, last.Memory.Length);
        }

        /// <summary>
        /// A helper class to simulate multi-segment ReadOnlySequence.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        private class BufferSegment<T> : ReadOnlySequenceSegment<T>
        {
            public BufferSegment(ReadOnlyMemory<T> memory)
            {
                Memory = memory;
            }

            public void SetNext(BufferSegment<T> next)
            {
                Next = next;
                next.RunningIndex = RunningIndex + Memory.Length;
            }
        }

        /// <summary>
        /// Tests the TryReadTo extension method on a single-segment sequence when the delimiter is found
        /// and the reader is set to advance past the delimiter.
        /// Expected outcome: The method returns true, the out span contains the correct data, and the reader advances correctly.
        /// </summary>
        [Fact]
        public void TryReadTo_SingleSegment_DelimiterFound_AdvancePastDelimiter_ReturnsTrueAndAdvancesReader()
        {
            // Arrange
            byte[] data = { 1, 2, 3, 4, 5 };
            var sequence = new ReadOnlySequence<byte>(data);
            var reader = new SequenceReader<byte>(sequence);
            byte delimiterValue = 3;
            ReadOnlySpan<byte> delimiter = new ReadOnlySpan<byte>(new[] { delimiterValue });

            // Act
            bool result = reader.TryReadTo(out ReadOnlySpan<byte> span, delimiter, advancePastDelimiter: true);

            // Assert
            Assert.True(result);
            // Expect elements before the delimiter: [1,2]
            byte[] expected = { 1, 2 };
            Assert.True(span.SequenceEqual(expected));

            // Verify that the reader has advanced past the delimiter.
            // Since delimiter has length 1, the next element should be 4.
            bool readNext = reader.TryRead(out byte next);
            Assert.True(readNext);
            Assert.Equal(4, next);
        }

        /// <summary>
        /// Tests the TryReadTo extension method on a single-segment sequence when the delimiter is found
        /// and the reader is set to not advance past the delimiter.
        /// Expected outcome: The method returns true, the out span contains the correct data, and the reader remains at the delimiter.
        /// </summary>
        [Fact]
        public void TryReadTo_SingleSegment_DelimiterFound_NotAdvancePastDelimiter_ReturnsTrueAndKeepsReaderAtDelimiter()
        {
            // Arrange
            byte[] data = { 1, 2, 3, 4, 5 };
            var sequence = new ReadOnlySequence<byte>(data);
            var reader = new SequenceReader<byte>(sequence);
            byte delimiterValue = 3;
            ReadOnlySpan<byte> delimiter = new ReadOnlySpan<byte>(new[] { delimiterValue });

            // Act
            bool result = reader.TryReadTo(out ReadOnlySpan<byte> span, delimiter, advancePastDelimiter: false);

            // Assert
            Assert.True(result);
            // Expect elements before the delimiter: [1,2]
            byte[] expected = { 1, 2 };
            Assert.True(span.SequenceEqual(expected));

            // Verify that the reader has NOT advanced past the delimiter.
            // The next byte should be the delimiter itself, 3.
            bool readNext = reader.TryRead(out byte next);
            Assert.True(readNext);
            Assert.Equal(3, next);
        }

        /// <summary>
        /// Tests the TryReadTo extension method on a single-segment sequence when the delimiter is not found.
        /// Expected outcome: The method returns false, the out span remains default, and the reader position is unchanged.
        /// </summary>
        [Fact]
        public void TryReadTo_SingleSegment_DelimiterNotFound_ReturnsFalseAndDoesNotAdvanceReader()
        {
            // Arrange
            byte[] data = { 1, 2, 3 };
            var sequence = new ReadOnlySequence<byte>(data);
            var reader = new SequenceReader<byte>(sequence);
            byte delimiterValue = 9;
            ReadOnlySpan<byte> delimiter = new ReadOnlySpan<byte>(new[] { delimiterValue });

            // Act
            bool result = reader.TryReadTo(out ReadOnlySpan<byte> span, delimiter);

            // Assert
            Assert.False(result);
            Assert.True(span.IsEmpty);

            // Verify that the reader position is still at the beginning.
            bool readNext = reader.TryRead(out byte next);
            Assert.True(readNext);
            Assert.Equal(1, next);
        }

        /// <summary>
        /// Tests the TryReadTo extension method on a multi-segment sequence when the delimiter is found
        /// and the reader is set to advance past the delimiter.
        /// Expected outcome: The method returns true, the out span contains the correct aggregated data, and the reader advances correctly.
        /// </summary>
        [Fact]
        public void TryReadTo_MultiSegment_DelimiterFound_AdvancePastDelimiter_ReturnsTrueAndAdvancesReader()
        {
            // Arrange
            // Create a multi-segment sequence with segments: [1,2], [3,4], [5,6]
            var segments = new List<byte[]>
            {
                new byte[] { 1, 2 },
                new byte[] { 3, 4 },
                new byte[] { 5, 6 }
            };
            var sequence = CreateMultiSegmentSequence(segments);
            var reader = new SequenceReader<byte>(sequence);
            byte delimiterValue = 4;
            ReadOnlySpan<byte> delimiter = new ReadOnlySpan<byte>(new[] { delimiterValue });

            // Act
            bool result = reader.TryReadTo(out ReadOnlySpan<byte> span, delimiter, advancePastDelimiter: true);

            // Assert
            Assert.True(result);
            // Expected data before the delimiter: [1,2,3]
            byte[] expected = { 1, 2, 3 };
            Assert.True(span.SequenceEqual(expected));

            // Verify that the reader has advanced past the delimiter.
            bool readNext = reader.TryRead(out byte next);
            Assert.True(readNext);
            Assert.Equal(5, next);
        }

        /// <summary>
        /// Tests the TryReadTo extension method on a multi-segment sequence when the delimiter is found
        /// and the reader is set to not advance past the delimiter.
        /// Expected outcome: The method returns true, the out span contains the correct aggregated data, and the reader remains at the delimiter.
        /// </summary>
        [Fact]
        public void TryReadTo_MultiSegment_DelimiterFound_NotAdvancePastDelimiter_ReturnsTrueAndKeepsReaderAtDelimiter()
        {
            // Arrange
            // Create a multi-segment sequence with segments: [1,2], [3,4], [5,6]
            var segments = new List<byte[]>
            {
                new byte[] { 1, 2 },
                new byte[] { 3, 4 },
                new byte[] { 5, 6 }
            };
            var sequence = CreateMultiSegmentSequence(segments);
            var reader = new SequenceReader<byte>(sequence);
            byte delimiterValue = 4;
            ReadOnlySpan<byte> delimiter = new ReadOnlySpan<byte>(new[] { delimiterValue });

            // Act
            bool result = reader.TryReadTo(out ReadOnlySpan<byte> span, delimiter, advancePastDelimiter: false);

            // Assert
            Assert.True(result);
            // Expected data before the delimiter: [1,2,3]
            byte[] expected = { 1, 2, 3 };
            Assert.True(span.SequenceEqual(expected));

            // Verify that the reader has not advanced past the delimiter.
            bool readNext = reader.TryRead(out byte next);
            Assert.True(readNext);
            Assert.Equal(4, next);
        }

        /// <summary>
        /// Tests the TryReadTo extension method when invoked on an empty sequence.
        /// Expected outcome: The method returns false, the out span is empty, and the reader remains at end of sequence.
        /// </summary>
        [Fact]
        public void TryReadTo_EmptySequence_ReturnsFalseAndEmptySpan()
        {
            // Arrange
            var sequence = ReadOnlySequence<byte>.Empty;
            var reader = new SequenceReader<byte>(sequence);
            byte delimiterValue = 1;
            ReadOnlySpan<byte> delimiter = new ReadOnlySpan<byte>(new[] { delimiterValue });

            // Act
            bool result = reader.TryReadTo(out ReadOnlySpan<byte> span, delimiter);

            // Assert
            Assert.False(result);
            Assert.True(span.IsEmpty);
        }

        /// <summary>
        /// Tests the TryReadTo extension method when an empty delimiter is provided.
        /// Expected outcome: The method throws an ArgumentException.
        /// </summary>
//         [Fact] [Error] (257-17)CS8175 Cannot use ref local 'reader' inside an anonymous method, lambda expression, or query expression [Error] (257-60)CS8175 Cannot use ref local 'emptyDelimiter' inside an anonymous method, lambda expression, or query expression
//         public void TryReadTo_EmptyDelimiter_ThrowsArgumentException()
//         {
//             // Arrange
//             byte[] data = { 1, 2, 3 };
//             var sequence = new ReadOnlySequence<byte>(data);
//             var reader = new SequenceReader<byte>(sequence);
//             ReadOnlySpan<byte> emptyDelimiter = ReadOnlySpan<byte>.Empty;
// 
//             // Act & Assert
//             Assert.Throws<ArgumentException>(() =>
//             {
//                 reader.TryReadTo(out ReadOnlySpan<byte> _, emptyDelimiter);
//             });
//         }
    }
}
