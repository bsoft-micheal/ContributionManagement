using System;
using System.Collections.Generic;
using System.Linq;

namespace TeamContributionManagementSystem.Application.Common;

/// <summary>
/// Centralized rounding helper ensuring consistent whole-number calculations across the application.
/// Rule: decimal part >= .50 rounds up; decimal part < .50 rounds down (MidpointRounding.AwayFromZero).
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
    /// Distributes an exact total amount among given raw calculated shares using the Largest Remainder Method (Hare-Niemeyer).
    /// Guarantees that sum(resultShares) == RoundToWhole(totalAmount) exactly.
    /// Safely handles empty collections, zero values, and negative amounts.
    /// </summary>
    public static List<decimal> DistributeRoundedTotal(decimal totalAmount, IReadOnlyList<decimal> rawShares)
    {
        if (rawShares == null || rawShares.Count == 0)
        {
            return new List<decimal>();
        }

        decimal roundedTarget = RoundToWhole(totalAmount);
        var shares = rawShares.Select(RoundToWhole).ToList();
        decimal currentSum = shares.Sum();
        decimal diff = roundedTarget - currentSum;

        if (diff != 0)
        {
            // Calculate remainder fraction for each share: rawShare - floor(rawShare)
            // When diff > 0 (under-allocated), prioritize highest fractional remainders.
            // When diff < 0 (over-allocated), prioritize lowest fractional remainders (closest to rounding down).
            var indexedRemainders = rawShares
                .Select((rawVal, idx) =>
                {
                    decimal floorVal = Math.Floor(rawVal);
                    decimal rem = rawVal - floorVal;
                    return new { Index = idx, Remainder = rem };
                })
                .OrderByDescending(x => diff > 0 ? x.Remainder : (1m - x.Remainder))
                .ToList();

            int step = diff > 0 ? 1 : -1;
            int countToAdjust = (int)Math.Abs(diff);

            for (int i = 0; i < indexedRemainders.Count && countToAdjust > 0; i++)
            {
                shares[indexedRemainders[i].Index] += step;
                countToAdjust--;
            }
        }

        return shares;
    }

    /// <summary>
    /// Splits a total amount equally across a given number of participants into whole numbers.
    /// Safely prevents division by zero when count <= 0.
    /// </summary>
    public static List<decimal> DistributeEqualSplit(decimal totalAmount, int participantCount)
    {
        if (participantCount <= 0)
        {
            return new List<decimal>();
        }

        decimal rawShare = totalAmount / participantCount;
        var rawShares = Enumerable.Repeat(rawShare, participantCount).ToList();
        return DistributeRoundedTotal(totalAmount, rawShares);
    }
}
