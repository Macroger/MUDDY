// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
namespace Server.Core.CommandPipeline.Policies
{
    /// <summary>
    /// Represents the result of a policy check, indicating whether the policy was satisfied
    /// and providing error details if the check failed.
    /// </summary>
    public class PolicyResult
    {
        /// <summary>Indicates whether the policy check passed.</summary>
        public bool IsValid { get; init; }

        /// <summary>Error message if the policy check failed; null if the check passed.</summary>
        public string? ErrorMessage { get; init; }

        /// <summary>Creates a successful policy result.</summary>
        public static PolicyResult Success() => new PolicyResult { IsValid = true, ErrorMessage = null };

        /// <summary>Creates a failed policy result with an error message.</summary>
        public static PolicyResult Failure(string errorMessage) => new PolicyResult { IsValid = false, ErrorMessage = errorMessage };
    }
}