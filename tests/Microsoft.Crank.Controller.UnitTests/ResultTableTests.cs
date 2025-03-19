using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="ResultTable"/> class.
    /// </summary>
    public class ResultTableTests
    {
        private readonly ResultTable _resultTable;

        public ResultTableTests()
        {
            _resultTable = new ResultTable(3);
        }

        /// <summary>
        /// Tests the <see cref="ResultTable.AddRow"/> method to ensure it correctly adds a new row.
        /// </summary>
        [Fact]
        public void AddRow_WhenCalled_AddsNewRow()
        {
            // Act
            var row = _resultTable.AddRow();

            // Assert
            Assert.Contains(row, _resultTable.Rows);
        }

        /// <summary>
        /// Tests the <see cref="ResultTable.CalculateColumnWidths"/> method to ensure it correctly calculates column widths.
        /// </summary>
        [Fact]
        public void CalculateColumnWidths_WhenCalled_ReturnsCorrectWidths()
        {
            // Arrange
            _resultTable.Headers.AddRange(new[] { "Header1", "Header2", "Header3" });
            var row = _resultTable.AddRow();
            row.Add(new Cell(new CellElement("Data1")));
            row.Add(new Cell(new CellElement("Data2")));
            row.Add(new Cell(new CellElement("Data3")));

            // Act
            var widths = _resultTable.CalculateColumnWidths();

            // Assert
            Assert.Equal(3, widths.Length);
            Assert.Equal(6, widths[0]);
            Assert.Equal(6, widths[1]);
            Assert.Equal(6, widths[2]);
        }

        /// <summary>
        /// Tests the <see cref="ResultTable.RemoveEmptyRows"/> method to ensure it correctly removes empty rows.
        /// </summary>
        [Fact]
        public void RemoveEmptyRows_WhenCalled_RemovesEmptyRows()
        {
            // Arrange
            var row1 = _resultTable.AddRow();
            row1.Add(new Cell(new CellElement("Data1")));
            var row2 = _resultTable.AddRow();
            row2.Add(new Cell(new CellElement("")));

            // Act
            _resultTable.RemoveEmptyRows();

            // Assert
            Assert.Single(_resultTable.Rows);
            Assert.Contains(row1, _resultTable.Rows);
        }

        /// <summary>
        /// Tests the <see cref="ResultTable.Render(TextWriter)"/> method to ensure it correctly renders the table.
        /// </summary>
        [Fact]
        public void Render_WhenCalled_WritesCorrectOutput()
        {
            // Arrange
            _resultTable.Headers.AddRange(new[] { "Header1", "Header2", "Header3" });
            var row = _resultTable.AddRow();
            row.Add(new Cell(new CellElement("Data1")));
            row.Add(new Cell(new CellElement("Data2")));
            row.Add(new Cell(new CellElement("Data3")));
            var writer = new StringWriter();

            // Act
            _resultTable.Render(writer);
            var output = writer.ToString();

            // Assert
            var expectedOutput = "| Header1 | Header2 | Header3 |\n| ------- | ------- | ------- |\n| Data1   | Data2   | Data3   |\n";
            Assert.Equal(expectedOutput, output);
        }

        /// <summary>
        /// Tests the <see cref="ResultTable.Render(TextWriter, int[])"/> method to ensure it correctly renders the table with specified column widths.
        /// </summary>
        [Fact]
        public void Render_WithColumnWidths_WritesCorrectOutput()
        {
            // Arrange
            _resultTable.Headers.AddRange(new[] { "Header1", "Header2", "Header3" });
            var row = _resultTable.AddRow();
            row.Add(new Cell(new CellElement("Data1")));
            row.Add(new Cell(new CellElement("Data2")));
            row.Add(new Cell(new CellElement("Data3")));
            var writer = new StringWriter();
            var columnWidths = new[] { 10, 10, 10 };

            // Act
            _resultTable.Render(writer, columnWidths);
            var output = writer.ToString();

            // Assert
            var expectedOutput = "| Header1    | Header2    | Header3    |\n| ---------- | ---------- | ---------- |\n| Data1      | Data2      | Data3      |\n";
            Assert.Equal(expectedOutput, output);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="Cell"/> class.
    /// </summary>
    public class CellTests
    {
        /// <summary>
        /// Tests the <see cref="Cell.Cell(CellElement[])"/> constructor to ensure it correctly initializes the elements.
        /// </summary>
        [Fact]
        public void Cell_WithElements_InitializesElements()
        {
            // Arrange
            var elements = new[] { new CellElement("Text1"), new CellElement("Text2") };

            // Act
            var cell = new Cell(elements);

            // Assert
            Assert.Equal(elements, cell.Elements);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="CellElement"/> class.
    /// </summary>
    public class CellElementTests
    {
        /// <summary>
        /// Tests the <see cref="CellElement.CellElement(string)"/> constructor to ensure it correctly initializes the text.
        /// </summary>
        [Fact]
        public void CellElement_WithText_InitializesText()
        {
            // Arrange
            var text = "SampleText";

            // Act
            var cellElement = new CellElement(text);

            // Assert
            Assert.Equal(text, cellElement.Text);
        }

        /// <summary>
        /// Tests the <see cref="CellElement.CellElement(string, CellTextAlignment)"/> constructor to ensure it correctly initializes the text and alignment.
        /// </summary>
        [Fact]
        public void CellElement_WithTextAndAlignment_InitializesTextAndAlignment()
        {
            // Arrange
            var text = "SampleText";
            var alignment = CellTextAlignment.Right;

            // Act
            var cellElement = new CellElement(text, alignment);

            // Assert
            Assert.Equal(text, cellElement.Text);
            Assert.Equal(alignment, cellElement.Alignment);
        }
    }
}
