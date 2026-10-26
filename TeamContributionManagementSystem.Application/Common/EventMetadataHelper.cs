using System.Text.Json;
using System.Text.RegularExpressions;

namespace TeamContributionManagementSystem.Application.Common;

public static class EventMetadataHelper
{
    private static readonly Regex PrevEventAdjustmentRegex = new(
        @"<!--\s*PREV_EVENT_ADJUSTMENT:\s*(\{.*?\})\s*-->",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private class PrevEventAdjustmentPayload
    {
        public bool u { get; set; } // use previous event balance
        public decimal a { get; set; } // adjustment amount
        public string? t { get; set; } // adjustment type ("DeficitRecovery" | "SurplusCredit")
        public decimal p { get; set; } // original planned budget
        public string? s { get; set; } // source previous event name
    }

    public static string PackDescription(
        string? cleanDescription,
        bool usePreviousBalance,
        decimal adjustmentAmount,
        string? adjustmentType,
        decimal originalPlannedBudget,
        string? previousEventName)
    {
        var desc = (cleanDescription ?? string.Empty).Trim();
        desc = PrevEventAdjustmentRegex.Replace(desc, string.Empty).Trim();

        if (!usePreviousBalance || adjustmentAmount <= 0 || string.IsNullOrWhiteSpace(adjustmentType))
        {
            return desc;
        }

        var payload = new PrevEventAdjustmentPayload
        {
            u = true,
            a = RoundingHelper.RoundToWhole(adjustmentAmount),
            t = adjustmentType,
            p = RoundingHelper.RoundToWhole(originalPlannedBudget),
            s = previousEventName
        };

        var json = JsonSerializer.Serialize(payload);
        var metaTag = $"<!-- PREV_EVENT_ADJUSTMENT: {json} -->";

        // Fit within PostgreSQL varchar(1000) constraint
        const int maxLength = 980;
        if (desc.Length + metaTag.Length + 1 > maxLength)
        {
            var allowedCleanLen = Math.Max(0, maxLength - metaTag.Length - 1);
            desc = desc.Length > allowedCleanLen ? desc.Substring(0, allowedCleanLen).Trim() : desc;
        }

        return string.IsNullOrWhiteSpace(desc) ? metaTag : $"{desc}\n{metaTag}";
    }

    public static (string CleanDescription, bool UsePreviousBalance, decimal AdjustmentAmount, string AdjustmentType, decimal OriginalPlannedBudget, string? PreviousEventName) UnpackDescription(string? rawDescription)
    {
        if (string.IsNullOrWhiteSpace(rawDescription))
        {
            return (string.Empty, false, 0m, "None", 0m, null);
        }

        var match = PrevEventAdjustmentRegex.Match(rawDescription);
        var cleanDesc = PrevEventAdjustmentRegex.Replace(rawDescription, string.Empty).Trim();

        if (!match.Success)
        {
            return (cleanDesc, false, 0m, "None", 0m, null);
        }

        try
        {
            var json = match.Groups[1].Value;
            var payload = JsonSerializer.Deserialize<PrevEventAdjustmentPayload>(json);
            if (payload != null && payload.u && payload.a > 0)
            {
                return (
                    cleanDesc,
                    true,
                    payload.a,
                    payload.t ?? "None",
                    payload.p,
                    payload.s
                );
            }
        }
        catch
        {
            // Fallback gracefully on parsing error
        }

        return (cleanDesc, false, 0m, "None", 0m, null);
    }
}
