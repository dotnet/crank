// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Microsoft.Crank.PullRequestBot
{
    /// Provides types resolution for YAML
    /// Without this booleans and numbers are parsed as strings
    public class JsonTypeResolver : INodeTypeResolver
    {
        public bool Resolve(NodeEvent nodeEvent, ref Type currentType)
        {
            if (nodeEvent is Scalar scalar && scalar.IsPlainImplicit)
            {
                if (decimal.TryParse(scalar.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                {
                    currentType = typeof(decimal);
                    return true;
                }
                else if (bool.TryParse(scalar.Value, out var b))
                {
                    currentType = typeof(bool);
                    return true;
                }
            }

            return false;
        }
    }
}
