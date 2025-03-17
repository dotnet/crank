using Moq;
using System;
using System.Buffers;
using System.IO.Pipelines;
using Microsoft.Crank.Jobs.PipeliningClient;

namespace Microsoft.Crank.Jobs.PipeliningClient.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="SequenceReaderExtensions"/> class.
    /// </summary>
    [TestClass]
    public class SequenceReaderExtensionsTests
    {
        /// <summary>
        /// Tests the <see cref="SequenceReaderExtensions.TryReadTo{T}(ref SequenceReader{T}, out ReadOnlySpan{T}, ReadOnlySpan{T}, bool)"/> method
        /// to ensure it correctly reads to the delimiter and returns the expected span.
        /// </summary>
        [TestMethod]
        public void TryReadTo_WithValidDelimiter_ReturnsTrueAndExpectedSpan()
        {
            // Arrange
            var data = new byte[] { 1, 2, 3, 4, 5, 6 };
            var delimiter = new byte[] { 3, 4 };
            var sequence = new ReadOnlySequence<byte>(data);
            var sequenceReader = new SequenceReader<byte>(sequence);

            // Act
            var result = SequenceReaderExtensions.TryReadTo(ref sequenceReader, out ReadOnlySpan<byte> span, delimiter);

            // Assert
            Assert.IsTrue(result);
            CollectionAssert.AreEqual(new byte[] { 1, 2 }, span.ToArray());
        }

        /// <summary>
        /// Tests the <see cref="SequenceReaderExtensions.TryReadTo{T}(ref SequenceReader{T}, out ReadOnlySpan{T}, ReadOnlySpan{T}, bool)"/> method
        /// to ensure it returns false when the delimiter is not found.
        /// </summary>
        [TestMethod]
        public void TryReadTo_WithInvalidDelimiter_ReturnsFalse()
        {
            // Arrange
            var data = new byte[] { 1, 2, 3, 4, 5, 6 };
            var delimiter = new byte[] { 7, 8 };
            var sequence = new ReadOnlySequence<byte>(data);
            var sequenceReader = new SequenceReader<byte>(sequence);

            // Act
            var result = SequenceReaderExtensions.TryReadTo(ref sequenceReader, out ReadOnlySpan<byte> span, delimiter);

            // Assert
            Assert.IsFalse(result);
            Assert.IsTrue(span.IsEmpty);
        }

        /// <summary>
        /// Tests the <see cref="SequenceReaderExtensions.TryReadTo{T}(ref SequenceReader{T}, out ReadOnlySpan{T}, ReadOnlySpan{T}, bool)"/> method
        /// to ensure it correctly handles sequences that span multiple segments.
        /// </summary>
        [TestMethod]
        public void TryReadTo_WithMultiSegmentSequence_ReturnsTrueAndExpectedSpan()
        {
            // Arrange
            var segment1 = new byte[] { 1, 2 };
            var segment2 = new byte[] { 3, 4, 5, 6 };
            var sequence = CreateReadOnlySequence(segment1, segment2);
            var sequenceReader = new SequenceReader<byte>(sequence);
            var delimiter = new byte[] { 3, 4 };

            // Act
            var result = SequenceReaderExtensions.TryReadTo(ref sequenceReader, out ReadOnlySpan<byte> span, delimiter);

            // Assert
            Assert.IsTrue(result);
            CollectionAssert.AreEqual(new byte[] { 1, 2 }, span.ToArray());
        }

        /// <summary>
        /// Tests the <see cref="SequenceReaderExtensions.TryReadTo{T}(ref SequenceReader{T}, out ReadOnlySpan{T}, ReadOnlySpan{T}, bool)"/> method
        /// to ensure it correctly handles the advancePastDelimiter parameter.
        /// </summary>
        [TestMethod]
        public void TryReadTo_WithAdvancePastDelimiterFalse_ReturnsTrueAndExpectedSpan()
        {
            // Arrange
            var data = new byte[] { 1, 2, 3, 4, 5, 6 };
            var delimiter = new byte[] { 3, 4 };
            var sequence = new ReadOnlySequence<byte>(data);
            var sequenceReader = new SequenceReader<byte>(sequence);

            // Act
            var result = SequenceReaderExtensions.TryReadTo(ref sequenceReader, out ReadOnlySpan<byte> span, delimiter, false);

            // Assert
            Assert.IsTrue(result);
            CollectionAssert.AreEqual(new byte[] { 1, 2 }, span.ToArray());
            Assert.AreEqual(2, sequenceReader.Consumed);
        }

        private static ReadOnlySequence<byte> CreateReadOnlySequence(params byte[][] segments)
        {
            if (segments.Length == 1)
            {
                return new ReadOnlySequence<byte>(segments[0]);
            }

            var first = new BufferSegment(segments[0]);
            var last = first;
            for (int i = 1; i < segments.Length; i++)
            {
                last = last.Append(segments[i]);
            }

            return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
        }

        private class BufferSegment : ReadOnlySequenceSegment<byte>
        {
            public BufferSegment(ReadOnlyMemory<byte> memory)
            {
                Memory = memory;
            }

            public BufferSegment Append(ReadOnlyMemory<byte> memory)
            {
                var segment = new BufferSegment(memory)
                {
                    RunningIndex = RunningIndex + Memory.Length
                };

                Next = segment;
                return segment;
            }
        }
    }
}
