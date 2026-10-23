using System;
using System.Globalization;

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

        /// <summary>
        /// Full, localized display for the Pulse HUD and property cockpit. Keeps
        /// the real amount visible instead of abbreviating it (e.g. 18 000 $).
        /// This is presentation only; calculations should continue using doubles.
        /// </summary>
        public static string FormatExact(double amount)
        {
            if (double.IsNaN(amount) || double.IsInfinity(amount)) return "—";

            double rounded = Math.Round(Math.Abs(amount), MidpointRounding.AwayFromZero);
            string grouped = rounded.ToString("#,##0", CultureInfo.InvariantCulture)
                .Replace(",", "\u202f");
            return (amount < 0 ? "−" : string.Empty) + grouped + " $";
        }

        /// <summary>Full grouped number without a currency suffix for a split UI value.</summary>
        public static string FormatExactNumber(double amount)
        {
            if (double.IsNaN(amount) || double.IsInfinity(amount)) return "—";

            double rounded = Math.Round(Math.Abs(amount), MidpointRounding.AwayFromZero);
            string grouped = rounded.ToString("#,##0", CultureInfo.InvariantCulture)
                .Replace(",", "\u202f");
            return (amount < 0 ? "−" : string.Empty) + grouped;
        }
    }
}
