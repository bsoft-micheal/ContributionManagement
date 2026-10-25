using System;
using System.Collections.Generic;
using System.Linq;

namespace TeamContributionManagementSystem.Application.Common;

/// <summary>
/// Centralized rounding helper ensuring consistent whole-number calculations across the application.
/// Rule: decimal part &gt;= .50 rounds up; decimal part &lt; .50 rounds down (MidpointRounding.AwayFromZero).
/// Includes exact remainder distribution so total amounts match target values without penny/rupee drift.
/// </summary>
public static class RoundingHelper
{
    /// <summary>
    /// Rounds a decimal value to a whole integer using standard half-up (AwayFromZero) rounding.
    /// Handles zero and negative amounts safely.
    /// </summary>
    public static decimal RoundToWhole(decimal amount)
    {
        return Math.Round(amount, 0, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Rounds a nullable decimal value to a whole integer, preserving null.
    /// </summary>
    public static decimal? RoundToWholeNullable(decimal? amount)
    {
        return amount.HasValue ? RoundToWhole(amount.Value) : null;
    }

    /// <summary>
    /// Distributes whole-number shares by rounding each raw share using standard half-up rounding (&gt;= 0.50 rounds up, else down).
    /// Prevents artificial discrepancies between members so that everyone with equal raw share receives identical amount.
    /// Safely handles empty collections, zero values, and negative amounts.
    /// </summary>
    public static List<decimal> DistributeRoundedTotal(decimal totalAmount, IReadOnlyList<decimal> rawShares)
    {
        if (rawShares == null || rawShares.Count == 0)
        {
            return new List<decimal>();
        }

        return rawShares.Select(RoundToWhole).ToList();
    }

    /// <summary>
    /// Splits a total amount equally across a given number of participants into whole numbers.
    /// Each participant receives an identical rounded whole-number share (&gt;= .50 rounds up, &lt; .50 rounds down).
    /// Safely prevents division by zero when count &lt;= 0.
    /// </summary>
    public static List<decimal> DistributeEqualSplit(decimal totalAmount, int participantCount)
    {
        if (participantCount <= 0)
        {
            return new List<decimal>();
        }

        decimal singleShare = RoundToWhole(totalAmount / participantCount);
        return Enumerable.Repeat(singleShare, participantCount).ToList();
    }
}
