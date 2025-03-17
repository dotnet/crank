using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Crank.Models.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Source"/> class.
    /// </summary>
    [TestClass]
    public class SourceTests
    {
        private readonly Source _source;

        public SourceTests()
        {
            _source = new Source();
        }

        /// <summary>
        /// Tests the <see cref="Source.GetSourceKeyData"/> method to ensure it returns a <see cref="SourceKeyData"/> object with the correct properties.
        /// </summary>
        [TestMethod]
        public void GetSourceKeyData_WhenCalled_ReturnsCorrectSourceKeyData()
        {
            // Arrange
            _source.BranchOrCommit = "main";
            _source.Repository = "https://github.com/dotnet/crank";
            _source.InitSubmodules = true;
            _source.LocalFolder = "/local/path";

            // Act
            SourceKeyData result = _source.GetSourceKeyData();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("main", result.BranchOrCommit);
            Assert.AreEqual("https://github.com/dotnet/crank", result.Repository);
            Assert.IsTrue(result.InitSubmodules);
            Assert.AreEqual("/local/path", result.LocalFolder);
        }

        /// <summary>
        /// Tests the <see cref="Source.GetSourceKeyData"/> method to ensure it handles null and default values correctly.
        /// </summary>
        [TestMethod]
        public void GetSourceKeyData_WithNullAndDefaultValues_ReturnsCorrectSourceKeyData()
        {
            // Arrange
            _source.BranchOrCommit = null;
            _source.Repository = null;
            _source.InitSubmodules = false;
            _source.LocalFolder = null;

            // Act
            SourceKeyData result = _source.GetSourceKeyData();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNull(result.BranchOrCommit);
            Assert.IsNull(result.Repository);
            Assert.IsFalse(result.InitSubmodules);
            Assert.IsNull(result.LocalFolder);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="SourceKeyData"/> class.
    /// </summary>
    [TestClass]
    public class SourceKeyDataTests
    {
        private readonly SourceKeyData _sourceKeyData;

        public SourceKeyDataTests()
        {
            _sourceKeyData = new SourceKeyData();
        }

        /// <summary>
        /// Tests the properties of the <see cref="SourceKeyData"/> class to ensure they can be set and retrieved correctly.
        /// </summary>
        [TestMethod]
        public void Properties_WhenSetAndRetrieved_ReturnCorrectValues()
        {
            // Arrange
            _sourceKeyData.BranchOrCommit = "main";
            _sourceKeyData.Repository = "https://github.com/dotnet/crank";
            _sourceKeyData.InitSubmodules = true;
            _sourceKeyData.LocalFolder = "/local/path";

            // Act & Assert
            Assert.AreEqual("main", _sourceKeyData.BranchOrCommit);
            Assert.AreEqual("https://github.com/dotnet/crank", _sourceKeyData.Repository);
            Assert.IsTrue(_sourceKeyData.InitSubmodules);
            Assert.AreEqual("/local/path", _sourceKeyData.LocalFolder);
        }

        /// <summary>
        /// Tests the properties of the <see cref="SourceKeyData"/> class to ensure they handle null and default values correctly.
        /// </summary>
        [TestMethod]
        public void Properties_WithNullAndDefaultValues_ReturnCorrectValues()
        {
            // Arrange
            _sourceKeyData.BranchOrCommit = null;
            _sourceKeyData.Repository = null;
            _sourceKeyData.InitSubmodules = false;
            _sourceKeyData.LocalFolder = null;

            // Act & Assert
            Assert.IsNull(_sourceKeyData.BranchOrCommit);
            Assert.IsNull(_sourceKeyData.Repository);
            Assert.IsFalse(_sourceKeyData.InitSubmodules);
            Assert.IsNull(_sourceKeyData.LocalFolder);
        }
    }
}
