// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;

namespace Microsoft.Crank.AzureDevOpsWorker
{
    internal sealed class WorkerConfiguration
    {
        internal const string MaxAutoLockRenewalDurationEnvironmentVariable = "CRANK_AZDO_MAX_LOCK_RENEWAL_DURATION";
        internal static readonly TimeSpan DefaultMaxAutoLockRenewalDuration = TimeSpan.FromDays(1);
        internal static readonly TimeSpan MessageLockRenewalSafetyMargin = TimeSpan.FromMinutes(5);

        private WorkerConfiguration(TimeSpan maxAutoLockRenewalDuration)
        {
            MaxAutoLockRenewalDuration = maxAutoLockRenewalDuration;
        }

        public TimeSpan MaxAutoLockRenewalDuration { get; }

        internal static WorkerConfiguration Create(
            string maxAutoLockRenewalDuration,
            Func<string, string> environmentVariableReader = null)
        {
            environmentVariableReader ??= Environment.GetEnvironmentVariable;

            var value = String.IsNullOrWhiteSpace(maxAutoLockRenewalDuration)
                ? environmentVariableReader(MaxAutoLockRenewalDurationEnvironmentVariable)
                : maxAutoLockRenewalDuration;
            if (String.IsNullOrWhiteSpace(value))
            {
                return new WorkerConfiguration(DefaultMaxAutoLockRenewalDuration);
            }

            if (!TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var duration) || duration <= TimeSpan.Zero)
            {
                throw new ArgumentException("The maximum lock renewal duration must be a positive TimeSpan.");
            }

            return new WorkerConfiguration(duration);
        }

        internal bool HasSufficientLockRenewalDuration(JobPayload jobPayload, out TimeSpan requiredDuration)
        {
            ArgumentNullException.ThrowIfNull(jobPayload);

            var attempts = (long)Math.Max(0, jobPayload.Retries) + 1;
            var jobTimeoutTicks = Math.Max(0, jobPayload.Timeout.Ticks);
            try
            {
                var attemptsDurationTicks = checked(jobTimeoutTicks * attempts);
                var requiredDurationTicks = checked(
                    attemptsDurationTicks + MessageLockRenewalSafetyMargin.Ticks);
                requiredDuration = TimeSpan.FromTicks(requiredDurationTicks);
            }
            catch (OverflowException)
            {
                requiredDuration = TimeSpan.MaxValue;
                return false;
            }

            return MaxAutoLockRenewalDuration >= requiredDuration;
        }

    }
}
