using System;
using System.Collections.Generic;
using Microsoft.Crank.Models;
using Xunit;

namespace Microsoft.Crank.Models.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="CommandDefinition"/> class.
    /// </summary>
    public class CommandDefinitionTests
    {
        private readonly CommandDefinition _commandDefinition;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandDefinitionTests"/> class.
        /// </summary>
        public CommandDefinitionTests()
        {
            _commandDefinition = new CommandDefinition();
        }

        /// <summary>
        /// Tests that the default values in the constructor are initialized correctly.
        /// Expected: Condition equals "true", ScriptType equals ScriptType.Powershell, Script and FilePath are null,
        /// ContinueOnError is false, and SuccessExitCodes contains only the exit code 0.
        /// </summary>
        [Fact]
        public void Constructor_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act are performed via object construction in the constructor.

            // Assert
            Assert.Equal("true", _commandDefinition.Condition);
            Assert.Equal(ScriptType.Powershell, _commandDefinition.ScriptType);
            Assert.Null(_commandDefinition.Script);
            Assert.Null(_commandDefinition.FilePath);
            Assert.False(_commandDefinition.ContinueOnError);
            Assert.NotNull(_commandDefinition.SuccessExitCodes);
            Assert.Single(_commandDefinition.SuccessExitCodes);
            Assert.Equal(0, _commandDefinition.SuccessExitCodes[0]);
        }

        /// <summary>
        /// Tests that the Condition property can be set and retrieved properly.
        /// Expected: The value assigned to Condition is returned by the getter.
        /// </summary>
        [Fact]
        public void Condition_SetAndGet_ReturnsExpectedValue()
        {
            // Arrange
            string expected = "custom condition";

            // Act
            _commandDefinition.Condition = expected;
            string actual = _commandDefinition.Condition;

            // Assert
            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Tests that the ScriptType property can be set and retrieved properly.
        /// Expected: The value assigned to ScriptType is returned by the getter.
        /// </summary>
        [Fact]
        public void ScriptType_SetAndGet_ReturnsExpectedValue()
        {
            // Arrange
            // Since only ScriptType.Powershell is known from defaults,
            // we will reassign the same value to validate the setter and getter.
            ScriptType expected = ScriptType.Powershell;

            // Act
            _commandDefinition.ScriptType = expected;
            ScriptType actual = _commandDefinition.ScriptType;

            // Assert
            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Tests that the Script property can be set and retrieved properly.
        /// Expected: The value assigned to Script is returned by the getter.
        /// </summary>
        [Fact]
        public void Script_SetAndGet_ReturnsExpectedValue()
        {
            // Arrange
            string expected = "echo Hello World";

            // Act
            _commandDefinition.Script = expected;
            string actual = _commandDefinition.Script;

            // Assert
            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Tests that the FilePath property can be set and retrieved properly.
        /// Expected: The value assigned to FilePath is returned by the getter.
        /// </summary>
        [Fact]
        public void FilePath_SetAndGet_ReturnsExpectedValue()
        {
            // Arrange
            string expected = @"C:\temp\script.ps1";

            // Act
            _commandDefinition.FilePath = expected;
            string actual = _commandDefinition.FilePath;

            // Assert
            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Tests that the ContinueOnError property can be set and retrieved properly.
        /// Expected: The boolean value assigned to ContinueOnError is returned by the getter.
        /// </summary>
        [Fact]
        public void ContinueOnError_SetAndGet_ReturnsExpectedValue()
        {
            // Arrange
            bool expected = true;

            // Act
            _commandDefinition.ContinueOnError = expected;
            bool actual = _commandDefinition.ContinueOnError;

            // Assert
            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Tests that the SuccessExitCodes property can be set and retrieved properly.
        /// Expected: The list assigned to SuccessExitCodes is returned by the getter.
        /// </summary>
        [Fact]
        public void SuccessExitCodes_SetAndGet_ReturnsExpectedValue()
        {
            // Arrange
            List<int> expected = new List<int> { 0, 1, 2 };

            // Act
            _commandDefinition.SuccessExitCodes = expected;
            List<int> actual = _commandDefinition.SuccessExitCodes;

            // Assert
            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Tests that modifications to the SuccessExitCodes list are reflected properly.
        /// Expected: Adding elements to the list should update the collection accordingly.
        /// </summary>
        [Fact]
        public void SuccessExitCodes_ModifyList_ReflectsChanges()
        {
            // Arrange
            _commandDefinition.SuccessExitCodes = new List<int> { 0 };

            // Act
            _commandDefinition.SuccessExitCodes.Add(1);
            _commandDefinition.SuccessExitCodes.Add(2);

            // Assert
            Assert.Equal(3, _commandDefinition.SuccessExitCodes.Count);
            Assert.Contains(0, _commandDefinition.SuccessExitCodes);
            Assert.Contains(1, _commandDefinition.SuccessExitCodes);
            Assert.Contains(2, _commandDefinition.SuccessExitCodes);
        }
    }
}
