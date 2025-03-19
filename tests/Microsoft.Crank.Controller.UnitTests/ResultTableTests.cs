// using Microsoft.VisualStudio.TestTools.UnitTesting; [Error] (1-30)CS0234 The type or namespace name 'TestTools' does not exist in the namespace 'Microsoft.VisualStudio' (are you missing an assembly reference?)
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
//     [TestClass] [Error] (13-6)CS0246 The type or namespace name 'TestClassAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (13-6)CS0246 The type or namespace name 'TestClass' could not be found (are you missing a using directive or an assembly reference?)
    public class ResultTableTests
    {
        private readonly int _columns = 2;

        /// <summary>
        /// Tests that the constructor correctly sets the Columns property.
        /// </summary>
//         [TestMethod] [Error] (21-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (21-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (28-13)CS0103 The name 'Assert' does not exist in the current context
//         public void Constructor_SetsColumnsProperty()
//         {
//             // Arrange & Act
//             var table = new ResultTable(_columns);
// 
//             // Assert
//             Assert.AreEqual(_columns, table.Columns, "Columns property was not set correctly in the constructor.");
//         }

        /// <summary>
        /// Tests that AddRow adds a new row and increases the count of Rows.
        /// </summary>
//         [TestMethod] [Error] (34-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (34-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (44-13)CS0103 The name 'Assert' does not exist in the current context [Error] (45-13)CS0103 The name 'Assert' does not exist in the current context
//         public void AddRow_WhenCalled_AddsRowToRowsList()
//         {
//             // Arrange
//             var table = new ResultTable(_columns);
// 
//             // Act
//             var row = table.AddRow();
// 
//             // Assert
//             Assert.IsNotNull(row, "AddRow returned a null row.");
//             Assert.AreEqual(1, table.Rows.Count, "The row was not added to the Rows list.");
//         }

        /// <summary>
        /// Tests that CalculateColumnWidths returns the maximum width between headers and cell content.
        /// </summary>
//         [TestMethod] [Error] (51-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (51-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (77-13)CS0103 The name 'Assert' does not exist in the current context [Error] (78-13)CS0103 The name 'Assert' does not exist in the current context [Error] (79-13)CS0103 The name 'Assert' does not exist in the current context
//         public void CalculateColumnWidths_WhenHeadersAndRowsProvided_ReturnsCorrectWidths()
//         {
//             // Arrange
//             var table = new ResultTable(_columns);
//             // Set headers
//             table.Headers.Add("Col1");
//             table.Headers.Add("LongHeader");
// 
//             // Create cells for first column: one cell with text "Data1"
//             var cell1 = new Cell(new CellElement("Data1", CellTextAlignment.Left));
//             // For second column: one cell with two elements: "AB" and "CD" (total length 2+2 + 1 space = 5)
//             var cell2 = new Cell(new CellElement("AB", CellTextAlignment.Left), new CellElement("CD", CellTextAlignment.Left));
// 
//             // Create a row with two cells
//             var row = table.AddRow();
//             // Ensure row has two cells by adding the cells manually.
//             row.Add(cell1);
//             row.Add(cell2);
// 
//             // Act
//             var widths = table.CalculateColumnWidths();
// 
//             // Assert
//             // For column0: header "Col1" length=4, cell "Data1": length = 5 -> expected width = 5
//             // For column1: header "LongHeader" length=10, cell: sum("AB".Length + "CD".Length) + 1 separator = 2+2+1 = 5, expected width = 10
//             Assert.AreEqual(2, widths.Length, "Number of calculated widths does not match the number of columns.");
//             Assert.AreEqual(5, widths[0], "Calculated width for column 0 is incorrect.");
//             Assert.AreEqual(10, widths[1], "Calculated width for column 1 is incorrect.");
//         }

        /// <summary>
        /// Tests that RemoveEmptyRows removes rows where all cells have empty text starting at index 0.
        /// </summary>
//         [TestMethod] [Error] (85-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (85-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (108-13)CS0103 The name 'Assert' does not exist in the current context [Error] (110-13)CS0103 The name 'Assert' does not exist in the current context
//         public void RemoveEmptyRows_WhenRowsAreEmpty_RemovesThem()
//         {
//             // Arrange
//             var table = new ResultTable(_columns);
//             // Setting headers to avoid affecting width calculations (though not used in removal)
//             table.Headers.Add("Header1");
//             table.Headers.Add("Header2");
// 
//             // Add a non-empty row.
//             var nonEmptyRow = table.AddRow();
//             nonEmptyRow.Add(new Cell(new CellElement("Data", CellTextAlignment.Left)));
//             nonEmptyRow.Add(new Cell(new CellElement("More", CellTextAlignment.Left)));
// 
//             // Add an empty row.
//             var emptyRow = table.AddRow();
//             emptyRow.Add(new Cell(new CellElement(string.Empty)));
//             emptyRow.Add(new Cell(new CellElement(string.Empty)));
// 
//             // Act
//             table.RemoveEmptyRows();
// 
//             // Assert
//             Assert.AreEqual(1, table.Rows.Count, "Empty rows were not removed correctly.");
//             // Also validate that the remaining row is the non-empty row.
//             Assert.IsTrue(table.Rows[0].Any(cell => cell.Elements.Any(x => !string.IsNullOrEmpty(x.Text))),
//                 "The remaining row should contain non-empty data.");
//         }

        /// <summary>
        /// Tests that RemoveEmptyRows with a startIndex parameter works correctly.
        /// It should only consider cells from the startIndex for determining if a row is empty.
        /// </summary>
//         [TestMethod] [Error] (118-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (118-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (147-13)CS0103 The name 'Assert' does not exist in the current context
//         public void RemoveEmptyRows_WithStartIndex_RemovesRowsProperly()
//         {
//             // Arrange
//             var table = new ResultTable(3);
//             table.Headers.Add("Header1");
//             table.Headers.Add("Header2");
//             table.Headers.Add("Header3");
// 
//             // Row where first column is non-empty but others are empty.
//             var row1 = table.AddRow();
//             row1.Add(new Cell(new CellElement("Data", CellTextAlignment.Left)));
//             row1.Add(new Cell(new CellElement(string.Empty)));
//             row1.Add(new Cell(new CellElement(string.Empty)));
// 
//             // Row where columns starting from index 1 are empty.
//             var row2 = table.AddRow();
//             row2.Add(new Cell(new CellElement(string.Empty)));
//             row2.Add(new Cell(new CellElement(string.Empty)));
//             row2.Add(new Cell(new CellElement(string.Empty)));
// 
//             // Act: Remove empty rows considering cells from index 1 onward.
//             table.RemoveEmptyRows(startIndex: 1);
// 
//             // Assert: row1 should remain because from index 1, not all cells are null? Actually row1: indexes 1 and 2 are empty.
//             // However, the condition in RemoveEmptyRows is: row.Skip(startIndex).All(cell => cell.Elements.All(x => String.IsNullOrEmpty(x.Text))).
//             // So for row1, columns 1 and 2 are empty => row1 qualifies for removal.
//             // row2: columns 1 and 2 are empty as well.
//             // Therefore, both rows should be removed.
//             Assert.AreEqual(0, table.Rows.Count, "Rows starting with empty data from the specified index were not removed as expected.");
//         }

        /// <summary>
        /// Tests the Render method (without custom column widths) by verifying the generated markdown table.
        /// </summary>
//         [TestMethod] [Error] (153-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (153-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (194-17)CS0103 The name 'Assert' does not exist in the current context
//         public void Render_WhenCalled_GeneratesCorrectMarkdownTable()
//         {
//             // Arrange
//             var table = new ResultTable(_columns);
//             // Set headers
//             table.Headers.Add("Col1");
//             table.Headers.Add("Col2");
// 
//             // Create a row where:
//             // Column 0: cell with a left aligned text "Data1"
//             // Column 1: cell with a right aligned text "Data2"
//             var row = table.AddRow();
//             row.Add(new Cell(new CellElement("Data1", CellTextAlignment.Left)));
//             row.Add(new Cell(new CellElement("Data2", CellTextAlignment.Right)));
// 
//             // Calculate expected column widths.
//             // For column0: max("Col1" (4) and "Data1" (5)) = 5.
//             // For column1: max("Col2" (4) and "Data2" (5)) = 5.
//             int[] columnWidths = new int[] { 5, 5 };
//             // Expected output:
//             // Header row: "| Col1  | Col2  |"
//             // Separator row: "| ----- | ----- |"
//             // Data row:
//             // For column0: left aligned "Data1" then no extra spaces.
//             // For column1: no left text, then right aligned "Data2".
//             // Thus: "| Data1 | Data2 |"
//             string expectedOutput = string.Join(Environment.NewLine, new[]
//             {
//                 "| Col1  | Col2  |",
//                 "| ----- | ----- |",
//                 "| Data1 | Data2 |"
//             }) + Environment.NewLine;
// 
//             using (var writer = new StringWriter())
//             {
//                 // Act
//                 table.Render(writer, columnWidths);
//                 string actualOutput = writer.ToString();
// 
//                 // Assert
//                 Assert.AreEqual(expectedOutput, actualOutput, "The generated markdown table did not match the expected output.");
//             }
//         }

        /// <summary>
        /// Tests the Render(TextWriter) overload which internally calls CalculateColumnWidths.
        /// </summary>
//         [TestMethod] [Error] (201-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (201-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (255-17)CS0103 The name 'Assert' does not exist in the current context
//         public void RenderOverload_WhenCalled_GeneratesCorrectMarkdownTable()
//         {
//             // Arrange
//             var table = new ResultTable(_columns);
//             table.Headers.Add("H1");
//             table.Headers.Add("Header2");
// 
//             // Create two rows:
//             // Row 1: for col0: "A" left; for col1: "B" right.
//             var row1 = table.AddRow();
//             row1.Add(new Cell(new CellElement("A", CellTextAlignment.Left)));
//             row1.Add(new Cell(new CellElement("B", CellTextAlignment.Right)));
// 
//             // Row 2: for col0: "LongText" left; for col1: empty.
//             var row2 = table.AddRow();
//             row2.Add(new Cell(new CellElement("LongText", CellTextAlignment.Left)));
//             row2.Add(new Cell(new CellElement(string.Empty)));
// 
//             // Expected column widths:
//             // Column0: Header "H1" (length=2 after padding?) Actually "H1" length=2, row1 has "A" (1), row2 has "LongText" (8). So width=max(2,1,8)=8.
//             // Column1: Header "Header2" (7), row1 "B" (1), row2 empty (0) -> width=max(7,1,1)=7 (minimum is header length, and minimum in code is Math.Max(Headers[i].Length,1)).
//             // So widths are {8,7}.
//             // Build expected output manually:
//             // Line 1: "| H1      | Header2 |"
//             // Line 2: "| -------- | ------- |"  (8 dashes and 7 dashes)
//             // Row 1:
//             // Column0: left: "A" then padding to fill 8 => "A"
//             // Actually, rendering: writes "| " then writes left aligned "A " then calculates leftWidth = 1 then writes spaces = new string(' ', 8 -1) i.e. 7 spaces? Let's calculate carefully:
//             // For row1 col0: cell contains one left element "A", so leftWidth = "A".Length+1 - 1 = 1.
//             // It then writes new string(' ', 8 - 1 - 0)= new string(' ', 7) after writing "A ".
//             // So output becomes: "| A " followed immediately by 7 spaces.
//             // Thus, the content becomes "A " + "       " = "A        " (total 8 characters).
//             // Column1 row1: cell with one right aligned element "B": leftElements = none => leftWidth=0; rightWidth = 1 so no spaces. Output becomes "B ".
//             // So row1 line: "| A        | B |"
//             // Row 2:
//             // Column0: cell with one left element "LongText": leftWidth = 8; writes "LongText " then new string(' ', 8-8)=0.
//             // Column1: empty cell so writes new string(' ', 7+1) which is 8 spaces.
//             // So row2 line: "| LongText |         |"
//             string expectedOutput = string.Join(Environment.NewLine, new[]
//             {
//                 "| H1      | Header2 |",
//                 "| -------- | ------- |",
//                 "| A        | B |",
//                 "| LongText |         |"
//             }) + Environment.NewLine;
//             
//             using (var writer = new StringWriter())
//             {
//                 // Act
//                 table.Render(writer);
//                 string actualOutput = writer.ToString();
// 
//                 // Assert
//                 Assert.AreEqual(expectedOutput, actualOutput, "The generated markdown table via Render(TextWriter) did not match the expected output.");
//             }
//         }
    }

    /// <summary>
    /// Unit tests for the <see cref="Cell"/> class.
    /// </summary>
