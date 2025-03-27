// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Crank.Controller;
using Newtonsoft.Json.Linq;
using System;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="JobDefinition"/> class.
    /// </summary>
    public class JobDefinitionTests
    {
        /// <summary>
        /// Tests that the <see cref="JobDefinition"/> constructor initializes the dictionary with a case-insensitive comparer.
        /// Verifies that keys with different casing can be used interchangeably.
        /// </summary>
        [Fact]
        public void Constructor_CaseInsensitiveBehavior_ReturnsCaseInsensitiveDictionary()
        {
            // Arrange
            var jobDefinition = new JobDefinition();
            var originalKey = "TestKey";
            var value = JObject.FromObject(new { Data = "value" });
            jobDefinition.Add(originalKey, value);

            // Act
            var retrievedValueLower = jobDefinition["testkey"];
            var retrievedValueUpper = jobDefinition["TESTKEY"];

            // Assert
            Assert.Equal(value, retrievedValueLower);
            Assert.Equal(value, retrievedValueUpper);
        }

        /// <summary>
        /// Tests that adding a null key to the dictionary throws an <see cref="ArgumentNullException"/>.
        /// This boundary case ensures that the dictionary maintains key integrity.
        /// </summary>
        [Fact]
        public void Add_NullKey_ThrowsArgumentNullException()
        {
            // Arrange
            var jobDefinition = new JobDefinition();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => jobDefinition.Add(null, new JObject()));
        }

        /// <summary>
        /// Tests that adding a duplicate key (accounting for case-insensitivity) using the <c>Add</c> method throws an <see cref="ArgumentException"/>.
        /// This ensures that duplicate keys are not allowed in the dictionary.
        /// </summary>
        [Fact]
        public void Add_DuplicateKey_ThrowsArgumentException()
        {
            // Arrange
            var jobDefinition = new JobDefinition();
            var key = "DuplicateKey";
            var initialValue = JObject.FromObject(new { Data = "first" });
            var duplicateValue = JObject.FromObject(new { Data = "second" });
            jobDefinition.Add(key, initialValue);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => jobDefinition.Add("duplicatekey", duplicateValue));
        }

        /// <summary>
        /// Tests that setting a value via the indexer for an existing key overwrites the previous value.
        /// This validates the dictionary's indexer behavior in updating values.
        /// </summary>
        [Fact]
        public void Indexer_SetValue_OverwritesExistingValue()
        {
            // Arrange
            var jobDefinition = new JobDefinition();
            var key = "TestKey";
            var initialValue = JObject.FromObject(new { Data = "initial" });
            var updatedValue = JObject.FromObject(new { Data = "updated" });
            jobDefinition.Add(key, initialValue);

            // Act
            jobDefinition[key] = updatedValue;

            // Assert
            Assert.Equal(updatedValue, jobDefinition[key]);
        }
    }
}
