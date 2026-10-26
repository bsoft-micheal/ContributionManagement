using System.Text.Json;
using System.Text.RegularExpressions;
using TeamContributionManagementSystem.Application.DTOs.Expenses;

namespace TeamContributionManagementSystem.Application.Common;

public static class ExpenseMetadataHelper
{
    private static readonly Regex ExtraExpensesRegex = new(
        @"<!--\s*EXTRA_EXPENSES:\s*(\{.*?\})\s*-->",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex HumanSummaryRegex = new(
        @"[\r\n\s]*\[Additional Expenses Breakdown\].*$",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

    private class ExtraExpensesPayload
    {
        public decimal? b { get; set; } // budget amount
        public decimal? budget { get; set; }
        public decimal? e { get; set; } // extra amount
        public decimal? extra { get; set; }
        public List<ExtraItemPayload>? items { get; set; }
    }

    private class ExtraItemPayload
    {
        public string? r { get; set; } // reason
        public string? reason { get; set; }
        public decimal? a { get; set; } // amount
        public decimal? amount { get; set; }
    }

    public static string PackDescription(
        string? cleanDescription,
        decimal budgetAmount,
        List<ExpenseAdditionalItemDto>? additionalExpenses,
        decimal totalAmount)
    {
        var desc = (cleanDescription ?? string.Empty).Trim();

        // Strip any existing metadata or summary if present
        desc = ExtraExpensesRegex.Replace(desc, string.Empty).Trim();
        desc = HumanSummaryRegex.Replace(desc, string.Empty).Trim();

        if (additionalExpenses == null || additionalExpenses.Count == 0)
        {
            return desc;
        }

        var extraTotal = additionalExpenses.Sum(x => x.Amount);
        var payload = new ExtraExpensesPayload
        {
            b = budgetAmount,
            e = extraTotal,
            items = additionalExpenses.Select(x => new ExtraItemPayload
            {
                r = x.Reason.Trim(),
                a = x.Amount
            }).ToList()
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false });
        var metaTag = $"<!-- EXTRA_EXPENSES: {json} -->";

        var summaryItems = string.Join("; ", additionalExpenses.Select(x => $"{x.Reason.Trim()}: ₹{x.Amount:N0}"));
        var humanSummary = $"[Additional Expenses: {summaryItems} (Budget: ₹{budgetAmount:N0} | Extra: ₹{extraTotal:N0} | Total: ₹{totalAmount:N0})]";

        var combined = string.IsNullOrWhiteSpace(desc)
            ? $"{metaTag}\n{humanSummary}"
            : $"{desc}\n\n{metaTag}\n{humanSummary}";

        // Postgres description column is varchar(1000)
        if (combined.Length > 980)
        {
            // Omit human summary to stay well under 1000 characters
            combined = string.IsNullOrWhiteSpace(desc)
                ? metaTag
                : $"{desc.Substring(0, Math.Min(desc.Length, 600))}\n\n{metaTag}";
        }

        return combined;
    }

    public static void UnpackDescription(
        string? rawDescription,
        decimal totalAmount,
        out string cleanDescription,
        out decimal budgetAmount,
        out decimal extraAmount,
        out List<ExpenseAdditionalItemDto> additionalExpenses)
    {
        additionalExpenses = new List<ExpenseAdditionalItemDto>();
        budgetAmount = totalAmount;
        extraAmount = 0m;

        if (string.IsNullOrWhiteSpace(rawDescription))
        {
            cleanDescription = string.Empty;
            return;
        }

        var match = ExtraExpensesRegex.Match(rawDescription);
        if (match.Success)
        {
            try
            {
                var json = match.Groups[1].Value;
                var payload = JsonSerializer.Deserialize<ExtraExpensesPayload>(json);
                if (payload != null)
                {
                    var bVal = payload.b ?? payload.budget ?? totalAmount;
                    var eVal = payload.e ?? payload.extra ?? 0m;
                    budgetAmount = bVal;
                    extraAmount = eVal;

                    if (payload.items != null)
                    {
                        foreach (var it in payload.items)
                        {
                            var r = it.r ?? it.reason ?? string.Empty;
                            var a = it.a ?? it.amount ?? 0m;
                            if (!string.IsNullOrWhiteSpace(r) || a > 0)
                            {
                                additionalExpenses.Add(new ExpenseAdditionalItemDto
                                {
                                    Reason = r.Trim(),
                                    Amount = a
                                });
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fallback to defaults on parse issue
                budgetAmount = totalAmount;
                extraAmount = 0m;
            }
        }

        // Clean description for UI display
        var cleaned = ExtraExpensesRegex.Replace(rawDescription, string.Empty);
        cleaned = HumanSummaryRegex.Replace(cleaned, string.Empty);
        cleaned = Regex.Replace(cleaned, @"\[Additional Expenses:.*?\]", string.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        cleanDescription = cleaned.Trim();
    }
}