//     [TestClass] [Error] (263-6)CS0246 The type or namespace name 'TestClassAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (263-6)CS0246 The type or namespace name 'TestClass' could not be found (are you missing a using directive or an assembly reference?)
    public class CellTests
    {
        /// <summary>
        /// Tests that the constructor of Cell adds provided CellElements.
        /// </summary>
//         [TestMethod] [Error] (269-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (269-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (280-13)CS0103 The name 'Assert' does not exist in the current context [Error] (281-13)CS0103 The name 'CollectionAssert' does not exist in the current context
//         public void Constructor_WithElements_AddsElementsToCell()
//         {
//             // Arrange
//             var element1 = new CellElement("Test", CellTextAlignment.Left);
//             var element2 = new CellElement("Example", CellTextAlignment.Right);
// 
//             // Act
//             var cell = new Cell(element1, element2);
// 
//             // Assert
//             Assert.AreEqual(2, cell.Elements.Count, "The cell did not contain the expected number of elements.");
//             CollectionAssert.AreEqual(new List<CellElement> { element1, element2 }, cell.Elements, "The cell elements are not added in the correct order.");
//         }

        /// <summary>
        /// Tests that the constructor of Cell handles a null array of elements gracefully.
        /// </summary>
//         [TestMethod] [Error] (287-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (287-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (294-13)CS0103 The name 'Assert' does not exist in the current context [Error] (295-13)CS0103 The name 'Assert' does not exist in the current context
//         public void Constructor_WithNullElements_DoesNotThrowAndCreatesEmptyElementsList()
//         {
//             // Arrange & Act
//             var cell = new Cell(null);
// 
//             // Assert
//             Assert.IsNotNull(cell.Elements, "Elements list should be initialized even if null is passed.");
//             Assert.AreEqual(0, cell.Elements.Count, "Elements list should be empty when null collection is provided.");
//         }
    }

    /// <summary>
    /// Unit tests for the <see cref="CellElement"/> class.
    /// </summary>
