using System;
using System.Globalization;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Microsoft.Crank.Controller
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
