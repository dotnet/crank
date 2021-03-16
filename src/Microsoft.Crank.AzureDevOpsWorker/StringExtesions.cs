using System;

namespace Microsoft.Crank.AzureDevOpsWorker
{
    internal static class StringExtesions
    {
        internal static bool TryGetEnvironmentVariableValue(
            this string? envVarKey, out string envVarValue)
        {
            envVarValue = envVarKey is { Length: > 0 }
                ? Environment.GetEnvironmentVariable(envVarKey) ?? ""
                : "";

            return envVarValue is { Length: > 0 };
        }
    }
}
