// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Azure.Relay;
using Microsoft.Crank.Models.Security;
using Microsoft.Crank.RegressionBot.Models;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Microsoft.Crank.UnitTests
{
    public class DependencyCompatibilityTests
    {
        [Fact]
        public void RelayRetainsConstructorRequiredByAspNetCoreAdapter()
        {
            // The precompiled adapter needs this exact signature, not an overload with an optional third argument.
            var constructor = typeof(HybridConnectionListener).GetConstructor([typeof(Uri), typeof(TokenProvider)]);

            Assert.NotNull(constructor);
        }

        [Theory]
        [InlineData(SqlAuthenticationMethod.ActiveDirectoryDefault)]
        [InlineData(SqlAuthenticationMethod.ActiveDirectoryManagedIdentity)]
        [InlineData(SqlAuthenticationMethod.ActiveDirectoryServicePrincipal)]
        public void SqlClientRetainsEntraAuthentication(SqlAuthenticationMethod method)
        {
            var provider = SqlAuthenticationProvider.GetProvider(method);

            Assert.NotNull(provider);
            Assert.True(provider.IsSupported(method));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("11111111-1111-1111-1111-111111111111")]
        public void ManagedIdentityCredentialsSupportBothIdentityTypes(string clientId)
        {
            Assert.NotNull(new ManagedIdentityOptions(clientId).GetManagedIdentityCredential());
        }

        [Theory]
        [InlineData(MessagePackCompression.None)]
        [InlineData(MessagePackCompression.Lz4BlockArray)]
        public void RegressionDataRoundTripsWithContractlessMessagePack(MessagePackCompression compression)
        {
            var options = ContractlessStandardResolver.Options.WithCompression(compression);
            var regression = new Regression
            {
                PreviousResult = new BenchmarksResult { Id = 1, Scenario = "plaintext", Document = "{}" },
                CurrentResult = new BenchmarksResult
                {
                    Id = 2,
                    Scenario = "plaintext",
                    Description = "Baseline",
                    DateTimeUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                    Document = "{}"
                },
                Change = -10,
                Average = 100,
                StandardDeviation = 2
            };
            regression.Labels.Add("regression");
            regression.Owners.Add("test-owner");

            var bytes = MessagePackSerializer.Serialize(new[] { regression }, options);
            var restored = Assert.Single(MessagePackSerializer.Deserialize<Regression[]>(bytes, options));

            Assert.Equal(regression.Identifier, restored.Identifier);
            Assert.Equal(regression.CurrentResult.Document, restored.CurrentResult.Document);
            Assert.Equal(1, restored.PreviousResult.Id);
            Assert.Equal(-10, restored.Change);
            Assert.Equal(100, restored.Average);
            Assert.Equal(2, restored.StandardDeviation);
            Assert.Contains("regression", restored.Labels);
            Assert.Contains("test-owner", restored.Owners);
            Assert.False(restored.HasRecovered);
        }
    }
}
