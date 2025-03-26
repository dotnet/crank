using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Crank.RegressionBot;
using Xunit;

namespace Microsoft.Crank.RegressionBot.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Source"/> class.
    /// </summary>
    public class SourceTests
    {
        private readonly Source _source;

        public SourceTests()
        {
            _source = new Source();
        }

        #region Match Method Tests

        /// <summary>
        /// Tests that Match returns an empty enumerable when no rules are present.
        /// </summary>
//         [Fact] [Error] (31-29)CS0029 Cannot implicitly convert type 'System.Collections.Generic.List<Microsoft.Crank.RegressionBot.UnitTests.Rule>' to 'System.Collections.Generic.List<Microsoft.Crank.RegressionBot.Rule>'
//         public void Match_NoRules_ReturnsEmptyEnumerable()
//         {
//             // Arrange
//             _source.Rules = new List<Rule>();
//             string descriptor = "any";
// 
//             // Act
//             var result = _source.Match(descriptor);
// 
//             // Assert
//             Assert.Empty(result);
//         }

        /// <summary>
        /// Tests that Match returns the rule when the rule's Include property is null or empty.
        /// </summary>
//         [Fact] [Error] (49-29)CS0029 Cannot implicitly convert type 'System.Collections.Generic.List<Microsoft.Crank.RegressionBot.UnitTests.Rule>' to 'System.Collections.Generic.List<Microsoft.Crank.RegressionBot.Rule>' [Error] (57-29)CS1503 Argument 1: cannot convert from 'Microsoft.Crank.RegressionBot.UnitTests.Rule' to 'string' [Error] (57-35)CS1503 Argument 2: cannot convert from 'System.Collections.Generic.List<Microsoft.Crank.RegressionBot.Rule>' to 'string?'
//         public void Match_RuleWithEmptyInclude_ReturnsRule()
//         {
//             // Arrange
//             var rule = new Rule { Include = string.Empty };
//             _source.Rules = new List<Rule> { rule };
//             string descriptor = "anything";
// 
//             // Act
//             var result = _source.Match(descriptor).ToList();
// 
//             // Assert
//             Assert.Single(result);
//             Assert.Contains(rule, result);
//         }

        /// <summary>
        /// Tests that Match returns the rule when the rule's Include regex matches the descriptor.
        /// </summary>
//         [Fact] [Error] (68-29)CS0029 Cannot implicitly convert type 'System.Collections.Generic.List<Microsoft.Crank.RegressionBot.UnitTests.Rule>' to 'System.Collections.Generic.List<Microsoft.Crank.RegressionBot.Rule>' [Error] (76-29)CS1503 Argument 1: cannot convert from 'Microsoft.Crank.RegressionBot.UnitTests.Rule' to 'string' [Error] (76-35)CS1503 Argument 2: cannot convert from 'System.Collections.Generic.List<Microsoft.Crank.RegressionBot.Rule>' to 'string?'
//         public void Match_RuleWithMatchingInclude_ReturnsRule()
//         {
//             // Arrange
//             var rule = new Rule { Include = "^test" };
//             _source.Rules = new List<Rule> { rule };
//             string descriptor = "testcase";
// 
//             // Act
//             var result = _source.Match(descriptor).ToList();
// 
//             // Assert
//             Assert.Single(result);
//             Assert.Contains(rule, result);
//         }

        /// <summary>
        /// Tests that Match does not return the rule when the rule's Include regex does not match the descriptor.
        /// </summary>
//         [Fact] [Error] (87-29)CS0029 Cannot implicitly convert type 'System.Collections.Generic.List<Microsoft.Crank.RegressionBot.UnitTests.Rule>' to 'System.Collections.Generic.List<Microsoft.Crank.RegressionBot.Rule>'
//         public void Match_RuleWithNonMatchingInclude_ReturnsEmptyEnumerable()
//         {
//             // Arrange
//             var rule = new Rule { Include = "^test" };
//             _source.Rules = new List<Rule> { rule };
//             string descriptor = "example";
// 
//             // Act
//             var result = _source.Match(descriptor);
// 
//             // Assert
//             Assert.Empty(result);
//         }

        /// <summary>
        /// Tests that Match returns multiple rules correctly based on their Include property.
        /// </summary>
//         [Fact] [Error] (107-29)CS0029 Cannot implicitly convert type 'System.Collections.Generic.List<Microsoft.Crank.RegressionBot.UnitTests.Rule>' to 'System.Collections.Generic.List<Microsoft.Crank.RegressionBot.Rule>' [Error] (116-29)CS1503 Argument 1: cannot convert from 'Microsoft.Crank.RegressionBot.UnitTests.Rule' to 'string' [Error] (116-43)CS1503 Argument 2: cannot convert from 'System.Collections.Generic.List<Microsoft.Crank.RegressionBot.Rule>' to 'string?' [Error] (117-29)CS1503 Argument 1: cannot convert from 'Microsoft.Crank.RegressionBot.UnitTests.Rule' to 'string' [Error] (117-47)CS1503 Argument 2: cannot convert from 'System.Collections.Generic.List<Microsoft.Crank.RegressionBot.Rule>' to 'string?'
//         public void Match_MultipleRules_ReturnsOnlyMatchingRules()
//         {
//             // Arrange
//             var matchingRule = new Rule { Include = "match" };
//             var nonMatchingRule = new Rule { Include = "nomatch" };
//             var emptyIncludeRule = new Rule { Include = "" };
//             _source.Rules = new List<Rule> { nonMatchingRule, matchingRule, emptyIncludeRule };
//             string descriptor = "match descriptor";
// 
//             // Act
//             var result = _source.Match(descriptor).ToList();
// 
//             // Assert
//             // The nonMatchingRule should be skipped because its pattern does not match.
//             Assert.Equal(2, result.Count);
//             Assert.Contains(matchingRule, result);
//             Assert.Contains(emptyIncludeRule, result);
//         }

        #endregion

        #region Include Method Tests

        /// <summary>
        /// Tests that Include returns false when there are no rules.
        /// </summary>
//         [Fact] [Error] (131-29)CS0029 Cannot implicitly convert type 'System.Collections.Generic.List<Microsoft.Crank.RegressionBot.UnitTests.Rule>' to 'System.Collections.Generic.List<Microsoft.Crank.RegressionBot.Rule>'
//         public void Include_NoRules_ReturnsFalse()
//         {
//             // Arrange
//             _source.Rules = new List<Rule>();
//             string descriptor = "anything";
// 
//             // Act
//             bool result = _source.Include(descriptor);
// 
//             // Assert
//             Assert.False(result);
//         }

        /// <summary>
        /// Tests that Include returns true when a single rule's Include regex matches the descriptor.
        /// </summary>
//         [Fact] [Error] (149-29)CS0029 Cannot implicitly convert type 'System.Collections.Generic.List<Microsoft.Crank.RegressionBot.UnitTests.Rule>' to 'System.Collections.Generic.List<Microsoft.Crank.RegressionBot.Rule>'
//         public void Include_SingleMatchingInclude_ReturnsTrue()
//         {
//             // Arrange
//             var rule = new Rule { Include = "test" };
//             _source.Rules = new List<Rule> { rule };
//             string descriptor = "this is a test string";
// 
//             // Act
//             bool result = _source.Include(descriptor);
// 
//             // Assert
//             Assert.True(result);
//         }

        /// <summary>
        /// Tests that Include returns false when a single rule's Include regex does not match the descriptor.
        /// </summary>
//         [Fact] [Error] (167-29)CS0029 Cannot implicitly convert type 'System.Collections.Generic.List<Microsoft.Crank.RegressionBot.UnitTests.Rule>' to 'System.Collections.Generic.List<Microsoft.Crank.RegressionBot.Rule>'
//         public void Include_SingleNonMatchingInclude_ReturnsFalse()
//         {
//             // Arrange
//             var rule = new Rule { Include = "test" };
//             _source.Rules = new List<Rule> { rule };
//             string descriptor = "this is a sample string";
// 
//             // Act
//             bool result = _source.Include(descriptor);
// 
//             // Assert
//             Assert.False(result);
//         }

        /// <summary>
        /// Tests that Include returns false when a rule's Exclude regex matches the descriptor after an Include match.
        /// </summary>
//         [Fact] [Error] (185-29)CS0029 Cannot implicitly convert type 'System.Collections.Generic.List<Microsoft.Crank.RegressionBot.UnitTests.Rule>' to 'System.Collections.Generic.List<Microsoft.Crank.RegressionBot.Rule>'
//         public void Include_ExcludeOverridesInclude_ReturnsFalse()
//         {
//             // Arrange
//             var rule = new Rule { Include = "test", Exclude = "test" };
//             _source.Rules = new List<Rule> { rule };
//             string descriptor = "test";
// 
//             // Act
//             bool result = _source.Include(descriptor);
// 
//             // Assert
//             Assert.False(result);
//         }

        /// <summary>
        /// Tests that Include returns the outcome based on the last rule when multiple rules are processed.
        /// The last rule should prevail.
        /// </summary>
//         [Fact] [Error] (207-29)CS0029 Cannot implicitly convert type 'System.Collections.Generic.List<Microsoft.Crank.RegressionBot.UnitTests.Rule>' to 'System.Collections.Generic.List<Microsoft.Crank.RegressionBot.Rule>'
//         public void Include_MultipleRules_LastRulePrevails()
//         {
//             // Arrange
//             // First rule sets include to true if matches.
//             var rule1 = new Rule { Include = "test" };
//             // Second rule's exclude, if matches, overrides previous include.
//             var rule2 = new Rule { Exclude = "test" };
//             _source.Rules = new List<Rule> { rule1, rule2 };
//             string descriptor = "test";
// 
//             // Act
//             bool result = _source.Include(descriptor);
// 
//             // Assert
//             Assert.False(result);
//         }

        /// <summary>
        /// Tests that Include returns true when a later rule includes the descriptor overriding an earlier non-match.
        /// </summary>
//         [Fact] [Error] (228-29)CS0029 Cannot implicitly convert type 'System.Collections.Generic.List<Microsoft.Crank.RegressionBot.UnitTests.Rule>' to 'System.Collections.Generic.List<Microsoft.Crank.RegressionBot.Rule>'
//         public void Include_MultipleRules_LastRuleOverridesToTrue()
//         {
//             // Arrange
//             // First rule does not match.
//             var rule1 = new Rule { Include = "abc" };
//             // Second rule matches.
//             var rule2 = new Rule { Include = "test" };
//             _source.Rules = new List<Rule> { rule1, rule2 };
//             string descriptor = "this is a test string";
// 
//             // Act
//             bool result = _source.Include(descriptor);
// 
//             // Assert
//             Assert.True(result);
//         }

        #endregion
    }

    // Minimal definition for Rule class required for tests.
    // This assumes the real Rule class has these properties as used in Source.
    public class Rule
    {
        /// <summary>
        /// Regular expression string for inclusion.
        /// </summary>
        public string Include { get; set; }

        /// <summary>
        /// Regular expression string for exclusion.
        /// </summary>
        public string Exclude { get; set; }

        /// <summary>
        /// Cached Regex for Include.
        /// </summary>
        public Regex IncludeRegex { get; set; }

        /// <summary>
        /// Cached Regex for Exclude.
        /// </summary>
        public Regex ExcludeRegex { get; set; }
    }
}
