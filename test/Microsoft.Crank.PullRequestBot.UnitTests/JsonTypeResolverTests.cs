// using System;
// using System.Globalization;
// using Microsoft.Crank.PullRequestBot;
// using Moq;
// using Xunit;
// using YamlDotNet.Core.Events;
// using YamlDotNet.Core;
// 
// namespace Microsoft.Crank.PullRequestBot.UnitTests
// {
//     /// <summary>
//     /// Unit tests for the <see cref="JsonTypeResolver"/> class.
//     /// </summary>
//     public class JsonTypeResolverTests
//     {
//         private readonly JsonTypeResolver _resolver;
// 
//         /// <summary>
//         /// Initializes a new instance of the <see cref="JsonTypeResolverTests"/> class.
//         /// </summary>
//         public JsonTypeResolverTests()
//         {
//             _resolver = new JsonTypeResolver();
//         }
// 
//         /// <summary>
//         /// Tests that when a plain scalar with a numeric value is passed, Resolve returns true and sets the current type to decimal.
//         /// </summary>
//         [Theory]
//         [InlineData("123.45")]
//         [InlineData("-9876.54321")]
//         public void Resolve_WhenPlainScalarWithNumericValue_ReturnsTrueAndSetsDecimal(string numericValue)
//         {
//             // Arrange
//             Type originalType = typeof(string);
//             Type currentType = originalType;
//             Scalar scalar = new Scalar(
//                 anchor: null,
//                 tag: null,
//                 isPlainImplicit: true,
//                 isQuotedImplicit: false,
//                 value: numericValue,
//                 style: ScalarStyle.Any);
// 
//             // Act
//             bool result = _resolver.Resolve(scalar, ref currentType);
// 
//             // Assert
//             Assert.True(result);
//             Assert.Equal(typeof(decimal), currentType);
//         }
// 
//         /// <summary>
//         /// Tests that when a plain scalar with a boolean value is passed, Resolve returns true and sets the current type to bool.
//         /// </summary>
//         [Theory]
//         [InlineData("true")]
//         [InlineData("false")]
//         [InlineData("True")]
//         [InlineData("False")]
//         public void Resolve_WhenPlainScalarWithBooleanValue_ReturnsTrueAndSetsBool(string boolValue)
//         {
//             // Arrange
//             Type originalType = typeof(string);
//             Type currentType = originalType;
//             Scalar scalar = new Scalar(
//                 anchor: null,
//                 tag: null,
//                 isPlainImplicit: true,
//                 isQuotedImplicit: false,
//                 value: boolValue,
//                 style: ScalarStyle.Any);
// 
//             // Act
//             bool result = _resolver.Resolve(scalar, ref currentType);
// 
//             // Assert
//             Assert.True(result);
//             Assert.Equal(typeof(bool), currentType);
//         }
// 
//         /// <summary>
//         /// Tests that when a plain scalar with a non-numeric and non-boolean value is passed, Resolve returns false and does not change the current type.
//         /// </summary>
//         [Fact]
//         public void Resolve_WhenPlainScalarWithNonConvertibleValue_ReturnsFalseAndDoesNotChangeType()
//         {
//             // Arrange
//             Type originalType = typeof(string);
//             Type currentType = originalType;
//             string nonConvertibleValue = "not a number or boolean";
//             Scalar scalar = new Scalar(
//                 anchor: null,
//                 tag: null,
//                 isPlainImplicit: true,
//                 isQuotedImplicit: false,
//                 value: nonConvertibleValue,
//                 style: ScalarStyle.Any);
// 
//             // Act
//             bool result = _resolver.Resolve(scalar, ref currentType);
// 
//             // Assert
//             Assert.False(result);
//             Assert.Equal(originalType, currentType);
//         }
// 
//         /// <summary>
//         /// Tests that when a scalar that is not plain implicit is passed, Resolve returns false and does not change the current type.
//         /// </summary>
//         [Theory]
//         [InlineData("123.45")]
//         [InlineData("true")]
//         public void Resolve_WhenScalarIsNotPlainImplicit_ReturnsFalseAndDoesNotChangeType(string value)
//         {
//             // Arrange
//             Type originalType = typeof(string);
//             Type currentType = originalType;
//             // Creating a scalar with isPlainImplicit set to false.
//             Scalar scalar = new Scalar(
//                 anchor: null,
//                 tag: null,
//                 isPlainImplicit: false,
//                 isQuotedImplicit: false,
//                 value: value,
//                 style: ScalarStyle.Any);
// 
//             // Act
//             bool result = _resolver.Resolve(scalar, ref currentType);
// 
//             // Assert
//             Assert.False(result);
//             Assert.Equal(originalType, currentType);
//         }
// 
//         /// <summary>
//         /// Tests that when a non-scalar node event is passed, Resolve returns false and does not change the current type.
//         /// </summary>
//         [Fact]
//         public void Resolve_WhenNonScalarNodeEvent_ReturnsFalseAndDoesNotChangeType()
//         {
//             // Arrange
//             Type originalType = typeof(string);
//             Type currentType = originalType;
//             NodeEvent dummyEvent = new DummyNodeEvent();
// 
//             // Act
//             bool result = _resolver.Resolve(dummyEvent, ref currentType);
// 
//             // Assert
//             Assert.False(result);
//             Assert.Equal(originalType, currentType);
//         }
// 
//         // A dummy node event to simulate a non-scalar node event.
// //         private class DummyNodeEvent : NodeEvent [Error] (156-23)CS0534 'JsonTypeResolverTests.DummyNodeEvent' does not implement inherited abstract member 'NodeEvent.IsCanonical.get' [Error] (156-23)CS0534 'JsonTypeResolverTests.DummyNodeEvent' does not implement inherited abstract member 'ParsingEvent.Type.get' [Error] (156-23)CS0534 'JsonTypeResolverTests.DummyNodeEvent' does not implement inherited abstract member 'ParsingEvent.Accept(IParsingEventVisitor)'
// //         {
// //             /// <summary>
// //             /// Initializes a new instance of the <see cref="DummyNodeEvent"/> class.
// //             /// </summary>
// //             public DummyNodeEvent() : base(null, null)
// //             {
// //             }
// //         }
//     }
// }
