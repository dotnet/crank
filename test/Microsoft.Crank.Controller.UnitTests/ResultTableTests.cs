using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Crank.Controller;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="ResultTable"/> class.
    /// </summary>
    public class ResultTableTests
    {
        private readonly int _columns = 2;

        /// <summary>
        /// Tests that the ResultTable constructor sets the Columns property correctly and initializes Headers and Rows.
        /// </summary>
        [Fact]
        public void Constructor_ValidColumns_InitializesProperties()
        {
            // Arrange & Act
            var table = new ResultTable(_columns);

            // Assert
            Assert.Equal(_columns, table.Columns);
            Assert.NotNull(table.Headers);
            Assert.NotNull(table.Rows);
            Assert.Empty(table.Headers);
            Assert.Empty(table.Rows);
        }

        /// <summary>
        /// Tests that AddRow adds a new empty row to the Rows collection.
        /// </summary>
        [Fact]
        public void AddRow_WhenCalled_AddsRowToRows()
        {
            // Arrange
            var table = new ResultTable(_columns);

            // Act
            var row = table.AddRow();

            // Assert
            Assert.Single(table.Rows);
            Assert.Same(row, table.Rows.Last());
            Assert.Empty(row);
        }

        /// <summary>
        /// Tests that CalculateColumnWidths returns widths based solely on header values when no row data is present.
        /// </summary>
        [Fact]
        public void CalculateColumnWidths_NoRows_ReturnsWidthsFromHeadersOrAtLeastOne()
        {
            // Arrange
            var table = new ResultTable(_columns);
            table.Headers.Add("Col1");
            table.Headers.Add(string.Empty); // header is empty

            // Act
            var widths = table.CalculateColumnWidths();

            // Assert
            // For first column, width should be Max(Header length=4, 1) => 4.
            // For second column, header is empty => Max(0,1) => 1.
            Assert.Equal(2, widths.Length);
            Assert.Equal(4, widths[0]);
            Assert.Equal(1, widths[1]);
        }

        /// <summary>
        /// Tests that CalculateColumnWidths returns the maximum width calculated from both header and row cell elements over all rows.
        /// </summary>
        [Fact]
        public void CalculateColumnWidths_WithRows_ReturnsCorrectMaxWidths()
        {
            // Arrange
            var table = new ResultTable(2);
            table.Headers.Add("Header"); // length 6
            table.Headers.Add("Col2");   // length 4

            // Create a row with cells containing elements.
            // For column 0, cell with one element "abc" -> width: 3.
            // For column 1, cell with two elements "a" and "bc" -> width: (1 + 2) + (2 - 1) = 3 + 1 = 4.
            var row1 = table.AddRow();
            row1.Add(new Cell(new CellElement("abc", CellTextAlignment.Left)));
            row1.Add(new Cell(new CellElement("a", CellTextAlignment.Left), new CellElement("bc", CellTextAlignment.Left)));

            // Create a second row with cells containing elements that yield a larger width.
            // For column 0, cell with "abcdefg" -> 7.
            // For column 1, cell with one element "longtext" -> 8.
            var row2 = table.AddRow();
            row2.Add(new Cell(new CellElement("abcdefg", CellTextAlignment.Left)));
            row2.Add(new Cell(new CellElement("longtext", CellTextAlignment.Left)));

            // Act
            var widths = table.CalculateColumnWidths();

            // Assert
            // For column 0, max of header (6) and row widths: row1 cell width = 3, row2 cell width = 7, so result=7.
            // For column 1, max of header (4) and row widths: row1 cell width = (1+2 + 1)=4, row2=8, so result=8.
            Assert.Equal(2, widths.Length);
            Assert.Equal(7, widths[0]);
            Assert.Equal(8, widths[1]);
        }

        /// <summary>
        /// Tests that RemoveEmptyRows with default start index removes rows where all cell elements have empty text.
        /// </summary>
        [Fact]
        public void RemoveEmptyRows_DefaultStartIndex_RemovesRowsWithEmptyCells()
        {
            // Arrange
            var table = new ResultTable(1);
            table.Headers.Add("H");
            // Row to be removed: cell with empty element.
            var emptyRow = table.AddRow();
            emptyRow.Add(new Cell(new CellElement(string.Empty, CellTextAlignment.Left)));
            // Row to be kept: cell with non-empty element.
            var nonEmptyRow = table.AddRow();
            nonEmptyRow.Add(new Cell(new CellElement("data", CellTextAlignment.Left)));

            // Act
            table.RemoveEmptyRows();

            // Assert
            Assert.Single(table.Rows);
            Assert.Contains(nonEmptyRow, table.Rows);
        }

        /// <summary>
        /// Tests that RemoveEmptyRows with a non-zero start index only considers cells from that index onward.
        /// </summary>
        [Fact]
        public void RemoveEmptyRows_NonZeroStartIndex_PreservesRowsBasedOnEarlierColumns()
        {
            // Arrange
            var table = new ResultTable(2);
            table.Headers.Add("H1");
            table.Headers.Add("H2");

            // Row where first column is non-empty but second column is empty.
            var row = table.AddRow();
            row.Add(new Cell(new CellElement("data", CellTextAlignment.Left)));
            row.Add(new Cell(new CellElement(string.Empty, CellTextAlignment.Left)));

            // Act: Remove rows where cells starting at index 1 are empty.
            table.RemoveEmptyRows(startIndex: 1);

            // Assert: The row should be removed only if the cells from index 1 are all empty.
            // In this case, row[1] is empty so row is removed.
            Assert.Empty(table.Rows);
        }

        /// <summary>
        /// Tests that Render produces the expected markdown table output using the default Render method.
        /// </summary>
        [Fact]
        public void Render_WithValidData_ProducesExpectedMarkdownTable()
        {
            // Arrange
            var table = new ResultTable(2);
            table.Headers.Add("Col1");
            table.Headers.Add("Col2");

            // Create a row with cells.
            // For column 0: one left-aligned element "a".
            // For column 1: one right-aligned element "b".
            var row = table.AddRow();
            row.Add(new Cell(new CellElement("a", CellTextAlignment.Left)));
            row.Add(new Cell(new CellElement("b", CellTextAlignment.Right)));

            // Calculate expected column widths.
            // For col0: header "Col1" length = 4; cell content: "a" -> width = 1.
            // So width used: 4.
            // For col1: header "Col2" length = 4; cell content: "b" -> width = 1.
            // So width: 4.
            int[] expectedWidths = new int[] { 4, 4 };

            // Build the expected markdown output.
            // Header row: "| Col1 | Col2 |"
            // Separator row: "| ---- | ---- |"
            // Data row: 
            // For col0: left elements printed: "a " then fill with 3 spaces (4 - (1+ ? Calculation: leftWidth = (1+1)-1=1).
            // So cell becomes: "a " then 3 spaces -> "a    ".
            // For col1: right aligned: no left elements, so print fill with 3 spaces then "b ".
            // So combined row: "| a    |    b |"
            string newLine = Environment.NewLine;
            string expectedOutput =
                $"| {"Col1".PadRight(expectedWidths[0])} | {"Col2".PadRight(expectedWidths[1])} |{newLine}" +
                $"| {new string('-', expectedWidths[0])} | {new string('-', expectedWidths[1])} |{newLine}" +
                $"| " +
                // Column 0
                "a " + new string(' ', expectedWidths[0] - 1) +
                " | " +
                // Column 1
                new string(' ', expectedWidths[1] - 1) + "b " +
                "|"+ newLine;

            // Act
            using (var writer = new StringWriter())
            {
                table.Render(writer);
                var result = writer.ToString();

                // Assert
                Assert.Equal(expectedOutput, result);
            }
        }

        /// <summary>
        /// Tests that the overloaded Render method with provided column widths produces the expected markdown output.
        /// </summary>
        [Fact]
        public void Render_WithProvidedColumnWidths_ProducesExpectedMarkdownTable()
        {
            // Arrange
            var table = new ResultTable(2);
            table.Headers.Add("Header1");
            table.Headers.Add("Header2");

            // Create a row with cells.
            // For column 0: one unspecified element "data1".
            // For column 1: one unspecified element "data2".
            var row = table.AddRow();
            row.Add(new Cell(new CellElement("data1", CellTextAlignment.Unspecified)));
            row.Add(new Cell(new CellElement("data2", CellTextAlignment.Unspecified)));

            // Provide fixed column widths.
            int[] providedWidths = new int[] { 10, 8 };

            // Build expected output.
            // Header row: each header padded to provided width.
            string newLine = Environment.NewLine;
            string headerRow = $"| {"Header1".PadRight(providedWidths[0])} | {"Header2".PadRight(providedWidths[1])} |" + newLine;
            string separatorRow = $"| {new string('-', providedWidths[0])} | {new string('-', providedWidths[1])} |" + newLine;
            // Data row processing:
            // For col0: since the element is Unspecified, it is treated like left. Printed: "data1 " then fill with (10 - ((length("data1") + 1) -1)) = 10 - 5 = 5 spaces.
            // For col1: similarly: "data2 " then fill with (8 - 5) spaces = 3 spaces.
            string dataRowPart0 = "data1 " + new string(' ', providedWidths[0] - (( "data1".Length + 1) - 1));
            string dataRowPart1 = "data2 " + new string(' ', providedWidths[1] - (( "data2".Length + 1) - 1));
            string dataRow = $"| {dataRowPart0} | {dataRowPart1} |" + newLine;

            string expectedOutput = headerRow + separatorRow + dataRow;

            // Act
            using (var writer = new StringWriter())
            {
                table.Render(writer, providedWidths);
                var result = writer.ToString();

                // Assert
                Assert.Equal(expectedOutput, result);
            }
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="Cell"/> class.
    /// </summary>
    public class CellTests
    {
        /// <summary>
        /// Tests that the Cell constructor with null parameters initializes an empty Elements collection.
        /// </summary>
        [Fact]
        public void Constructor_NullParameters_InitializesEmptyElements()
        {
            // Arrange & Act
            var cell = new Cell(null);

            // Assert
            Assert.NotNull(cell.Elements);
            Assert.Empty(cell.Elements);
        }

        /// <summary>
        /// Tests that the Cell constructor adds provided CellElement objects to the Elements collection.
        /// </summary>
        [Fact]
        public void Constructor_WithElements_AddsElementsToCollection()
        {
            // Arrange
            var element1 = new CellElement("Test");
            var element2 = new CellElement("Data", CellTextAlignment.Right);

            // Act
            var cell = new Cell(element1, element2);

            // Assert
            Assert.Equal(2, cell.Elements.Count);
            Assert.Contains(element1, cell.Elements);
            Assert.Contains(element2, cell.Elements);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="CellElement"/> class.
    /// </summary>
    public class CellElementTests
    {
        /// <summary>
        /// Tests that the default constructor of CellElement initializes properties with default values.
        /// </summary>
        [Fact]
        public void DefaultConstructor_InitializesDefaultValues()
        {
            // Arrange & Act
            var element = new CellElement();

            // Assert
            Assert.Null(element.Text);
            Assert.Equal(CellTextAlignment.Unspecified, element.Alignment);
        }

        /// <summary>
        /// Tests that the constructor with text correctly sets the Text property.
        /// </summary>
        [Fact]
        public void Constructor_WithText_SetsTextProperty()
        {
            // Arrange
            var expectedText = "Sample";

            // Act
            var element = new CellElement(expectedText);

            // Assert
            Assert.Equal(expectedText, element.Text);
            Assert.Equal(CellTextAlignment.Unspecified, element.Alignment);
        }

        /// <summary>
        /// Tests that the constructor with text and alignment correctly sets both properties.
        /// </summary>
        [Fact]
        public void Constructor_WithTextAndAlignment_SetsBothProperties()
        {
            // Arrange
            var expectedText = "Aligned";
            var expectedAlignment = CellTextAlignment.Right;

            // Act
            var element = new CellElement(expectedText, expectedAlignment);

            // Assert
            Assert.Equal(expectedText, element.Text);
            Assert.Equal(expectedAlignment, element.Alignment);
        }
    }
}
