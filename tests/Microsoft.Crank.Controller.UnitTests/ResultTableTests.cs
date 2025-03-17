using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="ResultTable"/> class.
    /// </summary>
    [TestClass]
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
        [TestMethod]
        public void AddRow_WhenCalled_AddsNewRow()
        {
            // Act
            var row = _resultTable.AddRow();

            // Assert
            Assert.IsNotNull(row);
            Assert.AreEqual(1, _resultTable.Rows.Count);
        }

        /// <summary>
        /// Tests the <see cref="ResultTable.CalculateColumnWidths"/> method to ensure it correctly calculates column widths.
        /// </summary>
        [TestMethod]
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
            Assert.AreEqual(3, widths.Length);
            Assert.AreEqual(7, widths[0]);
            Assert.AreEqual(7, widths[1]);
            Assert.AreEqual(7, widths[2]);
        }

        /// <summary>
        /// Tests the <see cref="ResultTable.RemoveEmptyRows"/> method to ensure it correctly removes empty rows.
        /// </summary>
        [TestMethod]
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
            Assert.AreEqual(1, _resultTable.Rows.Count);
        }

        /// <summary>
        /// Tests the <see cref="ResultTable.Render(TextWriter)"/> method to ensure it correctly renders the table.
        /// </summary>
        [TestMethod]
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

            // Assert
            var output = writer.ToString();
            var expectedOutput = "| Header1 | Header2 | Header3 |\r\n| ------- | ------- | ------- |\r\n| Data1   | Data2   | Data3   |\r\n";
            Assert.AreEqual(expectedOutput, output);
        }

        /// <summary>
        /// Tests the <see cref="ResultTable.Render(TextWriter, int[])"/> method to ensure it correctly renders the table with specified column widths.
        /// </summary>
        [TestMethod]
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

            // Assert
            var output = writer.ToString();
            var expectedOutput = "| Header1    | Header2    | Header3    |\r\n| ---------- | ---------- | ---------- |\r\n| Data1      | Data2      | Data3      |\r\n";
            Assert.AreEqual(expectedOutput, output);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="Cell"/> class.
    /// </summary>
    [TestClass]
    public class CellTests
    {
        /// <summary>
        /// Tests the <see cref="Cell.Cell(CellElement[])"/> constructor to ensure it correctly initializes the elements.
        /// </summary>
        [TestMethod]
        public void Cell_Constructor_InitializesElements()
        {
            // Arrange
            var elements = new[] { new CellElement("Text1"), new CellElement("Text2") };

            // Act
            var cell = new Cell(elements);

            // Assert
            Assert.AreEqual(2, cell.Elements.Count);
            Assert.AreEqual("Text1", cell.Elements[0].Text);
            Assert.AreEqual("Text2", cell.Elements[1].Text);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="CellElement"/> class.
    /// </summary>
    [TestClass]
    public class CellElementTests
    {
        /// <summary>
        /// Tests the <see cref="CellElement.CellElement()"/> constructor to ensure it correctly initializes the properties.
        /// </summary>
        [TestMethod]
        public void CellElement_ParameterlessConstructor_InitializesProperties()
        {
            // Act
            var cellElement = new CellElement();

            // Assert
            Assert.IsNull(cellElement.Text);
            Assert.AreEqual(CellTextAlignment.Unspecified, cellElement.Alignment);
        }

        /// <summary>
        /// Tests the <see cref="CellElement.CellElement(string)"/> constructor to ensure it correctly initializes the text property.
        /// </summary>
        [TestMethod]
        public void CellElement_ConstructorWithText_InitializesText()
        {
            // Act
            var cellElement = new CellElement("Text");

            // Assert
            Assert.AreEqual("Text", cellElement.Text);
            Assert.AreEqual(CellTextAlignment.Unspecified, cellElement.Alignment);
        }

        /// <summary>
        /// Tests the <see cref="CellElement.CellElement(string, CellTextAlignment)"/> constructor to ensure it correctly initializes the properties.
        /// </summary>
        [TestMethod]
        public void CellElement_ConstructorWithTextAndAlignment_InitializesProperties()
        {
            // Act
            var cellElement = new CellElement("Text", CellTextAlignment.Left);

            // Assert
            Assert.AreEqual("Text", cellElement.Text);
            Assert.AreEqual(CellTextAlignment.Left, cellElement.Alignment);
        }
    }
}
