// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Crank.RegressionBot
{
    public static class Queries
    {
        public const string Latest = @"
            SELECT TOP (10000) *     -- Bounded to prevent from downloading too many records
            FROM [dbo].[{0}]        -- Substitute table name 
            WHERE 
                [Excluded] = 0
                AND [DateTimeUtc] >= @startDate
            ORDER BY [Id] DESC
        ";
    }
}
