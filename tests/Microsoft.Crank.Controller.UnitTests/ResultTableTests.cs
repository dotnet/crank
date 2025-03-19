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
            Assert.NotNull(row);
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
            row.Add(new Cell(new CellElement("Cell1")));
            row.Add(new Cell(new CellElement("Cell2")));
            row.Add(new Cell(new CellElement("Cell3")));

            // Act
            var widths = _resultTable.CalculateColumnWidths();

            // Assert
            Assert.Equal(3, widths.Length);
            Assert.Equal(6, widths[0]); // "Header1" length
            Assert.Equal(6, widths[1]); // "Header2" length
            Assert.Equal(6, widths[2]); // "Header3" length
        }

        /// <summary>
        /// Tests the <see cref="ResultTable.RemoveEmptyRows"/> method to ensure it correctly removes empty rows.
        /// </summary>
        [Fact]
        public void RemoveEmptyRows_WhenCalled_RemovesEmptyRows()
        {
            // Arrange
            var row1 = _resultTable.AddRow();
            row1.Add(new Cell(new CellElement("Cell1")));
            var row2 = _resultTable.AddRow();
            row2.Add(new Cell(new CellElement("")));

            // Act
            _resultTable.RemoveEmptyRows();

            // Assert
            Assert.Single(_resultTable.Rows);
            Assert.Contains(row1, _resultTable.Rows);
            Assert.DoesNotContain(row2, _resultTable.Rows);
        }

        /// <summary>
        /// Tests the <see cref="ResultTable.Render(TextWriter)"/> method to ensure it correctly renders the table.
        /// </summary>
        [Fact]
        public void Render_WhenCalled_RendersTable()
        {
            // Arrange
            _resultTable.Headers.AddRange(new[] { "Header1", "Header2", "Header3" });
            var row = _resultTable.AddRow();
            row.Add(new Cell(new CellElement("Cell1")));
            row.Add(new Cell(new CellElement("Cell2")));
            row.Add(new Cell(new CellElement("Cell3")));
            var writer = new StringWriter();

            // Act
            _resultTable.Render(writer);

            // Assert
            var output = writer.ToString();
            Assert.Contains("| Header1 | Header2 | Header3 |", output);
            Assert.Contains("| Cell1   | Cell2   | Cell3   |", output);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="Cell"/> class.
    /// </summary>
    public class CellTests
    {
        /// <summary>
        /// Tests the <see cref="Cell"/> constructor to ensure it correctly initializes elements.
        /// </summary>
        [Fact]
        public void Cell_Constructor_InitializesElements()
        {
            // Arrange
            var elements = new[] { new CellElement("Element1"), new CellElement("Element2") };

            // Act
            var cell = new Cell(elements);

            // Assert
            Assert.Equal(elements.Length, cell.Elements.Count);
            Assert.Contains(elements[0], cell.Elements);
            Assert.Contains(elements[1], cell.Elements);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="CellElement"/> class.
    /// </summary>
    public class CellElementTests
    {
        /// <summary>
        /// Tests the <see cref="CellElement"/> constructor to ensure it correctly initializes text and alignment.
        /// </summary>
        [Fact]
        public void CellElement_Constructor_InitializesProperties()
        {
            // Arrange
            var text = "Element";
            var alignment = CellTextAlignment.Right;

            // Act
            var element = new CellElement(text, alignment);

            // Assert
            Assert.Equal(text, element.Text);
            Assert.Equal(alignment, element.Alignment);
        }
    }
}
