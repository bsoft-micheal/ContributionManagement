using System.Text.Json;
using System.Text.RegularExpressions;

namespace TeamContributionManagementSystem.Application.Common;

public static class EventMetadataHelper
{
    private static readonly Regex PrevEventAdjustmentRegex = new(
        @"<!--\s*PREV_EVENT_ADJUSTMENT:\s*(\{.*?\})\s*-->",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex CreatorRoleRegex = new(
        @"<!--\s*CREATOR_ROLE:\s*([A-Za-z0-9_-]+)\s*-->",
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
        string? previousEventName,
        string? creatorRole = null)
    {
        var desc = (cleanDescription ?? string.Empty).Trim();
        desc = PrevEventAdjustmentRegex.Replace(desc, string.Empty).Trim();
        desc = CreatorRoleRegex.Replace(desc, string.Empty).Trim();

        string metaTag = string.Empty;
        if (usePreviousBalance && adjustmentAmount > 0 && !string.IsNullOrWhiteSpace(adjustmentType))
        {
            var payload = new PrevEventAdjustmentPayload
            {
                u = true,
                a = RoundingHelper.RoundToWhole(adjustmentAmount),
                t = adjustmentType,
                p = RoundingHelper.RoundToWhole(originalPlannedBudget),
                s = previousEventName
            };

            var json = JsonSerializer.Serialize(payload);
            metaTag = $"<!-- PREV_EVENT_ADJUSTMENT: {json} -->";
        }

        string roleTag = !string.IsNullOrWhiteSpace(creatorRole)
            ? $"<!-- CREATOR_ROLE: {creatorRole.Trim()} -->"
            : string.Empty;

        var tags = new List<string>();
        if (!string.IsNullOrWhiteSpace(metaTag)) tags.Add(metaTag);
        if (!string.IsNullOrWhiteSpace(roleTag)) tags.Add(roleTag);
        var combinedTags = string.Join("\n", tags);

        if (string.IsNullOrWhiteSpace(combinedTags))
        {
            return desc;
        }

        // Fit within PostgreSQL varchar(1000) constraint
        const int maxLength = 980;
        if (desc.Length + combinedTags.Length + 1 > maxLength)
        {
            var allowedCleanLen = Math.Max(0, maxLength - combinedTags.Length - 1);
            desc = desc.Length > allowedCleanLen ? desc.Substring(0, allowedCleanLen).Trim() : desc;
        }

        return string.IsNullOrWhiteSpace(desc) ? combinedTags : $"{desc}\n{combinedTags}";
    }

    public static (string CleanDescription, bool UsePreviousBalance, decimal AdjustmentAmount, string AdjustmentType, decimal OriginalPlannedBudget, string? PreviousEventName) UnpackDescription(string? rawDescription)
    {
        var (cleanDesc, usePrev, adjAmt, adjType, origBudget, prevName, _) = UnpackDescriptionWithRole(rawDescription);
        return (cleanDesc, usePrev, adjAmt, adjType, origBudget, prevName);
    }

    public static (string CleanDescription, bool UsePreviousBalance, decimal AdjustmentAmount, string AdjustmentType, decimal OriginalPlannedBudget, string? PreviousEventName, string? CreatorRole) UnpackDescriptionWithRole(string? rawDescription)
    {
        if (string.IsNullOrWhiteSpace(rawDescription))
        {
            return (string.Empty, false, 0m, "None", 0m, null, null);
        }

        var roleMatch = CreatorRoleRegex.Match(rawDescription);
        string? creatorRole = roleMatch.Success ? roleMatch.Groups[1].Value.Trim() : null;

        var match = PrevEventAdjustmentRegex.Match(rawDescription);
        var cleanDesc = PrevEventAdjustmentRegex.Replace(rawDescription, string.Empty);
        cleanDesc = CreatorRoleRegex.Replace(cleanDesc, string.Empty).Trim();

        if (!match.Success)
        {
            return (cleanDesc, false, 0m, "None", 0m, null, creatorRole);
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
                    payload.s,
                    creatorRole
                );
            }
        }
        catch
        {
            // Fallback gracefully on parsing error
        }

        return (cleanDesc, false, 0m, "None", 0m, null, creatorRole);
    }
}
