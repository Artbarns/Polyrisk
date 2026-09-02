using System;

namespace PolyRisk.UI
{
    /// <summary>
    /// Utility class for formatting currency amounts into readable Fintech formats.
    /// Handles large number abbreviations (K, M, B).
    /// </summary>
    public static class CurrencyFormatter
    {
        /// <summary>
        /// Formats a double amount into a readable string with prefix and suffix.
        /// Examples: 1500 -> "$1.5K", 1000000 -> "$1M"
        /// </summary>
        /// <param name="amount">The amount to format.</param>
        /// <returns>Formatted string.</returns>
        public static string Format(double amount)
        {
            if (amount < 0) return "-$" + Format(-amount).Replace("$", "");
            
            if (amount >= 1_000_000_000)
            {
                return $"${(amount / 1_000_000_000D).ToString("0.##")}B";
            }
            if (amount >= 1_000_000)
            {
                return $"${(amount / 1_000_000D).ToString("0.##")}M";
            }
            if (amount >= 1_000)
            {
                return $"${(amount / 1_000D).ToString("0.##")}K";
            }

            return $"${amount.ToString("0")}";
        }
    }
}
