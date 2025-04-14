using Microsoft.Crank.RegressionBot;
using Microsoft.Crank.RegressionBot.Models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Xunit;

namespace Microsoft.Crank.RegressionBot.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref = "Rule"/> class.
    /// </summary>
    public class RuleTests
    {
        private readonly Rule _rule;
        /// <summary>
        /// Initializes a new instance of the <see cref = "RuleTests"/> class.
        /// </summary>
        public RuleTests()
        {
            _rule = new Rule();
        }

        /// <summary>
        /// Tests that by default the Labels property is initialized to an empty list.
        /// </summary>
//         [Fact] [Error] (31-41)CS1061 'Rule' does not contain a definition for 'Labels' and no accessible extension method 'Labels' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?)
//         public void Constructor_Default_LabelsInitializedAsEmptyList()
//         {
//             // Arrange & Act (constructor is called automatically)
//             List<string> labels = _rule.Labels;
//             // Assert
//             Assert.NotNull(labels);
//             Assert.Empty(labels);
//         }

        /// <summary>
        /// Tests that by default the Owners field is initialized to an empty list.
        /// </summary>
//         [Fact] [Error] (44-41)CS1061 'Rule' does not contain a definition for 'Owners' and no accessible extension method 'Owners' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?)
//         public void Constructor_Default_OwnersInitializedAsEmptyList()
//         {
//             // Arrange & Act
//             List<string> owners = _rule.Owners;
//             // Assert
//             Assert.NotNull(owners);
//             Assert.Empty(owners);
//         }

        /// <summary>
        /// Tests that the Include property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void IncludeProperty_SetAndGet_ReturnsSameValue()
        {
            // Arrange
            string expectedInclude = "IncludeTestValue";
            // Act
            _rule.Include = expectedInclude;
            string actualInclude = _rule.Include;
            // Assert
            Assert.Equal(expectedInclude, actualInclude);
        }

        /// <summary>
        /// Tests that the Exclude property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void ExcludeProperty_SetAndGet_ReturnsSameValue()
        {
            // Arrange
            string expectedExclude = "ExcludeTestValue";
            // Act
            _rule.Exclude = expectedExclude;
            string actualExclude = _rule.Exclude;
            // Assert
            Assert.Equal(expectedExclude, actualExclude);
        }

        /// <summary>
        /// Tests that the Labels property supports adding and retrieving multiple values.
        /// </summary>
//         [Fact] [Error] (94-19)CS1061 'Rule' does not contain a definition for 'Labels' and no accessible extension method 'Labels' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?) [Error] (95-47)CS1061 'Rule' does not contain a definition for 'Labels' and no accessible extension method 'Labels' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?)
//         public void LabelsProperty_AddMultipleItems_ReturnsCorrectCollection()
//         {
//             // Arrange
//             var expectedLabels = new List<string>
//             {
//                 "Label1",
//                 "Label2",
//                 "Label3"
//             };
//             // Act
//             _rule.Labels.AddRange(expectedLabels);
//             List<string> actualLabels = _rule.Labels;
//             // Assert
//             Assert.Equal(expectedLabels.Count, actualLabels.Count);
//             for (int i = 0; i < expectedLabels.Count; i++)
//             {
//                 Assert.Equal(expectedLabels[i], actualLabels[i]);
//             }
//         }

        /// <summary>
        /// Tests that the Owners field supports adding and retrieving multiple values.
        /// </summary>
//         [Fact] [Error] (117-19)CS1061 'Rule' does not contain a definition for 'Owners' and no accessible extension method 'Owners' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?) [Error] (118-47)CS1061 'Rule' does not contain a definition for 'Owners' and no accessible extension method 'Owners' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?)
//         public void OwnersField_AddMultipleItems_ReturnsCorrectCollection()
//         {
//             // Arrange
//             var expectedOwners = new List<string>
//             {
//                 "Owner1",
//                 "Owner2"
//             };
//             // Act
//             _rule.Owners.AddRange(expectedOwners);
//             List<string> actualOwners = _rule.Owners;
//             // Assert
//             Assert.Equal(expectedOwners.Count, actualOwners.Count);
//             for (int i = 0; i < expectedOwners.Count; i++)
//             {
//                 Assert.Equal(expectedOwners[i], actualOwners[i]);
//             }
//         }

        /// <summary>
        /// Tests that the IgnoreRegressions property can be set to true, false, and null.
        /// </summary>
//         [Fact] [Error] (134-19)CS1061 'Rule' does not contain a definition for 'IgnoreRegressions' and no accessible extension method 'IgnoreRegressions' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?) [Error] (135-31)CS1061 'Rule' does not contain a definition for 'IgnoreRegressions' and no accessible extension method 'IgnoreRegressions' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?) [Error] (135-67)CS1061 'Rule' does not contain a definition for 'IgnoreRegressions' and no accessible extension method 'IgnoreRegressions' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?) [Error] (137-19)CS1061 'Rule' does not contain a definition for 'IgnoreRegressions' and no accessible extension method 'IgnoreRegressions' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?) [Error] (138-31)CS1061 'Rule' does not contain a definition for 'IgnoreRegressions' and no accessible extension method 'IgnoreRegressions' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?) [Error] (138-68)CS1061 'Rule' does not contain a definition for 'IgnoreRegressions' and no accessible extension method 'IgnoreRegressions' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?) [Error] (140-19)CS1061 'Rule' does not contain a definition for 'IgnoreRegressions' and no accessible extension method 'IgnoreRegressions' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?) [Error] (141-32)CS1061 'Rule' does not contain a definition for 'IgnoreRegressions' and no accessible extension method 'IgnoreRegressions' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?)
//         public void IgnoreRegressionsProperty_SetVariousValues_ReturnsSameValues()
//         {
//             // Arrange & Act & Assert for true
//             _rule.IgnoreRegressions = true;
//             Assert.True(_rule.IgnoreRegressions.HasValue && _rule.IgnoreRegressions.Value);
//             // Act & Assert for false
//             _rule.IgnoreRegressions = false;
//             Assert.True(_rule.IgnoreRegressions.HasValue && !_rule.IgnoreRegressions.Value);
//             // Act & Assert for null (unset)
//             _rule.IgnoreRegressions = null;
//             Assert.False(_rule.IgnoreRegressions.HasValue);
//         }

        /// <summary>
        /// Tests that the IgnoreErrors property can be set to true, false, and null.
        /// </summary>
//         [Fact] [Error] (151-19)CS1061 'Rule' does not contain a definition for 'IgnoreErrors' and no accessible extension method 'IgnoreErrors' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?) [Error] (152-31)CS1061 'Rule' does not contain a definition for 'IgnoreErrors' and no accessible extension method 'IgnoreErrors' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?) [Error] (152-62)CS1061 'Rule' does not contain a definition for 'IgnoreErrors' and no accessible extension method 'IgnoreErrors' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?) [Error] (154-19)CS1061 'Rule' does not contain a definition for 'IgnoreErrors' and no accessible extension method 'IgnoreErrors' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?) [Error] (155-31)CS1061 'Rule' does not contain a definition for 'IgnoreErrors' and no accessible extension method 'IgnoreErrors' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?) [Error] (155-63)CS1061 'Rule' does not contain a definition for 'IgnoreErrors' and no accessible extension method 'IgnoreErrors' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?) [Error] (157-19)CS1061 'Rule' does not contain a definition for 'IgnoreErrors' and no accessible extension method 'IgnoreErrors' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?) [Error] (158-32)CS1061 'Rule' does not contain a definition for 'IgnoreErrors' and no accessible extension method 'IgnoreErrors' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?)
//         public void IgnoreErrorsProperty_SetVariousValues_ReturnsSameValues()
//         {
//             // Arrange & Act & Assert for true
//             _rule.IgnoreErrors = true;
//             Assert.True(_rule.IgnoreErrors.HasValue && _rule.IgnoreErrors.Value);
//             // Act & Assert for false
//             _rule.IgnoreErrors = false;
//             Assert.True(_rule.IgnoreErrors.HasValue && !_rule.IgnoreErrors.Value);
//             // Act & Assert for null (unset)
//             _rule.IgnoreErrors = null;
//             Assert.False(_rule.IgnoreErrors.HasValue);
//         }

        /// <summary>
        /// Tests that the IgnoreFailures property can be set to true, false, and null.
        /// </summary>
//         [Fact] [Error] (168-19)CS1061 'Rule' does not contain a definition for 'IgnoreFailures' and no accessible extension method 'IgnoreFailures' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?) [Error] (169-31)CS1061 'Rule' does not contain a definition for 'IgnoreFailures' and no accessible extension method 'IgnoreFailures' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?) [Error] (169-64)CS1061 'Rule' does not contain a definition for 'IgnoreFailures' and no accessible extension method 'IgnoreFailures' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?) [Error] (171-19)CS1061 'Rule' does not contain a definition for 'IgnoreFailures' and no accessible extension method 'IgnoreFailures' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?) [Error] (172-31)CS1061 'Rule' does not contain a definition for 'IgnoreFailures' and no accessible extension method 'IgnoreFailures' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?) [Error] (172-65)CS1061 'Rule' does not contain a definition for 'IgnoreFailures' and no accessible extension method 'IgnoreFailures' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?) [Error] (174-19)CS1061 'Rule' does not contain a definition for 'IgnoreFailures' and no accessible extension method 'IgnoreFailures' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?) [Error] (175-32)CS1061 'Rule' does not contain a definition for 'IgnoreFailures' and no accessible extension method 'IgnoreFailures' accepting a first argument of type 'Rule' could be found (are you missing a using directive or an assembly reference?)
//         public void IgnoreFailuresProperty_SetVariousValues_ReturnsSameValues()
//         {
//             // Arrange & Act & Assert for true
//             _rule.IgnoreFailures = true;
//             Assert.True(_rule.IgnoreFailures.HasValue && _rule.IgnoreFailures.Value);
//             // Act & Assert for false
//             _rule.IgnoreFailures = false;
//             Assert.True(_rule.IgnoreFailures.HasValue && !_rule.IgnoreFailures.Value);
//             // Act & Assert for null (unset)
//             _rule.IgnoreFailures = null;
//             Assert.False(_rule.IgnoreFailures.HasValue);
//         }

        /// <summary>
        /// Tests that the IncludeRegex internal property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void IncludeRegexProperty_SetAndGet_ReturnsSameRegex()
        {
            // Arrange
            Regex expectedRegex = new Regex("Include.*Test");
            // Act
            _rule.IncludeRegex = expectedRegex;
            Regex actualRegex = _rule.IncludeRegex;
            // Assert
            Assert.Equal(expectedRegex.ToString(), actualRegex.ToString());
            Assert.Equal(expectedRegex.Options, actualRegex.Options);
        }

        /// <summary>
        /// Tests that the ExcludeRegex internal property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void ExcludeRegexProperty_SetAndGet_ReturnsSameRegex()
        {
            // Arrange
            Regex expectedRegex = new Regex("Exclude.*Test", RegexOptions.IgnoreCase);
            // Act
            _rule.ExcludeRegex = expectedRegex;
            Regex actualRegex = _rule.ExcludeRegex;
            // Assert
            Assert.Equal(expectedRegex.ToString(), actualRegex.ToString());
            Assert.Equal(expectedRegex.Options, actualRegex.Options);
        }
    }
}