//     [TestClass] [Error] (302-6)CS0246 The type or namespace name 'TestClassAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (302-6)CS0246 The type or namespace name 'TestClass' could not be found (are you missing a using directive or an assembly reference?)
    public class CellElementTests
    {
        /// <summary>
        /// Tests that the parameterless constructor initializes properties with default values.
        /// </summary>
//         [TestMethod] [Error] (308-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (308-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (315-13)CS0103 The name 'Assert' does not exist in the current context [Error] (316-13)CS0103 The name 'Assert' does not exist in the current context
//         public void ParameterlessConstructor_InitializesDefaultValues()
//         {
//             // Arrange & Act
//             var element = new CellElement();
// 
//             // Assert
//             Assert.IsNull(element.Text, "Text should be null by default.");
//             Assert.AreEqual(CellTextAlignment.Unspecified, element.Alignment, "Alignment should be Unspecified by default.");
//         }

        /// <summary>
        /// Tests that the constructor with a text parameter sets the Text property correctly.
        /// </summary>
//         [TestMethod] [Error] (322-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (322-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (332-13)CS0103 The name 'Assert' does not exist in the current context [Error] (333-13)CS0103 The name 'Assert' does not exist in the current context
//         public void Constructor_WithText_SetsTextProperty()
//         {
//             // Arrange
//             string expectedText = "Sample";
// 
//             // Act
//             var element = new CellElement(expectedText);
// 
//             // Assert
//             Assert.AreEqual(expectedText, element.Text, "Text property was not set correctly.");
//             Assert.AreEqual(CellTextAlignment.Unspecified, element.Alignment, "Alignment should remain Unspecified when not provided.");
//         }

        /// <summary>
        /// Tests that the constructor with text and alignment parameters sets both properties correctly.
        /// </summary>
//         [TestMethod] [Error] (339-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (339-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (350-13)CS0103 The name 'Assert' does not exist in the current context [Error] (351-13)CS0103 The name 'Assert' does not exist in the current context
//         public void Constructor_WithTextAndAlignment_SetsPropertiesCorrectly()
//         {
//             // Arrange
//             string expectedText = "Aligned";
//             CellTextAlignment expectedAlignment = CellTextAlignment.Right;
// 
//             // Act
//             var element = new CellElement(expectedText, expectedAlignment);
// 
//             // Assert
//             Assert.AreEqual(expectedText, element.Text, "Text property was not set correctly.");
//             Assert.AreEqual(expectedAlignment, element.Alignment, "Alignment property was not set correctly.");
//         }
    }
}
